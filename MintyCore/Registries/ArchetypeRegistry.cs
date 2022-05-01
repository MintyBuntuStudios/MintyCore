﻿using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all Archetypes
/// </summary>
[Registry("archetype")]
public class ArchetypeRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Archetype;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[] {RegistryIDs.Component};

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        ArchetypeManager.RemoveArchetype(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
        ArchetypeManager.RemoveGeneratedAssemblies();
    }

    /// <inheritdoc />
    public void Clear()
    {
        ClearRegistryEvents();
        ArchetypeManager.Clear();
    }

    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    /// <summary>
    ///     Register a Archetype. Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="archetype">The <see cref="ArchetypeContainer" /> for the Archetype</param>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the Archetype</param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the Archetype></param>
    /// <param name="setup">Configuration for auto setup of the entity</param>
    /// <param name="additionalDlls">Additional dlls which need to be referenced for the auto generated <see cref="IArchetypeStorage"/></param>
    /// <returns>Generated <see cref="Identification" /> for the Archetype</returns>
    [Obsolete]
    public static Identification RegisterArchetype(ArchetypeContainer archetype, ushort modId,
        string stringIdentifier, IEntitySetup? setup = null, IEnumerable<string>? additionalDlls = null)
    {
        RegistryManager.AssertMainObjectRegistryPhase();

        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Archetype, stringIdentifier);
        ArchetypeManager.AddArchetype(id, archetype, setup, additionalDlls);
        return id;
    }

    /// <summary>
    ///     Extend a <see cref="ArchetypeContainer" />
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    [Obsolete]
    public static void ExtendArchetype(Identification archetypeId, ArchetypeContainer archetype,
        IEnumerable<string>? additionalDlls = null)
    {
        ExtendArchetype(archetypeId, archetype.ArchetypeComponents, additionalDlls);
    }

    /// <summary>
    ///     Extend a <see cref="ArchetypeContainer" /> with the given component <see cref="Identification" />
    ///     Call this at <see cref="OnPostRegister" />
    /// </summary>
    [Obsolete]
    public static void ExtendArchetype(Identification archetypeId, IEnumerable<Identification> componentIDs,
        IEnumerable<string>? additionalDlls = null)
    {
        RegistryManager.AssertPostObjectRegistryPhase();

        ArchetypeManager.ExtendArchetype(archetypeId, componentIDs, additionalDlls);
    }

    /// <summary>
    /// Register a Archetype
    /// Used by the SourceGenerator for the <see cref="Registries.RegisterArchetypeAttribute"/>
    /// </summary>
    /// <param name="archetypeId">Id of the archetype</param>
    /// <param name="info">Archetype info with the required information's</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterArchetype(Identification archetypeId, ArchetypeInfo info)
    {
        ArchetypeManager.AddArchetype(archetypeId, new ArchetypeContainer(info.ComponentIDs),
            info.EntitySetup, info.AdditionalDlls);
    }

    /// <summary>
    /// Extend a Archetype
    /// Used by the SourceGenerator for the <see cref="Registries.ExtendArchetypeAttribute"/>
    /// </summary>
    /// <param name="archetypeId">Id of the archetype</param>
    /// <param name="info">Archetype info with the required information's</param>
    [RegisterMethod(ObjectRegistryPhase.Post, RegisterMethodOptions.UseExistingId)]
    public static void ExtendArchetype(Identification archetypeId, ArchetypeInfo info)
    {
        ArchetypeManager.AddArchetype(archetypeId, new ArchetypeContainer(info.ComponentIDs),
            info.EntitySetup, info.AdditionalDlls);
    }
}

/// <summary>
/// Container storing the required information for adding an Archetype
/// </summary>
public struct ArchetypeInfo
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="componentIDs">Ids of the components to use in the archetype</param>
    /// <param name="entitySetup">Optional entity setup which gets executed when the entity is created</param>
    /// <param name="additionalDlls">Additional dlls which need to be referenced for the auto generated <see cref="IArchetypeStorage"/></param>
    public ArchetypeInfo(IEnumerable<Identification> componentIDs, IEntitySetup? entitySetup = null,
        IEnumerable<string>? additionalDlls = null)
    {
        ComponentIDs = componentIDs;
        AdditionalDlls = additionalDlls ?? Enumerable.Empty<string>();
        EntitySetup = entitySetup;
    }

    /// <summary>
    /// Ids of the components to use in the archetype
    /// </summary>
    public IEnumerable<Identification> ComponentIDs;

    /// <summary>
    /// Optional entity setup which gets executed when the entity is created
    /// </summary>
    public IEntitySetup? EntitySetup;

    /// <summary>
    /// Additional dlls which need to be referenced for the auto generated <see cref="IArchetypeStorage"/>
    /// </summary>
    public IEnumerable<string> AdditionalDlls;
}