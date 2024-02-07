using MintyCore.FontStashSharp;

namespace MintyCore.Myra.Graphics2D.UI.Styles
{
	public class MenuStyle : WidgetStyle
	{
		public PressableImageStyle ImageStyle { get; set; }
		public LabelStyle LabelStyle { get; set; }
		public LabelStyle ShortcutStyle { get; set; }
		public SeparatorStyle SeparatorStyle { get; set; }
		public IBrush SelectionHoverBackground { get; set; }
		public IBrush SelectionBackground { get; set; }
		public FSColor? SpecialCharColor { get; set; }

		public MenuStyle()
		{
		}

		public MenuStyle(MenuStyle style) : base(style)
		{
			ImageStyle = style.ImageStyle is not null ? new PressableImageStyle(style.ImageStyle) : null;
			LabelStyle = style.LabelStyle is not null ? new LabelStyle(style.LabelStyle) : null;
			ShortcutStyle = style.ShortcutStyle is not null ? new LabelStyle(style.ShortcutStyle) : null;
			SeparatorStyle = style.SeparatorStyle is not null ? new SeparatorStyle(style.SeparatorStyle) : null;
			SpecialCharColor = style.SpecialCharColor;
		}

		public override WidgetStyle Clone()
		{
			return new MenuStyle(this);
		}
	}
}
