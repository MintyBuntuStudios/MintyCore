﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Mesh" />
/// </summary>
[Registry("mesh", "models", applicableGameType: GameType.Client)]
[PublicAPI]
public class MeshRegistry : IRegistry
{
    public required IMeshManager MeshManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        MeshManager.RemoveMesh(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Log.Information("Clearing Meshes");
        MeshManager.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Mesh;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary>
    /// Register a mesh
    /// Used by the source generator
    /// </summary>
    /// <param name="id">Id of the mesh to add</param>
    [RegisterMethod(ObjectRegistryPhase.Main, RegisterMethodOptions.HasFile)]
    public void RegisterMesh(Identification id)
    {
        if (Engine.HeadlessModeActive)
            return;

        MeshManager.AddStaticMesh(id);
    }
}