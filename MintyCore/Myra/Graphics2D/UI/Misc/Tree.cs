using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using MintyCore.Myra.Graphics2D.UI.Styles;
using MintyCore.Myra.Platform;

namespace MintyCore.Myra.Graphics2D.UI.Misc
{
	[Obsolete("Use TreeView")]
	public class Tree : TreeNode
	{
		private readonly List<TreeNode> _allNodes = new List<TreeNode>();
		private TreeNode _selectedRow;
		private bool _rowInfosDirty = true;
		private bool _hasRoot = true;

		public List<TreeNode> AllNodes
		{
			get
			{
				return _allNodes;
			}
		}

		private TreeNode HoverRow { get; set; }

		public TreeNode SelectedRow
		{
			get
			{
				return _selectedRow;
			}

			set
			{
				if (value == _selectedRow)
				{
					return;
				}

				_selectedRow = value;

				var ev = SelectionChanged;
				if (ev is not null)
				{
					ev(this, EventArgs.Empty);
				}
			}
		}

		[DefaultValue(true)]
		public bool HasRoot
		{
			get
			{
				return _hasRoot;
			}

			set
			{
				if (value == _hasRoot)
				{
					return;
				}

				_hasRoot = value;
				UpdateHasRoot();
			}
		}

		public event EventHandler SelectionChanged;

		public Tree(string styleName = Stylesheet.DefaultStyleName) : base(null, styleName)
		{
			AcceptsKeyboardFocus = true;
		}

		private void UpdateHasRoot()
		{
			if (_hasRoot)
			{
				Mark.Visible = true;
				Label.Visible = true;
				ChildNodesGrid.Visible = Mark.IsPressed;
			}
			else
			{
				Mark.Visible = false;
				Label.Visible = false;
				Mark.IsPressed = true;
				ChildNodesGrid.Visible = true;
			}
		}

		public override void OnKeyDown(Keys k)
		{
			base.OnKeyDown(k);

			if (SelectedRow is null)
			{
				return;
			}

			int index = 0;
			IList<Widget> parentWidgets = null;
			if (SelectedRow.ParentNode is not null)
			{
				parentWidgets = SelectedRow.ParentNode.ChildNodesGrid.Widgets;
				index = parentWidgets.IndexOf(SelectedRow);
				if (index == -1)
				{
					return;
				}
			}

			switch (k)
			{
				case Keys.Enter:
					SelectedRow.IsExpanded = !SelectedRow.IsExpanded;
					break;
				case Keys.Up:
				{
					if (parentWidgets is not null)
					{
						if (index == 0 && SelectedRow.ParentNode != this)
						{
							SelectedRow = SelectedRow.ParentNode;
						}
						else if (index > 0)
						{
							var previousRow = (TreeNode)parentWidgets[index - 1];
							if (!previousRow.IsExpanded || previousRow.ChildNodesCount == 0)
							{
								SelectedRow = previousRow;
							}
							else
							{
								SelectedRow = (TreeNode)previousRow.ChildNodesGrid.Widgets[previousRow.ChildNodesCount - 1];
							}
						}
					}
				}
				break;
				case Keys.Down:
				{
					if (SelectedRow.IsExpanded && SelectedRow.ChildNodesCount > 0)
					{
						SelectedRow = (TreeNode)SelectedRow.ChildNodesGrid.Widgets[0];
					}
					else if (parentWidgets is not null && index + 1 < parentWidgets.Count)
					{
						SelectedRow = (TreeNode)parentWidgets[index + 1];
					}
					else if (parentWidgets is not null && index + 1 >= parentWidgets.Count)
					{
						var parentOfParent = SelectedRow.ParentNode.ParentNode;
						if (parentOfParent is not null)
						{
							var parentIndex = parentOfParent.ChildNodesGrid.Widgets.IndexOf(SelectedRow.ParentNode);
							if (parentIndex + 1 < parentOfParent.ChildNodesCount)
							{
								SelectedRow = (TreeNode)parentOfParent.ChildNodesGrid.Widgets[parentIndex + 1];
							}
						}
					}
				}
				break;
			}
		}

		public override void OnTouchDown()
		{
			base.OnTouchDown();

			if (Desktop is null)
			{
				return;
			}

			SetHoverRow(Desktop.TouchPosition.Value);

			if (HoverRow is not null && HoverRow.RowVisible)
			{
				SelectedRow = HoverRow;
			}
		}

		public override void OnTouchDoubleClick()
		{
			base.OnTouchDoubleClick();

			if (HoverRow is not null)
			{
				if (!HoverRow.RowVisible)
				{
					return;
				}

				if (HoverRow.Mark.Visible && !HoverRow.Mark.IsTouchInside)
				{
					HoverRow.Mark.DoClick();
				}
			}
		}

		private Rectangle BuildRowRect(TreeNode rowInfo)
		{
			var rowPos = ToLocal(rowInfo.ToGlobal(rowInfo.ActualBounds.Location));

			return new Rectangle(ActualBounds.Left, rowPos.Y, ActualBounds.Width, rowInfo.Grid.GetRowHeight(0));
		}

		private void SetHoverRow(Point position)
		{
			if (!ContainsGlobalPoint(position))
			{
				return;
			}

			position = ToLocal(position);
			foreach (var rowInfo in _allNodes)
			{
				if (rowInfo.RowVisible)
				{
					var rect = BuildRowRect(rowInfo);
					if (rect.Contains(position))
					{
						HoverRow = rowInfo;
						return;
					}
				}
			}
		}

		public override void OnMouseMoved()
		{
			base.OnMouseMoved();

			HoverRow = null;

			if (Desktop is null)
			{
				return;
			}

			SetHoverRow(Desktop.MousePosition);
		}

		public override void OnMouseLeft()
		{
			base.OnMouseLeft();

			HoverRow = null;
		}

		public override void RemoveAllSubNodes()
		{
			base.RemoveAllSubNodes();

			_allNodes.Clear();
			_allNodes.Add(this);

			_selectedRow = null;
			HoverRow = null;
		}

		private bool Iterate(TreeNode node, Func<TreeNode, bool> action)
		{
			if (!action(node))
			{
				return false;
			}

			foreach (var widget in node.ChildNodesGrid.ChildrenCopy)
			{
				var subNode = (TreeNode)widget;
				if (!Iterate(subNode, action))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Iterates through all nodes
		/// </summary>
		/// <param name="action">Called for each node, returning false breaks iteration</param>
		public void Iterate(Func<TreeNode, bool> action)
		{
			Iterate(this, action);
		}

		private static void RecursiveUpdateRowVisibility(TreeNode tree)
		{
			tree.RowVisible = true;

			if (tree.IsExpanded)
			{
				foreach (var widget in tree.ChildNodesGrid.ChildrenCopy)
				{
					var treeNode = (TreeNode)widget;
					RecursiveUpdateRowVisibility(treeNode);
				}
			}
		}

		private void UpdateRowInfos()
		{
			foreach (var rowInfo in _allNodes)
			{
				rowInfo.RowVisible = false;
			}

			RecursiveUpdateRowVisibility(this);
		}

		protected override void InternalArrange()
		{
			base.InternalArrange();
			_rowInfosDirty = true;
		}

		public override void InternalRender(RenderContext context)
		{
			if (_rowInfosDirty)
			{
				UpdateRowInfos();
				_rowInfosDirty = false;
			}

			if (HoverRow is not null && HoverRow != SelectedRow && SelectionHoverBackground is not null)
			{
				var rect = BuildRowRect(HoverRow);
				SelectionHoverBackground.Draw(context, rect);
			}

			if (SelectedRow is not null && SelectedRow.RowVisible && SelectionBackground is not null)
			{
				var rect =  BuildRowRect(SelectedRow);
				SelectionBackground.Draw(context, rect);
			}

			base.InternalRender(context);
		}

		protected override void UpdateMark()
		{
			if (!HasRoot)
			{
				return;
			}

			base.UpdateMark();
		}

		private static bool FindPath(Stack<TreeNode> path, TreeNode node)
		{
			var top = path.Peek();

			for (var i = 0; i < top.ChildNodesCount; ++i)
			{
				var child = top.GetSubNode(i);

				if (child == node)
				{
					return true;
				}

				path.Push(child);

				if (FindPath(path, node))
				{
					return true;
				}

				path.Pop();
			}

			return false;
		}


		/// <summary>
		/// Expands path to the node
		/// </summary>
		/// <param name="node"></param>
		public void ExpandPath(TreeNode node)
		{
			var path = new Stack<TreeNode>();

			path.Push(this);

			if (!FindPath(path, node))
			{
				// Path not found
				return;
			}

			while (path.Count > 0)
			{
				var p = path.Pop();
				p.IsExpanded = true;
			}
		}
	}
}