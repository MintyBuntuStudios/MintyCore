using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using MintyCore.FontStashSharp;
using MintyCore.Myra.Graphics2D.UI.Simple;
using MintyCore.Myra.MML;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra.Graphics2D.UI.Selectors
{
    [Obsolete("Use ListView")]
    public class ListItem : BaseObject, ISelectorItem
    {
        private string _text;
        private FSColor? _color;
        private Widget _widget;

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
                FireChanged();
            }
        }

        [DefaultValue(null)]
        public FSColor? Color
        {
            get { return _color; }

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

        [Browsable(false)] [XmlIgnore] public object Tag { get; set; }

        [Browsable(false)] [XmlIgnore] public bool IsSeparator { get; set; }

        public IImage Image { get; set; }

        [Browsable(false)] [XmlIgnore] public int ImageTextSpacing { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        internal Widget Widget
        {
            get { return _widget; }

            set
            {
                _widget = value;

                var asButton = _widget as ImageTextButton;
                if (asButton is not null)
                {
                    asButton.PressedChanged += (s, a) => FireSelectedChanged();
                }
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool IsSelected
        {
            get
            {
                var asButton = _widget as ImageTextButton;
                if (asButton is null)
                {
                    return false;
                }

                return asButton.IsPressed;
            }

            set
            {
                var asButton = _widget as ImageTextButton;
                if (asButton is null)
                {
                    return;
                }

                asButton.IsPressed = value;
            }
        }

        public event EventHandler Changed;
        public event EventHandler SelectedChanged;

        public ListItem()
        {
        }

        public ListItem(string text, FSColor? color, object tag)
        {
            Text = text;
            Color = color;
            Tag = tag;
        }

        public ListItem(string text, FSColor? color) : this(text, color, null)
        {
        }

        public ListItem(string text) : this(text, null)
        {
        }

        public void FireSelectedChanged()
        {
            var ev = SelectedChanged;
            if (ev is not null)
            {
                ev(this, EventArgs.Empty);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Text))
            {
                sb.Append(Text);
                sb.Append(" ");
            }

            if (!string.IsNullOrEmpty(Id))
            {
                sb.Append("(#");
                sb.Append(Id);
                sb.Append(")");
            }

            return sb.ToString();
        }

        protected internal override void OnIdChanged()
        {
            base.OnIdChanged();

            FireChanged();
        }

        protected void FireChanged()
        {
            Changed.Invoke(this);
        }

        public ListItem Clone()
        {
            return new ListItem
            {
                Id = Id,
                Text = Text,
                Color = Color,
                Tag = Tag,
                IsSeparator = IsSeparator,
                Image = Image,
                ImageTextSpacing = ImageTextSpacing,
            };
        }
    }
}