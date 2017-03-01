using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

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

		[KSPField]
		public int buttonUp = 0;

		[KSPField]
		public int buttonDown = 1;

		[KSPField]
		public int buttonEnter = 2;

		[KSPField]
		public int buttonEsc = 3;

		[KSPField]
		public int buttonHome = 4;

		private AstrogationModel            model             { get; set; }
		private AstrogationLoadBehaviorette loader            { get; set; }
		private List<DateTimeParts>         timeToWait        { get; set; }
		private double                      lastUniversalTime { get; set; }
		private int                         cursorTransfer    { get; set; }
		private bool                        cursorMoved       { get; set; }
		private string                      menu              { get; set; }
		private int?                        activeButton      { get; set; }

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
								string.Format("{0}0,-{1}{2}", "{", width, "}"),
								col.header
							);
							break;
						case TextAnchor.LowerCenter:
							sb.Append(centerString(col.header, width));
							break;
						case TextAnchor.LowerRight:
							sb.AppendFormat(
								string.Format("{0}0,{1}{2}", "{", width, "}"),
								col.header
							);
							break;
					}
					sb.Append(" ");
				}
			}
		}

		private string colContentFormat(ColumnDefinition col)
		{
			switch (col.contentStyle.alignment) {
				case TextAnchor.MiddleLeft:
					return string.Format("{0}0,-{1}{2}", "{", col.monospaceWidth, "}");
					break;
				case TextAnchor.MiddleRight:
					return string.Format("{0}0,{1}{2}", "{", col.monospaceWidth, "}");
					break;
			}
			return "{0}";
		}

		private void addRow(StringBuilder sb, TransferModel m, DateTimeParts dt, bool selected)
		{
			string destLabel = CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(TheName(m.destination));

			sb.Append(Environment.NewLine);
			sb.Append(selected ? "> " : "  ");
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];
				// TODO - check style's text color and convert to [#rrggbbaa]
				switch (col.content) {
					case ContentEnum.PlanetName:
						sb.AppendFormat(
							colContentFormat(col),
							(destLabel.Length > col.monospaceWidth ? destLabel.Substring(0, col.monospaceWidth) : destLabel)
						);
						break;

					case ContentEnum.YearsTillBurn:
						sb.AppendFormat(
							colContentFormat(col),
							TimePieceString("{0}y", dt.years, dt.needYears)
						);
						break;

					case ContentEnum.DaysTillBurn:
						sb.AppendFormat(
							colContentFormat(col),
							TimePieceString("{0}d", dt.days, dt.needDays)
						);
						break;

					case ContentEnum.HoursTillBurn:
						sb.AppendFormat(
							colContentFormat(col),
							TimePieceString("{0}h", dt.hours, dt.needHours)
						);
						break;

					case ContentEnum.MinutesTillBurn:
						sb.AppendFormat(
							colContentFormat(col),
							TimePieceString("{0}m", dt.minutes, dt.needMinutes)
						);
						break;

					case ContentEnum.SecondsTillBurn:
						sb.AppendFormat(
							colContentFormat(col),
							TimePieceString("{0}s", dt.seconds, true)
						);
						break;

					case ContentEnum.DeltaV:
						sb.AppendFormat(
							colContentFormat(col),
							FormatSpeed(
								((m.planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV)
									? m.ejectionBurn?.totalDeltaV
									: (m.ejectionBurn?.totalDeltaV + m.planeChangeBurn.totalDeltaV)) ?? 0,
								Settings.Instance.DisplayUnits)
						);
						break;

				}
				sb.Append(" ");
			}
		}

		public string ShowMenu(int columns, int rows)
		{
			if ((Refresh() || cursorMoved) && model.transfers.Count == timeToWait.Count) {
				StringBuilder sb = new StringBuilder();
				sb.Append(centerString(" " + AstrogationView.DisplayName + " ", columns, '-'));
				sb.Append(Environment.NewLine);
				sb.Append("[#a0a0a0ff]");
				sb.Append(centerString(String.Format("Transfers from {0}", TheName(model.origin)), columns));
				sb.Append(Environment.NewLine);
				sb.Append(Environment.NewLine);

				// [#rrggbbaa]
				sb.Append("[#22ff22ff]");
				addHeaders(sb);

				// Wrap the cursor around the edges now because it only tells us dimensions here.
				while (cursorTransfer < 0) {
					cursorTransfer += model.transfers.Count;
				}
				while (cursorTransfer >= model.transfers.Count) {
					cursorTransfer -= model.transfers.Count;
				}
				// TODO - handle multiple pages of transfers

				for (int i = 0; i < model.transfers.Count && i < rows - 4; ++i) {
					addRow(sb, model.transfers[i], timeToWait[i], (cursorTransfer == i));
				}
				menu = sb.ToString();
				cursorMoved = false;
			}
			return menu;
		}

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
				model.transfers[cursorTransfer].CreateManeuvers();
			} else if (activeButton == buttonEsc) {
				ClearManeuverNodes();
			} else if (activeButton == buttonHome) {
				model.transfers[cursorTransfer].WarpToBurn();
			}
		}

		public void ButtonRelease(int buttonNumber)
		{
			DbgFmt("ButtonRelease: {0}", buttonNumber);
			activeButton = null;
		}

		private bool Refresh()
		{
			double now = Math.Floor(Planetarium.GetUniversalTime());
			if (lastUniversalTime != now) {
				timeToWait = new List<DateTimeParts>();
				for (int i = 0; i < model.transfers.Count; ++i) {

					if (model.transfers[i].ejectionBurn != null && model.transfers[i].ejectionBurn.atTime != null) {
						timeToWait.Add(new DateTimeParts((model.transfers[i].ejectionBurn.atTime ?? 0) - Planetarium.GetUniversalTime()));
					} else {
						timeToWait.Add(new DateTimeParts(0));
					}

				}
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

	}

}
