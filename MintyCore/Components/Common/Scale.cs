﻿using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common;

/// <summary>
///     Component to store the scale value of an entity
/// </summary>
public struct Scale : IComponent
{
    /// <summary>
    ///     The scale of an entity as a <see cref="Vector3" />
    /// </summary>
    public Vector3 Value;

    /// <inheritdoc />
    public byte Dirty { get; set; }

    /// <inheritdoc />
    public Identification Identification => ComponentIDs.Scale;

    /// <inheritdoc />
    public void DecreaseRefCount()
    {
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, World world, Entity entity)
    {
        if (!reader.TryGetVector3(out var result)) return false;

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
        Value = Vector3.One;
    }

    /// <inheritdoc />
    public void Serialize(DataWriter writer, World world, Entity entity)
    {
        writer.Put(Value);
    }
}