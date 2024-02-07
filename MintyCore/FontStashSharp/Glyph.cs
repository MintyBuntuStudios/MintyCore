
using System.Drawing;

namespace MintyCore.FontStashSharp
{
	public struct Glyph
	{
		public int Index;
		public int Codepoint;
		public Rectangle Bounds;
		public int XAdvance;
	}
}
