
using System.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cyotek.Drawing.BitmapFont;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;
using StbImageSharp;

namespace MintyCore.FontStashSharp
{
	public class TextureWithOffset
	{
		public FontTextureWrapper Texture { get; set; }
		public Point Offset { get; set; }

		public TextureWithOffset(FontTextureWrapper texture)
		{
			if (texture is null)
			{
				throw new ArgumentNullException("texture");
			}

			Texture = texture;
		}

		public TextureWithOffset(FontTextureWrapper texture, Point offset) : this(texture)
		{
			Offset = offset;
		}
	}

	public partial class StaticSpriteFont : SpriteFontBase
	{
		private readonly Int32Map<int> _kernings = new Int32Map<int>();

		public Int32Map<FontGlyph> Glyphs { get; } = new Int32Map<FontGlyph>();

		public int? DefaultCharacter { get; set; }

		public bool UseKernings { get; set; } = true;

		public StaticSpriteFont(int fontSize, int lineHeight): base(fontSize, lineHeight)
		{
		}

		private FontGlyph? InternalGetGlyph(int codepoint)
		{
			Glyphs.TryGetValue(codepoint, out var result);

			return result;
		}


		protected internal override FontGlyph? GetGlyph(ITexture2DManager? device, int codepoint, FontSystemEffect effect, int effectAmount)
		{
			var result = InternalGetGlyph(codepoint);
			if (result is null && DefaultCharacter is not null)
			{
				result = InternalGetGlyph(DefaultCharacter.Value);
			}
			return result;
		}

		internal override void PreDraw(TextSource source, FontSystemEffect effect, int effectAmount, out int ascent, out int lineHeight)
		{
			ascent = 0;
			lineHeight = LineHeight;
		}

		private static int KerningKey(int codepoint1, int codepoint2)
		{
			return ((codepoint1 << 16) | (codepoint1 >> 16)) ^ codepoint2;
		}

		public int GetGlyphKernAdvance(int codepoint1, int codepoint2)
		{
			var key = KerningKey(codepoint1, codepoint2);
			_kernings.TryGetValue(key, out var result);

			return result;
		}

		public void SetGlyphKernAdvance(int codepoint1, int codepoint2, int value)
		{
			var key = KerningKey(codepoint1, codepoint2);
			_kernings[key] = value;
		}

		internal override float GetKerning(FontGlyph glyph, FontGlyph prevGlyph)
		{
			if (!UseKernings)
			{
				return 0.0f;
			}

			return GetGlyphKernAdvance(prevGlyph.Codepoint, glyph.Codepoint);
		}

		private static BitmapFont LoadBMFont(string data)
		{
			var bmFont = new BitmapFont();
			if (data.StartsWith("<"))
			{
				// xml
				bmFont.LoadXml(data);
			}
			else
			{
				bmFont.LoadText(data);
			}

			return bmFont;
		}

		private static StaticSpriteFont FromBMFont(BitmapFont bmFont, Func<string, TextureWithOffset> textureGetter)
		{
			var result = new StaticSpriteFont(bmFont.LineHeight, bmFont.LineHeight);

			var characters = bmFont.Characters.Values.OrderBy(c => c.Char);

			foreach (var ch in characters)
			{
				var texture = textureGetter(bmFont.Pages[ch.TexturePage].FileName);

				var glyph = new FontGlyph
				{
					Id = ch.Char,
					Codepoint = ch.Char,
					RenderOffset = new Point(ch.XOffset, ch.YOffset),
					TextureOffset = new Point(ch.X + texture.Offset.X, ch.Y + texture.Offset.Y),
					Size = new Point(ch.Width, ch.Height),
					XAdvance = ch.XAdvance,
					Texture = texture.Texture
				};

				result.Glyphs[glyph.Codepoint] = glyph;
			}

			foreach (var kern in bmFont.Kernings)
			{
				result.SetGlyphKernAdvance(kern.Key.FirstCharacter, kern.Key.SecondCharacter, kern.Value);
			}

			return result;
		}

		public static StaticSpriteFont FromBMFont(string data, Func<string, TextureWithOffset> textureGetter)
		{
			var bmFont = LoadBMFont(data);
			return FromBMFont(bmFont, textureGetter);
		}


		public static StaticSpriteFont FromBMFont(string data, Func<string, Stream> imageStreamOpener, ITexture2DManager textureManager)
		{
			var bmFont = LoadBMFont(data);

			var textures = new Dictionary<string, FontTextureWrapper>();
			for (var i = 0; i < bmFont.Pages.Length; ++i)
			{
				var fileName = bmFont.Pages[i].FileName;
				Stream? stream = null;
				try
				{
					stream = imageStreamOpener(fileName);
					if (!stream.CanSeek)
					{
						// If stream isn't seekable, use MemoryStream instead
						var ms = new MemoryStream();
						stream.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
						stream.Dispose();
						stream = ms;
					}

					var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
					if (image.SourceComp == ColorComponents.Grey)
					{
						// If input image is single byte per pixel, then StbImageSharp will set alpha to 255 in the resulting 32-bit image
						// Such behavior isn't acceptable for us
						// So reset alpha to color value
						for (var j = 0; j < image.Data.Length / 4; ++j)
						{
							image.Data[j * 4 + 3] = image.Data[j * 4];
						}
					}


					var texture = textureManager.CreateTexture(image.Width, image.Height);
					textureManager.SetTextureData(texture, new Rectangle(0, 0, image.Width, image.Height), image.Data);

					textures[fileName] = texture;
				}
				finally
				{
					stream?.Dispose();
				}
			}

			return FromBMFont(bmFont, fileName => new TextureWithOffset(textures[fileName]));
		}
	}
}