using System.Drawing;
using MintyCore.FontStashSharp;

namespace MintyCore.Myra.Graphics2D
{
	public interface IBrush
	{
		void Draw(RenderContext context, Rectangle dest, FSColor color);
	}

	public static class IBrushExtensions
	{
		public static void Draw(this IBrush brush, RenderContext context, Rectangle dest)
		{
			brush.Draw(context, dest, FSColor.White);
		}
	}
}
