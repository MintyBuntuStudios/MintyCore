﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Graphics.Utils;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers.Implementations;

/// <summary>
///     Handler class for render passes
/// </summary>
[Singleton<IRenderPassManager>(SingletonContextFlags.NoHeadless)]
internal unsafe class RenderPassManager : IRenderPassManager
{
    private readonly Dictionary<Identification, RenderPass> _renderPasses = new();
    private readonly HashSet<Identification> _unmanagedRenderPasses = new();

    public IVulkanEngine VulkanEngine { set; private get; } = null!;

    /// <summary>
    ///     Get a Render pass
    /// </summary>
    /// <param name="renderPassId"></param>
    /// <returns></returns>
    public RenderPass GetRenderPass(Identification renderPassId)
    {
        return _renderPasses[renderPassId];
    }

    public void AddRenderPass(Identification id, RenderPass renderPass)
    {
        _renderPasses.Add(id, renderPass);
        _unmanagedRenderPasses.Add(id);
    }

    public void AddRenderPass(Identification id, Span<AttachmentDescription> attachments,
        SubpassDescriptionInfo[] subPasses, Span<SubpassDependency> dependencies,
        RenderPassCreateFlags flags = 0)
    {
        Stack<GCHandle> arrayHandles = new();
        Span<AttachmentReference> depthStencilAttachments = stackalloc AttachmentReference[subPasses.Length];
        Span<AttachmentReference> resolveAttachments = stackalloc AttachmentReference[subPasses.Length];

        Span<SubpassDescription> subpassDescriptions = stackalloc SubpassDescription[subPasses.Length];
        for (var i = 0; i < subPasses.Length; i++)
        {
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].ColorAttachments, GCHandleType.Pinned));
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].InputAttachments, GCHandleType.Pinned));
            arrayHandles.Push(GCHandle.Alloc(subPasses[i].PreserveAttachments, GCHandleType.Pinned));

            depthStencilAttachments[i] = subPasses[i].DepthStencilAttachment;
            resolveAttachments[i] = subPasses[i].ResolveAttachment;

            subpassDescriptions[i] = new SubpassDescription
            {
                Flags = subPasses[i].Flags,
                PipelineBindPoint = subPasses[i].PipelineBindPoint,
                PDepthStencilAttachment = subPasses[i].HasDepthStencilAttachment
                    ? (AttachmentReference*)Unsafe.AsPointer(ref depthStencilAttachments[i])
                    : null,
                PColorAttachments = subPasses[i].ColorAttachments.Length > 0
                    ? (AttachmentReference*)Unsafe.AsPointer(ref subPasses[i].ColorAttachments[0])
                    : null,
                ColorAttachmentCount = (uint)subPasses[i].ColorAttachments.Length,
                PInputAttachments = subPasses[i].InputAttachments.Length > 0
                    ? (AttachmentReference*)Unsafe.AsPointer(ref subPasses[i].InputAttachments[0])
                    : null,
                InputAttachmentCount = (uint)subPasses[i].InputAttachments.Length,
                PResolveAttachments = subPasses[i].HasResolveAttachment
                    ? (AttachmentReference*)Unsafe.AsPointer(ref resolveAttachments[i])
                    : null,
                PPreserveAttachments = subPasses[i].PreserveAttachments.Length > 0
                    ? (uint*)Unsafe.AsPointer(ref subPasses[i].PreserveAttachments[0])
                    : null,
                PreserveAttachmentCount = (uint)subPasses[i].PreserveAttachments.Length
            };
        }

        fixed (AttachmentDescription* attachmentPtr = &attachments[0])
        fixed (SubpassDependency* dependencyPtr = &dependencies[0])
        {
            RenderPassCreateInfo createInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                PNext = null,
                Flags = flags,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentPtr,
                DependencyCount = (uint)dependencies.Length,
                PDependencies = dependencyPtr,
                SubpassCount = (uint)subPasses.Length,
                PSubpasses = (SubpassDescription*)Unsafe.AsPointer(ref subpassDescriptions[0])
            };
            VulkanUtils.Assert(VulkanEngine.Vk.CreateRenderPass(VulkanEngine.Device, createInfo,
                null, out var renderPass));

            _renderPasses.Add(id, renderPass);
        }

        while (arrayHandles.TryPop(out var handle)) handle.Free();
    }


    public void Clear()
    {
        foreach (var (id, renderPass) in _renderPasses)
            if (!_unmanagedRenderPasses.Remove(id))
                VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, null);

        _renderPasses.Clear();
    }

    public void RemoveRenderPass(Identification objectId)
    {
        if (_renderPasses.Remove(objectId, out var renderPass)
            && !_unmanagedRenderPasses.Remove(objectId))
            VulkanEngine.Vk.DestroyRenderPass(VulkanEngine.Device, renderPass, null);
    }
}