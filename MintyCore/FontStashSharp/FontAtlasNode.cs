using System.Runtime.InteropServices;

namespace MintyCore.FontStashSharp
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontAtlasNode
	{
		public int X;
		public int Y;
		public int Width;
	}
}
