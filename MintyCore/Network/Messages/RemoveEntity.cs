﻿using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Network.Messages;

/// <summary>
///     Message which is sent to issue a entity remove command
/// </summary>
[RegisterMessage("remove_entity")]
public partial class RemoveEntity : IMessage
{
    /// <summary>
    ///     Entity to remove
    /// </summary>
    public Entity Entity;

    /// <summary>
    /// World Id to remove entity from
    /// </summary>
    public Identification WorldId;

    /// <inheritdoc />
    public bool IsServer { get; set; }

    /// <inheritdoc />
    public bool ReceiveMultiThreaded => false;

    /// <inheritdoc />
    public Identification MessageId => MessageIDs.RemoveEntity;

    /// <inheritdoc />
    public DeliveryMethod DeliveryMethod => DeliveryMethod.Reliable;

    /// <inheritdoc />
    public ushort Sender { get; set; }

    /// <summary/>
    public required IWorldHandler WorldHandler { private get; init; }

    /// <summary/>
    public required INetworkHandler NetworkHandler { get; init; }

    /// <inheritdoc />
    public void Serialize(DataWriter writer)
    {
        Entity.Serialize(writer);
        WorldId.Serialize(writer);
    }

    /// <inheritdoc />
    public bool Deserialize(DataReader reader)
    {
        if (IsServer) return true;

        if (!Entity.Deserialize(reader, out var entity)) return false;
        if (!Identification.Deserialize(reader, out var worldId)) return false;
        if (!WorldHandler.TryGetWorld(GameType.Client, worldId, out var world)) return false;

        world.EntityManager.RemoveEntity(entity);

        return true;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Entity = default;
    }
}