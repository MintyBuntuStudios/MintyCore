﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.Registries
{
    //TODO Add Try Get
    public static class RegistryManager
    {
        private static Dictionary<ushort, IRegistry> _registries = new();

        private static Dictionary<string, ushort> _modID = new();
        private static Dictionary<string, ushort> _categoryID = new();

        private static Dictionary<Identification, Dictionary<string, uint>> _objectID =
            new();

        private static Dictionary<ushort, string> _reversedModID = new();
        private static Dictionary<ushort, string> _reversedCategoryID = new();

        private static Dictionary<Identification, Dictionary<uint, string>> _reversedObjectID =
            new();

        private static Dictionary<ushort, string> _modFolderName = new();
        private static Dictionary<ushort, string> _categoryFolderName = new();
        private static Dictionary<Identification, string> _objectFileName = new();

        public static RegistryPhase RegistryPhase { get; internal set; } = RegistryPhase.None;

        public static ushort RegisterModID(string stringIdentifier, string folderName)
        {
            AssertModRegistryPhase();

            if (_modID.ContainsKey(stringIdentifier))
            {
                return _modID[stringIdentifier];
            }

            ushort modID = Constants.InvalidID;
            do
            {
                modID++;
            } while (_reversedModID.ContainsKey(modID));

            _modID.Add(stringIdentifier, modID);
            _reversedModID.Add(modID, stringIdentifier);
            _modFolderName.Add(modID, folderName);

            return modID;
        }

        public static ushort RegisterCategoryID(string stringIdentifier, string? folderName)
        {
            AssertCategoryRegistryPhase();

            if (_categoryID.ContainsKey(stringIdentifier))
            {
                return _categoryID[stringIdentifier];
            }

            ushort categoryID = Constants.InvalidID;
            do
            {
                categoryID++;
            } while (_reversedCategoryID.ContainsKey(categoryID));

            _categoryID.Add(stringIdentifier, categoryID);
            _reversedCategoryID.Add(categoryID, stringIdentifier);
            if (folderName is not null)
            {
                _categoryFolderName.Add(categoryID, folderName);
            }

            return categoryID;
        }

        public static Identification RegisterObjectID(ushort modID, ushort categoryID, string stringIdentifier,
            string? fileName = null)
        {
            AssertObjectRegistryPhase();

            Identification modCategoryID = new Identification(modID, categoryID, Constants.InvalidID);

            if (!_objectID.ContainsKey(modCategoryID))
            {
                _objectID.Add(modCategoryID, new Dictionary<string, uint>());
                _reversedObjectID.Add(modCategoryID, new Dictionary<uint, string>());
            }

            if (_objectID[modCategoryID].ContainsKey(stringIdentifier))
            {
                return new Identification(modID, categoryID, _objectID[modCategoryID][stringIdentifier]);
            }

            uint objectID = Constants.InvalidID;
            do
            {
                objectID++;
            } while (_reversedObjectID[modCategoryID].ContainsKey(objectID));

            Identification id = new(modID, categoryID, objectID);

            _objectID[modCategoryID].Add(stringIdentifier, objectID);
            _reversedObjectID[modCategoryID].Add(objectID, stringIdentifier);
            if (fileName is not null)
            {
                if (!_categoryFolderName.ContainsKey(categoryID))
                {
                    throw new ArgumentException(
                        "An object file name is only allowed if a category folder name is defined");
                }

                var fileLocation = $@".\{_modFolderName[modID]}\Resources\{_categoryFolderName[categoryID]}\{fileName}";

				if (!File.Exists(fileLocation))
				{
                    Logger.WriteLog($"File added as reference for id {id} at the location {fileLocation} does not exists.", LogImportance.EXCEPTION, "Registry");
				}

                _objectFileName.Add(id, $@".\{_modFolderName[modID]}\Resources\{_categoryFolderName[categoryID]}\{fileName}");
            }

            return id;
        }

        public static string GetResourceFileName(Identification id)
        {
            return _objectFileName[id];
        }

        public static ushort AddRegistry<T>(string stringIdentifier, string? assetFolderName = null)
            where T : class, IRegistry, new()
        {
            AssertCategoryRegistryPhase();

            var categoryId = RegisterCategoryID(stringIdentifier, assetFolderName);

            _registries.Add(categoryId, new T());
            return categoryId;
        }

        internal static void ProcessRegistries()
        {
            AssertObjectRegistryPhase();
            foreach (var registry in _registries)
            {
                foreach (var dependency in registry.Value.RequiredRegistries)
                {
                    if (!_registries.ContainsKey(dependency))
                    {
                        throw new Exception(
                            $"Registry'{_reversedCategoryID[registry.Key]}' depends on not present registry '{_reversedCategoryID[dependency]}'");
                    }
                }
            }


            Queue<IRegistry> registryOrder = new(_registries.Count);

            HashSet<IRegistry> registriesToProcess = new(_registries.Values);

            while (registriesToProcess.Count > 0)
            {
                foreach (var registry in new HashSet<IRegistry>(registriesToProcess))
                {
                    bool allDependenciesPresent = true;
                    foreach (var dependency in registry.RequiredRegistries)
                    {
                        if (registriesToProcess.Contains(_registries[dependency]))
                        {
                            allDependenciesPresent = false;
                            break;
                        }
                    }

                    if (!allDependenciesPresent)
                    {
                        continue;
                    }

                    registryOrder.Enqueue(registry);
                    registriesToProcess.Remove(registry);
                }
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().PreRegister();
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().Register();
            }

            for (Queue<IRegistry> registries = new(registryOrder); registries.Count > 0;)
            {
                registries.Dequeue().PostRegister();
            }
        }

        public static string GetModStringID(ushort modID)
        {
            return modID != 0 ? _reversedModID[modID] : "invalid";
        }

        public static string GetCategoryStringID(ushort categoryID)
        {
            return categoryID != 0 ? _reversedCategoryID[categoryID] : "invalid";
        }

        public static string GetObjectStringID(ushort modID, ushort categoryID, uint objectID)
        {
            if (modID == 0 || categoryID == 0 || objectID == 0) return "invalid";
            return _reversedObjectID[new Identification(modID, categoryID, Constants.InvalidID)][objectID];
        }

        [Conditional("DEBUG")]
        public static void AssertModRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.Mods)
            {
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Mods}", LogImportance.EXCEPTION, "Registry");
            }
        }

        [Conditional("DEBUG")]
        public static void AssertCategoryRegistryPhase()
        {
            if (RegistryPhase != RegistryPhase.Categories)
            {
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Categories}", LogImportance.EXCEPTION, "Registry");
            }
        }

        [Conditional("DEBUG")]
        public static void AssertObjectRegistryPhase()
		{
            if(RegistryPhase != RegistryPhase.Objects)
			{
                Logger.WriteLog($"Game is not in the {nameof(RegistryPhase)}.{RegistryPhase.Objects}", LogImportance.EXCEPTION, "Registry");
			}
		}
    }

    public enum RegistryPhase
	{
        None,
        Mods,
        Categories,
        Objects
	}
}