namespace MintyCore.Myra.Graphics2D.UI.Styles
{
	public class SpinButtonStyle : WidgetStyle
	{
		public ImageButtonStyle UpButtonStyle
		{
			get; set;
		}
		public ImageButtonStyle DownButtonStyle
		{
			get; set;
		}
		public TextBoxStyle TextBoxStyle
		{
			get; set;
		}

		public SpinButtonStyle()
		{
		}

		public SpinButtonStyle(SpinButtonStyle style) : base(style)
		{
			UpButtonStyle = style.UpButtonStyle is not null ? new ImageButtonStyle(style.UpButtonStyle) : null;
			DownButtonStyle = style.DownButtonStyle is not null ? new ImageButtonStyle(style.DownButtonStyle) : null;
			TextBoxStyle = style.TextBoxStyle is not null ? new TextBoxStyle(style.TextBoxStyle) : null;
		}

		public override WidgetStyle Clone()
		{
			return new SpinButtonStyle(this);
		}
	}
}