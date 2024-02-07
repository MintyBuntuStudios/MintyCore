using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra.Graphics2D.UI.Layouts
{
	public class SingleItemLayout<T> : ILayout where T : Widget
	{
		private readonly Widget _container;

		private ObservableCollection<Widget> Children => _container.Children;

		public T Child
		{
			get { return Children.Count > 0 ? (T)Children[0] : null; }
			set
			{
				Children.Clear();

				if (value is not null)
				{
					Children.Add(value);
				}
			}
		}

		public SingleItemLayout(Widget container)
		{
			_container = container ?? throw new ArgumentNullException(nameof(container));
		}

		public Point Measure(IEnumerable<Widget> widgets, Point availableSize)
		{
			var result = Mathematics.PointZero;

			if (Child is not null)
			{
				result = Child.Measure(availableSize);
			}

			return result;
		}

		public void Arrange(IEnumerable<Widget> widgets, Rectangle bounds)
		{
			if (Child is not null && Child.Visible)
			{
				Child.Arrange(bounds);
			}
		}
	}
}
