using System.ComponentModel;
using MintyCore.Myra.Attributes;

namespace MintyCore.Myra.Graphics2D.UI
{
	public abstract class ContentControl: Widget, IContent
	{
		[Content]
		[DefaultValue(null)]
		public abstract Widget Content { get; set; }

		protected internal override void CopyFrom(Widget w)
		{
			base.CopyFrom(w);

			var contentControl = (ContentControl)w;
			Content = contentControl.Content.Clone();
		}
	}
}
