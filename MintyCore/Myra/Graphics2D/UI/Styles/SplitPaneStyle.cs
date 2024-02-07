namespace MintyCore.Myra.Graphics2D.UI.Styles
{
	public class SplitPaneStyle: WidgetStyle
	{
		public ButtonStyle HandleStyle { get; set; }

		public SplitPaneStyle()
		{
		}

		public SplitPaneStyle(SplitPaneStyle style) : base(style)
		{
			HandleStyle = style.HandleStyle is not null ? new ButtonStyle(style.HandleStyle) : null;
		}

		public override WidgetStyle Clone()
		{
			return new SplitPaneStyle(this);
		}
	}
}
