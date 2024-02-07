
using System.Drawing;
using System;
using System.Numerics;
using MintyCore.FontStashSharp.Interfaces;

namespace MintyCore.FontStashSharp
{
	public class DynamicSpriteFont : SpriteFontBase
	{
		private class GlyphStorage
		{
			public Int32Map<DynamicFontGlyph> Glyphs = new Int32Map<DynamicFontGlyph>();
			public FontSystemEffect Effect;
			public int EffectAmount;
		}

		private readonly Int32Map<GlyphStorage> _storages = new Int32Map<GlyphStorage>();
		private GlyphStorage? _lastStorage;
		private readonly Int32Map<int> Kernings = new Int32Map<int>();
		private FontMetrics[]? IndexedMetrics;

		public FontSystem FontSystem { get; private set; }

		internal DynamicSpriteFont(FontSystem system, float size, int lineHeight) : base(size, lineHeight)
		{
			FontSystem = system ?? throw new ArgumentNullException(nameof(system));
			RenderFontSizeMultiplicator = FontSystem.FontResolutionFactor;
		}

		internal Int32Map<DynamicFontGlyph> GetGlyphs(FontSystemEffect effect, int effectAmount)
		{
			if (_lastStorage is not null && _lastStorage.Effect == effect && _lastStorage.EffectAmount == effectAmount)
			{
				return _lastStorage.Glyphs;
			}

			var key = (int)effect << 16 | effectAmount;

			if (!_storages.TryGetValue(key, out var result))
			{
				result = new GlyphStorage
				{
					Effect = effect,
					EffectAmount = effectAmount
				};

				_storages[key] = result;
			}

			_lastStorage = result;

			return result.Glyphs;
		}

		private DynamicFontGlyph? GetGlyphWithoutBitmap(int codepoint, FontSystemEffect effect, int effectAmount)
		{
			if (effect == FontSystemEffect.None)
			{
			}
			else if (effectAmount == 0)
			{
				effect = FontSystemEffect.None;
			}

			var storage = GetGlyphs(effect, effectAmount);

			if (storage.TryGetValue(codepoint, out var glyph))
			{
				return glyph;
			}

			var g = FontSystem.GetCodepointIndex(codepoint, out var fontSourceIndex);
			if (g is null)
			{
				storage[codepoint] = null!;
				return null;
			}

			var fontSize = FontSize * FontSystem.FontResolutionFactor;
			var font = FontSystem.FontSources[fontSourceIndex];

			font.GetGlyphMetrics(g.Value, fontSize, out var advance, out var x0, out var y0, out var x1, out var y1);

			var gw = x1 - x0 + effectAmount * 2;
			var gh = y1 - y0 + effectAmount * 2;

			glyph = new DynamicFontGlyph
			{
				Codepoint = codepoint,
				Id = g.Value,
				FontSize = fontSize,
				FontSourceIndex = fontSourceIndex,
				RenderOffset = new Point(x0, y0),
				Size = new Point(gw, gh),
				XAdvance = advance,
				Effect = effect,
				EffectAmount = effectAmount
			};

			storage[codepoint] = glyph;

			return glyph;
		}


		private DynamicFontGlyph? GetGlyphInternal(ITexture2DManager? device, int codepoint, FontSystemEffect effect, int effectAmount)
		{
			var glyph = GetGlyphWithoutBitmap(codepoint, effect, effectAmount);
			if (glyph is null)
			{
				return null;
			}

			if (device is null || glyph.Texture is not null)
				return glyph;

			FontSystem.RenderGlyphOnAtlas(device, glyph);

			return glyph;
		}


		private DynamicFontGlyph? GetDynamicGlyph(ITexture2DManager? device, int codepoint, FontSystemEffect effect, int effectAmount)
		{
			var result = GetGlyphInternal(device, codepoint, effect, effectAmount);
			if (result is null && FontSystem.DefaultCharacter is not null)
			{
				result = GetGlyphInternal(device, FontSystem.DefaultCharacter.Value, effect, effectAmount);
			}

			return result;
		}


		protected internal override FontGlyph? GetGlyph(ITexture2DManager? device, int codepoint, FontSystemEffect effect, int effectAmount)
		{
			return GetDynamicGlyph(device, codepoint, effect, effectAmount);
		}

		private void GetMetrics(int fontSourceIndex, out FontMetrics result)
		{
			if (IndexedMetrics is null || IndexedMetrics.Length != FontSystem.FontSources.Count)
			{
				IndexedMetrics = new FontMetrics[FontSystem.FontSources.Count];
				for (var i = 0; i < IndexedMetrics.Length; ++i)
				{
					FontSystem.FontSources[i].GetMetricsForSize(FontSize * RenderFontSizeMultiplicator, out var ascent, out var descent, out var lineHeight);

					IndexedMetrics[i] = new FontMetrics(ascent, descent, lineHeight);
				}
			}

			result = IndexedMetrics[fontSourceIndex];
		}

		internal override void PreDraw(TextSource source, FontSystemEffect effect, int effectAmount,
			out int ascent, out int lineHeight)
		{
			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			while (true)
			{
				if (!source.GetNextCodepoint(out var codepoint))
				{
					break;
				}

				var glyph = GetDynamicGlyph(null, codepoint, effect, effectAmount);
				if (glyph is null)
				{
					continue;
				}

				GetMetrics(glyph.FontSourceIndex, out var metrics);
				ascent = metrics.Ascent;
				lineHeight = metrics.LineHeight;
				break;
			}

			source.Reset();
		}

		internal override Bounds InternalTextBounds(TextSource source, Vector2 position,
			float characterSpacing, float lineSpacing, FontSystemEffect effect, int effectAmount)
		{
			if (source.IsNull)
				return Bounds.Empty;

			var result = base.InternalTextBounds(source, position, characterSpacing, lineSpacing, effect, effectAmount);
			if (effect != FontSystemEffect.None)
			{
				result.X2 += effectAmount * 2;
			}

			return result;
		}

		private static int GetKerningsKey(int glyph1, int glyph2)
		{
			return ((glyph1 << 16) | (glyph1 >> 16)) ^ glyph2;
		}

		internal override float GetKerning(FontGlyph glyph, FontGlyph prevGlyph)
		{
			if (!FontSystem.UseKernings)
			{
				return 0.0f;
			}


			var dynamicGlyph = (DynamicFontGlyph)glyph;
			var dynamicPrevGlyph = (DynamicFontGlyph)prevGlyph;
			if (dynamicGlyph.FontSourceIndex != dynamicPrevGlyph.FontSourceIndex)
			{
				return 0.0f;
			}

			var key = GetKerningsKey(prevGlyph.Id, dynamicGlyph.Id);
			if (!Kernings.TryGetValue(key, out var result))
			{
				var fontSource = FontSystem.FontSources[dynamicGlyph.FontSourceIndex];
				result = fontSource.GetGlyphKernAdvance(prevGlyph.Id, dynamicGlyph.Id, dynamicGlyph.FontSize);

				Kernings[key] = result;
			}

			return result;
		}
	}
}