namespace MintyCore.Myra.Graphics2D.UI.Styles
{
	public class TreeStyle : WidgetStyle
	{
		public ImageButtonStyle MarkStyle { get; set; }
		public LabelStyle LabelStyle { get; set; }
		public IBrush SelectionBackground
		{
			get; set;
		}
		public IBrush SelectionHoverBackground
		{
			get; set;
		}

		public TreeStyle()
		{
		}

		public TreeStyle(TreeStyle style): base(style)
		{
			MarkStyle = style.MarkStyle is not null ? new ImageButtonStyle(style.MarkStyle) : null;
			LabelStyle = style.LabelStyle is not null ? new LabelStyle(style.LabelStyle) : null;
			SelectionBackground = style.SelectionBackground;
			SelectionHoverBackground = style.SelectionHoverBackground;
		}

		public override WidgetStyle Clone()
		{
			return new TreeStyle(this);
		}
	}
}