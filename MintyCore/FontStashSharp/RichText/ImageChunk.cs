
using System.Drawing;
using System;
using System.Numerics;

namespace MintyCore.FontStashSharp.RichText
{
	public class ImageChunk : BaseChunk
	{
		private readonly IRenderable _renderable;

		public override Point Size => _renderable.Size;

		public ImageChunk(IRenderable renderable)
		{
			if (renderable is null)
			{
				throw new ArgumentNullException(nameof(renderable));
			}

			_renderable = renderable;
		}

		public override void Draw(FSRenderContext context, Vector2 position, FSColor color)
		{
			_renderable.Draw(context, position, color);
		}
	}
}
