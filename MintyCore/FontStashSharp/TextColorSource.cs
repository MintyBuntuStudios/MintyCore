using System.Text;
using OneOf;

namespace MintyCore.FontStashSharp
{
	internal ref struct TextColorSource
	{
		public TextSource TextSource;
		public OneOf<FSColor, FSColor[]> Color;
		public int ColorPosition;

		public TextColorSource(string text, FSColor color)
		{
			TextSource = new TextSource(text);
			Color = color;
			ColorPosition = 0;
		}

		public TextColorSource(string text, FSColor[] colors)
		{
			TextSource = new TextSource(text);
			Color = colors;
			ColorPosition = 0;
		}

		public TextColorSource(StringSegment text, FSColor color)
		{
			TextSource = new TextSource(text);
			Color = color;
			ColorPosition = 0;
		}

		public TextColorSource(StringSegment text, FSColor[] colors)
		{
			TextSource = new TextSource(text);
			Color = colors;
			ColorPosition = 0;
		}

		public TextColorSource(StringBuilder text, FSColor color)
		{
			TextSource = new TextSource(text);
			Color = color;
			ColorPosition = 0;
		}

		public TextColorSource(StringBuilder text, FSColor[] colors)
		{
			TextSource = new TextSource(text);
			Color = colors;
			ColorPosition = 0;
		}

		public bool IsNull => TextSource.IsNull;

		[System.Obsolete("Possible phase out.")]
		public bool GetNextCodepoint(out int codepoint, out FSColor color)
		{
			color = FSColor.Transparent;
			if (!TextSource.GetNextCodepoint(out codepoint))
			{
				return false;
			}

			if(Color.TryPickT0(out var singleColor, out var colors))
			{
				color = singleColor;
			}
			else
			{
				color = colors[ColorPosition];
				++ColorPosition;
			}

			return true;
		}
		
		public bool GetNextCodepoint(out int codepoint)
		{
			return TextSource.GetNextCodepoint(out codepoint);
		}
		
		public FSColor GetNextColor()
		{
			FSColor color;
			
			if(Color.TryPickT0(out var singleColor, out var colors))
			{
				color = singleColor;
			}
			else
			{
				color = colors[ColorPosition % colors.Length];
				++ColorPosition;
			}
			
		
			
			return color;
		}
	}
}
