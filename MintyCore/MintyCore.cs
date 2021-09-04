﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BulletSharp;
using BulletSharp.Math;
using ImGuiNET;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Network;
using MintyCore.Network.Messages;
using MintyCore.Physics;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace MintyCore
{
    /// <summary>
    ///     Engine/CoreGame main class
    /// </summary>
    public static class MintyCore
    {
        private static readonly Stopwatch _tickTimeWatch = new();

        private static readonly MintyCoreMod _mod = new();

        internal static RenderModeEnum RenderMode = RenderModeEnum.NORMAL;

        private static World? _world;

        public static World? ServerWorld = null;
        public static World? ClientWorld = null;

        public static Server? Server = null;
        public static Client? Client = null;

        internal static bool GameLoopRunning;
        private static Stopwatch _tick;
        private static Stopwatch _render;
        private static double _accumulatedMillis;
        private static Random _rnd;
        private static double _msDuration;

        private static RenderAble _renderAble;

        /// <summary>
        ///     The <see cref="GameType" /> of the running instance
        /// </summary>
        public static GameType GameType { get; private set; }

        /// <summary>
        ///     The reference to the main <see cref="Window" />
        /// </summary>
        public static Window? Window { get; private set; }

        /// <summary>
        ///     The delta time of the current tick as double in Seconds
        /// </summary>
        public static double DDeltaTime { get; private set; }

        /// <summary>
        ///     The delta time of the current tick in Seconds
        /// </summary>
        public static float DeltaTime { get; private set; }

        /// <summary>
        ///     Fixed delta time for physics simulation in Seconds
        /// </summary>
        public static float FixedDeltaTime { get; } = 0.02f;

        /// <summary>
        ///     The current Tick number. Capped between 0 and 1_000_000_000 (exclusive)
        /// </summary>
        public static int Tick { get; private set; }

        private static void Main(string[] args)
        {
            Init();
            //Run();
            MainMenu();
            CleanUp();
        }


        private static void Init()
        {
            Logger.InitializeLog();
            Window = new Window();

            VulkanEngine.Setup();

            //Temporary until a proper mod loader is ready
            RegistryManager.RegistryPhase = RegistryPhase.MODS;
            var modId = RegistryManager.RegisterModId("techardry_core", "");
            RegistryManager.RegistryPhase = RegistryPhase.CATEGORIES;
            _mod.Register(modId);
            RegistryManager.RegistryPhase = RegistryPhase.OBJECTS;
            RegistryManager.ProcessRegistries();
            RegistryManager.RegistryPhase = RegistryPhase.NONE;
        }

        private static void MainMenu()
        {
            string bufferTargetAddress = "localhost";
            string bufferPort = "5665";
            while (Window.Exists)
            {
                SetDeltaTime();
                var snapshot = Window.PollEvents();
                VulkanEngine.PrepareDraw(snapshot);

                bool run = false;

                if (ImGui.Begin("\"Main Menu\""))
                {
                    ImGui.InputText("ServerAddress", ref bufferTargetAddress, 50);
                    ImGui.InputText("Port", ref bufferPort, 5);

                    if (ImGui.Button("Connect to Server"))
                        Console.WriteLine(bufferTargetAddress);


                    if (ImGui.Button("Run Game"))
                    {
                        run = true;
                    }
                }

                ImGui.End();

                VulkanEngine.BeginDraw();
                VulkanEngine.EndDraw();

                if (run) Run();
            }
        }

        internal static bool StopCommandIssued = false;
        internal static bool Stop => StopCommandIssued || (Window is not null && !Window.Exists);

        internal static void GameLoop()
        {
            var serverUpdateData = new ComponentUpdate.ComponentData();
            var clientUpdateData = new ComponentUpdate.ComponentData();
            var serverUpdateDic = serverUpdateData.components;
            var clientUpdateDic = clientUpdateData.components;

            
            while (Stop == false)
            {
                SetDeltaTime();
                
                var snapshot = Window.PollEvents();

                VulkanEngine.PrepareDraw(snapshot);
                VulkanEngine.BeginDraw();
            
                ServerWorld?.Tick();
                ClientWorld?.Tick();
                
                foreach (var archetypeId in ArchetypeManager.GetArchetypes().Keys)
                {
                    var serverStorage = ServerWorld?.EntityManager.GetArchetypeStorage(archetypeId);
                    var clientStorage = ClientWorld?.EntityManager.GetArchetypeStorage(archetypeId);

                    if (serverStorage is not null)
                    {
                        ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(serverStorage);
                        while (dirtyComponentQuery.MoveNext())
                        {
                            var component = dirtyComponentQuery.Current;
                            if(ComponentManager.IsPlayerControlled(component.ComponentId)) continue;;
                            if(!serverUpdateDic.ContainsKey(component.Entity)) serverUpdateDic.Add(component.Entity, new());
                            serverUpdateDic[component.Entity].Add((component.ComponentId, component.ComponentPtr));
                        }
                    }
                    
                    if (clientStorage is not null)
                    {
                        ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(clientStorage);
                        while (dirtyComponentQuery.MoveNext())
                        {
                            var component = dirtyComponentQuery.Current;
                            if(!ComponentManager.IsPlayerControlled(component.ComponentId)) continue;;
                            if(!clientUpdateDic.ContainsKey(component.Entity)) clientUpdateDic.Add(component.Entity, new());
                            clientUpdateDic[component.Entity].Add((component.ComponentId, component.ComponentPtr));
                        }
                    }
                }
                
                Server?._messageHandler.SendMessage(MessageIDs.ComponentUpdate, serverUpdateData);
                Client?._messageHandler.SendMessage(MessageIDs.ComponentUpdate, serverUpdateData);

                Server?.Update();
                Client?.Update();
                
                VulkanEngine.EndDraw();
            }
        }

        private static void SetDeltaTime()
        {
            _tickTimeWatch.Stop();
            DDeltaTime = _tickTimeWatch.Elapsed.TotalSeconds;
            DeltaTime = (float)_tickTimeWatch.Elapsed.TotalSeconds;
            _tickTimeWatch.Restart();
        }

        internal static void NextRenderMode()
        {
            var numRenderMode = (int)RenderMode;
            numRenderMode %= 3;
            numRenderMode++;
            RenderMode = (RenderModeEnum)numRenderMode;
        }

        private static unsafe void Run()
        {
            _world = new World();
            
            SpawnPlayer(Constants.ServerId);
            
            //SpawnWallOfDirt();

            var materials = new UnmanagedArray<GCHandle>(1);
            materials[0] = MaterialHandler.GetMaterialHandle(MaterialIDs.Color);
            
            _renderAble = new RenderAble();
            _renderAble.SetMesh(MeshIDs.Cube);
            _renderAble.SetMaterials(materials);

            var cubePos = new Position { Value = new Vector3(0, 0, 0) };

            var mass = new Mass();
            mass.MassValue = 1;

            Rotation rotation = new();
            rotation.Value = Quaternion.CreateFromYawPitchRoll(0, 0, 0);

            Transform transform = new();
            transform.PopulateWithDefaultValues();

            SpawnCube(_renderAble, mass, cubePos, transform,
                CreateBoxCollider(cubePos.Value, rotation.Value, Vector3.One));


            var plane = _world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);
            mass.SetInfiniteMass();

            var scale = new Scale();
            scale.Value = new Vector3(100, 1, 100);
            cubePos.Value = new Vector3(0, -3, 0);

            var planeCollider = CreateBoxCollider(cubePos.Value, Quaternion.Identity, scale.Value);

            _world.EntityManager.SetComponent(plane, _renderAble);
            _world.EntityManager.SetComponent(plane, mass);
            _world.EntityManager.SetComponent(plane, scale);
            _world.EntityManager.SetComponent(plane, cubePos);
            _world.EntityManager.SetComponent(plane, planeCollider);

            //The ref count was increased by the entity, so the local reference can be removed
            planeCollider.DecreaseRefCount();


            _world.SetupTick();

            _tick = Stopwatch.StartNew();
            _render = new Stopwatch();
            _accumulatedMillis = 0;
            _rnd = new Random();
            _msDuration = 0.1;

            GameLoopRunning = true;
            while (Window.Exists)
            {
                var mass1 = new Mass();
                mass1.MassValue = 7800;
                Position cubePos1 = new();
                Transform transform1 = new();
                if (Tick % 1000 == 0)
                {
                    _tick.Stop();
                    //Logger.WriteLog($"Tick duration for the last 100 frames:", LogImportance.INFO, "General", null, true);
                    //Logger.WriteLog($"Complete: {tick.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);
                    //Logger.WriteLog($"Rendering: {render.Elapsed.TotalMilliseconds / 100}", LogImportance.INFO, "General", null, true);

                    if (_tick.Elapsed.TotalMilliseconds >= _msDuration)
                    {
                        Logger.WriteLog(
                            $"Took: {_tick.Elapsed.TotalMilliseconds / 1000} for {_world.EntityManager.EntityCount}",
                            LogImportance.INFO, "General");
                        _msDuration *= 10;
                    }

                    _accumulatedMillis += _tick.Elapsed.TotalMilliseconds;
                    while (_accumulatedMillis > 50)
                    {
                        _accumulatedMillis -= 50;
                        var x = _rnd.Next(-300, 300) / 100f;
                        var z = _rnd.Next(-300, 300) / 100f;
                        float y = 10;
                        cubePos1.Value = new Vector3(x, y, z);
                        transform1.Value = Matrix4x4.CreateTranslation(x, y, z);
                        SpawnCube(_renderAble, mass1, cubePos1, transform1,
                            CreateBoxCollider(cubePos1.Value, rotation.Value, Vector3.One));
                    }

                    _render.Reset();
                    _tick.Reset();
                    _tick.Start();
                }

                SetDeltaTime();
                var snapshot = Window.PollEvents();

                VulkanEngine.PrepareDraw(snapshot);
                VulkanEngine.BeginDraw();
                _world.Tick();


                foreach (var archetypeId in ArchetypeManager.GetArchetypes().Keys)
                {
                    var storage = _world.EntityManager.GetArchetypeStorage(archetypeId);
                    ArchetypeStorage.DirtyComponentQuery dirtyComponentQuery = new(storage);
                    while (dirtyComponentQuery.MoveNext())
                    {
                    }
                }

                _render.Start();
                VulkanEngine.EndDraw();
                _render.Stop();

                Logger.AppendLogToFile();
                Tick = (Tick + 1) % 1_000_000_000;
            }

            _renderAble.SetMaterials(default);
            materials.DecreaseRefCount();

            GameLoopRunning = false;
            _world.Dispose();

            Collider CreateBoxCollider(Vector3 position, Quaternion rotationQuaternion, Vector3 scale)
            {
                var boxCollider = new Collider();
                var colliderScale = *(BulletSharp.Math.Vector3*)&scale / 2;
                var colliderPos = *(BulletSharp.Math.Vector3*)&position;
                var colliderRot = *(BulletSharp.Math.Quaternion*)&rotationQuaternion;

                var collisionShape = PhysicsObjects.CreateBoxShape(colliderScale);
                var motionState = PhysicsObjects.CreateMotionState(Matrix.Translation(colliderPos) *
                                                                   Matrix.RotationQuaternion(
                                                                       colliderRot));

                boxCollider.CollisionShape = collisionShape;
                boxCollider.MotionState = motionState;

                //The reference is automatically "taken" by the created collider, so the local reference can be "disposed"
                collisionShape.Dispose();
                motionState.Dispose();

                return boxCollider;
            }
        }

        private static void SpawnCube(RenderAble renderAble, Mass mass, Position cubePos, Transform transform,
            Collider collider)
        {
            var physicsCube = _world.EntityManager.CreateEntity(ArchetypeIDs.RigidBody);
            mass.MassValue = 7800;
            _world.EntityManager.SetComponent(physicsCube, renderAble);
            _world.EntityManager.SetComponent(physicsCube, mass);
            _world.EntityManager.SetComponent(physicsCube, cubePos);
            _world.EntityManager.SetComponent(physicsCube, transform);
            _world.EntityManager.SetComponent(physicsCube, collider);

            collider.DecreaseRefCount();
        }

        internal static void SpawnPlayer(ushort playerID)
        {
            var materials = new UnmanagedArray<GCHandle>(1);
            materials[0] = MaterialHandler.GetMaterialHandle(MaterialIDs.Color);
            
            _renderAble = new RenderAble();
            _renderAble.SetMesh(MeshIDs.Capsule);
            _renderAble.SetMaterials(materials);

            var playerEntity = _world.EntityManager.CreateEntity(ArchetypeIDs.Player, playerID);

            var playerPos = new Position { Value = new Vector3(0, 0, 5) };
            _world.EntityManager.SetComponent(playerEntity, playerPos);
            _world.EntityManager.SetComponent(playerEntity, _renderAble);
            
            materials.DecreaseRefCount();
            _renderAble.DecreaseRefCount();
        }

        private static void CleanUp()
        {
            var tracker = BulletObjectTracker.Current;
            if (tracker.GetUserOwnedObjects().Count != 0)
                Logger.WriteLog($"{tracker.GetUserOwnedObjects().Count} BulletObjects were not disposed",
                    LogImportance.WARNING, "Physics");
            else
                Logger.WriteLog("All BulletObjects were disposed", LogImportance.INFO, "Physics");


            VulkanEngine.Stop();
            AllocationHandler.CheckUnFreed();
        }

        [Flags]
        internal enum RenderModeEnum
        {
            NORMAL = 1,
            WIREFRAME = 2
        }

        internal static Dictionary<ushort, ulong> _playerIDs = new();
        internal static Dictionary<ushort, string> _playerNames = new();
        private static ushort _lastFreedId = Constants.ServerId + 1;

        public static ushort LocalPlayerGameId { get; internal set; } = Constants.InvalidId;

        public static void RemovePlayer(ushort playerId)
        {
            //TODO Do something like remove all player owned entities and sync the player disconnect with other clients

            _playerIDs.Remove(playerId);
            _playerNames.Remove(playerId);
            _lastFreedId = playerId < _lastFreedId ? playerId : _lastFreedId;
        }

        public static bool AddPlayer(string playerName, ulong playerId, out ushort id)
        {
            //TODO implement
            id = _lastFreedId;
            while (_playerIDs.ContainsKey(id)) id++;

            _playerIDs.Add(id, playerId);
            _playerNames.Add(id, playerName);

            CreatePlayerEntities(id);

            return true;
        }

        private static void CreatePlayerEntities(ushort id)
        {
            throw new NotImplementedException();
        }

        public static void CreatePlayerWorld()
        {
            ClientWorld = new World(false);
        }
    }
}