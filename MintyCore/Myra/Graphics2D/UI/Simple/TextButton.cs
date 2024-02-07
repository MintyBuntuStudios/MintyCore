using System;
using System.ComponentModel;
using MintyCore.FontStashSharp;
using MintyCore.Myra.Attributes;
using MintyCore.Myra.Graphics2D.UI.Styles;

namespace MintyCore.Myra.Graphics2D.UI.Simple
{
	[Obsolete("Switch to Button")]
	[StyleTypeName("Button")]
	public class TextButton : ButtonBase<Label>
	{
		[Category("Appearance")]
		[DefaultValue(null)]
		public string Text
		{
			get
			{
				return InternalChild.Text;
			}
			set
			{
				InternalChild.Text = value;
			}
		}

		[Category("Appearance")]
		[StylePropertyPath("/LabelStyle/TextColor")]
		public FSColor TextColor
		{
			get
			{
				return InternalChild.TextColor;
			}
			set
			{
				InternalChild.TextColor = value;
			}
		}

		[Category("Appearance")]
		[StylePropertyPath("/LabelStyle/OverTextColor")]
		public FSColor? OverTextColor
		{
			get
			{
				return InternalChild.OverTextColor;
			}
			set
			{
				InternalChild.OverTextColor = value;
			}
		}

		[Category("Appearance")]
		[StylePropertyPath("/LabelStyle/PressedTextColor")]
		public FSColor? PressedTextColor
		{
			get
			{
				return InternalChild.PressedTextColor;
			}
			set
			{
				InternalChild.PressedTextColor = value;
			}
		}

		[Category("Appearance")]
		public SpriteFontBase Font
		{
			get
			{
				return InternalChild.Font;
			}
			set
			{
				InternalChild.Font = value;
			}
		}

		public TextButton(string styleName = Stylesheet.DefaultStyleName)
		{
			InternalChild = new Label(null)
			{
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				Wrap = true
			};

			SetStyle(styleName);
		}

		public void ApplyTextButtonStyle(ButtonStyle style)
		{
			ApplyButtonStyle(style);

			if (style.LabelStyle is not null)
			{
				InternalChild.ApplyLabelStyle(style.LabelStyle);
			}
		}

		public override void OnPressedChanged()
		{
			base.OnPressedChanged();

			InternalChild.IsPressed = IsPressed;
		}

		protected override void InternalSetStyle(Stylesheet stylesheet, string name)
		{
			ApplyTextButtonStyle(stylesheet.ButtonStyles.SafelyGetStyle(name));
		}
	}
}