﻿using System.Collections.Generic;
using System.IO;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    /// <summary>
    ///     The handler for all shader specific stuff. Get populated by the <see cref="ShaderRegistry" />
    /// </summary>
    public static class ShaderHandler
    {
        private static readonly Dictionary<Identification, Shader> _shaders = new();

        internal static void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint)
        {
            var shaderFile = RegistryManager.GetResourceFileName(shaderId);
            var shaderCode = File.Exists(shaderFile)
                ? File.ReadAllBytes(shaderFile)
                : throw new IOException("Shader file to load does not exists");
            

            _shaders.Add(shaderId, Shader.CreateShader(shaderCode, shaderEntryPoint, shaderStage));
        }

        /// <summary>
        ///     Get a <see cref="Shader" />
        /// </summary>
        /// <param name="shaderId"><see cref="Identification" /> of the <see cref="Shader" /></param>
        /// <returns></returns>
        public static Shader GetShader(Identification shaderId)
        {
            return _shaders[shaderId];
        }

        internal static void Clear()
        {
            foreach (var shader in _shaders) shader.Value.Dispose();
            _shaders.Clear();
        }

    }
}