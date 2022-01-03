﻿namespace MintyCore.Identifications
{
	/// <summary>
	///     <see langword="static" /> partial class which contains all <see cref="Registries.IRegistry" /> ids
	/// </summary>
	public static class RegistryIDs
    {
	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.ComponentRegistry" />
	    /// </summary>
	    public static ushort Component { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.SystemRegistry" />
	    /// </summary>
	    public static ushort System { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.ArchetypeRegistry" />
	    /// </summary>
	    public static ushort Archetype { get; internal set; }


	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.ShaderRegistry" />
	    /// </summary>
	    public static ushort Shader { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.PipelineRegistry" />
	    /// </summary>
	    public static ushort Pipeline { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.TextureRegistry" />
	    /// </summary>
	    public static ushort Texture { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.MaterialRegistry" />
	    /// </summary>
	    public static ushort Material { get; internal set; }


	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.MeshRegistry" />
	    /// </summary>
	    public static ushort Mesh { get; internal set; }

	    /// <summary>
	    ///     <see cref="ushort" /> id of <see cref="Registries.MessageRegistry" />
	    /// </summary>
	    public static ushort Message { get; set; }

	    public static ushort RenderPass { get; set; }
	    public static ushort DescriptorSet { get; set; }
	    public static ushort IndexRenderData { get; set; }
    }
}