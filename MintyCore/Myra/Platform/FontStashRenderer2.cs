using System;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.UI;

namespace MintyCore.Myra.Platform
{
	internal class FontStashRenderer2: IFontStashRenderer2
	{
		private readonly IMyraRenderer _myraRenderer;

		public ITexture2DManager TextureManager => MyraEnvironment.Platform.Renderer.TextureManager;

		public FontStashRenderer2(IMyraRenderer myraRenderer)
		{
			if (myraRenderer is null)
			{
				throw new ArgumentNullException(nameof(myraRenderer));
			}

			_myraRenderer = myraRenderer;
		}

		public void DrawQuad(FontTextureWrapper texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
		{
			_myraRenderer.DrawQuad(texture, ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
		}
	}
}
