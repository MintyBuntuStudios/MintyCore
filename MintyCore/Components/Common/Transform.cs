﻿using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common;

/// <summary>
///     Component to store the transform value of an entity (as "T:Ara3d.Matrix4x4")
/// </summary>
public struct Transform : IComponent
{
    /// <summary>
    ///     The value of an entities transform
    /// </summary>
    public Matrix4x4 Value;

    /// <inheritdoc />
    public byte Dirty { get; set; }

    /// <inheritdoc />
    public Identification Identification => ComponentIDs.Transform;

    /// <inheritdoc />
    public void DecreaseRefCount()
    {
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetMatrix4X4(out var result)) return false;

        Value = result;
        return true;
    }

    /// <inheritdoc />
    public void IncreaseRefCount()
    {
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        Value = Matrix4x4.Identity;
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        writer.Put(Value);
    }
}