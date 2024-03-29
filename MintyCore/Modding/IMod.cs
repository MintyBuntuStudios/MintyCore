﻿using System;

namespace MintyCore.Modding;

/// <summary>
///     Base interface for all mod implementations
/// </summary>
public interface IMod : IDisposable
{
    /// <summary>
    ///     PreLoad method
    /// </summary>
    void PreLoad();

    /// <summary>
    ///     Main load method
    /// </summary>
    void Load();

    /// <summary>
    ///     Post load method
    /// </summary>
    void PostLoad();

    /// <summary>
    ///     Method to unload the mod. Free all unmanaged resources etc
    /// </summary>
    void Unload();
}