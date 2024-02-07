using System;
using System.Drawing;
using MintyCore.FontStashSharp;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.Myra.Graphics2D.TextureAtlases
{
	public class NinePatchRegion : TextureRegion
	{
		private readonly Thickness _info;

		private readonly TextureRegion? _topLeft,
			_topCenter,
			_topRight,
			_centerLeft,
			_center,
			_centerRight,
			_bottomLeft,
			_bottomCenter,
			_bottomRight;

		public Thickness Info
		{
			get { return _info; }
		}

		public NinePatchRegion(FontTextureWrapper texture, Rectangle bounds, Thickness info) : base(texture, bounds)
		{
			_info = info;

			var centerWidth = bounds.Width - info.Left - info.Right;
			var centerHeight = bounds.Height - info.Top - info.Bottom;

			var y = bounds.Y;
			if (info.Top > 0)
			{
				if (info.Left > 0)
				{
					_topLeft = new TextureRegion(texture,
						new Rectangle(bounds.X,
							y,
							info.Left,
							info.Top));
				}

				if (centerWidth > 0)
				{
					_topCenter = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left,
							y,
							centerWidth,
							info.Top));
				}

				if (info.Right > 0)
				{
					_topRight = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left + centerWidth,
							y,
							info.Right,
							info.Top));
				}
			}

			y += info.Top;
			if (centerHeight > 0)
			{
				if (info.Left > 0)
				{
					_centerLeft = new TextureRegion(texture,
						new Rectangle(bounds.X,
							y,
							info.Left,
							centerHeight));
				}

				if (centerWidth > 0)
				{
					_center = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left,
							y,
							centerWidth,
							centerHeight));
				}

				if (info.Right > 0)
				{
					_centerRight = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left + centerWidth,
							y,
							info.Right,
							centerHeight));
				}
			}

			y += centerHeight;
			if (info.Bottom > 0)
			{
				if (info.Left > 0)
				{
					_bottomLeft = new TextureRegion(texture,
						new Rectangle(bounds.X,
							y,
							info.Left,
							info.Bottom));
				}

				if (centerWidth > 0)
				{
					_bottomCenter = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left,
							y,
							centerWidth,
							info.Bottom));
				}

				if (info.Right > 0)
				{
					_bottomRight = new TextureRegion(texture,
						new Rectangle(bounds.X + info.Left + centerWidth,
							y,
							info.Right,
							info.Bottom));
				}
			}
		}

		public override void Draw(RenderContext context, Rectangle dest, FSColor color)
		{
			var y = dest.Y;

			var left = Math.Min(_info.Left, dest.Width);
			var top = Math.Min(_info.Top, dest.Height);
			var right = Math.Min(_info.Right, dest.Width);
			var bottom = Math.Min(_info.Bottom, dest.Height);

			var centerWidth = dest.Width - left - right;
			if (centerWidth < 0)
			{
				centerWidth = 0;
			}

			var centerHeight = dest.Height - top - bottom;
			if (centerHeight < 0)
			{
				centerHeight = 0;
			}

			if (_topLeft is not null)
			{
				_topLeft.Draw(context,
					new Rectangle(dest.X,
						y,
						left,
						top),
					color);
			}

			if (_topCenter is not null && centerWidth > 0)
			{
				_topCenter.Draw(context,
					new Rectangle(dest.X + left,
						y,
						centerWidth,
						top),
					color);
			}

			if (_topRight is not null)
			{
				_topRight.Draw(context,
					new Rectangle(dest.X + Info.Left + centerWidth,
						y,
						right,
						top),
					color);
			}

			y += top;
			if (_centerLeft is not null && centerHeight > 0)
			{
				_centerLeft.Draw(context,
					new Rectangle(dest.X,
						y,
						left,
						centerHeight),
					color);
			}

			if (_center is not null && centerWidth > 0 && centerHeight > 0)
			{
				_center.Draw(context,
					new Rectangle(dest.X + left,
						y,
						centerWidth,
						centerHeight),
					color);
			}

			if (_centerRight is not null && centerHeight > 0)
			{
				_centerRight.Draw(context,
					new Rectangle(dest.X + Info.Left + centerWidth,
						y,
						right,
						centerHeight),
					color);
			}

			y += centerHeight;
			if (_bottomLeft is not null)
			{
				_bottomLeft.Draw(context,
					new Rectangle(dest.X,
						y,
						left,
						bottom),
					color);
			}

			if (_bottomCenter is not null && centerWidth > 0)
			{
				_bottomCenter.Draw(context,
					new Rectangle(dest.X + left,
						y,
						centerWidth,
						bottom),
					color);
			}

			if (_bottomRight is not null)
			{
				_bottomRight.Draw(context,
					new Rectangle(dest.X + Info.Left + centerWidth,
						y,
						right,
						bottom),
					color);
			}
		}
	}
}