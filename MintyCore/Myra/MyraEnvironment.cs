using System;
using System.Drawing;
using AssetManagementBase;
using MintyCore.FontStashSharp;
using MintyCore.Myra.Graphics2D.UI;
using MintyCore.Myra.Graphics2D.UI.Simple;
using MintyCore.Myra.Graphics2D.UI.Styles;
using MintyCore.Myra.Platform;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra
{
	public static class MyraEnvironment
	{
		private static MouseCursorType _mouseCursorType;
		private static AssetManager? _defaultAssetManager;

		public static MouseCursorType MouseCursorType
		{
			get => _mouseCursorType;
			set
			{
				if (_mouseCursorType == value)
				{
					return;
				}

				_mouseCursorType = value;

				Platform.SetMouseCursorType(value);
			}
		}

		public static MouseCursorType DefaultMouseCursorType { get; set; }
		
		private static IMyraPlatform? _platform;

		public static IMyraPlatform Platform
		{
			get
			{
				if (_platform is null)
				{
					throw new Exception("MyraEnvironment.Platform is null. Please, set it before using Myra.");
				}

				return _platform;
			}

			set => _platform = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Default Assets Manager
		/// </summary>
		public static AssetManager DefaultAssetManager
		{
			get
			{
				if (_defaultAssetManager is null)
				{
					_defaultAssetManager = AssetManager.CreateFileAssetManager(PathUtils.ExecutingAssemblyDirectory);
				}

				return _defaultAssetManager;
			}

			set
			{
				if (value is null)
				{
					throw new ArgumentNullException(nameof(value));

				}
				_defaultAssetManager = value;
			}
		}

		public static bool DrawWidgetsFrames { get; set; }
		public static bool DrawKeyboardFocusedWidgetFrame { get; set; }
		public static bool DrawMouseHoveredWidgetFrame { get; set; }
		public static bool DrawTextGlyphsFrames { get; set; }
		public static bool DisableClipping { get; set; }

		public static Func<MouseInfo> MouseInfoGetter { get; set; } = DefaultMouseInfoGetter;
		public static Action<bool[]> DownKeysGetter { get; set; } = DefaultDownKeysGetter;

		public static int DoubleClickIntervalInMs { get; set; } = 500;
		public static int DoubleClickRadius { get; set; } = 2;
		public static int TooltipDelayInMs { get; set; } = 500;
		public static Point TooltipOffset { get; set; } = new Point(0, 20);
		public static Func<Widget, Widget> TooltipCreator { get; set; } = w =>
		{
			var tooltip = new Label(null)
			{
				Text = w.Tooltip,
				Tag = w
			};

			tooltip.ApplyLabelStyle(Stylesheet.Current.TooltipStyle);

			return tooltip;
		};

		/// <summary>
		/// Makes the text rendering more smooth(especially when scaling) for the cost of sacrificing some performance 
		/// </summary>
		public static bool SmoothText { get; set; }
		public static bool EnableModalDarkening { get; set; }

		public static FSColor DarkeningColor { get; set; } = new FSColor(0, 0, 0, 192);

		private static void GameOnDisposed(object sender, EventArgs eventArgs)
		{
			Reset();
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Reset()
		{
			DefaultAssets.Dispose();
			Stylesheet.Current = null!;
		}

		internal static string InternalClipboard = String.Empty;

		public static MouseInfo DefaultMouseInfoGetter()
		{

			return Platform.GetMouseInfo();
		}

		public static void DefaultDownKeysGetter(bool[] keys)
		{
			Platform.SetKeysDown(keys);
		}
	}
}