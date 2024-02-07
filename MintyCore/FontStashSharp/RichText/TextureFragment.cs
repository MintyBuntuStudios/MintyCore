
using System.Numerics;
using System;
using System.Drawing;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp.RichText
{
	public class TextureFragment : IRenderable
	{
		public FontTextureWrapper Texture { get; private set; }
		public Rectangle Region { get; private set; }

		public Point Size => new((int)(Region.Width * Scale.X + 0.5f), (int)(Region.Height * Scale.Y + 0.5f));

		public Vector2 Scale = Vector2.One;

		public TextureFragment(FontTextureWrapper texture, Rectangle region)
		{
			if (texture is null)
			{
				throw new ArgumentNullException(nameof(texture));
			}

			Texture = texture;
			Region = region;
		}



		public void Draw(FSRenderContext context, Vector2 position, FSColor color)
		{
			context.DrawImage(Texture, Region, position, Scale, FSColor.White);
		}
	}
}