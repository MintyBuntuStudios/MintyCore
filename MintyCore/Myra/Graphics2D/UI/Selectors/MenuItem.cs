using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using MintyCore.FontStashSharp;
using MintyCore.FontStashSharp.RichText;
using MintyCore.Myra.Attributes;
using MintyCore.Myra.Graphics2D.UI.Simple;
using MintyCore.Myra.MML;

namespace MintyCore.Myra.Graphics2D.UI.Selectors
{
	public class MenuItem : BaseObject, IMenuItem
	{
		private string _shortcutText;
		private FSColor? _shortcutColor;
		private IImage _image;
		private string _text;
		private FSColor? _color;
		private bool _displayTextDirty = true;
		private string _displayText, _disabledDisplayText;

		internal readonly Image ImageWidget = new Image
		{
			VerticalAlignment = VerticalAlignment.Center
		};

		internal readonly Label Label = new Label(null)
		{
			VerticalAlignment = VerticalAlignment.Center
		};

		internal readonly Label Shortcut = new Label(null)
		{
			VerticalAlignment = VerticalAlignment.Center
		};


		internal readonly Menu SubMenu = new VerticalMenu();

		[DefaultValue(null)]
		public string Text
		{
			get { return _text; }
			set
			{
				if (value == _text)
				{
					return;
				}

				_text = value;
				_displayTextDirty = true;

				UnderscoreChar = null;
				if (value is not null)
				{
					var underscoreIndex = value.IndexOf('&');
					if (underscoreIndex >= 0 && underscoreIndex + 1 < value.Length)
					{
						UnderscoreChar = char.ToLower(value[underscoreIndex + 1]);
					}
				}

				FireChanged();
			}
		}

		internal string DisplayText
		{
			get
			{
				UpdateDisplayText();
				return _displayText;
			}
		}

		internal string DisabledDisplayText
		{
			get
			{
				UpdateDisplayText();
				return _disabledDisplayText;
			}
		}

		[DefaultValue(null)]
		public FSColor? Color
		{
			get
			{
				return _color;
			}

			set
			{
				if (value == _color)
				{
					return;
				}

				_color = value;
				FireChanged();
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public object Tag
		{
			get; set;
		}

		public IImage Image
		{
			get
			{
				return _image;
			}

			set
			{
				if (value == _image)
				{
					return;
				}

				_image = value;
				FireChanged();
			}
		}

		[DefaultValue(null)]
		public string ShortcutText
		{
			get
			{
				return _shortcutText;
			}

			set
			{
				if (value == _shortcutText)
				{
					return;
				}

				_shortcutText = value;
				FireChanged();
			}
		}

		[DefaultValue(null)]
		public FSColor? ShortcutColor
		{
			get
			{
				return _shortcutColor;
			}

			set
			{
				if (value == _shortcutColor)
				{
					return;
				}

				_shortcutColor = value;
				FireChanged();
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public Menu Menu { get; set; }

		[Browsable(false)]
		[Content]
		public ObservableCollection<IMenuItem> Items
		{
			get { return SubMenu.Items; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public bool Enabled
		{
			get { return ImageWidget.Enabled; }

			set
			{
				ImageWidget.Enabled = Label.Enabled = Shortcut.Enabled = value;
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public char? UnderscoreChar { get; private set; }

		[Browsable(false)]
		[XmlIgnore]
		public bool CanOpen
		{
			get
			{
				return Items.Count > 0;
			}
		}

		[Browsable(false)]
		[XmlIgnore]
		public int Index { get; set; }

		public event EventHandler Selected;
		public event EventHandler Changed;

		public MenuItem(string id, string text, FSColor? color, object tag)
		{
			Id = id;
			Text = text;
			Color = color;
			Tag = tag;
		}

		public MenuItem(string id, string text, FSColor? color) : this(id, text, color, null)
		{
		}

		public MenuItem(string id, string text) : this(id, text, null)
		{
		}

		public MenuItem(string id) : this(id, string.Empty)
		{
		}

		public MenuItem() : this(string.Empty)
		{
		}

		private void UpdateDisplayText()
		{
			if (!_displayTextDirty)
			{
				return;
			}

			if (UnderscoreChar is null)
			{
				_disabledDisplayText = _displayText = Text;
			}
			else
			{
				var originalColor = Menu.MenuStyle.LabelStyle.TextColor;
				if (Color is not null)
				{
					originalColor = Color.Value;
				}

				var specialCharColor = Menu.MenuStyle.SpecialCharColor;
				var underscoreIndex = Text.IndexOf('&');

				var underscoreChar = Text[underscoreIndex + 1];

				_disabledDisplayText = Text.Substring(0, underscoreIndex) + Text.Substring(underscoreIndex + 1);

				if (specialCharColor is not null)
				{
					_displayText = Text.Substring(0, underscoreIndex) +
						@"/c[" + specialCharColor.Value.ToHexString() + "]" +
						underscoreChar.ToString() +
						@"/c[" + originalColor.ToHexString() + "]" +
						Text.Substring(underscoreIndex + 2);
				}
				else
				{
					_displayText = _disabledDisplayText;
				}
			}

			_displayTextDirty = false;
		}

		internal MenuItem FindMenuItemById(string id)
		{
			if (Id == id)
			{
				return this;
			}

			foreach (var item in SubMenu.Items)
			{
				var asMenuItem = item as MenuItem;
				if (asMenuItem is null)
				{
					continue;
				}

				var result = asMenuItem.FindMenuItemById(id);
				if (result is not null)
				{
					return result;
				}
			}

			return null;
		}

		public void FireSelected()
		{
			var ev = Selected;

			if (ev is not null)
			{
				ev(this, EventArgs.Empty);
			}
		}

		protected internal override void OnIdChanged()
		{
			base.OnIdChanged();

			FireChanged();
		}

		protected void FireChanged()
		{
			var ev = Changed;
			if (ev is not null)
			{
				ev(this, EventArgs.Empty);
			}
		}
	}
}