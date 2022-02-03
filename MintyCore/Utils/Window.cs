﻿using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace MintyCore.Utils;

/// <summary>
///     Class to manage <see cref="Silk.NET.Windowing.IWindow" />
/// </summary>
public class Window
{
    /// <summary>
    /// Interface representing the connected keyboard
    /// </summary>
    public IKeyboard Keyboard { get; }

    /// <summary>
    /// Interface representing the connected mouse
    /// </summary>
    public IMouse Mouse { get; }

    /// <summary>
    /// Interface representing the window
    /// </summary>
    public IWindow WindowInstance { get; }

    /// <summary>
    ///     Create a new window
    /// </summary>
    public Window()
    {
        var options =
            new WindowOptions(ViewOptions.DefaultVulkan); //(100, 100, 960, 540, WindowState.Normal, "Techardry");
        options.Size = new Vector2D<int>(960, 540);
        options.Title = "Techardry";

        WindowInstance = Silk.NET.Windowing.Window.Create(options);

        WindowInstance.Initialize();

        if (WindowInstance.VkSurface is null) throw new MintyCoreException("Vulkan surface was not created");

        var inputContext = WindowInstance.CreateInput();
        Mouse = inputContext.Mice[0];
        Keyboard = inputContext.Keyboards[0];
        InputHandler.Setup(Mouse, Keyboard);
    }

    /// <summary>
    /// The size of the window
    /// </summary>
    public Vector2D<int> Size => WindowInstance.Size;

    /// <summary>
    /// The framebuffer size of the window
    /// </summary>
    public Vector2D<int> FramebufferSize => WindowInstance.FramebufferSize;

    /// <summary>
    /// Get or set the mouse locked state
    /// </summary>
    public bool MouseLocked
    {
        get => Mouse.Cursor.CursorMode == CursorMode.Hidden;
        set => Mouse.Cursor.CursorMode = value ? CursorMode.Hidden : CursorMode.Normal;
    }

    /// <summary>
    ///     Check if the window exists
    /// </summary>
    public bool Exists => !WindowInstance.IsClosing;

    internal void DoEvents()
    {
        WindowInstance.DoEvents();
        InputHandler.Update();
        if (Mouse.Cursor.CursorMode == CursorMode.Hidden)
        {
            Mouse.Position = new Vector2(WindowInstance.Size.X / 2f, WindowInstance.Size.Y / 2f);
            InputHandler.LastMousePos = Vector2.Zero;
        }
    }
}