using MintyCore.Myra.Graphics2D.UI.Styles;

namespace MintyCore.Myra.Graphics2D.UI.Containers
{
	public class VerticalSplitPane : SplitPane
	{
		public override Orientation Orientation
		{
			get
			{
				return Orientation.Vertical;
			}
		}

		public VerticalSplitPane(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
		{
		}

		protected override void InternalSetStyle(Stylesheet stylesheet, string name)
		{
			ApplySplitPaneStyle(stylesheet.VerticalSplitPaneStyles.SafelyGetStyle(name));
		}
	}
}