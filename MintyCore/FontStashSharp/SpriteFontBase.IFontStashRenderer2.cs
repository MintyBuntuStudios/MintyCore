
using System.Numerics;
using Matrix = System.Numerics.Matrix3x2;
using System;
using System.Drawing;
using System.Text;
using MintyCore.FontStashSharp.Interfaces;

namespace MintyCore.FontStashSharp
{
	partial class SpriteFontBase
	{
		private void RenderStyle(IFontStashRenderer2 renderer, TextStyle textStyle, Vector2 pos,
			int lineHeight, int ascent, FSColor color, ref Matrix transformation, float layerDepth,
			ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
			ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
		{
			if (textStyle == TextStyle.None || pos.X == 0)
			{
				return;
			}
			
			var white = GetWhite(renderer.TextureManager);

			var start = Vector2.Zero;
			if (textStyle == TextStyle.Strikethrough)
			{
				start.Y = pos.Y - ascent - FontSystemDefaults.TextStyleLineHeight / 2 + lineHeight / 2;
			}
			else
			{
				start.Y = pos.Y + 1;
			}

			var size = new Vector2(pos.X, FontSystemDefaults.TextStyleLineHeight);
			renderer.DrawQuad(white, color, start, ref transformation,
				layerDepth, size, new Rectangle(0, 0, 1, 1),
				ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
		}

		private float DrawText(IFontStashRenderer2 renderer, TextColorSource source,
			Vector2 position, float rotation, Vector2 origin, Vector2? sourceScale,
			float layerDepth, float characterSpacing, float lineSpacing,
			TextStyle textStyle, FontSystemEffect effect, int effectAmount)
		{
			if (renderer is null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}


			if (renderer.TextureManager is null)
			{
				throw new ArgumentNullException("renderer.TextureManager can't be null.");
			}

			if (source.IsNull) return 0.0f;

			var scale = sourceScale ?? Utility.DefaultScale;
			Prepare(position, rotation, origin, ref scale, out var transformation);

			PreDraw(source.TextSource, effect, effectAmount, out var ascent, out var lineHeight);

			var pos = new Vector2(0, ascent);

			FontGlyph? prevGlyph = null;
			var topLeft = new VertexPositionColorTexture();
			var topRight = new VertexPositionColorTexture();
			var bottomLeft = new VertexPositionColorTexture();
			var bottomRight = new VertexPositionColorTexture();
			FSColor? firstColor = null;

			while (true)
			{
				if (!source.GetNextCodepoint(out var codepoint))
					break;

				if (codepoint == '\n')
				{
					if (textStyle != TextStyle.None && firstColor is not null)
					{
						RenderStyle(renderer, textStyle, pos,
							lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
							ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
					}

					pos.X = 0.0f;
					pos.Y += lineHeight;
					prevGlyph = null;
					continue;
				}


				var glyph = GetGlyph(renderer.TextureManager, codepoint, effect, effectAmount);
				if (glyph is null)
				{
					continue;
				}

				if (prevGlyph is not null)
				{
					pos.X += characterSpacing;
					pos.X += GetKerning(glyph, prevGlyph);
				}

				if (!glyph.IsEmpty)
				{
					var color = source.GetNextColor();
					firstColor = color;

					var baseOffset = new Vector2(glyph.RenderOffset.X, glyph.RenderOffset.Y) + pos;

					var size = new Vector2(glyph.Size.X, glyph.Size.Y);
					renderer.DrawQuad(glyph.Texture!, color, baseOffset, ref transformation,
						layerDepth, size, glyph.TextureRectangle,
						ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
				}

				pos.X += glyph.XAdvance;
				prevGlyph = glyph;
			}

			if (textStyle != TextStyle.None && firstColor is not null)
			{
				RenderStyle(renderer, textStyle, pos,
					lineHeight, ascent, firstColor.Value, ref transformation, layerDepth,
					ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}

			return position.X + position.X;
		}

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, string text, Vector2 position, FSColor color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, string text, Vector2 position, FSColor[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringSegment text, Vector2 position, FSColor color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringSegment text, Vector2 position, FSColor[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="color">A color mask</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringBuilder text, Vector2 position, FSColor color,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, color), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);

		/// <summary>
		/// Draws a text
		/// </summary>
		/// <param name="renderer">A renderer</param>
		/// <param name="text">The text which will be drawn</param>
		/// <param name="position">The drawing location on screen</param>
		/// <param name="colors">Colors of glyphs</param>
		/// <param name="rotation">A rotation of this text in radians</param>
		/// <param name="origin">Center of the rotation</param>
		/// <param name="scale">A scaling of this text. Null means the scaling is (1, 1)</param>
		/// <param name="layerDepth">A depth of the layer of this string</param>
		/// <param name="characterSpacing">A character spacing</param>
		/// <param name="lineSpacing">A line spacing</param>
		public float DrawText(IFontStashRenderer2 renderer, StringBuilder text, Vector2 position, FSColor[] colors,
			float rotation = 0, Vector2 origin = default(Vector2), Vector2? scale = null, 
			float layerDepth = 0.0f, float characterSpacing = 0.0f, float lineSpacing = 0.0f,
			TextStyle textStyle = TextStyle.None, FontSystemEffect effect = FontSystemEffect.None, int effectAmount = 0) =>
				DrawText(renderer, new TextColorSource(text, colors), position, rotation, origin, scale, layerDepth,
					characterSpacing, lineSpacing, textStyle, effect, effectAmount);
	}
}
