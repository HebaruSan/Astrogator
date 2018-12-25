using System;
using System.Globalization;
using KSP.Localization;

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
		public TransferView(TransferModel m)
			: base()
		{
			model = m;
			CreateLayout();
		}

		private TransferModel model             { get; set; }
		private double        lastUniversalTime { get; set; }
		private DateTimeParts timeToWait        { get; set; }
		private DateTimeParts duration          { get; set; }

		private void CreateLayout()
		{
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];

				// Skip columns that require an active vessel if we don't have one
				if (col.vesselSpecific && FlightGlobals.ActiveVessel == null) {
					continue;
				}

				// Skip columns that require maneuver nodes if they're not available
				if (col.requiresPatchedConics && (
					   !patchedConicsUnlocked()
					|| !vesselControllable(FlightGlobals.ActiveVessel)
					|| model.origin == null
					|| Landed(model.origin)
				)) {
					continue;
				}

				// Add a blank space if this column requires a time but this
				// row doesn't have one, because other rows might.
				if (col.requiresTime && model.ejectionBurn?.atTime == null) {
					AddChild(new DialogGUISpace(col.width));
					continue;
				}

				switch (col.content) {

					case ContentEnum.PlanetName:
						AddChild(LabelWithStyleAndSize(
							Localizer.Format("astrogator_planetLabel", TheName(model.destination)),
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
							col.contentStyle, col.width));
						break;

					case ContentEnum.DurationMinutes:
						AddChild(LabelWithStyleAndSize(getBurnDurationMinutesValue,
							col.contentStyle, col.width));
						break;

					case ContentEnum.DurationSeconds:
						AddChild(LabelWithStyleAndSize(getBurnDurationSecondsValue,
							col.contentStyle, col.width));
						break;

					case ContentEnum.CreateManeuverNodeButton:
						AddChild(iconButton(maneuverIcon,
							col.contentStyle, Localizer.Format("astrogator_maneuverButtonTooltip"), model.CreateManeuvers));
						break;

					case ContentEnum.WarpToBurnButton:
						AddChild(iconButton(warpIcon,
							col.contentStyle, Localizer.Format("astrogator_warpButtonTooltip"), model.WarpToBurn));
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
				timeToWait = model.ejectionBurn.atTime != null
					? new DateTimeParts((model.ejectionBurn.atTime ?? 0) - Planetarium.GetUniversalTime())
					: null;
				duration = model.ejectionBurnDuration.HasValue
					? new DateTimeParts(model.ejectionBurnDuration.Value)
					: null;
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

		private const string LoadingText = "---";

		private bool showLoadingText {
			get {
				return timeToWait == null
					|| model.ejectionBurn.atTime == null
					|| model.ejectionBurn.atTime < Planetarium.GetUniversalTime();
			}
		}

		/// <returns>
		/// String representing years till burn.
		/// </returns>
		public string getYearValue()
		{
			Refresh();
			return showLoadingText ? LoadingText
				: TimePieceString("astrogator_yearsValue", timeToWait.years, timeToWait.needYears);
		}

		/// <returns>
		/// String representing days till burn.
		/// </returns>
		public string getDayValue()
		{
			Refresh();
			return showLoadingText ? LoadingText
				: TimePieceString("astrogator_daysValue", timeToWait.days, timeToWait.needDays);
		}

		/// <returns>
		/// String representing hours till burn.
		/// </returns>
		public string getHourValue()
		{
			Refresh();
			return showLoadingText ? LoadingText
				: TimePieceString("astrogator_hoursValue", timeToWait.hours, timeToWait.needHours);
		}

		/// <returns>
		/// String representing minutes till burn.
		/// </returns>
		public string getMinuteValue()
		{
			Refresh();
			return showLoadingText ? LoadingText
				: TimePieceString("astrogator_minutesValue", timeToWait.minutes, timeToWait.needMinutes);
		}

		/// <returns>
		/// String representing seconds till burn.
		/// </returns>
		public string getSecondValue()
		{
			Refresh();
			return showLoadingText ? LoadingText
				: TimePieceString("astrogator_secondsValue", timeToWait.seconds, true);
		}

		/// <returns>
		/// String representing delta V of the burn.
		/// </returns>
		public string getDeltaV()
		{
			Refresh();
			return model.ejectionBurn == null ? LoadingText
				: (model.planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV)
				? FormatSpeed(
					model.ejectionBurn.totalDeltaV,
					Settings.Instance.DisplayUnits)
				: FormatSpeed(
					model.ejectionBurn.totalDeltaV + model.planeChangeBurn.totalDeltaV,
					Settings.Instance.DisplayUnits);
		}

		/// <returns>
		/// String representing minutes of burn duration.
		/// </returns>
		public string getBurnDurationMinutesValue()
		{
			Refresh();
			return duration == null ? LoadingText
				: duration.Invalid  ? ""
				: duration.Infinite ? ""
				: TimePieceString("astrogator_minutesValue", duration.totalMinutes, duration.needMinutes);
		}

		/// <returns>
		/// String representing seconds of burn duration.
		/// </returns>
		public string getBurnDurationSecondsValue()
		{
			Refresh();
			return duration == null ? LoadingText
				: duration.Invalid  ? ""
				: duration.Infinite ? "N/A"
				: duration.totalSeconds < 1
					? TimePieceString("astrogator_secondsValue", Math.Round(duration.totalSeconds, 1), true)
				: TimePieceString("astrogator_secondsValue", duration.seconds, true);
		}

	}

}
