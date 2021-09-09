using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using KSP.UI.TooltipTypes;
using KSP.Localization;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;
	using static TooltipExtensions;
	using static PhysicsTools;

	/// Anything UI-related that needs to be used from multiple places.
	public static class ViewTools {

		/// <value>
		/// Height of a row of our table.
		/// </value>
		public const int rowHeight = 16;

		/// <value>
		/// Size of font to use in our table.
		/// </value>
		public const int fontSize = 12;

		/// <value>
		/// Size of button icons to use in our table.
		/// </value>
		public const int buttonIconWidth = 16;

		private static readonly Version modVersion = Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// A string representing the version number of the mod.
		/// </summary>
		public static readonly string versionString = Localizer.Format(
			"astrogator_versionFormat", modVersion.Major, modVersion.Minor, modVersion.Build
		);

		/// <summary>
		/// Return a list of a model's transfers sorted according to settings
		/// </summary>
		/// <param name="m">Model from which to get transfers</param>
		/// <param name="how">Method for sorting</param>
		/// <param name="descend">True for descending sort, false for ascending</param>
		/// <returns>
		/// Sorted list
		/// </returns>
		public static List<TransferModel> SortTransfers(AstrogationModel m, SortEnum how, bool descend)
		{
			List<TransferModel> transfers = new List<TransferModel>(m.transfers);
			switch (how) {
				case SortEnum.Name:
					transfers.Sort((a, b) =>
						  a?.destination == null && b?.destination == null ? 0
						: a?.destination == null ?  1
						: b?.destination == null ? -1
						: a.destination.GetName().CompareTo(b.destination.GetName()));
					break;
				case SortEnum.Position:
					// Use the natural/default ordering in the model
					break;
				case SortEnum.Time:
					transfers.Sort((a, b) =>
						  a?.ejectionBurn?.atTime == null && b?.ejectionBurn?.atTime == null ? 0
						: a?.ejectionBurn?.atTime == null ?  1
						: b?.ejectionBurn?.atTime == null ? -1
						: a.ejectionBurn.atTime.Value.CompareTo(b.ejectionBurn.atTime.Value));
					break;
				case SortEnum.DeltaV:
				case SortEnum.Duration:
					transfers.Sort((a, b) =>
						  a?.ejectionBurn == null && b?.ejectionBurn == null ? 0
						: a?.ejectionBurn == null ?  1
						: b?.ejectionBurn == null ? -1
						: a.ejectionBurn.totalDeltaV.CompareTo(b.ejectionBurn.totalDeltaV));
					break;
				default:
					DbgFmt("Bad sort argument: {0}", how.ToString());
					break;
			}
			if (descend) {
				transfers.Reverse();
			}
			return transfers;
		}

		/// <value>
		/// The icon to show for this mod in the app launcher.
		/// </value>
		public static readonly Texture2D AppIcon = GetImage(FilePath("Icons/Astrogator"));

		/// <returns>
		/// A texture object for the image file at the given path.
		/// </returns>
		/// <param name="filepath">Path to image file to load</param>
		private static Texture2D GetImage(string filepath)
		{
			return GameDatabase.Instance.GetTexture(filepath, false);
		}

		/// <summary>
		/// Borrowed from Kerbal Engineer Redux and Americanized.
		/// </summary>
		/// <param name="c">The color to use</param>
		/// <returns>
		/// A 1x1 texture
		/// </returns>
		private static Texture2D SolidColorTexture(Color c)
		{
			Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			tex.SetPixel(1, 1, c);
			tex.Apply();
			return tex;
		}

		private static Sprite SpriteFromTexture(Texture2D tex)
		{
			if (tex != null) {
				return Sprite.Create(
					tex,
					new Rect(0, 0, tex.width, tex.height),
					new Vector2(0.5f, 0.5f),
					tex.width
				);
			} else {
				return null;
			}
		}

		/// <returns>
		/// A sprite object for the image at the given path.
		/// </returns>
		private static Sprite GetSprite(string filepath)
		{
			return SpriteFromTexture(GetImage(filepath));
		}

		/// <returns>
		/// A 1x1 sprite object of the given color.
		/// </returns>
		private static Sprite SolidColorSprite(Color c)
		{
			return SpriteFromTexture(SolidColorTexture(c));
		}

		/// <value>
		/// Black image with 50% opacity.
		/// </value>
		public static readonly Sprite halfTransparentBlack = SolidColorSprite(new Color(0f, 0f, 0f, 0.5f));

		/// <value>
		/// Completely transparent sprite so we can use buttons for the headers
		/// without the default button graphic.
		/// </value>
		public static readonly Sprite transparent = SolidColorSprite(new Color(0f, 0f, 0f, 0f));

		/// <value>
		/// Backgrounds and text colors for the tooltip and main window.
		/// </value>
		public static readonly UIStyleState windowStyleState = new UIStyleState() {
			background	= halfTransparentBlack,
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <value>
		/// Text color for table headers.
		/// </value>
		public static readonly UIStyleState headingFont = new UIStyleState() {
			background	= transparent,
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <value>
		/// Text color for main table content.
		/// </value>
		public static readonly UIStyleState numberFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.8f)
		};

		/// <value>
		/// Text color for the line under the title.
		/// </value>
		public static readonly UIStyleState subTitleFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.6f)
		};

		/// <value>
		/// Text color for the line under the title when it's an error message.
		/// </value>
		public static readonly UIStyleState subTitleErrorFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0f, 0.9f, 0.9f)
		};

		/// <value>
		/// Text color for the line under the title.
		/// </value>
		public static readonly UIStyleState linkFont = new UIStyleState() {
			background	= transparent,
			textColor	= Color.HSVToRGB(0.6f, 0.7f, 0.9f)
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for the tooltip and main window.
		/// </value>
		public static readonly UIStyle windowStyle = new UIStyle() {
			normal	= windowStyleState,
			active	= windowStyleState,
			disabled	= windowStyleState,
			highlight	= windowStyleState,
			alignment	= TextAnchor.UpperCenter,
			fontSize	= UISkinManager.defaultSkin.window.fontSize,
			fontStyle	= FontStyle.Bold,
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for planet header.
		/// </value>
		public static readonly UIStyle leftHdrStyle = new UIStyle() {
			normal	= headingFont,
			active	= headingFont,
			disabled	= headingFont,
			highlight	= headingFont,
			alignment	= TextAnchor.LowerLeft,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Bold,
			fixedHeight	= rowHeight
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for the time header.
		/// </value>
		public static readonly UIStyle midHdrStyle = new UIStyle() {
			normal	= headingFont,
			active	= headingFont,
			disabled	= headingFont,
			highlight	= headingFont,
			alignment	= TextAnchor.LowerCenter,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Bold,
			fixedHeight	= rowHeight
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for the delta V header.
		/// </value>
		public static readonly UIStyle rightHdrStyle = new UIStyle() {
			normal	= headingFont,
			active	= headingFont,
			disabled	= headingFont,
			highlight	= headingFont,
			alignment	= TextAnchor.LowerRight,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Bold,
			fixedHeight	= rowHeight
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for the planet names.
		/// </value>
		public static readonly UIStyle planetStyle = new UIStyle() {
			normal	= headingFont,
			active	= headingFont,
			disabled	= headingFont,
			highlight	= headingFont,
			alignment	= TextAnchor.MiddleLeft,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Normal,
			fixedHeight	= rowHeight
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for normal content.
		/// </value>
		public static readonly UIStyle numberStyle = new UIStyle() {
			normal	= numberFont,
			active	= numberFont,
			disabled	= numberFont,
			highlight	= numberFont,
			alignment	= TextAnchor.MiddleRight,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Normal,
			fixedHeight	= rowHeight
		};

		/// <value>
		/// Icon for normal state of settings button.
		/// </value>
		public static readonly Sprite settingsIcon = GetSprite(FilePath("Icons/settings"));

		/// <value>
		/// Icon for normal state of settings button.
		/// </value>
		public static readonly UIStyleState settingsStyleState = new UIStyleState() {
			background	= settingsIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of settings button.
		/// </value>
		public static readonly Sprite settingsHoverIcon = GetSprite(FilePath("Icons/settingsHover"));

		/// <value>
		/// Icon for hovered state of settings button.
		/// </value>
		public static readonly UIStyleState settingsHoverStyleState = new UIStyleState() {
			background	= settingsHoverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Normal/highlight icons for the settings button.
		/// </value>
		public static readonly UIStyle settingsStyle = new UIStyle() {
			normal	= settingsStyleState,
			highlight	= settingsHoverStyleState,
			active	= settingsHoverStyleState,
			disabled	= settingsStyleState,
			alignment	= TextAnchor.UpperRight,
		};

		/// <summary>
		/// Icon for the normal state of the back button.
		/// </summary>
		public static readonly Sprite backIcon = GetSprite(FilePath("Icons/back"));

		/// <summary>
		/// Icon for the normal state of the back button.
		/// </summary>
		public static readonly UIStyleState backStyleState = new UIStyleState() {
			background	= backIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Icon for the hovered state of the back button.
		/// </summary>
		public static readonly Sprite backHoverIcon = GetSprite(FilePath("Icons/backHover"));

		/// <summary>
		/// Icon for the hovered state of the back button.
		/// </summary>
		public static readonly UIStyleState backHoverStyleState = new UIStyleState() {
			background	= backHoverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Normal/highlight icons for the back button.
		/// </value>
		public static readonly UIStyle backStyle = new UIStyle() {
			normal	= backStyleState,
			highlight	= backHoverStyleState,
			active	= backHoverStyleState,
			disabled	= backStyleState,
			alignment	= TextAnchor.UpperRight,
		};

		/// <value>
		/// Icon for normal state of maneuver node creation button.
		/// </value>
		public static readonly Sprite maneuverIcon = GetSprite(FilePath("Icons/maneuver"));

		/// <value>
		/// Icon for normal state of maneuver creation button.
		/// </value>
		public static readonly UIStyleState maneuverStyleState = new UIStyleState() {
			background	= maneuverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of maneuver node creation button.
		/// </value>
		public static readonly Sprite maneuverHoverIcon = GetSprite(FilePath("Icons/maneuverHover"));

		/// <value>
		/// Icon for hovered state of maneuver creation button.
		/// </value>
		public static readonly UIStyleState maneuverHoverStyleState = new UIStyleState() {
			background	= maneuverHoverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Normal/highlight icons for the maneuver creation button.
		/// </value>
		public static readonly UIStyle maneuverStyle = new UIStyle() {
			normal	= maneuverStyleState,
			highlight	= maneuverHoverStyleState,
			active	= maneuverHoverStyleState,
			disabled	= maneuverStyleState,
		};

		/// <value>
		/// Icon for normal state of warp button.
		/// </value>
		public static readonly Sprite warpIcon = GetSprite(FilePath("Icons/warp"));

		/// <value>
		/// Icon for normal state of warp button.
		/// </value>
		public static readonly UIStyleState warpStyleState = new UIStyleState() {
			background	= warpIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of warp button.
		/// </value>
		public static readonly Sprite warpHoverIcon = GetSprite(FilePath("Icons/warpHover"));

		/// <value>
		/// Icon for hovered state of warp button.
		/// </value>
		public static readonly UIStyleState warpHoverStyleState = new UIStyleState() {
			background	= warpHoverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Normal/highlight icons for the warp button.
		/// </summary>
		public static readonly UIStyle warpStyle = new UIStyle() {
			normal	= warpStyleState,
			highlight	= warpHoverStyleState,
			active	= warpHoverStyleState,
			disabled	= warpStyleState,
		};

		/// <summary>
		/// Icon for close X button when not hovered.
		/// </summary>
		public static readonly Sprite closeIcon = GetSprite(FilePath("Icons/close"));

		/// <summary>
		/// Icon for close X button when not hovered.
		/// </summary>
		public static readonly UIStyleState closeStyleState = new UIStyleState() {
			background	= closeIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Icon for close X button when hovered.
		/// </summary>
		public static readonly Sprite closeHoverIcon = GetSprite(FilePath("Icons/closeHover"));

		/// <summary>
		/// Icon for close X button when hovered.
		/// </summary>
		public static readonly UIStyleState closeHoverStyleState = new UIStyleState() {
			background	= closeHoverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Style for close X button.
		/// </summary>
		public static readonly UIStyle closeStyle = new UIStyle() {
			normal	= closeStyleState,
			highlight	= closeHoverStyleState,
			active	= closeHoverStyleState,
			disabled	= closeStyleState,
		};

		/// <value>
		/// A centered variant of the normal content font.
		/// </value>
		public static readonly UIStyle subTitleStyle = new UIStyle() {
			normal	= subTitleFont,
			active	= subTitleFont,
			disabled	= subTitleFont,
			highlight	= subTitleFont,
			alignment	= TextAnchor.MiddleCenter,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Normal,
		};

		/// <value>
		/// Left aligned italic text for telling the user things.
		/// </value>
		public static readonly UIStyle notificationStyle = new UIStyle() {
			normal	= subTitleFont,
			active	= subTitleFont,
			disabled	= subTitleFont,
			highlight	= subTitleFont,
			alignment	= TextAnchor.MiddleLeft,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Italic,
		};

		/// <value>
		/// A red, centered variant of the normal content font for error messages.
		/// </value>
		public static readonly UIStyle subTitleErrorStyle = new UIStyle() {
			normal	= subTitleErrorFont,
			active	= subTitleErrorFont,
			disabled	= subTitleErrorFont,
			highlight	= subTitleErrorFont,
			alignment	= TextAnchor.MiddleCenter,
			fontSize	= windowStyle.fontSize,
			fontStyle	= FontStyle.Bold,
		};

		/// <value>
		/// Variant of the standard checkbox style to support long strings.
		/// </value>
		public static readonly UIStyle toggleStyle = new UIStyle() {
			normal	= UISkinManager.defaultSkin.toggle.normal,
			active	= UISkinManager.defaultSkin.toggle.active,
			disabled	= UISkinManager.defaultSkin.toggle.disabled,
			highlight	= UISkinManager.defaultSkin.toggle.highlight,
			alignment	= TextAnchor.MiddleLeft,
			fontSize	= UISkinManager.defaultSkin.toggle.fontSize,
			lineHeight	= UISkinManager.defaultSkin.toggle.fontSize + 1,
			fontStyle	= UISkinManager.defaultSkin.toggle.fontStyle,
			wordWrap	= true,
			stretchHeight	= true,
		};

		/// <value>
		/// The skin we use for our tooltip and main window.
		/// </value>
		public static readonly UISkinDef AstrogatorSkin = new UISkinDef() {
			name	= "Astrogator Skin",
			window	= windowStyle,
			box	= UISkinManager.defaultSkin.box,
			font	= UISkinManager.defaultSkin.font,
			label	= subTitleStyle,
			toggle	= toggleStyle,
			button	= UISkinManager.defaultSkin.button,
		};

		/// <value>
		/// Left aligned blue text for a link to the README in the settings.
		/// </value>
		public static readonly UIStyle linkStyle = new UIStyle() {
			normal	= linkFont,
			active	= linkFont,
			disabled	= linkFont,
			highlight	= linkFont,
			alignment	= TextAnchor.MiddleLeft,
			fontSize	= fontSize + 2,
			fontStyle	= FontStyle.BoldAndItalic,
		};

		/// <value>
		/// The skin we use for our tooltip and main window.
		/// </value>
		public static readonly UISkinDef AstrogatorErrorSkin = new UISkinDef() {
			name	= "Astrogator Error Skin",
			window	= windowStyle,
			box	= UISkinManager.defaultSkin.box,
			font	= UISkinManager.defaultSkin.font,
			label	= subTitleErrorStyle,
			toggle	= toggleStyle,
			button	= UISkinManager.defaultSkin.button,
		};

		/// <summary>
		/// Types of columns in our table.
		/// </summary>
		public enum ContentEnum {

			/// <summary>
			/// Left most column, left aligned, sort of a header, contains planet names
			/// </summary>
			PlanetName,

			/// <summary>
			/// First time column
			/// </summary>
			YearsTillBurn,

			/// <summary>
			/// Second time columnm
			/// </summary>
			DaysTillBurn,

			/// <summary>
			/// Third time column
			/// </summary>
			HoursTillBurn,

			/// <summary>
			/// Fourth time column
			/// </summary>
			MinutesTillBurn,

			/// <summary>
			/// Fifth time column
			/// </summary>
			SecondsTillBurn,

			/// <summary>
			/// Delta V column
			/// </summary>
			DeltaV,

			/// <summary>
			/// Burn duration minutes column
			/// </summary>
			DurationMinutes,

			/// <summary>
			/// Burn duration seconds column
			/// </summary>
			DurationSeconds,

			/// <summary>
			/// Maneuver node creation button column
			/// </summary>
			CreateManeuverNodeButton,

			/// <summary>
			/// Warp button column
			/// </summary>
			WarpToBurnButton,
		}

		/// <summary>
		/// A type defining the different sort orders available.
		/// Can't be the same as the column list, because we have
		/// four different columns for time data.
		/// </summary>
		public enum SortEnum {
			/// <summary>
			/// Sort by discovery order; first the satellites of the current
			/// body in inner->outer order, then satellites of its parent, etc.
			/// </summary>
			Position,

			/// <summary>
			/// Sort by name (currently not available in UI)
			/// </summary>
			Name,

			/// <summary>
			/// Sort by time till burn
			/// </summary>
			Time,

			/// <summary>
			/// Sort by delta V
			/// </summary>
			DeltaV,

			/// <summary>
			/// Sort by burn duration
			/// </summary>
			Duration,
		}

		/// <summary>
		/// Structure defining the properties of a column of our table.
		/// </summary>
		public class ColumnDefinition {

			/// <summary>
			/// The string to display at the top of the column
			/// </summary>
			public string header { get; set; }

			/// <summary>
			/// Width of the column
			/// </summary>
			public int width { get; set; }

			/// <summary>
			/// Number of cells occupied horizontally by the header
			/// </summary>
			public int headerColSpan { get; set; }

			/// <summary>
			/// Font, color, and alignment of the header
			/// </summary>
			public UIStyle headerStyle { get; set; }

			/// <summary>
			/// Font, color, and alignment of the normal content
			/// </summary>
			public UIStyle contentStyle { get; set; }

			/// <summary>
			/// How to generate the content for this column
			/// </summary>
			public ContentEnum content { get; set; }

			/// <summary>
			/// True to hide this column when there's no active vessel (tracking station, KSC)
			/// </summary>
			public bool vesselSpecific { get; set; }

			/// <summary>
			/// True to hide this column if maneuver nodes aren't available in this game mode.
			/// </summary>
			public bool requiresPatchedConics { get; set; }

			/// <summary>
			/// Sort order to use when the user clicks the header.
			/// </summary>
			public SortEnum sortKey { get; set; }

			/// <summary>
			/// How wide the column is when rendering in a fixed-width font text screen.
			/// </summary>
			public int monospaceWidth { get; set; }

			/// <summary>
			/// True if this column should be hidden if a transfer doesn't have a definite time
			/// </summary>
			public bool requiresTime { get; set; }
		}

		/// <value>
		/// Columns for our table.
		/// </value>
		public static readonly ColumnDefinition[] Columns = new ColumnDefinition[] {
			new ColumnDefinition() {
				header	= Localizer.Format("astrogator_transferColumnHeader"),
				width	= 80,
				headerColSpan	= 1,
				headerStyle	= leftHdrStyle,
				contentStyle	= planetStyle,
				content	= ContentEnum.PlanetName,
				sortKey	= SortEnum.Position,
				monospaceWidth	= 6,
			}, new ColumnDefinition() {
				header	= Localizer.Format("astrogator_timeColumnHeader"),
				width	= 30,
				headerColSpan	= 5,
				headerStyle	= midHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.YearsTillBurn,
				sortKey	= SortEnum.Time,
				monospaceWidth	= 4,
			}, new ColumnDefinition() {
				header	= "",
				width	= 30,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DaysTillBurn,
				monospaceWidth	= 4,
			}, new ColumnDefinition() {
				header	= "",
				width	= 20,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.HoursTillBurn,
				monospaceWidth	= 3,
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.MinutesTillBurn,
				monospaceWidth	= 3,
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.SecondsTillBurn,
				monospaceWidth	= 3,
			}, new ColumnDefinition() {
				header	= Localizer.Format("astrogator_deltaVColumnHeader"),
				width	= 60,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DeltaV,
				sortKey	= SortEnum.DeltaV,
				monospaceWidth	= 9,
			}, new ColumnDefinition() {
				header	= Localizer.Format("astrogator_durationColumnHeader"),
				width	= 35,
				headerColSpan	= 2,
				headerStyle	= midHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DurationMinutes,
				sortKey	= SortEnum.Duration,
				vesselSpecific	= true,
				requiresTime	= true,
				monospaceWidth	= 0,
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				contentStyle	= numberStyle,
				content	= ContentEnum.DurationSeconds,
				vesselSpecific	= true,
				requiresTime	= true,
				monospaceWidth	= 0,
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= maneuverStyle,
				content	= ContentEnum.CreateManeuverNodeButton,
				vesselSpecific	= true,
				requiresPatchedConics	= true,
				requiresTime	= true,
				monospaceWidth	= 0,
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= warpStyle,
				content	= ContentEnum.WarpToBurnButton,
				requiresTime	= true,
				monospaceWidth	= 0,
			},
		};

		/// <summary>
		/// The width of a row and/or the window.
		/// Calculated from the widths of the columns and the padding.
		/// </summary>
		public static readonly int RowWidthWithVessel = Columns.Select(c => c.width + 6).Sum();

		/// <summary>
		/// Width of row without vessel
		/// </summary>
		public static readonly int RowWidthWithoutVessel = Columns.Where(c => !c.vesselSpecific).Select(c => c.width + 6).Sum();

		/// <summary>
		/// Minimum width of the main window.
		/// </summary>
		public static readonly int mainWindowMinWidthWithVessel = RowWidthWithVessel;

		/// <summary>
		/// Minimum width of the main window without a vessel.
		/// </summary>
		public static readonly int mainWindowMinWidthWithoutVessel = RowWidthWithoutVessel;

		/// <summary>
		/// Minimum height of the main window.
		/// Unity will auto expand it for us! :)
		/// </summary>
		public const int mainWindowMinHeight = 10;

		/// <summary>
		/// Pixels between elements of the window
		/// </summary>
		public const int mainWindowSpacing = 4;

		/// <summary>
		/// Extra space around the edges of the window
		/// </summary>
		public static readonly RectOffset mainWindowPadding = new RectOffset(6, 6, 10, 10);

		/// <summary>
		/// Space around the edges of the settings button (only on top)
		/// </summary>
		public static readonly RectOffset wrenchPadding = new RectOffset(0, 0, 10, 0);

		/// <summary>
		/// Space around the edges of the settings pane
		/// </summary>
		public static readonly RectOffset settingsPadding = new RectOffset(0, 0, 0, 0);

		/// <summary>
		/// Distance from the left inner margin to the right inner margin of main window
		/// </summary>
		public static readonly float mainWindowInternalWidthWithVessel = mainWindowMinWidthWithVessel - mainWindowPadding.left - 2 * mainWindowPadding.right;

		/// <summary>
		/// Distance from the left inner margin to the right inner margin of main window without a vessel
		/// </summary>
		public static readonly float mainWindowInternalWidthWithoutVessel = mainWindowMinWidthWithoutVessel - mainWindowPadding.left - 2 * mainWindowPadding.right;

		/// <summary>
		/// Pixels between elements of the settings
		/// </summary>
		public const int settingsSpacing = 2;

		/// <summary>
		/// Window-relative coordinate of the spot that stays fixed in place when the size changes.
		/// This choice is equivalent to UpperCenter anchoring.
		/// Relates to mainWindowAnchorMax somehow, but I can't tell how.
		/// </summary>
		public static readonly Vector2 mainWindowAnchorMin = new Vector2(0.5f, 1f);

		/// <summary>
		/// Window-relative coordinate of the spot that stays fixed in place when the size changes.
		/// This choice is equivalent to UpperCenter anchoring.
		/// Relates to mainWindowAnchorMin somehow, but I can't tell how.
		/// </summary>
		public static readonly Vector2 mainWindowAnchorMax = new Vector2(0.5f, 1f);

		/// <summary>
		/// A label option for truncating long strings with an ellipsis.
		/// </summary>
		public static readonly DialogGUILabel.TextLabelOptions useEllipsis = new DialogGUILabel.TextLabelOptions() {
			OverflowMode = TMPro.TextOverflowModes.Ellipsis
		};

		/// <summary>
		/// Wrapper around stock label constructor to allow specification of style.
		/// </summary>
		/// <returns>
		/// A label with a parameterized text, style, and size.
		/// </returns>
		/// <param name="message">The text for the label</param>
		/// <param name="style">Visual appearance settings for the label</param>
		/// <param name="width">Width of the label</param>
		/// <param name="height">Height of the label</param>
		public static DialogGUILabel LabelWithStyleAndSize(string message, UIStyle style, float width, float height = -1)
		{
			return new DialogGUILabel(message, width, height) {
				guiStyle = style,
				textLabelOptions = useEllipsis
			};
		}

		/// <summary>
		/// Wrapper around stock label constructor to allow specification of style.
		/// </summary>
		/// <returns>
		/// A label with a parameterized text, style, and size.
		/// </returns>
		/// <param name="getString">A function that returns the text for the label</param>
		/// <param name="style">Visual appearance settings for the label</param>
		/// <param name="width">Width of the label</param>
		/// <param name="height">Height of the label</param>
		public static DialogGUILabel LabelWithStyleAndSize(Func<string> getString, UIStyle style, float width, float height = -1)
		{
			return new DialogGUILabel(getString, width, height) {
				guiStyle = style,
				textLabelOptions = useEllipsis
			};
		}

		/// <returns>
		/// A string formatted according to specifications, or a default string if the value is zero.
		/// </returns>
		/// <param name="fmt">String.Format format string</param>
		/// <param name="val">Value to represent, will be passed to the string formatter</param>
		/// <param name="forceShow">True to always use the given format even if val is 0</param>
		/// <param name="nullString">String to substitute when val is 0 and forceShow is false</param>
		public static string TimePieceString(string fmt, double val, bool forceShow = false, string nullString = "")
		{
			return !forceShow && val == 0
				? nullString
				: Localizer.Format(fmt, val);
		}

		/// <summary>
		/// Create a button that looks like a label
		/// </summary>
		/// <param name="text">String to display</param>
		/// <param name="style">Style to use for the text</param>
		/// <param name="tooltip">Tooltip to use (not currently visible)</param>
		/// <param name="width">Horizontal space to take up</param>
		/// <param name="height">Vertical space to take up</param>
		/// <param name="cb">Function to call when the user clicks the button</param>
		/// <returns>
		/// Button with the given properties
		/// </returns>
		public static DialogGUIBase headerButton(string text, UIStyle style, string tooltip, float width, float height, Callback cb)
		{
			// The 'transparent' Sprite makes the default button borders go away
			return DeferTooltip(new DialogGUIButton(transparent, text, cb, width, height, false) {
				guiStyle    = style,
				tooltipText = tooltip
			});
		}

		/// <returns>
		/// A button with parameterized icon, tooltip, and callback.
		/// </returns>
		/// <param name="icon">Sprite to use</param>
		/// <param name="style">Container for sprites to use on hover, disable, etc.</param>
		/// <param name="tooltip">Value for tooltipText (which doesn't seem to work)</param>
		/// <param name="cb">Function to call when the user clicks the button</param>
		public static DialogGUIBase iconButton(Sprite icon, UIStyle style, string tooltip, Callback cb)
		{
			return DeferTooltip(new DialogGUIButton(icon, cb, buttonIconWidth, buttonIconWidth) {
				guiStyle    = style,
				tooltipText = tooltip
			});
		}

		/// <summary>
		/// Add a button outside of the normal DialogGUI* flow layout,
		/// with positioning relative to edges of a parent element.
		/// By DMagic, with modifications.
		/// </summary>
		/// <param name="parentTransform">Transform of UI object within which to place this button</param>
		/// <param name="innerHorizOffset">Horizontal position; if positive, number of pixels between left edge of window and left edge of button, if negative, then vice versa on right side</param>
		/// <param name="innerVertOffset">Vertical position; if positive, number of pixels between bottom edge of window and bottom edge of button, if negative, then vice versa on top side</param>
		/// <param name="style">Style object containing the sprites for the button</param>
		/// <param name="tooltip">String to show when user hovers on button</param>
		/// <param name="onClick">Function to call when the user clicks the button</param>
		public static void AddFloatingButton(Transform parentTransform, float innerHorizOffset, float innerVertOffset, UIStyle style, string tooltip, UnityAction onClick)
		{
			// This creates a new button object using the prefab from KSP's UISkinManager.
			// The same prefab is used for the PopupDialog system buttons.
			// Anything we set on this object will be reflected in the button we create.
			GameObject btnGameObj = GameObject.Instantiate<GameObject>(UISkinManager.GetPrefab("UIButtonPrefab"));

			// Set the button's parent transform.
			btnGameObj.transform.SetParent(parentTransform, false);

			// Add a layout element and set it to be ignored.
			// Otherwise the button will end up on the bottom of the window.
			btnGameObj.AddComponent<LayoutElement>().ignoreLayout = true;

			// This is how we position the button.
			// The anchors and pivot make the button positioned relative to the top-right corner.
			// The anchored position sets the position with values in pixels.
			RectTransform rect = btnGameObj.GetComponent<RectTransform>();
			rect.anchoredPosition = new Vector2(innerHorizOffset, innerVertOffset);
			rect.sizeDelta        = new Vector2(buttonIconWidth, buttonIconWidth);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(
				rect.anchoredPosition.x < 0 ? 1 : 0,
				rect.anchoredPosition.y < 0 ? 1 : 0
			);

			// Set the button's image component to the normal sprite.
			// Since this object comes from the button's GameObject,
			// changing it affects the button directly!
			Image btnImg  = btnGameObj.GetComponent<Image>();
			btnImg.sprite = style.normal.background;

			// Now set the different states to their respective sprites.
			Button button      = btnGameObj.GetComponent<Button>();
			button.transition  = Selectable.Transition.SpriteSwap;
			button.spriteState = new SpriteState() {
				highlightedSprite = style.highlight.background,
				pressedSprite     = style.active.background,
				disabledSprite    = style.disabled.background
			};

			// The text will be "Button" if we don't clear it.
			btnGameObj.GetChild("Text").GetComponent<TextMeshProUGUI>().text = "";

			// Set the tooltip
			btnGameObj.SetTooltip(tooltip);

			// Set the code to call when clicked.
			button.onClick.AddListener(onClick);

			// Activate the button object, making it visible.
			btnGameObj.SetActive(true);
		}

		/// <summary>
		/// Symbols representing systems of physical units
		/// </summary>
		public enum DisplayUnitsEnum {

			/// <summary>
			/// Système International d'Unités
			/// https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Metric_system_adoption_map.svg/2000px-Metric_system_adoption_map.svg.png
			/// </summary>
			Metric,

			/// <summary>
			/// Time-tested traditional units.
			/// https://i.imgur.com/h5UsGxK.png
			/// </summary>
			UnitedStatesCustomary
		}

		/// <summary>
		/// Format a speed value for display
		/// </summary>
		/// <param name="speed">Speed value in m/s</param>
		/// <param name="units">Type of units to use</param>
		/// <returns>
		/// Converted number string with no decimal places concatenated
		/// with short unit string.
		/// </returns>
		public static string FormatSpeed(double speed, DisplayUnitsEnum units) {
			const double METERS_PER_SECOND_PER_MILES_PER_HOUR = 0.44704;
			switch (units) {
				case DisplayUnitsEnum.UnitedStatesCustomary:
					return Localizer.Format("astrogator_speedUSCustomary", (speed / METERS_PER_SECOND_PER_MILES_PER_HOUR).ToString("0"));
				default:
				case DisplayUnitsEnum.Metric:
					return Localizer.Format("astrogator_speedMetric", speed.ToString("0"));
			}
		}

		/// <summary>
		/// Generate a string describing the state of a model.
		/// </summary>
		/// <param name="model">Model to examine</param>
		/// <returns>
		/// Description of error or warning if applicable,
		/// otherwise "Transfers from X".
		/// </returns>
		public static string ModelDescription(AstrogationModel model)
		{
			if (model == null) {
				return "Internal error: Model not found";
			} else if (model.origin == null) {
				return "Internal error: Model's origin is null";
			} else if (model.hyperbolicOrbit) {
				if (model.inbound) {
					return Localizer.Format(
						"astrogator_inboundHyperbolicWarning",
						TheName(model.origin)
					);
				} else {
					return Localizer.Format(
						"astrogator_outboundHyperbolicError",
						TheName(model.origin)
					);
				}
			} else if (model.badInclination) {
				return Localizer.Format(
					"astrogator_highInclinationError",
					(AngleFromEquatorial(model.origin.GetOrbit().inclination * Mathf.Deg2Rad) * Mathf.Rad2Deg).ToString("0.0"),
					(AstrogationModel.maxInclination * Mathf.Rad2Deg).ToString("0")
				);
			} else if (model.transfers.Count == 0) {
				return Localizer.Format("astrogator_noTransfersError");
			} else if (Landed(model.origin) || solidBodyWithoutVessel(model.origin)) {
				CelestialBody b = model.origin as CelestialBody;
				if (b == null) {
					b = model.origin.GetOrbit().referenceBody;
				}
				return Localizer.Format(
					"astrogator_launchSubtitle",
					TheName(model.origin),
					FormatSpeed(DeltaVToOrbit(b), Settings.Instance.DisplayUnits)
				);
			} else {
				return Localizer.Format("astrogator_normalSubtitle", TheName(model.origin));
			}
		}
	}

	/// <summary>
	/// An object that holds the parts of a time span,
	/// broken down into years, days, hours, minutes, and seconds.
	/// </summary>
	public class DateTimeParts {
		private const double
			minutesPerHour   =  60,
			secondsPerMinute =  60,
			hoursPerDayEarth =  24,
			daysPerYearEarth = 365;

		private static readonly double
			hoursPerDay = solarDayLength(FlightGlobals.GetHomeBody()) / secondsPerMinute / minutesPerHour,
			daysPerYear = FlightGlobals.GetHomeBody().GetOrbit().period / secondsPerMinute / minutesPerHour / hoursPerDay;

		/// <summary>
		/// https://en.wikipedia.org/wiki/Sidereal_time#Sidereal_days_compared_to_solar_days_on_other_planets
		/// </summary>
		private static double solarDayLength(CelestialBody b)
		{
			if (b.rotationPeriod == b.GetOrbit().period) {
				// Tidally locked, don't divide by zero
				return 0;
			} else {
				return b.rotationPeriod / (1 - (b.rotationPeriod / b.GetOrbit().period));
			}
		}

		private static int mod(double numerator, double denominator)
		{
			return (int)Math.Floor(numerator % denominator);
		}

		/// <summary>
		/// Construct an object for the given timestamp.
		/// </summary>
		/// <param name="UT">Seconds since game start</param>
		public DateTimeParts(double UT)
		{
			if (UT == double.PositiveInfinity) {
				Infinite = true;
			} else if (UT == double.NaN) {
				Invalid = true;
			} else {
				totalSeconds = UT;
				seconds = mod(UT, secondsPerMinute);
				UT /= secondsPerMinute;
				totalMinutes = (int)Math.Floor(UT);
				minutes = mod(UT, minutesPerHour);
				UT /= minutesPerHour;
				if (GameSettings.KERBIN_TIME) {
					hours = mod(UT, hoursPerDay);
					UT /= hoursPerDay;
					days = mod(UT, daysPerYear);
					UT /= daysPerYear;
				} else {
					hours = mod(UT, hoursPerDayEarth);
					UT /= hoursPerDayEarth;
					days = mod(UT, daysPerYearEarth);
					UT /= daysPerYearEarth;
				}
				years = (int)Math.Floor(UT);
			}
		}

		/// <summary>
		/// Whether the time represented is infinite,
		/// e.g. for burn duration when we don't have enough delta V
		/// </summary>
		public bool Infinite { get; private set; }

		/// <summary>
		/// Whether the time represented is invalid,
		/// e.g. for burn duration without a vessel
		/// </summary>
		public bool Invalid  { get; private set; }

		/// <summary>
		/// The year component of the given time.
		/// </summary>
		public int years   { get; private set; }

		/// <summary>
		/// The day component of the given time.
		/// </summary>
		public int days    { get; private set; }

		/// <summary>
		/// The hour component of the given time.
		/// </summary>
		public int hours   { get; private set; }

		/// <summary>
		/// The minute component of the given time.
		/// </summary>
		public int minutes { get; private set; }

		/// <summary>
		/// Total time in minutes (includes hours, days, etc.)
		/// </summary>
		public int totalMinutes { get; private set; }

		/// <summary>
		/// Seconds including fraction
		/// </summary>
		public double totalSeconds { get; private set; }

		/// <summary>
		/// The second component of the given time.
		/// </summary>
		public int seconds { get; private set; }

		/// <returns>
		/// True if years must be displayed to correctly represent this time.
		/// </returns>
		public bool needYears   { get { return years   > 0; } }

		/// <returns>
		/// True if days must be displayed to correctly represent this time.
		/// </returns>
		public bool needDays    { get { return days    > 0 || needYears; } }

		/// <returns>
		/// True if hours must be displayed to accurately represent this time.
		/// </returns>
		public bool needHours   { get { return hours   > 0 || needDays; } }

		/// <returns>
		/// True if minutes must be displayed to accurately represent this time.
		/// </returns>
		public bool needMinutes { get { return minutes > 0 || needHours; } }

		// (Seconds must always be displayed.)
	}

}
