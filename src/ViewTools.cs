using System;
using System.Linq;
using UnityEngine;

namespace Astrogator {

	/// Anything UI-related that needs to be used from multiple places.
	public static class ViewTools {

		/// <summary>
		/// Height of a row of our table.
		/// </summary>
		public const int rowHeight = 16;

		/// <summary>
		/// Size of font to use in our table.
		/// </summary>
		public const int fontSize = 12;

		/// <summary>
		/// Size of button icons to use in our table.
		/// </summary>
		public const int buttonIconWidth = 16;

		/// <returns>
		/// The full relative path from the main KSP folder to a given resource from this mod.
		/// </returns>
		public static string FilePath(string filename)
		{
			return String.Format("GameData/{0}/{1}", Astrogator.Name, filename);
		}

		/// <summary>
		/// The icon to show for this mod in the app launcher.
		/// </summary>
		public static Texture2D AppIcon = GetImage(FilePath("Astrogator.png"));

		/// <returns>
		/// A texture object for the image file at the given path.
		/// </returns>
		public static Texture2D GetImage(string filepath)
		{
			Texture2D tex = new Texture2D(38, 38, TextureFormat.ARGB32, false);
			tex.LoadImage(System.IO.File.ReadAllBytes(filepath));
			return tex;
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
			return Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				tex.width
			);
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

		/// <summary>
		/// Icon for normal state of maneuver node creation button.
		/// </summary>
		public static Sprite maneuverIcon = GetSprite(FilePath("maneuver.png"));

		/// <summary>
		/// Icon for hovered state of maneuver node creation button.
		/// </summary>
		public static Sprite maneuverHoverIcon = GetSprite(FilePath("maneuverHover.png"));

		/// <summary>
		/// Icon for normal state of warp button.
		/// </summary>
		public static Sprite warpIcon = GetSprite(FilePath("warp.png"));

		/// <summary>
		/// Icon for hovered state of warp button.
		/// </summary>
		public static Sprite warpHoverIcon = GetSprite(FilePath("warpHover.png"));

		/// <summary>
		/// Black image with 50% opacity.
		/// </summary>
		public static Sprite halfTransparentBlack = SolidColorSprite(new Color(0.0f, 0.0f, 0.0f, 0.5f));

		/// <summary>
		/// Backgrounds and text colors for the tooltip and main window.
		/// </summary>
		public static UIStyleState windowStyleState = new UIStyleState() {
			background	= halfTransparentBlack,
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <summary>
		/// Text color for table headers.
		/// </summary>
		public static UIStyleState headingFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.8f, 0.8f)
		};

		/// <summary>
		/// Text color for main table content.
		/// </summary>
		public static UIStyleState numberFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.8f)
		};

		/// <summary>
		/// Text color for the line under the title.
		/// </summary>
		public static UIStyleState subTitleFont = new UIStyleState() {
			textColor	= Color.HSVToRGB(0.3f, 0.2f, 0.6f)
		};

		/// <summary>
		/// Icon for normal state of maneuver creation button.
		/// </summary>
		public static UIStyleState maneuverStyleState = new UIStyleState() {
			background	= maneuverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Icon for hovered state of maneuver creation button.
		/// </summary>
		public static UIStyleState maneuverHoverStyleState = new UIStyleState() {
			background	= maneuverHoverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Icon for normal state of warp button.
		/// </summary>
		public static UIStyleState warpStyleState = new UIStyleState() {
			background	= warpIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Icon for hovered state of warp button.
		/// </summary>
		public static UIStyleState warpHoverStyleState = new UIStyleState() {
			background	= warpHoverIcon,
			textColor	= Color.black
		};

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for the tooltip and main window.
		/// </summary>
		public static UIStyle windowStyle = new UIStyle() {
			normal	= windowStyleState,
			active	= windowStyleState,
			disabled	= windowStyleState,
			highlight	= windowStyleState,
			alignment	= TextAnchor.UpperCenter,
			fontSize	= UISkinManager.defaultSkin.window.fontSize,
			fontStyle	= FontStyle.Bold,
		};

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for planet header.
		/// </summary>
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

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for the time header.
		/// </summary>
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

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for the delta V header.
		/// </summary>
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

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for the planet names.
		/// </summary>
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

		/// <summary>
		/// Font sizes, normal/highlight styles, and alignment for normal content.
		/// </summary>
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

		/// <summary>
		/// Normal/highlight icons for the maneuver creation button.
		/// </summary>
		public static UIStyle maneuverStyle = new UIStyle() {
			normal	= maneuverStyleState,
			highlight	= maneuverHoverStyleState,
			active	= maneuverHoverStyleState,
			disabled	= maneuverStyleState,
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

		/// <summary>
		/// A centered variant of the normal content font.
		/// </summary>
		public static UIStyle subTitleStyle = new UIStyle() {
			normal	= subTitleFont,
			active	= subTitleFont,
			disabled	= subTitleFont,
			highlight	= subTitleFont,
			alignment	= TextAnchor.MiddleCenter,
			fontSize	= fontSize,
			fontStyle	= FontStyle.Normal,
		};

		/// <summary>
		/// The skin we use for our tooltip and main window.
		/// </summary>
		public static UISkinDef AstrogatorSkin = new UISkinDef() {
			name	= "Astrogator Skin",
			window	= windowStyle,
			box	= UISkinManager.defaultSkin.box,
			font	= UISkinManager.defaultSkin.font,
			label	= subTitleStyle,
		};

		/// <summary>
		/// Columns for our table.
		/// </summary>
		public static ColumnDefinition[] Columns = new ColumnDefinition[] {
			new ColumnDefinition() {
				header	= "",
				width	= 50,
				headerColSpan	= 1,
				headerStyle	= leftHdrStyle,
				contentStyle	= planetStyle,
				content	= ContentEnum.PlanetName
			}, new ColumnDefinition() {
				header	= "Time Till Burn",
				width	= 30,
				headerColSpan	= 5,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.YearsTillBurn
			}, new ColumnDefinition() {
				header	= "",
				width	= 30,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DaysTillBurn
			}, new ColumnDefinition() {
				header	= "",
				width	= 20,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.HoursTillBurn
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.MinutesTillBurn
			}, new ColumnDefinition() {
				header	= "",
				width	= 25,
				headerColSpan	= 0,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.SecondsTillBurn
			}, new ColumnDefinition() {
				header	= "Δv",
				width	= 60,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= numberStyle,
				content	= ContentEnum.DeltaV
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= maneuverStyle,
				content	= ContentEnum.CreateManeuverNodeButton,
				vesselSpecific	= true,
			}, new ColumnDefinition() {
				header	= "",
				width	= buttonIconWidth,
				headerColSpan	= 1,
				headerStyle	= rightHdrStyle,
				contentStyle	= warpStyle,
				content	= ContentEnum.WarpToBurnButton
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
		/// Pixels between elements of the window (?)
		/// </summary>
		public const int mainWindowSpacing = 4;

		/// <summary>
		/// Extra space around the edges of the window (?)
		/// </summary>
		public static RectOffset mainWindowPadding = new RectOffset(6, 6, 10, 10);

		/// <summary>
		/// Window-relative coordinate of the spot that stays fixed in place when the size changes.
		/// This choice is equivalent to UpperCenter anchoring.
		/// </summary>
		public static Vector2 mainWindowAnchor = new Vector2(0.5f, 1f);

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
			return new DialogGUILabel(message, width, height) { guiStyle = style };
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
			return new DialogGUILabel(getString, width, height) { guiStyle = style };
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
				return String.Format(fmt, val);
			}
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
	}

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
