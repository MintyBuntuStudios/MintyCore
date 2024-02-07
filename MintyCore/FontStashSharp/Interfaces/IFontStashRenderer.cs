using System.Drawing;
using System.Numerics;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp.Interfaces
{
	public interface IFontStashRenderer
	{
		ITexture2DManager TextureManager { get; }

		void Draw(FontTextureWrapper texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth);
	}
}
