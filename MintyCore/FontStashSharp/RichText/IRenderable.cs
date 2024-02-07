using System.Drawing;
using System.Numerics;

namespace MintyCore.FontStashSharp.RichText
{
	public interface IRenderable
	{
		Point Size { get; }

		void Draw(FSRenderContext context, Vector2 position, FSColor color);
	}
}
