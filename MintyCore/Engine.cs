﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ENet;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network;
using MintyCore.Render;
using MintyCore.UI;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore;

//TODO Adjust logic to be able to create a headless Server. (no window/vulkan creation)
//TODO Implement a proper World Handler, to add "custom" and multiple worlds
//TODO Implement proper exception handling, to prevent unnecessary game crashes 

/// <summary>
///     Engine/CoreGame main class
/// </summary>
public static class Engine
{
    /// <summary>
    ///     The maximum tick count before the tick counter will be set to 0, range 0 - (<see cref="MaxTickCount" /> - 1)
    /// </summary>
    public const int MaxTickCount = 1_000_000_000;

    private static readonly Stopwatch _tickTimeWatch = new();

    private static readonly List<DirectoryInfo> _additionalModDirectories = new();

    internal static bool ShouldStop;

    private static Element? _mainMenu;

    /// <summary>
    /// Indicates whether tests should be active. Meant to replace DEBUG compiler flags
    /// </summary>
    public static bool TestingModeActive { get; private set; }

    static Engine()
    {
#if DEBUG
        TestingModeActive = true;
#endif
    }

    /// <summary>
    ///     The <see cref="GameType" /> of the running instance
    /// </summary>
    public static GameType GameType { get; private set; } = GameType.INVALID;

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
    public static float FixedDeltaTime => 0.02f;

    /// <summary>
    ///     The current Tick number. Capped between 0 and <see cref="MaxTickCount" /> (exclusive)
    /// </summary>
    public static int Tick { get; private set; }

    internal static bool Stop => ShouldStop || Window is not null && !Window.Exists;


    /// <summary>
    ///     The entry/main method of the engine
    /// </summary>
    /// <remarks>Is public to allow easier mod development</remarks>
    public static void Main(string[] args)
    {
        CheckProgramArguments(args);

        Init();

        RunMainMenu();
        //DirectLocalGame();
        //MainMenu();
        CleanUp();
    }

    private static void RunMainMenu()
    {
        while (Window is not null && Window.Exists)
        {
            SetDeltaTime();

            Window.DoEvents();

            UiHandler.Update();

            if (_mainMenu is null)
            {
                _mainMenu = UiHandler.GetRootElement(UiIDs.MainMenu);
                _mainMenu.Initialize();
                _mainMenu.IsActive = true;
                MainUiRenderer.SetMainUiContext(_mainMenu);
            }

            VulkanEngine.PrepareDraw();

            MainUiRenderer.DrawMainUi();

            VulkanEngine.EndDraw();
        }

        MainUiRenderer.SetMainUiContext(null);
        _mainMenu = null;
    }

    private static void Init()
    {
        Thread.CurrentThread.Name = "MintyCoreMain";

        Logger.InitializeLog();

        Library.Initialize();
        Window = new Window();

        VulkanEngine.Setup();

        ModManager.SearchMods(_additionalModDirectories);
        ModManager.LoadRootMods();
        MainUiRenderer.SetupMainUiRendering();
    }

    private static void CheckProgramArguments(IEnumerable<string> args)
    {
        foreach (var argument in args)
        {
            if (argument.Length == 0 || argument[0] != '-') continue;

            if (argument.Equals("-testingModeActive"))
            {
                TestingModeActive = true;
                continue;
            }

            if (!argument.StartsWith("-addModDir=")) continue;

            var modDir = argument["-addModDir=".Length..];
            DirectoryInfo dir = new(modDir);
            if (dir.Exists) _additionalModDirectories.Add(dir);
        }
    }

    /// <summary>
    ///     Load the given mods
    /// </summary>
    /// <param name="modsToLoad">The mods to load</param>
    public static void LoadMods(IEnumerable<ModInfo> modsToLoad)
    {
        ModManager.LoadGameMods(modsToLoad);
    }

    /// <summary>
    ///     Create a server with the given parameters
    /// </summary>
    /// <param name="port">The port the server should run on</param>
    /// <param name="playerCount">The maximum player count</param>
    public static void CreateServer(ushort port, int playerCount = 16)
    {
        NetworkHandler.StartServer(port, playerCount);
    }

    /// <summary>
    ///     Connect to a server with the given address
    /// </summary>
    /// <param name="address">The address of the server</param>
    /// <param name="port">The port the server listens to</param>
    public static void ConnectToServer(string address, ushort port)
    {
        Address targetAddress = new() { Port = port };
        Logger.AssertAndThrow(targetAddress.SetHost(address), $"Failed to bind address {address}", "Engine");
        NetworkHandler.ConnectToServer(targetAddress);
    }

    /// <summary>
    ///     Set the type of the current game
    ///     Only usable if the game is not running
    /// </summary>
    /// <param name="gameType">The type to set to</param>
    public static void SetGameType(GameType gameType)
    {
        if (gameType is GameType.INVALID or > GameType.LOCAL)
        {
            Logger.WriteLog("Invalid game type to set", LogImportance.ERROR, "Engine");
            return;
        }

        if (GameType != GameType.INVALID)
        {
            Logger.WriteLog($"Cannot set {nameof(GameType)} while game is running", LogImportance.ERROR, "Engine");
            return;
        }

        GameType = gameType;
    }

    /// <summary>
    ///     The main game loop
    /// </summary>
    public static void GameLoop()
    {
        //If this is a client game (client or local) wait until the player is connected
        while (MathHelper.IsBitSet((int)GameType, (int)GameType.CLIENT) &&
               PlayerHandler.LocalPlayerGameId == Constants.InvalidId)
            NetworkHandler.Update();

        _tickTimeWatch.Restart();
        while (Stop == false)
        {
            SetDeltaTime();

            Window!.DoEvents();

            VulkanEngine.PrepareDraw();

            WorldHandler.UpdateWorlds(GameType.LOCAL);

            VulkanEngine.EndDraw();

            WorldHandler.SendEntityUpdates();

            NetworkHandler.Update();


            Logger.AppendLogToFile();
            Tick = (Tick + 1) % MaxTickCount;
        }

        CleanupGame();
    }

    /// <summary>
    ///     Cleanup all use resources of the previous running game
    /// </summary>
    public static void CleanupGame()
    {
        if (GameType == GameType.INVALID)
        {
            Logger.WriteLog("Tried to stop game, but game is not running", LogImportance.ERROR, "Engine");
            return;
        }

        GameType = GameType.INVALID;

        VulkanEngine.WaitForAll();

        NetworkHandler.StopClient();
        NetworkHandler.StopServer();

        WorldHandler.DestroyWorlds(GameType.LOCAL);

        _mainMenu = null;
        MainUiRenderer.SetMainUiContext(null);

        GameType = GameType.INVALID;

        PlayerHandler.ClearEvents();

        ModManager.UnloadMods(false);

        ShouldStop = false;
        Tick = 0;
    }

    /// <summary>
    ///     Update the delta time
    /// </summary>
    public static void SetDeltaTime()
    {
        _tickTimeWatch.Stop();
        DDeltaTime = _tickTimeWatch.Elapsed.TotalSeconds;
        DeltaTime = (float)_tickTimeWatch.Elapsed.TotalSeconds;
        _tickTimeWatch.Restart();
    }

    private static void CleanUp()
    {
        VulkanEngine.WaitForAll();
        MainUiRenderer.DestroyMainUiRendering();
        ModManager.UnloadMods(true);

        Library.Deinitialize();
        MemoryManager.Clear();
        VulkanEngine.Shutdown();
        AllocationHandler.CheckUnFreed();
    }
}