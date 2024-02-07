
using System.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp.Interfaces;
using FontStashSharp.Rasterizers.StbTrueTypeSharp;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp
{
	public class FontSystem : IDisposable
	{
		public const int GlyphPad = 2;

		private readonly List<IFontSource> _fontSources = new List<IFontSource>();
		private readonly Int32Map<DynamicSpriteFont> _fonts = new Int32Map<DynamicSpriteFont>();
		private readonly FontSystemSettings _settings;

		private FontAtlas? _currentAtlas;

		public int TextureWidth => _settings.TextureWidth;
		public int TextureHeight => _settings.TextureHeight;

		public bool PremultiplyAlpha => _settings.PremultiplyAlpha;
		public GlyphRenderer GlyphRenderer => _settings.GlyphRenderer;

		public float FontResolutionFactor => _settings.FontResolutionFactor;

		public int KernelWidth => _settings.KernelWidth;
		public int KernelHeight => _settings.KernelHeight;

		public FontTextureWrapper? ExistingTexture => _settings.ExistingTexture;
		public Rectangle ExistingTextureUsedSpace => _settings.ExistingTextureUsedSpace;

		public bool UseKernings { get; set; } = true;
		public int? DefaultCharacter { get; set; } = ' ';

		internal List<IFontSource> FontSources => _fontSources;

		public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();
		public FontAtlas? CurrentAtlas => _currentAtlas;

		public event EventHandler? CurrentAtlasFull;
		private readonly IFontLoader _fontLoader;

		public FontSystem(FontSystemSettings settings)
		{
			if (settings is null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			_settings = settings.Clone();

			if (_settings.FontLoader is null)
			{
				var loaderSettings = new StbTrueTypeSharpSettings
				{
					KernelWidth = _settings.KernelWidth,
					KernelHeight = _settings.KernelHeight,
					UseOldRasterizer = _settings.StbTrueTypeUseOldRasterizer
				};
				_fontLoader = new StbTrueTypeSharpLoader(loaderSettings);
			}
			else
			{
				_fontLoader = _settings.FontLoader;
			}

			UseKernings = FontSystemDefaults.UseKernings;
			DefaultCharacter = FontSystemDefaults.DefaultCharacter;
		}

		public FontSystem() : this(new FontSystemSettings())
		{
		}

		public void Dispose()
		{
			if (_fontSources is not null)
			{
				foreach (var font in _fontSources)
					font.Dispose();
				_fontSources.Clear();
			}

			Atlases?.Clear();
			SetFontAtlas(null);
			_fonts.Clear();
		}

		public void AddFont(byte[] data)
		{
			var fontSource = _fontLoader.Load(data);
			_fontSources.Add(fontSource);
		}

		public void AddFont(Stream stream)
		{
			AddFont(stream.ToByteArray());
		}

		public DynamicSpriteFont GetFont(float fontSize)
		{
			var intSize = fontSize.FloatAsInt();
			if (_fonts.TryGetValue(intSize, out var result))
			{
				return result;
			}

			if (_fontSources.Count == 0)
			{
				throw new Exception("Could not create a font without a single font source. Use AddFont to add at least one font source.");
			}

			var fontSource = _fontSources[0];

			int ascent, descent;
			fontSource.GetMetricsForSize(fontSize, out ascent, out descent, out var lineHeight);

			result = new DynamicSpriteFont(this, fontSize, lineHeight);
			_fonts[intSize] = result;
			return result;
		}

		public void SetFontAtlas(FontAtlas? fontAtlas)
		{
			if (fontAtlas is not null && !Atlases.Contains(fontAtlas))
			{
				Atlases.Add(fontAtlas);
			}
			_currentAtlas = fontAtlas;
		}

		public void Reset()
		{
			Atlases.Clear();
			_fonts.Clear();
			SetFontAtlas(null);
		}

		internal int? GetCodepointIndex(int codepoint, out int fontSourceIndex)
		{
			fontSourceIndex = 0;
			var g = default(int?);

			for (var i = 0; i < _fontSources.Count; ++i)
			{
				var f = _fontSources[i];
				g = f.GetGlyphId(codepoint);
				if (g is not null)
				{
					fontSourceIndex = i;
					break;
				}
			}

			return g;
		}


		private FontAtlas CreateFontAtlas(ITexture2DManager device, int textureWidth, int textureHeight)
		{
			FontTextureWrapper? existingTexture = null;
			if (ExistingTexture is not null && Atlases.Count == 0)
			{
				existingTexture = ExistingTexture;
			}

			FontAtlas fontAtlas = new FontAtlas(textureWidth, textureHeight, 256, existingTexture);

			// If existing texture is used, mark existing used rect as used
			if (existingTexture is not null && !ExistingTextureUsedSpace.IsEmpty)
			{
				if (!fontAtlas.AddSkylineLevel(0, ExistingTextureUsedSpace.X, ExistingTextureUsedSpace.Y, ExistingTextureUsedSpace.Width, ExistingTextureUsedSpace.Height))
				{
					throw new Exception(string.Format("Unable to specify existing texture used space: {0}", ExistingTextureUsedSpace));
				}

				// TODO: Clear remaining space
			}

			return fontAtlas;
		}


		internal void RenderGlyphOnAtlas(ITexture2DManager device, DynamicFontGlyph glyph)

		{
			var textureSize = new Point(TextureWidth, TextureHeight);

			if (ExistingTexture is not null)
			{

				textureSize = device.GetTextureSize(ExistingTexture);
			}

			int gx = 0, gy = 0;
			var gw = glyph.Size.X + GlyphPad * 2;
			var gh = glyph.Size.Y + GlyphPad * 2;

			// If CurrentAtlas is null create a new one
			if (CurrentAtlas is null)
			{
				SetFontAtlas(CreateFontAtlas(device, textureSize.X, textureSize.Y));
			}
			var atlas = CurrentAtlas!;
			if (!atlas.AddRect(gw, gh, ref gx, ref gy))
			{
				CurrentAtlasFull?.Invoke(this, EventArgs.Empty);

				// Create a new atlas if it was not set during the CurrentAtlasFull event
				if (CurrentAtlas == atlas)
				{
					SetFontAtlas(CreateFontAtlas(device, textureSize.X, textureSize.Y));
				}
				atlas = CurrentAtlas;

				// Try to add again
				if (!atlas!.AddRect(gw, gh, ref gx, ref gy))
				{
					throw new Exception($"Could not add rect to the newly created atlas. gw={gw}, gh={gh}");
				}
			}

			glyph.TextureOffset.X = gx + GlyphPad;
			glyph.TextureOffset.Y = gy + GlyphPad;

			atlas.RenderGlyph(device, glyph, FontSources[glyph.FontSourceIndex], GlyphRenderer, PremultiplyAlpha, KernelWidth, KernelHeight);

			glyph.Texture = atlas.Texture;
		}
	}
}