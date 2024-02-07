using System;
using System.Drawing;
using System.Numerics;
using MintyCore.FontStashSharp;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.UI;

namespace MintyCore.Myra.Platform
{
	internal class FontStashRenderer: IFontStashRenderer
	{
		private readonly IMyraRenderer _myraRenderer;

		public ITexture2DManager TextureManager => MyraEnvironment.Platform.Renderer.TextureManager;

		public FontStashRenderer(IMyraRenderer myraRenderer)
		{
			if (myraRenderer is null)
			{
				throw new ArgumentNullException(nameof(myraRenderer));
			}

			_myraRenderer = myraRenderer;
		}

		public void Draw(FontTextureWrapper texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth)
		{
			_myraRenderer.DrawSprite(texture, pos, src, color, rotation, scale, depth);
		}
	}
}
