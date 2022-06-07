﻿using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     <see cref="IRegistry" /> for <see cref="RenderPass" />
/// </summary>
[Registry("render_pass")]
public class RenderPassRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderPass;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

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
        if (Engine.HeadlessModeActive)
            return;
        RenderPassHandler.RemoveRenderPass(objectId);
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
        RenderPassHandler.Clear();
        ClearRegistryEvents();
    }

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a new render pass
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId">Id of the mod registering</param>
    /// <param name="stringIdentifier">String identifier of the content to register</param>
    /// <param name="attachments">Attachments used in the render pass</param>
    /// <param name="subPasses">Sub passes used in the render  pass</param>
    /// <param name="dependencies">Subpass dependencies used in the render pass</param>
    /// <param name="flags">Optional flags for render pass creation</param>
    /// <returns><see cref="Identification" /> of the created render pass</returns>
    [Obsolete]
    public static Identification RegisterRenderPass(ushort modId, string stringIdentifier,
        Span<AttachmentDescription> attachments, SubpassDescriptionInfo[] subPasses,
        Span<SubpassDependency> dependencies, RenderPassCreateFlags flags = 0)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.RenderPass, stringIdentifier);
        if (Engine.HeadlessModeActive)
            return id;
        RenderPassHandler.AddRenderPass(id, attachments, subPasses, dependencies, flags);
        return id;
    }

    /// <summary>
    /// Register a new render pass
    /// Used by the SourceGenerator to create <see cref="RegisterRenderPassAttribute"/>
    /// </summary>
    /// <param name="id"> <see cref="Identification"/> of the render pass</param>
    /// <param name="info"> <see cref="RenderPassInfo"/> of the render pass</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterRenderPass(Identification id, RenderPassInfo info)
    {
        if (Engine.HeadlessModeActive)
            return;

        RenderPassHandler.AddRenderPass(id, info.Attachments, info.SubPasses, info.Dependencies, info.Flags);
    }

    /// <summary>
    /// Register an existing render pass
    /// Used by the SourceGenerator to create <see cref="RegisterRenderPassAttribute"/>
    /// </summary>
    /// <param name="id"> <see cref="Identification"/> of the render pass</param>
    /// <param name="renderPass">RenderPass to register</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterExistingRenderPass(Identification id, RenderPass renderPass)
    {
        if(Engine.HeadlessModeActive)
            return;
        
        RenderPassHandler.AddRenderPass(id, renderPass);
    }
}

/// <summary>
///    Render pass info
/// </summary>
public struct RenderPassInfo
{
    /// <summary>
    /// Attachments used in the render pass
    /// </summary>
    public readonly AttachmentDescription[] Attachments;

    /// <summary>
    /// Sub passes used in the render  pass
    /// </summary>
    public readonly SubpassDescriptionInfo[] SubPasses;

    /// <summary>
    /// Subpass dependencies used in the render pass
    /// </summary>
    public readonly SubpassDependency[] Dependencies;

    /// <summary>
    ///   Optional flags for render pass creation
    /// </summary>
    public readonly RenderPassCreateFlags Flags;

    /// <summary>
    ///   Create a new render pass info
    /// </summary>
    /// <param name="attachments"> Attachments used in the render pass</param>
    /// <param name="subPasses"> Sub passes used in the render  pass</param>
    /// <param name="dependencies"> Subpass dependencies used in the render pass</param>
    /// <param name="flags"> Optional flags for render pass creation</param>
    public RenderPassInfo(AttachmentDescription[] attachments, SubpassDescriptionInfo[] subPasses,
        SubpassDependency[] dependencies, RenderPassCreateFlags flags)
    {
        Attachments = attachments;
        SubPasses = subPasses;
        Dependencies = dependencies;
        Flags = flags;
    }
}

public struct SubpassDescriptionInfo
{
    public SubpassDescriptionFlags Flags;
    public PipelineBindPoint PipelineBindPoint;
    public AttachmentReference[] InputAttachments;
    public AttachmentReference[] ColorAttachments;
    public AttachmentReference ResolveAttachment;
    public bool HasResolveAttachment;
    public AttachmentReference DepthStencilAttachment;
    public bool HasDepthStencilAttachment;
    public uint[] PreserveAttachments;
}