using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;

	/// <summary>
	/// https://github.com/Mihara/RasterPropMonitor/wiki/Page-handlers
	/// </summary>
	public class AstrogatorMenu : InternalModule {

		AstrogatorMenu()
			: base()
		{
			model = new AstrogationModel(
				   (ITargetable)FlightGlobals.ActiveVessel
				?? (ITargetable)FlightGlobals.getMainBody());
			loader = new AstrogationLoadBehaviorette(model, null);
			timeToWait = new List<DateTimeParts>();
			cursorTransfer = 0;

			loader.TryStartLoad(model.origin, null, null, null);
		}

		/// <summary>
		/// Key code for the upward pointing wedge button, overridable by RPM configuration.
		/// We use it to move the selection up by one row.
		/// </summary>
		[KSPField]
		public int buttonUp = 0;

		/// <summary>
		/// Key code for the downward pointing wedge button, overridable by RPM configuration.
		/// We use it to move the selection down by one row.
		/// </summary>
		[KSPField]
		public int buttonDown = 1;

		/// <summary>
		/// Key code for the left arrow button, overridable by RPM configuration.
		/// We use it to create maneuvers for the selected transfer.
		/// </summary>
		[KSPField]
		public int buttonEnter = 2;

		/// <summary>
		/// Key code for the X button, overridable by RPM configuration.
		/// We use it to delete all active maneuvers.
		/// </summary>
		[KSPField]
		public int buttonEsc = 3;

		/// <summary>
		/// Key code for the circle button, overridable by RPM configuration.
		/// We use it to warp to the selected transfer.
		/// </summary>
		[KSPField]
		public int buttonHome = 4;

		/// <summary>
		/// Key code for the rightward pointing wedge button, overridable by RPM configuration.
		/// We use it to scroll to the next page of transfers, if any.
		/// </summary>
		[KSPField]
		public int buttonRight = 5;

		/// <summary>
		/// Key code for the leftward pointing wedge button, overridable by RPM configuration.
		/// We use it to scroll to the previous page of transfers, if any.
		/// </summary>
		[KSPField]
		public int buttonLeft = 6;

		private AstrogationModel            model             { get; set; }
		private AstrogationLoadBehaviorette loader            { get; set; }
		private List<DateTimeParts>         timeToWait        { get; set; }
		private double                      lastUniversalTime { get; set; }
		private int                         cursorTransfer    { get; set; }
		private bool                        cursorMoved       { get; set; }
		private string                      menu              { get; set; }
		private int?                        activeButton      { get; set; }
		private int                         rowsPerPage       { get; set; }
		private List<TransferModel>         transfers         { get; set; }

		private void addHeaders(StringBuilder sb)
		{
			bool firstCol = true;
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];
				int width = 0;
				for (int span = 0; span < col.headerColSpan; ++span) {
					width += Columns[i + span].monospaceWidth;
				}
				if (width > 0) {
					width += (col.headerColSpan - 1);
					if (firstCol) {
						firstCol = false;
						width += 2;
					}
					switch (col.headerStyle.alignment) {
						case TextAnchor.LowerLeft:
							sb.AppendFormat(
								string.Format("{0}{1}0,-{2}{3}", styleColorString(col.headerStyle), "{", width, "}"),
								col.header
							);
							break;
						case TextAnchor.LowerCenter:
							sb.Append(centerString(col.header, width));
							break;
						case TextAnchor.LowerRight:
							sb.AppendFormat(
								string.Format("{0}{1}0,{2}{3}", styleColorString(col.headerStyle), "{", width, "}"),
								col.header
							);
							break;
					}
					sb.Append(" ");
				}
			}
		}

		private const string LoadingText = "---";

		private string styleColorString(UIStyle style)
		{
			// [#rrggbbaa]
			Color c = style.normal.textColor;
			return string.Format(
				"[#{0,2:X}{1,2:X}{2,2:X}ff]",
				(int)Math.Floor(255 * c.r),
				(int)Math.Floor(255 * c.g),
				(int)Math.Floor(255 * c.b)
			);
		}

		private string colContentFormat(ColumnDefinition col)
		{
			switch (col.contentStyle.alignment) {

				case TextAnchor.MiddleLeft:
					return string.Format("{0}{1}0,-{2}{3}", styleColorString(col.contentStyle), "{", col.monospaceWidth, "}");

				case TextAnchor.MiddleRight:
					return string.Format("{0}{1}0,{2}{3}", styleColorString(col.contentStyle), "{", col.monospaceWidth, "}");

			}
			return "{0}";
		}

		private void addRow(StringBuilder sb, TransferModel m, DateTimeParts dt, bool selected)
		{
			string destLabel = Localizer.Format("astrogatr_planetLabel", TheName(m.destination));

			sb.Append(Environment.NewLine);
			sb.Append(selected ? "> " : "  ");
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];

				switch (col.content) {
					case ContentEnum.PlanetName:
						sb.AppendFormat(
							colContentFormat(col),
							(destLabel.Length > col.monospaceWidth ? destLabel.Substring(0, col.monospaceWidth) : destLabel)
						);
						break;

					case ContentEnum.YearsTillBurn:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								TimePieceString("astrogator_yearsValue", dt.years, dt.needYears)
							);
						}
						break;

					case ContentEnum.DaysTillBurn:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								TimePieceString("astrogator_daysValue", dt.days, dt.needDays)
							);
						}
						break;

					case ContentEnum.HoursTillBurn:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								TimePieceString("astrogator_hoursValue", dt.hours, dt.needHours)
							);
						}
						break;

					case ContentEnum.MinutesTillBurn:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								TimePieceString("astrogator_minutesValue", dt.minutes, dt.needMinutes)
							);
						}
						break;

					case ContentEnum.SecondsTillBurn:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								TimePieceString("astrogator_secondsValue", dt.seconds, true)
							);
						}
						break;

					case ContentEnum.DeltaV:
						if (dt == null) {
							sb.AppendFormat(colContentFormat(col), LoadingText);
						} else {
							sb.AppendFormat(
								colContentFormat(col),
								FormatSpeed(
									((m.planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV)
										? m.ejectionBurn?.totalDeltaV
										: (m.ejectionBurn?.totalDeltaV + m.planeChangeBurn.totalDeltaV)) ?? 0,
									Settings.Instance.DisplayUnits)
							);
						}
						break;

				}
				sb.Append(" ");
			}
		}

		/// <summary>
		/// Generate a text string representing the current transfers.
		/// Called by RasterPropMonitor based on our cfg file.
		/// Line 1: Branding title
		/// Line 2: Subtitle, centered and gray
		/// Line 3: Blank
		/// Line 4: Table headers
		/// Line 5+: Transfer info
		/// </summary>
		/// <param name="columns">Number of characters from left edge to right edges</param>
		/// <param name="rows">Number of characters from top edge to bottom edge</param>
		/// <returns>
		/// A string to be displayed on a monitor in IVA.
		/// </returns>
		public string ShowMenu(int columns, int rows)
		{
			if ((Refresh() || cursorMoved) && transfers.Count == timeToWait.Count) {

				StringBuilder sb = new StringBuilder();
				sb.Append(centerString(" " + Localizer.Format("astrogator_mainTitle") + " " + versionString + " ", columns, '-'));
				sb.Append(Environment.NewLine);
				sb.Append("[#a0a0a0ff]");
				sb.Append(centerString(Localizer.Format("astrogator_normalSubtitle", TheName(model.origin)), columns));
				sb.Append(Environment.NewLine);
				sb.Append(Environment.NewLine);

				// [#rrggbbaa]
				addHeaders(sb);

				// Wrap the cursor around the edges now because it only tells us dimensions here.
				while (cursorTransfer < 0) {
					cursorTransfer += transfers.Count;
				}
				while (cursorTransfer >= transfers.Count) {
					cursorTransfer -= transfers.Count;
				}

				rowsPerPage = rows - 4;
				int screenPage = cursorTransfer / rowsPerPage;
				for (int t = screenPage * rowsPerPage, r = 0;
						t < transfers.Count && r < rowsPerPage;
						++t, ++r) {
					addRow(sb, transfers[t], timeToWait[t], (cursorTransfer == t));
				}
				menu = sb.ToString();
				cursorMoved = false;
			}
			return menu;
		}

		/// <summary>
		/// Turn data loading on and off.
		/// Called by RasterPropMonitor based on our cfg file to tell us we're visible or invisible.
		/// </summary>
		/// <param name="pageActive">True if active, false otherwise</param>
		/// <param name="pageNumber">A number that's meaningful to RasterPropMonitor but not to us</param>
		public void PageActive(bool pageActive, int pageNumber)
		{
			if (pageActive) {
				loader.OnDisplayOpened();
				loader.TryStartLoad(model.origin, null, null, null);
			} else {
				loader.OnDisplayClosed();
			}
		}

		private string centerString(string val, int columns, char padding = ' ')
		{
			int numPads = columns - val.Length;
			return val.PadLeft(columns - numPads/2, padding).PadRight(columns, padding);
		}

		/// <summary>
		/// React to the user pressing buttons on the multifunction display.
		/// Called by RasterPropMonitor based on our cfg file.
		/// </summary>
		/// <param name="buttonNumber">Which button was pressed, to be compared to the button* properties with KSPField attributes</param>
		public void ButtonClick(int buttonNumber)
		{
			DbgFmt("ButtonClick: {0}", buttonNumber);
			activeButton = buttonNumber;

			if (activeButton == buttonUp) {
				--cursorTransfer;
				cursorMoved = true;
			} else if (activeButton == buttonDown) {
				++cursorTransfer;
				cursorMoved = true;
			} else if (activeButton == buttonEnter) {
				transfers[cursorTransfer].CreateManeuvers();
			} else if (activeButton == buttonEsc) {
				ClearManeuverNodes();
			} else if (activeButton == buttonHome) {
				transfers[cursorTransfer].WarpToBurn();
			} else if (activeButton == buttonLeft) {
				cursorTransfer -= rowsPerPage;
				if (cursorTransfer < 0) {
					cursorTransfer = 0;
				}
				cursorMoved = true;
			} else if (activeButton == buttonRight) {
				cursorTransfer += rowsPerPage;
				if (cursorTransfer >= transfers.Count) {
					cursorTransfer = transfers.Count - 1;
				}
				cursorMoved = true;
			}
		}

		/// <summary>
		/// React to the user releasing buttons on the multifunction display.
		/// Called by RasterPropMonitor based on our cfg file.
		/// </summary>
		/// <param name="buttonNumber">Which button was released, to be compared to the button* properties with KSPField attributes</param>
		public void ButtonRelease(int buttonNumber)
		{
			DbgFmt("ButtonRelease: {0}", buttonNumber);
			activeButton = null;
		}

		private bool Refresh()
		{
			double now = Math.Floor(Planetarium.GetUniversalTime());
			if (lastUniversalTime != now) {

				transfers = SortTransfers(
					model,
					Settings.Instance.TransferSort,
					Settings.Instance.DescendingSort
				);

				timeToWait = new List<DateTimeParts>();
				for (int i = 0; i < transfers.Count; ++i) {

					if (transfers[i].ejectionBurn != null && transfers[i].ejectionBurn.atTime != null) {
						timeToWait.Add(new DateTimeParts((transfers[i].ejectionBurn.atTime ?? 0) - Planetarium.GetUniversalTime()));
					} else {
						timeToWait.Add(null);
					}

				}
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

	}

}
