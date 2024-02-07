using System;
using System.Drawing;
using MintyCore.FontStashSharp;
using MintyCore.FontStashSharp.RichText;
using MintyCore.Myra.Graphics2D.UI.Styles;

namespace MintyCore.Myra.Graphics2D.Brushes
{
	public class SolidBrush : IBrush
	{
		private FSColor _color = FSColor.White;

		public FSColor Color
		{
			get
			{
				return _color;
			}

			set
			{
				_color = value;
			}
		}

		public SolidBrush(FSColor color)
		{
			Color = color;
		}

		public SolidBrush(string color)
		{
			var c = ColorStorage.FromName(color);
			if (c is null)
			{
				throw new ArgumentException(string.Format("Could not recognize color '{0}'", color));
			}

			Color = c.Value;
		}

		public void Draw(RenderContext context, Rectangle dest, FSColor color)
		{
			var white = Stylesheet.Current.WhiteRegion;

			if (color == FSColor.White)
			{
				white.Draw(context, dest, Color);
			}
			else
			{
				var c = new FSColor((int)(Color.R * color.R / 255.0f),
					(int)(Color.G * color.G / 255.0f),
					(int)(Color.B * color.B / 255.0f),
					(int)(Color.A * color.A / 255.0f));

				white.Draw(context, dest, c);
			}
		}
	}
}