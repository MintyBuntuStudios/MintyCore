using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MintyCore.Myra.Graphics2D.UI.Styles;

namespace MintyCore.Myra.Graphics2D.UI.Simple
{
	[Obsolete("Use CheckButton")]
	public class CheckBox : ImageTextButton
	{
		[Browsable(false)]
		[XmlIgnore]
		[DefaultValue(true)]
		public override bool Toggleable
		{
			get { return base.Toggleable; }
			set { base.Toggleable = value; }
		}

		[Category("Behavior")]
		[DefaultValue(false)]
		public bool IsChecked
		{
			get => IsPressed;
			set => IsPressed = value;
		}

		public CheckBox(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
		{
			Toggleable = true;
		}

		protected override void InternalSetStyle(Stylesheet stylesheet, string name)
		{
			ApplyImageTextButtonStyle(stylesheet.CheckBoxStyles.SafelyGetStyle(name));
		}
	}
}
