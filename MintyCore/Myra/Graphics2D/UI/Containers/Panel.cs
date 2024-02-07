using System.Drawing;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra.Graphics2D.UI.Containers
{
	public class Panel : Container
	{
		protected override void InternalArrange()
		{
			foreach (var control in ChildrenCopy)
			{
				if (!control.Visible)
				{
					continue;
				}

				LayoutControl(control);
			}
		}

		private void LayoutControl(Widget control)
		{
			control.Arrange(ActualBounds);
		}

		protected override Point InternalMeasure(Point availableSize)
		{
			Point result = Mathematics.PointZero;

			foreach (var control in ChildrenCopy)
			{
				if (!control.Visible)
				{
					continue;
				}

				Point measure = control.Measure(availableSize);

				if (measure.X > result.X)
				{
					result.X = measure.X;
				}

				if (measure.Y > result.Y)
				{
					result.Y = measure.Y;
				}
			}

			return result;
		}
	}
}