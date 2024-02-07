using System.Collections.Generic;
using System.Drawing;

namespace MintyCore.Myra.Graphics2D.UI
{
	public interface ILayout
	{
		Point Measure(IEnumerable<Widget> widgets, Point availableSize);
		void Arrange(IEnumerable<Widget> widgets, Rectangle bounds);
	}
}
