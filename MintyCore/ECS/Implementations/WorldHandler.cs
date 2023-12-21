﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore.ECS.Implementations;

/// <summary>
/// General class to handle all created worlds
/// </summary>
[PublicAPI]
[Singleton<IWorldHandler>]
internal class WorldHandler : IWorldHandler
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _worldContainerBuilder = new();
    private readonly Dictionary<Identification, IWorld> _serverWorlds = new();
    private readonly Dictionary<Identification, IWorld> _clientWorlds = new();
    private ILifetimeScope? _worldLifetimeScope;

    /// <summary/>
    public required ILifetimeScope LifetimeScope { private get; init; }

    /// <summary/>
    public required IComponentManager ComponentManager { private get; init; }

    /// <summary/>
    public required IArchetypeManager ArchetypeManager { private get; init; }

    /// <summary/>
    public required IPlayerHandler PlayerHandler { private get; init; }

    /// <summary/>
    public required INetworkHandler NetworkHandler { private get; init; }

    /// <summary/>
    /// <summary>
    /// Event which gets fired right after a world gets created
    /// The <see cref="IWorld"/> parameter is the world which was created
    /// </summary>
    public event Action<IWorld> OnWorldCreate = delegate { };

    /// <summary>
    /// Event which gets fired right before a world gets destroyed
    /// The <see cref="IWorld"/> parameter is the world which will be destroyed
    /// </summary>
    public event Action<IWorld> OnWorldDestroy = delegate { };

    /// <summary>
    /// Event which gets fired before a specific World gets updated
    /// The <see cref="IWorld"/> parameter is the world which will be updated
    /// </summary>
    public event Action<IWorld> BeforeWorldUpdate = delegate { };

    /// <summary>
    /// Event which gets fired after a specific World was updated
    /// The <see cref="IWorld"/> parameter is the world which was updated
    /// </summary>
    public event Action<IWorld> AfterWorldUpdate = delegate { };

    /// <inheritdoc />
    public void AddWorld<TWorld>(Identification worldId) where TWorld : class, IWorld
    {
        _worldContainerBuilder[worldId] = builder =>
        {
            //I don't like this solution, but i need to set the IsServerWorld property
            builder.RegisterType<TWorld>().Keyed<IWorld>((worldId, GameType.Client)).As<IWorld>()
                .WithProperty(nameof(IWorld.IsServerWorld), false);
            
            builder.RegisterType<TWorld>().Keyed<IWorld>((worldId, GameType.Server)).As<IWorld>()
                .WithProperty(nameof(IWorld.IsServerWorld), true);
        };
        InvalidateLifetimeScope();
    }

    private void InvalidateLifetimeScope()
    {
        foreach (var world in _serverWorlds.Values)
        {
            OnWorldDestroy(world);
            world.Dispose();
        }

        foreach (var world in _clientWorlds.Values)
        {
            OnWorldDestroy(world);
            world.Dispose();
        }

        _clientWorlds.Clear();
        _serverWorlds.Clear();
        _worldLifetimeScope?.Dispose();
        _worldLifetimeScope = null;
    }

    public void CreateWorldLifetimeScope()
    {
        _worldLifetimeScope = LifetimeScope.BeginLifetimeScope(builder =>
        {
            foreach (var (_, value) in _worldContainerBuilder) value(builder);
        });
    }

    public void Clear()
    {
        DestroyWorlds(GameType.Local);

        OnWorldCreate = delegate { };
        OnWorldDestroy = delegate { };
        BeforeWorldUpdate = delegate { };
        AfterWorldUpdate = delegate { };
    }

    /// <summary>
    /// Try get a specific world
    /// </summary>
    /// <param name="worldType">Type of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <param name="worldId"><see cref="Identification"/> of the world</param>
    /// <param name="world">The fetched world. Null if not found</param>
    /// <returns>True if found</returns>
    public bool TryGetWorld(GameType worldType, Identification worldId, [MaybeNullWhen(false)] out IWorld world)
    {
        Logger.AssertAndThrow(worldType is GameType.Client or GameType.Server,
            $"{nameof(TryGetWorld)} must be invoked with {nameof(GameType.Server)} or {nameof(GameType.Client)}",
            "ECS");

        switch (worldType)
        {
            case GameType.Client:
            {
                return _clientWorlds.TryGetValue(worldId, out world);
            }
            case GameType.Server:
            {
                return _serverWorlds.TryGetValue(worldId, out world);
            }
            case GameType.None:
            case GameType.Local:
            default:
            {
                world = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Get an enumeration with all worlds for the given game type
    /// </summary>
    /// <param name="worldType">GameType of the world. Has to be <see cref="GameType.Server"/> or <see cref="GameType.Client"/></param>
    /// <returns>Enumerable containing all worlds</returns>
    public IEnumerable<IWorld> GetWorlds(GameType worldType)
    {
        Logger.AssertAndThrow(worldType is GameType.Client or GameType.Server,
            $"{nameof(GetWorlds)} must be invoked with {nameof(GameType.Server)} or {nameof(GameType.Client)}",
            "ECS");

        return worldType == GameType.Client ? _clientWorlds.Values : _serverWorlds.Values;
    }

    /// <summary>
    /// Create all available worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    public void CreateWorlds(GameType worldType)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Enumerable containing the worlds to create</param>
    public void CreateWorlds(GameType worldType, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worlds">Worlds to create</param>
    public void CreateWorlds(GameType worldType, params Identification[] worlds)
    {
        foreach (var worldId in worlds) CreateWorld(worldType, worldId);
    }

    /// <summary>
    /// Create a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get created</param>
    /// <param name="worldId">The id of the world to create</param>
    public void CreateWorld(GameType worldType, Identification worldId)
    {
        Logger.AssertAndThrow(_worldLifetimeScope is not null, "WorldLifetimeScope is null", nameof(WorldHandler));

        if (MathHelper.IsBitSet((int)worldType, (int)GameType.Client) &&
            //The assert function checks if there is no client world with id present and returns true
            Logger.AssertAndLog(!_clientWorlds.ContainsKey(worldId),
                $"A client world with id {worldId} is already created", "ECS", LogImportance.Warning))
        {
            Logger.WriteLog($"Create client world with id {worldId}", LogImportance.Info, "ECS");
            
            var world = _worldLifetimeScope.ResolveKeyed<IWorld>((worldId, GameType.Client));

            _clientWorlds.Add(worldId, world);
            OnWorldCreate(world);
        }

        // ReSharper disable once InvertIf; keep it in the same style as above
        if (MathHelper.IsBitSet((int)worldType, (int)GameType.Server) &&
            Logger.AssertAndLog(!_serverWorlds.ContainsKey(worldId),
                $"A server world with id {worldId} is already created", "ECS", LogImportance.Warning))
        {
            Logger.WriteLog($"Create server world with id {worldId}", LogImportance.Info, "ECS");
            
            var world = _worldLifetimeScope.ResolveKeyed<IWorld>((worldId, GameType.Server));
            _serverWorlds.Add(worldId, world);
            OnWorldCreate(world);
        }
    }

    /// <summary>
    /// Destroy all worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    public void DestroyWorlds(GameType worldType)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the worlds. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Enumerable containing the worlds to destroy</param>
    public void DestroyWorlds(GameType worldType, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy the specified worlds
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worlds">Worlds to destroy</param>
    public void DestroyWorlds(GameType worldType, params Identification[] worlds)
    {
        foreach (var worldId in worlds) DestroyWorld(worldType, worldId);
    }

    /// <summary>
    /// Destroy a specific world
    /// </summary>
    /// <param name="worldType">The type of the world. <see cref="GameType.Local"/> means that a server and client world get destroyed</param>
    /// <param name="worldId">The id of the world to destroy</param>    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DestroyWorld(GameType worldType, Identification worldId)
    {
        // ReSharper disable once InlineOutVariableDeclaration; A inline declaration prevents null checking
        IWorld world;
        if (MathHelper.IsBitSet((int)worldType, (int)GameType.Client)
            && Logger.AssertAndLog(_clientWorlds.Remove(worldId, out world!),
                $"No client world with id {worldId} present to destroy", "ECS", LogImportance.Warning))
            DestroyWorld(world);

        // ReSharper disable once InvertIf; Keep consistency between both blocks
        if (MathHelper.IsBitSet((int)worldType, (int)GameType.Server)
            && Logger.AssertAndLog(_serverWorlds.Remove(worldId, out world!),
                $"No server world with id {worldId} present to destroy", "ECS", LogImportance.Warning))
            DestroyWorld(world);
    }

    /// <summary>
    /// Destroys a specific world
    /// </summary>
    /// <param name="world">World to destroy</param>
    private void DestroyWorld(IWorld world)
    {
        Logger.WriteLog($"Destroy {(world.IsServerWorld ? "server" : "client")} world with id {world.Identification}",
            LogImportance.Info, "ECS");
        OnWorldDestroy(world);
        world.Dispose();
    }

    /// <summary>
    /// Send all entities of all worlds to the specified player
    /// </summary>
    /// <param name="player"></param>
    public void SendEntitiesToPlayer(Player player)
    {
        foreach (var worldId in _serverWorlds.Keys) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    public void SendEntitiesToPlayer(Player player, IEnumerable<Identification> worlds)
    {
        foreach (var worldId in worlds) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worlds">Ids of worlds to send entities from</param>
    public void SendEntitiesToPlayer(Player player, params Identification[] worlds)
    {
        foreach (var worldId in worlds) SendEntitiesToPlayer(player, worldId);
    }

    /// <summary>
    /// Send all entities of the given worlds to the specified player
    /// </summary>
    /// <param name="player">Player to send entities to</param>
    /// <param name="worldId">Id of the world to send entities from</param>
    public void SendEntitiesToPlayer(Player player, Identification worldId)
    {
        if (!Logger.AssertAndLog(_serverWorlds.TryGetValue(worldId, out var world),
                $"Cant send entities to player, server world {worldId} does not exist", "ECS", LogImportance.Error) ||
            world is null)
            return;

        var sendEntityData = NetworkHandler.CreateMessage<SendEntityData>();
        foreach (var entity in world.EntityManager.Entities)
        {
            sendEntityData.Entity = entity;
            sendEntityData.EntityOwner = world.EntityManager.GetEntityOwner(entity);
            sendEntityData.WorldId = worldId;

            sendEntityData.Send(player.GameId);
        }
    }

    /// <summary>
    /// Send entity updates for all worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"><see cref="GameType"/> worlds to send entity updates</param>
    public void SendEntityUpdates(GameType worldTypeToUpdate = GameType.Local)
    {
        foreach (var worldId in _worldContainerBuilder.Keys) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    public void SendEntityUpdates(GameType worldTypeToUpdate, IEnumerable<Identification> worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    public void SendEntityUpdates(GameType worldTypeToUpdate, params Identification[] worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate) SendEntityUpdate(worldTypeToUpdate, worldId);
    }

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    public void SendEntityUpdate(GameType worldTypeToUpdate, Identification worldToUpdate)
    {
        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Client) &&
            _clientWorlds.TryGetValue(worldToUpdate, out var world))
            SendEntityUpdate(world);

        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Server) &&
            _serverWorlds.TryGetValue(worldToUpdate, out world))
            SendEntityUpdate(world);
    }

    /// <summary>
    /// Send entity updates for the given world
    /// </summary>
    public void SendEntityUpdate(IWorld world)
    {
        var message = NetworkHandler.CreateMessage<ComponentUpdate>();
        message.WorldGameType = world.IsServerWorld ? GameType.Server : GameType.Client;
        message.WorldId = world.Identification;

        var updateDic = message.Components;

        foreach (var archetypeId in ArchetypeManager.GetArchetypes().Keys)
        {
            var storage = world.EntityManager.GetArchetypeStorage(archetypeId);
            foreach (var component in storage.GetDirtyEnumerator())
            {
                var playerControlled = ComponentManager.IsPlayerControlled(component.componentId);

                switch (world.IsServerWorld)
                {
                    //if server world and player controlled; we can skip
                    case true when playerControlled:
                    //if client world but not player controlled; we can skip
                    case false when !playerControlled:
                    //if client world and player controlled but the wrong player locally; we can skip
                    case false when playerControlled && world.EntityManager.GetEntityOwner(component.entity) !=
                        PlayerHandler.LocalPlayerGameId:
                        continue;
                }

                if (!updateDic.ContainsKey(component.entity))
                    updateDic.Add(component.entity, ComponentUpdate.GetComponentsList());

                var componentList = updateDic[component.entity];
                componentList.Add((component.componentId, component.componentPtr));
            }
        }

        if (world.IsServerWorld)
            message.Send(PlayerHandler.GetConnectedPlayers());
        else
            message.SendToServer();

        message.Clear();
    }

    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable)
    {
        foreach (var worldId in _worldContainerBuilder.Keys)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, IEnumerable<Identification> worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update the given worlds
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldsToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    public void UpdateWorlds(GameType worldTypeToUpdate, bool simulationEnable, params Identification[] worldsToUpdate)
    {
        foreach (var worldId in worldsToUpdate)
            UpdateWorld(worldTypeToUpdate, worldId, simulationEnable);
    }

    /// <summary>
    /// Update a specific world
    /// </summary>
    /// <param name="worldTypeToUpdate"></param>
    /// <param name="worldToUpdate"></param>
    /// <param name="simulationEnable"></param>
    /// <param name="drawingEnable">Whether or not the <see cref="SystemGroups.PresentationSystemGroup"/> get executed</param>
    public void UpdateWorld(GameType worldTypeToUpdate, Identification worldToUpdate, bool simulationEnable)
    {
        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Client) &&
            _clientWorlds.TryGetValue(worldToUpdate, out var world))
            UpdateWorld(world, simulationEnable);

        if (MathHelper.IsBitSet((int)worldTypeToUpdate, (int)GameType.Server) &&
            _serverWorlds.TryGetValue(worldToUpdate, out world))
            UpdateWorld(world, simulationEnable);
    }

    /// <summary>
    /// Updates a specific world
    /// </summary>
    /// <param name="world">World to update</param>
    /// <param name="simulationEnable"></param>
    private void UpdateWorld(IWorld world, bool simulationEnable)
    {
        BeforeWorldUpdate(world);

        //Disable simulation for one tick
        var reenableSimulation = world.SystemManager.GetSystemActive(SystemIDs.SimulationGroup);
        if (!simulationEnable) world.SystemManager.SetSystemActive(SystemIDs.SimulationGroup, false);

        world.EntityManager.Update();
        world.Tick();

        if (reenableSimulation) world.SystemManager.SetSystemActive(SystemIDs.SimulationGroup, true);

        AfterWorldUpdate(world);
    }

    /// <inheritdoc />
    public void RemoveWorld(Identification objectId)
    {
        _worldContainerBuilder.Remove(objectId);
        InvalidateLifetimeScope();
    }
}