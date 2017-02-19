using System;
using System.Globalization;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;
	using static ViewTools;

	/// A class that displays a given transfer's info.
	/// Corresponds to one row of the main window.
	public class TransferView : DialogGUIHorizontalLayout {

		/// <summary>
		/// Construct a view for the given model.
		/// </summary>
		/// <param name="m">Model for which to construct a view</param>
		/// <param name="reset">Callback to call when a UI layout change may be needed</param>
		public TransferView(TransferModel m, AstrogationView.ResetCallback reset)
			: base()
		{
			model = m;
			resetCallback = reset;

			CreateLayout();
		}

		private TransferModel                 model             { get; set; }
		private double                        lastUniversalTime { get; set; }
		private DateTimeParts                 timeToWait        { get; set; }
		private AstrogationView.ResetCallback resetCallback     { get; set; }

		private void CreateLayout()
		{
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];

				// Skip columns that require an active vessel if we don't have one
				if (col.vesselSpecific && FlightGlobals.ActiveVessel == null) {
					continue;
				}

				// Skip columns that require maneuver nodes if they're not unlocked
				if (col.requiresPatchedConics
						&& (!patchedConicsUnlocked() || model.Landed)) {
					continue;
				}

				switch (col.content) {

					case ContentEnum.PlanetName:
						AddChild(LabelWithStyleAndSize(CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(TheName(model.destination)),
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.YearsTillBurn:
						AddChild(LabelWithStyleAndSize(getYearValue,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.DaysTillBurn:
						AddChild(LabelWithStyleAndSize(getDayValue,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.HoursTillBurn:
						AddChild(LabelWithStyleAndSize(getHourValue,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.MinutesTillBurn:
						AddChild(LabelWithStyleAndSize(getMinuteValue,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.SecondsTillBurn:
						AddChild(LabelWithStyleAndSize(getSecondValue,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.DeltaV:
						AddChild(LabelWithStyleAndSize(getDeltaV,
							col.contentStyle, col.width, rowHeight));
						break;

					case ContentEnum.CreateManeuverNodeButton:
						AddChild(iconButton(maneuverIcon,
							col.contentStyle, "Create maneuver", model.CreateManeuvers));
						break;

					case ContentEnum.WarpToBurnButton:
						AddChild(iconButton(warpIcon,
							col.contentStyle, "Warp to window", model.WarpToBurn));
						break;

				}
			}
		}

		/// <summary>
		/// Update the data we display.
		/// </summary>
		/// <returns>
		/// True if the display needs to be refreshed, false otherwise.
		/// </returns>
		public bool Refresh()
		{
			double now = Math.Floor(Planetarium.GetUniversalTime());

			if (lastUniversalTime != now && model.ejectionBurn != null) {
				timeToWait = new DateTimeParts(model.ejectionBurn.atTime - Planetarium.GetUniversalTime());
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

		private const string LoadingText = "---";

		private bool showLoadingText {
			get {
				return timeToWait == null || model.ejectionBurn.atTime < Planetarium.GetUniversalTime();
			}
		}

		/// <returns>
		/// String representing years till burn.
		/// </returns>
		public string getYearValue()
		{
			Refresh();
			if (showLoadingText) {
				return LoadingText;
			} else {
				return TimePieceString("{0}y", timeToWait.years, timeToWait.needYears);
			}
		}

		/// <returns>
		/// String representing days till burn.
		/// </returns>
		public string getDayValue()
		{
			Refresh();
			if (showLoadingText) {
				return LoadingText;
			} else {
				return TimePieceString("{0}d", timeToWait.days, timeToWait.needDays);
			}
		}

		/// <returns>
		/// String representing hours till burn.
		/// </returns>
		public string getHourValue()
		{
			Refresh();
			if (showLoadingText) {
				return LoadingText;
			} else {
				return TimePieceString("{0}h", timeToWait.hours, timeToWait.needHours);
			}
		}

		/// <returns>
		/// String representing minutes till burn.
		/// </returns>
		public string getMinuteValue()
		{
			Refresh();
			if (showLoadingText) {
				return LoadingText;
			} else {
				return TimePieceString("{0}m", timeToWait.minutes, timeToWait.needMinutes);
			}
		}

		/// <returns>
		/// String representing seconds till burn.
		/// </returns>
		public string getSecondValue()
		{
			Refresh();
			if (showLoadingText) {
				return LoadingText;
			} else {
				return TimePieceString("{0}s", timeToWait.seconds, true);
			}
		}

		/// <returns>
		/// String representing delta V of the burn.
		/// </returns>
		public string getDeltaV()
		{
			Refresh();

			if (model.ejectionBurn == null) {
				return LoadingText;
			} else if (model.planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV) {
				return FormatSpeed(
					model.ejectionBurn.totalDeltaV,
					Settings.Instance.DisplayUnits);
			} else {
				return FormatSpeed(
					model.ejectionBurn.totalDeltaV + model.planeChangeBurn.totalDeltaV,
					Settings.Instance.DisplayUnits);
			}
		}

	}

}
