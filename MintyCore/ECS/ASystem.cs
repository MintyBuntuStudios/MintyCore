﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Abstract base class for all systems
/// </summary>
[PublicAPI]
public abstract class ASystem : IDisposable
{
    /// <summary>
    ///     Reference to the world the system is executed in
    /// </summary>
    public IWorld? World { get; internal set; }

    /// <summary/>
    public required IArchetypeManager ArchetypeManager { init; get; }

    /// <summary>
    ///     The <see cref="Identification" /> of this system
    /// </summary>
    public abstract Identification Identification { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    ///   Method to dispose of the system
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    ///     Method to setup the system at world creation
    /// </summary>
    public abstract void Setup(SystemManager systemManager);

    /// <summary>
    ///     Method to execute before <see cref="Execute" /> on the main thread
    /// </summary>
    public virtual void PreExecuteMainThread()
    {
    }

    /// <summary>
    ///     Method to execute after <see cref="Execute" /> on the main thread
    /// </summary>
    public virtual void PostExecuteMainThread()
    {
    }

    /// <summary>
    ///     Main system method. Executed on a job thread
    /// </summary>
    protected abstract void Execute();

    /// <summary>
    ///     Method to queue the system as a task. Only public to allow overrides
    /// </summary>
    public virtual Task QueueSystem(IEnumerable<Task> dependencies)
    {
        return Task.WhenAll(dependencies).ContinueWith(ExecuteWrapper);
    }

    private void ExecuteWrapper(Task task)
    {
        Execute();
    }
}