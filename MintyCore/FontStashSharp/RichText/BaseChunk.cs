using System.Drawing;
using System.Numerics;

namespace MintyCore.FontStashSharp.RichText
{
	public abstract class BaseChunk
	{
		public abstract Point Size { get; }

		public int LineIndex { get; internal set; }
		public int ChunkIndex { get; internal set; }
		public int VerticalOffset { get; internal set; }
		public FSColor? Color { get; set; }

		protected BaseChunk()
		{
		}

		public abstract void Draw(FSRenderContext context, Vector2 position, FSColor color);
	}
}
