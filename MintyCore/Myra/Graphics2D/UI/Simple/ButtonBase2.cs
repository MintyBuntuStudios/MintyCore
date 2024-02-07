using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MintyCore.Myra.Events;
using MintyCore.Myra.Graphics2D.UI.Styles;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra.Graphics2D.UI.Simple
{
	public abstract class ButtonBase2 : ContentControl
	{
		private bool _isPressed = false;
		private bool _isClicked = false;

		[Category("Appearance")]
		public virtual IBrush PressedBackground { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public virtual bool IsPressed
		{
			get
			{
				return _isPressed;
			}

			set
			{
				if (value == _isPressed)
				{
					return;
				}

				_isPressed = value;
				OnPressedChanged();
			}
		}

		public event EventHandler Click;
		public event EventHandler PressedChanged;

		/// <summary>
		/// Fires when the value is about to be changed
		/// Set Cancel to true if you want to cancel the change
		/// </summary>
		public event EventHandler<ValueChangingEventArgs<bool>> PressedChangingByUser;


		public void DoClick()
		{
			OnTouchDown();
			OnTouchUp();
		}

		public virtual void OnPressedChanged()
		{
			PressedChanged.Invoke(this);

			var asPressable = Content as IPressable;
			if (asPressable is not null)
			{
				asPressable.IsPressed = IsPressed;
			}
		}

		protected void SetValueByUser(bool value)
		{
			if (value != IsPressed && PressedChangingByUser is not null)
			{
				var args = new ValueChangingEventArgs<bool>(_isPressed, value);
				PressedChangingByUser(this, args);

				if (args.Cancel)
				{
					return;
				}
			}

			IsPressed = value;
		}

		protected abstract void InternalOnTouchUp();
		protected abstract void InternalOnTouchDown();

		public override void OnTouchUp()
		{
			base.OnTouchUp();

			if (!Enabled)
			{
				return;
			}

			InternalOnTouchUp();

			if (_isClicked)
			{
				Click.Invoke(this);
				_isClicked = false;
			}
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			if (!Enabled)
			{
				return;
			}

			InternalOnTouchDown();

			_isClicked = true;
		}

		public override IBrush GetCurrentBackground()
		{
			var result = base.GetCurrentBackground();

			if (Enabled)
			{
				if (IsPressed && PressedBackground is not null)
				{
					result = PressedBackground;
				}
				else if (UseOverBackground && OverBackground is not null)
				{
					result = OverBackground;
				}
			}
			else
			{
				if (DisabledBackground is not null)
				{
					result = DisabledBackground;
				}
			}

			return result;
		}

		public void ApplyButtonStyle(ButtonStyle style)
		{
			ApplyWidgetStyle(style);

			PressedBackground = style.PressedBackground;
		}

		public void ApplyImageButtonStyle(ImageButtonStyle style)
		{
			ApplyButtonStyle(style);

			if (style.ImageStyle is not null)
			{
				var image = (Image)Content;
				image.ApplyPressableImageStyle(style.ImageStyle);
			}
		}

		protected override void InternalSetStyle(Stylesheet stylesheet, string name)
		{
			ApplyButtonStyle(stylesheet.ButtonStyles.SafelyGetStyle(name));
		}

		protected internal override void CopyFrom(Widget w)
		{
			base.CopyFrom(w);

			var buttonBase = (ButtonBase2)w;
			PressedBackground = buttonBase.PressedBackground;
			IsPressed = buttonBase.IsPressed;
		}
	}
}
