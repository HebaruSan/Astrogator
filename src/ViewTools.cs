using System;
using System.Linq;
using UnityEngine;
using KSP.UI.TooltipTypes;

namespace Astrogator {

	using static DebugTools;

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

		/// <returns>
		/// The full relative path from the main KSP folder to a given resource from this mod.
		/// </returns>
		/// <param name="filename">Name of file located in our plugin folder</param>
		/// <param name="GameDataRelative">True if the KSP/GameData portion of the path is assumed, false if we need to provide the full path</param>
		public static string FilePath(string filename, bool GameDataRelative = true)
		{
			if (GameDataRelative) {
				return string.Format("{0}/{1}", Astrogator.Name, filename);
			} else {
				return string.Format("{0}/{1}",
					System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					filename);
			}
		}

		/// <summary>
		/// Parse a string into an enum for Settings
		/// Inverse of Enum.ToString()
		/// </summary>
		/// <param name="val">String from the settings</param>
		/// <param name="defaultVal">Default to use if can't match to any value from the enum</param>
		/// <returns>
		/// Enum value matching the string, if any
		/// </returns>
		public static T ParseEnum<T>(string val, T defaultVal) where T : IConvertible
		{
			try {
				return (T) Enum.Parse(typeof(T), val, true);
			} catch (Exception ex) {
				DbgExc("Problem parsing enum", ex);
				return defaultVal;
			}
		}

		/// <value>
		/// The icon to show for this mod in the app launcher.
		/// </value>
		public static Texture2D AppIcon = GetImage(FilePath("Astrogator"));

		/// <returns>
		/// A texture object for the image file at the given path.
		/// </returns>
		/// <param name="filepath">Path to image file to load</param>
		public static Texture2D GetImage(string filepath)
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
		public static Texture2D SolidColorTexture(Color c)
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
		public static Sprite GetSprite(string filepath)
		{
			return SpriteFromTexture(GetImage(filepath));
		}

		/// <returns>
		/// A 1x1 sprite object of the given color.
		/// </returns>
		public static Sprite SolidColorSprite(Color c)
		{
			return SpriteFromTexture(SolidColorTexture(c));
		}

		/// <value>
		/// Black image with 50% opacity.
		/// </value>
		public static Sprite halfTransparentBlack = SolidColorSprite(new Color(0f, 0f, 0f, 0.5f));

		/// <value>
		/// Completely transparent sprite so we can use buttons for the headers
		/// without the default button graphic.
		/// </value>
		public static Sprite transparent = SolidColorSprite(new Color(0f, 0f, 0f, 0f));

		/// <value>
		/// Backgrounds and text colors for the tooltip and main window.
		/// </value>
		public static UIStyleState windowStyleState = new UIStyleState() {
			background	= halfTransparentBlack,
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <value>
		/// Text color for table headers.
		/// </value>
		public static UIStyleState headingFont = new UIStyleState() {
			background	= transparent,
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <value>
		/// Text color for main table content.
		/// </value>
		public static UIStyleState numberFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.8f)
		};

		/// <value>
		/// Text color for the line under the title.
		/// </value>
		public static UIStyleState subTitleFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.6f)
		};

		/// <value>
		/// Text color for the line under the title when it's an error message.
		/// </value>
		public static UIStyleState subTitleErrorFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0f, 0.9f, 0.9f)
		};

		/// <value>
		/// Text color for the line under the title.
		/// </value>
		public static UIStyleState linkFont = new UIStyleState() {
			background	= transparent,
			textColor	= Color.HSVToRGB(0.6f, 0.7f, 0.9f)
		};

		/// <value>
		/// Font sizes, normal/highlight styles, and alignment for the tooltip and main window.
		/// </value>
		public static UIStyle windowStyle = new UIStyle() {
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
		public static UIStyle leftHdrStyle = new UIStyle() {
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
		public static UIStyle midHdrStyle = new UIStyle() {
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
		public static UIStyle rightHdrStyle = new UIStyle() {
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
		public static UIStyle planetStyle = new UIStyle() {
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
		public static UIStyle numberStyle = new UIStyle() {
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
		public static Sprite settingsIcon = GetSprite(FilePath("settings"));

		/// <value>
		/// Icon for normal state of settings button.
		/// </value>
		public static UIStyleState settingsStyleState = new UIStyleState() {
			background	= settingsIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of settings button.
		/// </value>
		public static Sprite settingsHoverIcon = GetSprite(FilePath("settingsHover"));

		/// <value>
		/// Icon for hovered state of settings button.
		/// </value>
		public static UIStyleState settingsHoverStyleState = new UIStyleState() {
			background	= settingsHoverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Normal/highlight icons for the settings button.
		/// </value>
		public static UIStyle settingsStyle = new UIStyle() {
			normal	= settingsStyleState,
			highlight	= settingsHoverStyleState,
			active	= settingsHoverStyleState,
			disabled	= settingsStyleState,
			alignment	 = TextAnchor.UpperRight,
		};

		/// <value>
		/// Icon for normal state of maneuver node creation button.
		/// </value>
		public static Sprite maneuverIcon = GetSprite(FilePath("maneuver"));

		/// <value>
		/// Icon for normal state of maneuver creation button.
		/// </value>
		public static UIStyleState maneuverStyleState = new UIStyleState() {
			background	= maneuverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of maneuver node creation button.
		/// </value>
		public static Sprite maneuverHoverIcon = GetSprite(FilePath("maneuverHover"));

		/// <value>
		/// Icon for hovered state of maneuver creation button.
		/// </value>
		public static UIStyleState maneuverHoverStyleState = new UIStyleState() {
			background	= maneuverHoverIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Normal/highlight icons for the maneuver creation button.
		/// </value>
		public static UIStyle maneuverStyle = new UIStyle() {
			normal	= maneuverStyleState,
			highlight	= maneuverHoverStyleState,
			active	= maneuverHoverStyleState,
			disabled	= maneuverStyleState,
		};

		/// <value>
		/// Icon for normal state of warp button.
		/// </value>
		public static Sprite warpIcon = GetSprite(FilePath("warp"));

		/// <value>
		/// Icon for normal state of warp button.
		/// </value>
		public static UIStyleState warpStyleState = new UIStyleState() {
			background	= warpIcon,
			textColor	= Color.black
		};

		/// <value>
		/// Icon for hovered state of warp button.
		/// </value>
		public static Sprite warpHoverIcon = GetSprite(FilePath("warpHover"));

		/// <value>
		/// Icon for hovered state of warp button.
		/// </value>
		public static UIStyleState warpHoverStyleState = new UIStyleState() {
			background	= warpHoverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Normal/highlight icons for the warp button.
		/// </summary>
		public static UIStyle warpStyle = new UIStyle() {
			normal	= warpStyleState,
			highlight	= warpHoverStyleState,
			active	= warpHoverStyleState,
			disabled	= warpStyleState,
		};

		/// <value>
		/// A centered variant of the normal content font.
		/// </value>
		public static UIStyle subTitleStyle = new UIStyle() {
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
		public static UIStyle notificationStyle = new UIStyle() {
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
		public static UIStyle subTitleErrorStyle = new UIStyle() {
			normal	= subTitleErrorFont,
			active	= subTitleErrorFont,
			disabled	= subTitleErrorFont,
			highlight	= subTitleErrorFont,
			alignment	= TextAnchor.MiddleCenter,
			fontSize	= windowStyle.fontSize,
			fontStyle	= FontStyle.Bold,
		};

		/// <value>
		/// The skin we use for our tooltip and main window.
		/// </value>
		public static UISkinDef AstrogatorSkin = new UISkinDef() {
			name	= "Astrogator Skin",
			window	= windowStyle,
			box	= UISkinManager.defaultSkin.box,
			font	= UISkinManager.defaultSkin.font,
			label	= subTitleStyle,
			toggle	= UISkinManager.defaultSkin.toggle,
		};

		/// <value>
		/// Left aligned blue text for a link to the README in the settings.
		/// </value>
		public static UIStyle linkStyle = new UIStyle() {
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
		public static UISkinDef AstrogatorErrorSkin = new UISkinDef() {
			name	= "Astrogator Error Skin",
			window	= windowStyle,
			box	= UISkinManager.defaultSkin.box,
			font	= UISkinManager.defaultSkin.font,
			label	= subTitleErrorStyle,
			toggle	= UISkinManager.defaultSkin.toggle,
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
			DeltaV
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
		}

		/// <value>
		/// Columns for our table.
		/// </value>
		public static ColumnDefinition[] Columns = new ColumnDefinition[] {
			new ColumnDefinition() {
				header	= "Transfer",
				width	= 60,
				headerColSpan	= 1,
				headerStyle	= leftHdrStyle,
				contentStyle	= planetStyle,
				content	= ContentEnum.PlanetName,
				sortKey	= SortEnum.Position,
				monospaceWidth	= 7
			}, new ColumnDefinition() {
				header	= "Time Till Burn",
				width	= 30,
				headerColSpan	= 5,
				headerStyle	= midHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.YearsTillBurn,
				sortKey	= SortEnum.Time,
				monospaceWidth	= 4
			}, new ColumnDefinition() {
				header	= "",
				width	= 30,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DaysTillBurn,
				monospaceWidth	= 4
			}, new ColumnDefinition() {
				header	= "",
				width	= 20,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.HoursTillBurn,
				monospaceWidth = 2
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.MinutesTillBurn,
				monospaceWidth	= 3
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.SecondsTillBurn,
				monospaceWidth	= 3,
			}, new ColumnDefinition() {
				header	= "Δv",
				width	= 60,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DeltaV,
				sortKey	= SortEnum.DeltaV,
				monospaceWidth	= 9
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= maneuverStyle,
				content	= ContentEnum.CreateManeuverNodeButton,
				vesselSpecific	= true,
				requiresPatchedConics	= true,
				monospaceWidth	= 0,
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= warpStyle,
				content	= ContentEnum.WarpToBurnButton,
				monospaceWidth = 0
			},
		};

		/// <summary>
		/// The width of a row and/or the window.
		/// Calculated from the widths of the columns and the padding.
		/// </summary>
		public static int RowWidth = Columns.Select(x => x.width + 6).Sum();

		/// <summary>
		/// Minimum width of the main window.
		/// </summary>
		public static int mainWindowMinWidth = RowWidth;

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
		public static RectOffset mainWindowPadding = new RectOffset(6, 6, 10, 10);

		/// <summary>
		/// Space around the edges of the settings button (only on top)
		/// </summary>
		public static RectOffset wrenchPadding = new RectOffset(0, 0, 10, 0);

		/// <summary>
		/// Space around the edges of the settings pane
		/// </summary>
		public static RectOffset settingsPadding = new RectOffset(0, 0, 0, 0);

		/// <summary>
		/// Window-relative coordinate of the spot that stays fixed in place when the size changes.
		/// This choice is equivalent to UpperCenter anchoring.
		/// Relates to mainWindowAnchorMax somehow, but I can't tell how.
		/// </summary>
		public static Vector2 mainWindowAnchorMin = new Vector2(0.5f, 1f);

		/// <summary>
		/// Window-relative coordinate of the spot that stays fixed in place when the size changes.
		/// This choice is equivalent to UpperCenter anchoring.
		/// Relates to mainWindowAnchorMin somehow, but I can't tell how.
		/// </summary>
		public static Vector2 mainWindowAnchorMax = new Vector2(0.5f, 1f);

		/// <summary>
		/// A label option for truncating long strings with an ellipsis.
		/// </summary>
		public static DialogGUILabel.TextLabelOptions useEllipsis = new DialogGUILabel.TextLabelOptions() {
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
		public static DialogGUILabel LabelWithStyleAndSize(string message, UIStyle style, float width, float height)
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
		public static DialogGUILabel LabelWithStyleAndSize(Func<string> getString, UIStyle style, float width, float height)
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
			if (!forceShow && val == 0) {
				return nullString;
			} else {
				return string.Format(fmt, val);
			}
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
		public static DialogGUIButton headerButton(string text, UIStyle style, string tooltip, float width, float height, Callback cb)
		{
			// The 'transparent' Sprite makes the default button borders go away
			return new DialogGUIButton(transparent, text, cb, width, height, false) {
				guiStyle    = style,
				tooltipText = tooltip
			};
		}

		/// <returns>
		/// A button with parameterized icon, tooltip, and callback.
		/// </returns>
		/// <param name="icon">Sprite to use</param>
		/// <param name="style">Container for sprites to use on hover, disable, etc.</param>
		/// <param name="tooltip">Value for tooltipText (which doesn't seem to work)</param>
		/// <param name="cb">Function to call when the user clicks the button</param>
		public static DialogGUIButton iconButton(Sprite icon, UIStyle style, string tooltip, Callback cb)
		{
			return new DialogGUIButton(icon, cb, buttonIconWidth, buttonIconWidth) {
				guiStyle    = style,
				tooltipText = tooltip
			};
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
					return string.Format("{0:0} mph", speed / METERS_PER_SECOND_PER_MILES_PER_HOUR);
				default:
				case DisplayUnitsEnum.Metric:
					return string.Format("{0:0} m/s", speed);
			}
		}

	}

	/// <summary>
	/// An object that holds the parts of a time span,
	/// broken down into years, days, hours, minutes, and seconds.
	/// </summary>
	public class DateTimeParts {
		private const double
			daysPerYear      = 426,
			hoursPerDay      =   6,
			minutesPerHour   =  60,
			secondsPerMinute =  60;

		private int mod(double numerator, double denominator)
		{
			return (int)Math.Floor(numerator % denominator);
		}

		/// <summary>
		/// Construct an object for the given timestamp.
		/// </summary>
		public DateTimeParts(double UT)
		{
			seconds = mod(UT, secondsPerMinute);
			UT /= secondsPerMinute;
			minutes = mod(UT, minutesPerHour);
			UT /= minutesPerHour;
			hours = mod(UT, hoursPerDay);
			UT /= hoursPerDay;
			days = mod(UT, daysPerYear);
			UT /= daysPerYear;
			years = (int)Math.Floor(UT);
		}

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
