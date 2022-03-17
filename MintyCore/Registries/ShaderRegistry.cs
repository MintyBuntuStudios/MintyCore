﻿using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Shader" />
/// </summary>
public class ShaderRegistry : IRegistry
{
    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        ShaderHandler.RemoveShader(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Shaders", LogImportance.INFO, "Registry");
        ClearRegistryEvents();
        ShaderHandler.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Shader;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a <see cref="Shader" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Shader" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Shader" /></param>
    /// <param name="shaderName">The file name of the <see cref="Shader" /></param>
    /// <param name="shaderStage">The <see cref="ShaderStageFlags" /> of the <see cref="Shader" /></param>
    /// <param name="shaderEntryPoint">The entry point (main method) of the <see cref="Shader" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Shader" /></returns>
    public static Identification RegisterShader(ushort modId, string stringIdentifier, string shaderName,
        ShaderStageFlags shaderStage, string shaderEntryPoint = "main")
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var shaderId =
            RegistryManager.RegisterObjectId(modId, RegistryIDs.Shader, stringIdentifier, shaderName);

        ShaderHandler.AddShader(shaderId, shaderStage, shaderEntryPoint);
        return shaderId;
    }
}