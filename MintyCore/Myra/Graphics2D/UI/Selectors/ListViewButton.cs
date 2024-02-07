using MintyCore.Myra.Graphics2D.UI.Simple;

namespace MintyCore.Myra.Graphics2D.UI.Selectors
{
	internal class ListViewButton : ToggleButton
	{
		public ListViewButton() : base(null)
		{
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
						var asListViewButton = child as ListViewButton;
						if (asListViewButton is null || asListViewButton == this)
						{
							continue;
						}

						if (asListViewButton.IsPressed)
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
				var asRadio = child as ListViewButton;
				if (asRadio is null || asRadio == this)
				{
					continue;
				}

				asRadio.IsPressed = false;
			}
		}
	}
}
