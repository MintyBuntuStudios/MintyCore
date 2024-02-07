using System.Drawing;
using MintyCore.FontStashSharp;

namespace MintyCore.Myra.Utility
{
	internal static class CrossEngineStuff
	{
		public static Point ViewSize
		{
			get
			{

				return MyraEnvironment.Platform.ViewSize;
			}
		}

		public static FSColor MultiplyColor(FSColor color, float value)
		{

			if (value < 0)
			{
				value = 0;
			}

			if (value > 1)
			{
				value = 1;
			}

			return new FSColor((int)(color.R * value),
				(int)(color.G * value),
				(int)(color.B * value),
				(int)(color.A * value));
		}
	}
}
