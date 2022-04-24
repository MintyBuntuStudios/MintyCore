﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Utils;

namespace MintyCore.ECS;

internal static class ArchetypeStorageBuilder
{
    /// <summary>
    /// Generate a new implementation of IArchetypeStorage based on the given archetype.
    /// </summary>
    /// <param name="archetype"> The archetype to generate the storage for. </param>
    /// <param name="archetypeId"> ID of the archetype. </param>
    /// <param name="additionalDlls">Collection of additional dll files needed to be referenced for the generated assembly</param>
    /// <param name="assemblyLoadContext">The load context the assembly was loaded in</param>
    /// <param name="createdAssembly">The object representation of the created assembly</param>
    /// <param name="createdFile">The optional created assembly file</param>
    /// <returns>Function that creates a instance of the storage</returns>
    public static Func<IArchetypeStorage?> GenerateArchetypeStorage(ArchetypeContainer archetype,
        Identification archetypeId, IEnumerable<string>? additionalDlls,
        out AssemblyLoadContext assemblyLoadContext, out Assembly createdAssembly, out string? createdFile)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: Engine.TestingModeActive ? OptimizationLevel.Debug : OptimizationLevel.Release,
            allowUnsafe: true);

        var storageName = $"{archetypeId.ToString().Replace(':', '_')}_Storage";
        var fullClassName = $"MintyCore.ECS.{storageName}";

        var compilation = CSharpCompilation.Create($"{archetypeId.ToString().Replace(':', '_')}_storage",
            new[]
            {
                SyntaxFactory.ParseSyntaxTree(GenerateArchetypeStorageSourceCode(archetype, storageName))
            },
            GetReferencedAssemblies(archetype, additionalDlls),
            options);

        /*
         *  Loading the assembly is a bit unintuitive to get it fully working
         *  1. By loading the assembly directly from the MemoryStream it was created with, Rider cannot decompile it
         *      Which is problematic for debugging
         *  2. If the assembly is stored in a file and loaded with "AssemblyLoadContext.LoadFromFile" the file gets locked
         *      for the whole process lifetime, even when the assembly is unloaded
         *
         *  To fix this the Assembly gets written to a file (only in Debug / Testing Mode) and get immediately reloaded with a file stream
         *  This allows both the decompilation and the non file locking
         */
        
        createdFile = null;
        Stream assemblyStream;
        if (Engine.TestingModeActive)
        {
            createdFile = $"{storageName}.dll";
            assemblyStream = new FileStream(createdFile, FileMode.Create);
        }
        else
        {
            assemblyStream = new MemoryStream();
        }

        var result = compilation.Emit(assemblyStream);
        if (!result.Diagnostics.IsDefaultOrEmpty)
        {
            Logger.WriteLog($"Diagnostics while generating archetype storage {archetypeId}: ", LogImportance.WARNING,
                "ECS");
            foreach (var diagnostic in result.Diagnostics)
            {
                Logger.WriteLog($"{diagnostic.Id}: {diagnostic.GetMessage()}", LogImportance.WARNING, "ECS");
            }
        }

        Logger.AssertAndThrow(result.Success,
            $"Failed to generate archetype storage for archetype {archetypeId}. View previous log messages for more information.",
            "ECS");

        var loadContext = new AssemblyLoadContext($"{archetypeId}_storage", true);

        if (Engine.TestingModeActive)
        {
            var file = new FileInfo(createdFile!);
            
            assemblyStream.Dispose();
            assemblyStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
        }
        
        assemblyStream.Position = 0;
        var assembly = loadContext.LoadFromStream(assemblyStream);
        assemblyStream.Dispose();

        var storage = assembly.GetType(fullClassName);
        Logger.AssertAndThrow(storage is not null,
            $"Generated ArchetypeStorage class for archetype {archetypeId} not found.",
            "ECS");

        assemblyLoadContext = loadContext;
        createdAssembly = assembly;

        return () => Activator.CreateInstance(storage) as IArchetypeStorage;
    }


    private static IEnumerable<MetadataReference> GetReferencedAssemblies(ArchetypeContainer archetype,
        IEnumerable<string>? additionalDlls)
    {
        var assemblyLocations = new HashSet<string>();

        if (additionalDlls is not null)
            assemblyLocations.UnionWith(additionalDlls);

        foreach (var component in archetype.ArchetypeComponents)
        {
            var location = ComponentManager.GetComponentType(component)?.Assembly.Location;
            if (location is not null)
                assemblyLocations.Add(location);
        }

        var systemDll = typeof(object).Assembly.Location;

        //Add all assemblies needed by the ArchetypeStorage implementation
        assemblyLocations.Add(typeof(Engine).Assembly.Location); //Add MintyCore assembly

        assemblyLocations.Add(systemDll); //Add System assembly
        assemblyLocations.Add(new FileInfo(systemDll).Directory?.FullName +
                              "\\System.Runtime.dll"); //Add System.Runtime assembly
        assemblyLocations.Add(new FileInfo(systemDll).Directory?.FullName +
                              "\\System.Numerics.Vectors.dll"); //Add System.Numerics.Vectors assembly
        assemblyLocations.Add(typeof(Unsafe).Assembly.Location); //Add System.Runtime.CompilerServices assembly


        return assemblyLocations.Select(location => MetadataReference.CreateFromFile(location));
    }

    private static unsafe string GenerateArchetypeStorageSourceCode(ArchetypeContainer archetype, string className)
    {
        var componentCount = archetype.ArchetypeComponents.Count;
        var componentTypeNames = new string[componentCount];
        var componentPointerNames = new string[componentCount];
        var componentIdNames = new string[componentCount];
        var componentSizeNames = new string[componentCount];

        var numericComponentIDs = new ulong[componentCount];

        var index = 0;
        foreach (var componentId in archetype.ArchetypeComponents)
        {
            var componentType = ComponentManager.GetComponentType(componentId);
            Logger.AssertAndThrow(componentType is not null,
                $"Type for component {componentId} not found in ComponentManager. This is a bug.", "ECS");

            var fullName = componentType.FullName;
            Logger.AssertAndThrow(fullName is not null, $"Type for component {componentId} has no Name", "ECS");

            //Replace '+' (Annotation for nested classes/structs) with '.' to make it a valid C# identifier
            componentTypeNames[index] = fullName.Replace('+', '.');
            var componentFieldName = componentTypeNames[index].Replace(".", "_");
            componentPointerNames[index] = $"{componentFieldName}_ptr";
            componentIdNames[index] = $"{componentFieldName}_id";
            componentSizeNames[index] = $"{componentFieldName}_size";

            numericComponentIDs[index] = *(ulong*) &componentId;

            index++;
        }

        var sb = new StringBuilder();

        WriteClassHead(sb, className);
        WriteComponentFields(sb, componentTypeNames, componentPointerNames);
        WriteConstructor(sb, componentTypeNames, componentPointerNames, className);
        WriteGetComponentMethods(sb, componentPointerNames, numericComponentIDs);
        WriteAddEntityMethod(sb, componentPointerNames);
        WriteRemoveEntityMethod(sb, componentPointerNames);
        WriteResizeMethod(sb, componentTypeNames, componentPointerNames);
        WriteDispose(sb, componentPointerNames);
        WriteDirtyEnumerable(sb, componentTypeNames, componentIdNames, componentPointerNames, componentSizeNames,
            className);
        WriteDebugView(sb, componentTypeNames, componentPointerNames, className);
        WriteClassEnd(sb);
        return sb.ToString();
    }

    private static void WriteClassHead(StringBuilder sb, string className)
    {
        sb.AppendLine(@$"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
/// Autogenerated archetype storage class
/// </summary>
[DebuggerTypeProxy(typeof(DebugView))]
public unsafe class {className} : IArchetypeStorage
{{
    /// <summary>
    ///     Initial size of the storage (entity count)
    /// </summary>
    private const int DefaultStorageSize = 16;

    /// <inheritdoc />
    public int Count {{ get; private set; }}

    /// <inheritdoc />
    public ReadOnlyEntityList Entities => new (_indexEntity, Count);

    /// <inheritdoc />
    public bool Contains(Entity entity) => _entityIndex.ContainsKey(entity);
    
    /// <summary>
    /// Indices of entities in the storage
    /// </summary>
    private readonly Dictionary<Entity, int> _entityIndex = new(DefaultStorageSize);
    
    /// <summary>
    /// Entity at the given index in the storage
    /// </summary>
    private Entity[] _indexEntity = new Entity[DefaultStorageSize];
    
    private int _storageSize = DefaultStorageSize;");
    }

    private static void WriteComponentFields(StringBuilder sb, string[] componentTypeNames,
        string[] componentPointerNames)
    {
        for (var i = 0; i < componentTypeNames.Length; i++)
        {
            sb.AppendLine($"private {componentTypeNames[i]}* {componentPointerNames[i]};");
        }
    }

    private static void WriteConstructor(StringBuilder sb, string[] componentTypeNames, string[] componentPointerNames,
        string className)
    {
        sb.AppendLine($@"
    public {className}()
        {{
");
        for (int i = 0; i < componentTypeNames.Length; i++)
        {
            sb.AppendLine(
                $"{componentPointerNames[i]} = ({componentTypeNames[i]}*) AllocationHandler.Malloc<{componentTypeNames[i]}>(DefaultStorageSize);");
        }

        sb.AppendLine("}");
    }

    private static void WriteGetComponentMethods(StringBuilder sb, string[] componentPointerNames,
        ulong[] numericComponentIDs)
    {
        sb.AppendLine(@"
    /// <inheritdoc />
    public ref TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
    {
        return ref GetComponent<TComponent>(entity, default(TComponent).Identification);
    }
    
    /// <inheritdoc />
    public ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId) where TComponent : unmanaged, IComponent
    {
        return ref GetComponent<TComponent>(_entityIndex[entity], componentId);
    }

    /// <inheritdoc />
    public ref TComponent GetComponent<TComponent>(int entityIndex, Identification componentId) where TComponent : unmanaged, IComponent
    {
        return ref *(TComponent*) GetComponentPtr(entityIndex, componentId);
    }

    /// <inheritdoc />
    public IntPtr GetComponentPtr(Entity entity, Identification componentId)
    {
        return GetComponentPtr(_entityIndex[entity], componentId);
    }

    /// <inheritdoc />
    public IntPtr GetComponentPtr(int entityIndex, Identification componentId)
    {
        return (*(ulong*) &componentId) switch
        {");

        for (int i = 0; i < componentPointerNames.Length; i++)
        {
            sb.AppendLine($"{numericComponentIDs[i]} => new IntPtr( {componentPointerNames[i]} + entityIndex), ");
        }

        sb.AppendLine(@"
            _ => throw new ArgumentException($""Component with id {componentId} does not exist in the storage"")
        };
    }");
    }

    private static void WriteAddEntityMethod(StringBuilder sb, string[] componentPointerNames)
    {
        sb.AppendLine(@"
    public bool AddEntity(Entity entity)
    {
        if(_entityIndex.ContainsKey(entity))
            return false;
        
        if(Count >= _storageSize)
            Resize(_storageSize * 2);
        
        var entityIndex = Count;
        _entityIndex.Add(entity, entityIndex);
        _indexEntity[entityIndex] = entity;
        Count++;");

        for (int i = 0; i < componentPointerNames.Length; i++)
        {
            sb.AppendLine($@"
        {componentPointerNames[i]}[entityIndex] = default;
        {componentPointerNames[i]}[entityIndex].PopulateWithDefaultValues();
        {componentPointerNames[i]}[entityIndex].IncreaseRefCount();
");
        }

        sb.AppendLine(@"
        return true;
    }");
    }

    private static void WriteRemoveEntityMethod(StringBuilder sb, string[] componentPointerNames)
    {
        sb.AppendLine(@"
    public void RemoveEntity(Entity entity)
    {
        if (!_entityIndex.ContainsKey(entity))
        {
            Logger.WriteLog($""Entity to delete {entity} not present"", LogImportance.ERROR, ""ECS"");
            return;
        }
            
        var index = _entityIndex[entity];
            
        _entityIndex.Remove(entity);
        _indexEntity[index] = default;
        Count--;
");

        foreach (var pointerName in componentPointerNames)
        {
            sb.AppendLine($"{pointerName}[index].DecreaseRefCount();");
        }

        sb.AppendLine(@"
        //if the entity is not the last one, move the last one to the index of the entity to delete
        if (Count != index)
        {
            var entityIndexToMove = Count;
            var entityToMove = _indexEntity[entityIndexToMove];
            
            _indexEntity[entityIndexToMove] = default;
            _indexEntity[index] = entityToMove;
            _entityIndex[entityToMove] = index;
");

        foreach (var pointerName in componentPointerNames)
        {
            sb.AppendLine($"{pointerName}[index] = {pointerName}[entityIndexToMove];");
        }

        sb.AppendLine(@"
        }

        if (Count <= _storageSize / 4 && _storageSize / 2 >= DefaultStorageSize)
            Resize(_storageSize / 2);
    }
");
    }

    private static void WriteResizeMethod(StringBuilder sb, string[] componentTypeNames, string[] componentPointerNames)
    {
        sb.AppendLine(@"
    private void Resize(int newSize)
    {
        if (newSize == _storageSize) return;

        if (newSize < _storageSize)
        {
            Logger.AssertAndThrow(newSize >= Count,
                $""The new size ({newSize}) of the archetype storage is smaller then the current entity count ({Count})"",
                ""ECS"");
        }
");

        for (int i = 0; i < componentTypeNames.Length; i++)
        {
            sb.AppendLine($@"
        {{
            {componentTypeNames[i]}* {componentPointerNames[i]}_new = ({componentTypeNames[i]}*) AllocationHandler.Malloc<{componentTypeNames[i]}>(newSize);
            
            var oldData = new Span<{componentTypeNames[i]}>({componentPointerNames[i]}, Count);
            var newData = new Span<{componentTypeNames[i]}>({componentPointerNames[i]}_new, newSize);
            oldData.CopyTo(newData);
            AllocationHandler.Free((IntPtr) {componentPointerNames[i]});
            {componentPointerNames[i]} = {componentPointerNames[i]}_new;
        }}
");
        }

        sb.AppendLine(@"
        Array.Resize(ref _indexEntity, newSize);
        _storageSize = newSize;
    }
");
    }

    private static void WriteDispose(StringBuilder sb, string[] componentPointerNames)
    {
        sb.AppendLine(@"
    public void Dispose()
    {
        //Call the remove for all entities to ensure that unmanaged resources are freed
        foreach (var entity in _entityIndex) ((IArchetypeStorage) this).RemoveEntity(entity.Key);

        _entityIndex.Clear();
        _indexEntity = Array.Empty<Entity>();
");
        foreach (var pointerName in componentPointerNames)
        {
            sb.AppendLine(@$"
AllocationHandler.Free((IntPtr) {pointerName});
{pointerName} = null;");
        }

        sb.AppendLine(@"}");
    }

    private static void WriteDirtyEnumerable(StringBuilder sb, string[] componentTypeNames, string[] componentIdNames,
        string[] componentPointerNames, string[] componentSizeNames, string className)
    {
        sb.AppendLine(@$"
    public IEnumerable<(Entity entity, Identification componentId, IntPtr componentPtr)> GetDirtyEnumerator()
    {{
        return new DirtyComponentEnumerable(this);
    }}

    private class DirtyComponentEnumerable 
        : IEnumerable<(Entity entity, Identification componentId, IntPtr componentPtr)>
    {{
        private readonly {className} _storage;

        public DirtyComponentEnumerable({className} storage)
        {{
            _storage = storage;
        }}

        public IEnumerator<(Entity entity, Identification componentId, IntPtr componentPtr)> GetEnumerator()
        {{
            int entityCount = _storage.Count;
            var entities = _storage._indexEntity;
");
        for (int i = 0; i < componentIdNames.Length; i++)
        {
            sb.AppendLine($@"
            var {componentIdNames[i]} = default({componentTypeNames[i]}).Identification;
            var {componentPointerNames[i]} = _storage.GetComponentPtr(0, {componentIdNames[i]});
            var {componentSizeNames[i]} = Unsafe.SizeOf<{componentTypeNames[i]}>();
");
        }

        sb.AppendLine(@"
            for (int i = 0; i < entityCount; i++)
            {
");

        for (int i = 0; i < componentIdNames.Length; i++)
        {
            sb.AppendLine($@"
            var {componentPointerNames[i]}_current = {componentPointerNames[i]} + i * {componentSizeNames[i]};
            
            if(CheckAndUnsetDirty<{componentTypeNames[i]}>({componentPointerNames[i]}_current))
            {{
                yield return (entities[i], {componentIdNames[i]}, {componentPointerNames[i]}_current);
            }}
");
        }

        sb.AppendLine(@"
            }
        }
    
        private static bool CheckAndUnsetDirty<TComponent>(IntPtr component) where TComponent : unmanaged, IComponent
        {
            var ptr = (TComponent*) component;
            var dirty = ptr->Dirty;
            ptr->Dirty = false;
            return dirty;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }");
    }

    private static void WriteDebugView(StringBuilder sb, string[] componentTypeNames, string[] componentPointerNames,
        string className)
    {
        sb.AppendLine(@$"
    private unsafe class DebugView
    {{
        private {className} _parent;

        public DebugView({className} parent)
        {{
            _parent = parent;
        }}

        /// <summary>
        ///     Create a human readable representation of the archetype storage, for the debug view
        /// </summary>
        public EntityView[] EntityViews
        {{
            get
            {{
                var returnValue = new EntityView[_parent.Count];
                var iteration = 0;
                foreach (var (entity, _) in _parent._entityIndex)
                {{
                    returnValue[iteration].Entity = entity;
");
        for (int i = 0; i < componentPointerNames.Length; i++)
        {
            sb.AppendLine(
                $@"
                    returnValue[iteration].{componentPointerNames[i]} = 
                        ({componentTypeNames[i]}*) _parent.GetComponentPtr(entity, default({componentTypeNames[i]}).Identification);");
        }

        sb.AppendLine(@"
                    iteration++;
                }

                return returnValue;
            }
        }

        public struct EntityView
        {
            public Entity Entity;");

        for (int i = 0; i < componentPointerNames.Length; i++)
        {
            sb.AppendLine($"public {componentTypeNames[i]}* {componentPointerNames[i]};");
        }

        sb.AppendLine(@"
        }
    }");
    }

    private static void WriteClassEnd(StringBuilder sb)
    {
        sb.AppendLine("}");
    }
}