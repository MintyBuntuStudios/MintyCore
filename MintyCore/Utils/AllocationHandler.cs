﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepuUtilities.Memory;

namespace MintyCore.Utils;

/// <summary>
///     AllocationHandler to manage and track memory allocations
/// </summary>
public static class AllocationHandler
{
    private static readonly Dictionary<IntPtr, StackTrace> _allocations = new();
    public static BufferPool BepuBufferPool { get; } = new();

    private static void AddAllocationToTrack(IntPtr allocation)
    {
#if DEBUG
        _allocations.Add(allocation, new StackTrace(2));
#else
            _allocations.Add( allocation, null );
#endif
    }

    private static bool RemoveAllocationToTrack(IntPtr allocation)
    {
        return _allocations.Remove(allocation);
    }

    internal static void CheckUnFreed()
    {
        if (_allocations.Count == 0) return;

        Logger.WriteLog($"{_allocations.Count} allocations were not freed.", LogImportance.WARNING, "Memory");
#if DEBUG
        Logger.WriteLog("Allocated at:", LogImportance.WARNING, "Memory");
        foreach (var entry in _allocations)
            Logger.WriteLog(entry.Value.ToString(), LogImportance.WARNING, "Memory");
#endif
    }

    /// <summary>
    ///     Malloc memory block with the given size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static IntPtr Malloc(int size)
    {
        var allocation = Marshal.AllocHGlobal(size);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <summary>
    ///     Malloc memory block with the given size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static IntPtr Malloc(IntPtr size)
    {
        var allocation = Marshal.AllocHGlobal(size);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <summary>
    ///     Malloc a memory block for <paramref name="count" /> <typeparamref name="T" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <returns></returns>
    public static unsafe IntPtr Malloc<T>(int count = 1) where T : unmanaged
    {
        var allocation = Marshal.AllocHGlobal(sizeof(T) * count);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <summary>
    ///     Free an allocation
    /// </summary>
    /// <param name="allocation"></param>
    public static void Free(IntPtr allocation)
    {
        if (!RemoveAllocationToTrack(allocation))
            throw new Exception($"Tried to free {allocation}, but the allocation wasn't tracked internally");

        Marshal.FreeHGlobal(allocation);
    }

    /// <summary>
    ///     Check if an allocation is still valid (not freed)
    /// </summary>
    /// <param name="allocation"></param>
    /// <returns></returns>
    public static bool AllocationValid(IntPtr allocation)
    {
        return _allocations.ContainsKey(allocation);
    }
}