﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    public static unsafe class DescriptorSetHandler
    {
        private const uint POOL_CAPACITY = 1000;
        private static Dictionary<DescriptorType, uint> _poolSize = new();
        private static Dictionary<Identification, DescriptorSetLayout> _descriptorSetLayouts = new();
        private static Dictionary<DescriptorPool, HashSet<DescriptorSet>> _allocatedDescriptorSets = new();

        public static void FreeDescriptorSet(DescriptorSet set)
        {
            foreach (var (pool, sets) in _allocatedDescriptorSets)
            {
                if (!sets.Contains(set)) continue;
                VulkanUtils.Assert(VulkanEngine.Vk.FreeDescriptorSets(VulkanEngine.Device, pool, 1, set));
                break;
            }
        }
        
        public static DescriptorSet AllocateDescriptorSet(Identification descriptorSetLayoutId)
        {
            DescriptorPool pool = default;
            foreach (var (descPool, descriptors) in _allocatedDescriptorSets)
            {
                if (descriptors.Count >= POOL_CAPACITY) continue;
                pool = descPool;
                break;
            }

            if (pool.Handle == default)
            {
                pool = CreateDescriptorPool();
            }

            var layout = _descriptorSetLayouts[descriptorSetLayoutId];
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                PNext = null,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout
            };
            VulkanUtils.Assert(VulkanEngine.Vk.AllocateDescriptorSets(VulkanEngine.Device, allocateInfo, out var set));
            _allocatedDescriptorSets[pool].Add(set);
            return set;
        }

        internal static void AddDescriptorSetLayout(Identification layoutId, ReadOnlySpan<DescriptorSetLayoutBinding> bindings)
        {
            DescriptorSetLayout layout;
            fixed (DescriptorSetLayoutBinding* pBinding = &bindings.GetPinnableReference())
            {
                DescriptorSetLayoutCreateInfo createInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    Flags = 0,
                    PNext = null,
                    BindingCount = (uint)bindings.Length,
                    PBindings = pBinding
                };

                VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorSetLayout(VulkanEngine.Device, createInfo,
                    VulkanEngine.AllocationCallback, out layout));
            }

            //Iterate over all descriptors, to specify the minimum pool size
            foreach (var binding in bindings)
            {
                if (!_poolSize.ContainsKey(binding.DescriptorType)) _poolSize.Add(binding.DescriptorType, 0);
                if (_poolSize[binding.DescriptorType] < binding.DescriptorCount)
                {
                    _poolSize[binding.DescriptorType] = binding.DescriptorCount;
                }
            }

            _descriptorSetLayouts.Add(layoutId, layout);
        }

        private static DescriptorPool CreateDescriptorPool()
        {
            int poolSizeCount = _poolSize.Count;
            Span<DescriptorPoolSize> poolSizes = stackalloc DescriptorPoolSize[poolSizeCount];
            int iteration = 0;
            foreach (var (descriptorType, count) in _poolSize)
            {
                poolSizes[iteration] = new DescriptorPoolSize()
                {
                    Type = descriptorType, DescriptorCount = POOL_CAPACITY
                };
                iteration++;
            }

            DescriptorPoolCreateInfo createInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PNext = null,
                PPoolSizes = (DescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes.GetPinnableReference()),
                MaxSets = POOL_CAPACITY,
                Flags = DescriptorPoolCreateFlags.DescriptorPoolCreateFreeDescriptorSetBit,
                PoolSizeCount = (uint)poolSizeCount
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateDescriptorPool(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback, out var pool));
            _allocatedDescriptorSets.Add(pool, new HashSet<DescriptorSet>());
            return pool;
        }

        internal static void Clear()
        {
            foreach (var pool in _allocatedDescriptorSets.Keys)
            {
                VulkanEngine.Vk.DestroyDescriptorPool(VulkanEngine.Device, pool, VulkanEngine.AllocationCallback);
            }

            foreach (var layout in _descriptorSetLayouts.Values)
            {
                VulkanEngine.Vk.DestroyDescriptorSetLayout(VulkanEngine.Device, layout, VulkanEngine.AllocationCallback);
            }
            
            _poolSize.Clear();
            _allocatedDescriptorSets.Clear();
            _descriptorSetLayouts.Clear();
        }

        public static DescriptorSetLayout GetDescriptorSetLayout(Identification id)
        {
            return _descriptorSetLayouts[id];
        }
    }
}