using System.Drawing;
using System.Numerics;

namespace MintyCore.FontStashSharp.RichText
{
	public class SpaceChunk : BaseChunk
	{
		private readonly int _width;

		public override Point Size => new(_width, 0);

		public SpaceChunk(int width)
		{
			_width = width;
		}

		public override void Draw(FSRenderContext context, Vector2 position, FSColor color)
		{
		}
	}
}
