using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MintyCore.Utils;

namespace MintyCore.Network.UnconnectedMessages;

public record ConnectionRequestData(List<(string modId, Version version)> loadedRootMods, string PlayerName, ulong PlayerId)
{
    public void Serialize(DataWriter writer)
    {
        writer.Put(loadedRootMods.Count);

        foreach (var (modId, version) in loadedRootMods)
        {
            writer.Put(modId);
            writer.Put(version);
        }
        
        writer.Put(PlayerName);
        writer.Put(PlayerId);
    }

    public static bool TryDeserialize(DataReader reader, [MaybeNullWhen(false)] out ConnectionRequestData request)
    {
        request = null;
        if (!reader.TryGetInt(out var count))
            return false;

        var loadedRootMods = new List<(string modId, Version version)>(count);
        for (var i = 0; i < count; i++)
        {
            if (!reader.TryGetString(out var modId) || !reader.TryGetVersion(out var version))
                return false;

            loadedRootMods.Add((modId, version));
        }
        
        if (!reader.TryGetString(out var playerName) || !reader.TryGetULong(out var playerId))
            return false;
        
        request = new ConnectionRequestData(loadedRootMods, playerName, playerId);
        return true;
    }
}