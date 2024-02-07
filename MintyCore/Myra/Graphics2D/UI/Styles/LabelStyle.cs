using MintyCore.FontStashSharp;

namespace MintyCore.Myra.Graphics2D.UI.Styles
{
	public class LabelStyle: WidgetStyle
	{
		public FSColor TextColor { get; set; }
		public FSColor? DisabledTextColor { get; set; }
		public FSColor? OverTextColor { get; set; }
		public FSColor? PressedTextColor { get; set; }
		public SpriteFontBase Font { get; set; }

		public LabelStyle()
		{
		}

		public LabelStyle(LabelStyle style) : base(style)
		{
			TextColor = style.TextColor;
			DisabledTextColor = style.DisabledTextColor;
			OverTextColor = style.OverTextColor;
			PressedTextColor = style.PressedTextColor;
			Font = style.Font;
		}

		public override WidgetStyle Clone()
		{
			return new LabelStyle(this);
		}
	}
}
