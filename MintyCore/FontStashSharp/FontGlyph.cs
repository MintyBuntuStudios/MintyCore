
using System.Drawing;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp
{
	public class FontGlyph
	{
		public int Codepoint;
		public int Id;
		public int XAdvance;
		public FontTextureWrapper? Texture;
		public Point RenderOffset;
		public Point TextureOffset;
		public Point Size;

		public bool IsEmpty => Size.X == 0 || Size.Y == 0;

		public Rectangle TextureRectangle => new Rectangle(TextureOffset.X, TextureOffset.Y, Size.X, Size.Y);
		public Rectangle RenderRectangle => new Rectangle(RenderOffset.X, RenderOffset.Y, Size.X, Size.Y);
	}

	public class DynamicFontGlyph : FontGlyph
	{
		public float FontSize;
		public int FontSourceIndex;
		public FontSystemEffect Effect;
		public int EffectAmount;
	}
}
