﻿using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.SystemGroups;

/// <summary>
///     Root system group for initialization
/// </summary>
[RegisterSystem("initialization_group")]
[RootSystemGroup]
public class InitializationSystemGroup : ASystemGroup
{
    /// <inheritdoc />
    public override Identification Identification => SystemIDs.InitializationGroup;
}