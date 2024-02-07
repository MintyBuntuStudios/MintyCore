using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MintyCore.Myra.Attributes;
using MintyCore.Myra.Graphics2D.UI.Styles;

namespace MintyCore.Myra.Graphics2D.UI.Simple
{
	[StyleTypeName("RadioButton")]
	public class RadioButton : CheckButtonBase
	{
		private string _text;

		[Obsolete("Set Content to Label instead")]
		[Browsable(false)]
		[XmlIgnore]
		[Category("Appearance")]
		public string Text
		{
			get => _text;
			set
			{
				if (_text == value)
				{
					return;
				}

				Content = new Label
				{
					Text = value
				};

				_text = value;
			}
		}


		public override bool IsPressed
		{
			get => base.IsPressed;

			set
			{
				if (IsPressed && Parent is not null)
				{
					// If this is last pressed button
					// Don't allow it to be unpressed
					var allow = false;
					foreach (var child in Parent.ChildrenCopy)
					{
						var asRadio = child as RadioButton;
						if (asRadio is null || asRadio == this)
						{
							continue;
						}

						if (asRadio.IsPressed)
						{
							allow = true;
							break;
						}
					}

					if (!allow)
					{
						return;
					}
				}

				base.IsPressed = value;
			}
		}

		public RadioButton(string styleName = Stylesheet.DefaultStyleName)
		{
			SetStyle(styleName);
		}

		public override void OnPressedChanged()
		{
			base.OnPressedChanged();

			if (Parent is null || !IsPressed)
			{
				return;
			}

			// Release other pressed radio buttons
			foreach (var child in Parent.ChildrenCopy)
			{
				var asRadio = child as RadioButton;

				if (asRadio is null || asRadio == this)
				{
					continue;
				}

				asRadio.IsPressed = false;
			}
		}

		protected override void InternalSetStyle(Stylesheet stylesheet, string name)
		{
			ApplyCheckButtonStyle(stylesheet.RadioButtonStyles.SafelyGetStyle(name));
		}
	}
}