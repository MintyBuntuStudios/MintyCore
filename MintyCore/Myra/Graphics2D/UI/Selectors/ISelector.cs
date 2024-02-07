using System;
using System.Collections.ObjectModel;

namespace MintyCore.Myra.Graphics2D.UI.Selectors
{
	public enum SelectionMode
	{
		/// <summary>
		/// Only one item can be selected
		/// </summary>
		Single,

		/// <summary>
		/// Multiple items can be selected
		/// </summary>
		Multiple
	}

	internal interface ISelector
	{
		SelectionMode SelectionMode { get; set; }
		int? SelectedIndex { get; set; }

		event EventHandler SelectedIndexChanged;
	}

	internal interface ISelectorT<ItemType>: ISelector
	{
		ObservableCollection<ItemType> Items { get; }
		ItemType SelectedItem { get; set; }
	}
}
