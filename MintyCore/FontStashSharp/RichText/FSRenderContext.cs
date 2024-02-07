using System.Drawing;
using Matrix = System.Numerics.Matrix3x2;
using System;
using System.Numerics;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp.RichText
{
	public class FSRenderContext
	{
		private IFontStashRenderer _renderer;
		private IFontStashRenderer2 _renderer2;
		private Matrix _transformation;
		private Vector2 _scale;
		private float _rotation;
		private float _layerDepth;

		public void SetRenderer(IFontStashRenderer renderer)
		{
			if (renderer is null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

			_renderer = renderer;
			_renderer2 = null;
		}

		public void SetRenderer(IFontStashRenderer2 renderer)
		{
			if (renderer is null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}
			_renderer = null;
			_renderer2 = renderer;
		}

		public void Prepare(Vector2 position, float rotation, Vector2 origin, Vector2 scale, float layerDepth)
		{
			_scale = scale;
			_rotation = rotation;
			_layerDepth = layerDepth;
			Utility.BuildTransform(position, _rotation, origin, _scale, out _transformation);
		}

		public void DrawText(string text, SpriteFontBase font, Vector2 pos, FSColor color, 
			TextStyle textStyle, FontSystemEffect effect, int effectAmount)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			pos = pos.Transform(ref _transformation);
			if (_renderer is not null)
			{
				font.DrawText(_renderer, text, pos, color, _rotation, default(Vector2), _scale, _layerDepth, 
					textStyle: textStyle, effect: effect, effectAmount: effectAmount);
			}
			else
			{
				font.DrawText(_renderer2, text, pos, color, _rotation, default(Vector2), _scale, _layerDepth,
					textStyle: textStyle, effect: effect, effectAmount: effectAmount);
			}
		}

		public void DrawImage(FontTextureWrapper texture, Rectangle sourceRegion, Vector2 position, Vector2 scale, FSColor color)
		{
			if (_renderer is not null)
			{
				position = position.Transform(ref _transformation);
				_renderer.Draw(texture, position, sourceRegion, color, _rotation, _scale, _layerDepth);
			}
			else
			{
				var topLeft = new VertexPositionColorTexture();
				var topRight = new VertexPositionColorTexture();
				var bottomLeft = new VertexPositionColorTexture();
				var bottomRight = new VertexPositionColorTexture();

				var size = new Vector2(sourceRegion.Width, sourceRegion.Height) * _scale * scale;
				_renderer2.DrawQuad(texture, color, position, ref _transformation,
					_layerDepth, size, sourceRegion,
					ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}
		}
	}

}
