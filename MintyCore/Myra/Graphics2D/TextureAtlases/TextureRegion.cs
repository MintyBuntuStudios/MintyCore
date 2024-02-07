using System;
using System.Drawing;
using MintyCore.FontStashSharp;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.Myra.Graphics2D.TextureAtlases
{
	public class TextureRegion: IImage
	{
		private readonly Rectangle _bounds;


		private readonly FontTextureWrapper _texture;
		public FontTextureWrapper Texture
		{
			get { return _texture; }
		}

		public Rectangle Bounds
		{
			get { return _bounds; }
		}

		public Point Size
		{
			get
			{
				return new Point(Bounds.Width, Bounds.Height);
			}
		}



		public TextureRegion(FontTextureWrapper texture, Rectangle bounds)
		{
			if (texture is null)
			{
				throw new ArgumentNullException("texture");
			}

			_texture = texture;
			_bounds = bounds;
		}

		public TextureRegion(TextureRegion region, Rectangle bounds)
		{
			if (region is null)
			{
				throw new ArgumentNullException("region");
			}

			_texture = region.Texture;
			bounds.Offset(region.Bounds.Location);
			_bounds = bounds;
		}

		public virtual void Draw(RenderContext context, Rectangle dest, FSColor color)
		{
			context.Draw(Texture, dest, Bounds, color);
		}
	}
}