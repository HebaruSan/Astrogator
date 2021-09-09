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

			// Create objects for caching view strings when unchanged
			// (saves on lots of garbage)
			yearFormatter = new TidyFormatter<int>(
				v => showLoadingText || !v.HasValue ? LoadingText
					: TimePieceString("astrogator_yearsValue", v.Value, timeToWait.needYears)
			);
			dayFormatter = new TidyFormatter<int>(
				v => showLoadingText || !v.HasValue ? LoadingText
					: TimePieceString("astrogator_daysValue", v.Value, timeToWait.needDays)
			);
			hourFormatter = new TidyFormatter<int>(
				v => showLoadingText || !v.HasValue ? LoadingText
					: TimePieceString("astrogator_hoursValue", v.Value, timeToWait.needHours)
			);
			minuteFormatter = new TidyFormatter<int>(
				v => showLoadingText || !v.HasValue ? LoadingText
					: TimePieceString("astrogator_minutesValue", v.Value, timeToWait.needMinutes)
			);
			secondFormatter = new TidyFormatter<int>(
				v => showLoadingText || !v.HasValue ? LoadingText
					: TimePieceString("astrogator_secondsValue", v.Value, true)
			);
			deltaVFormatter = new TidyFormatter<double>(
				v => model.ejectionBurn == null || !v.HasValue ? LoadingText
					: FormatSpeed(v.Value, Settings.Instance.DisplayUnits),
				DoublesFurtherThanPointOne
			);
			durationMinuteFormatter = new TidyFormatter<int>(
				v =>  duration == null || !v.HasValue ? LoadingText
					: duration.Invalid  ? ""
					: duration.Infinite ? ""
					: TimePieceString("astrogator_minutesValue", v.Value, duration.needMinutes)
			);
			durationSecondFormatter = new TidyFormatter<int>(
				v =>  duration == null || !v.HasValue ? LoadingText
					: duration.Invalid  ? ""
					: duration.Infinite ? "N/A"
					: TimePieceString("astrogator_secondsValue", v.Value, true)
			);
			durationTotalSecondFormatter = new TidyFormatter<double>(
				v =>  duration == null || !v.HasValue ? LoadingText
					: duration.Invalid  ? ""
					: duration.Infinite ? "N/A"
					: TimePieceString("astrogator_secondsValue", Math.Round(v.Value, 1), true),
				(prev, next) => DoublesSmallerThanOne(prev, next)
					         && DoublesFurtherThanPointOne(prev, next)
			);

		}

		/// <summary>
		/// Check whether either number is smaller than 1
		/// </summary>
		/// <param name="a">One number</param>
		/// <param name="b">The other number</param>
		/// <returns>
		/// True if either one is less than 1, false otherwise
		/// </returns>
		private static bool DoublesSmallerThanOne(double? a, double? b)
		{
			return (a ?? 0) < 1.0 || (b ?? 0) < 1.0;
		}

		/// <summary>
		/// Check whether the numbers are more than 0.1 apart
		/// </summary>
		/// <param name="a">One number</param>
		/// <param name="b">The other number</param>
		/// <returns>
		/// False if both null, true if one null and the other not,
		/// true if difference is > 0.1.
		/// </returns>
		private static bool DoublesFurtherThanPointOne(double? a, double? b)
		{
			return a == null && b == null ? false
				 : a == null ? true
				 : b == null ? true
				 : Math.Abs(a.Value - b.Value) > 0.1;
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
					|| model.ejectionBurn?.atTime == null
					|| model.ejectionBurn.atTime < Planetarium.GetUniversalTime();
			}
		}

		private TidyFormatter<int>    yearFormatter;
		private TidyFormatter<int>    dayFormatter;
		private TidyFormatter<int>    hourFormatter;
		private TidyFormatter<int>    minuteFormatter;
		private TidyFormatter<int>    secondFormatter;
		private TidyFormatter<double> deltaVFormatter;
		private TidyFormatter<int>    durationMinuteFormatter;
		private TidyFormatter<int>    durationSecondFormatter;
		private TidyFormatter<double> durationTotalSecondFormatter;

		/// <returns>
		/// String representing years till burn.
		/// </returns>
		public string getYearValue()
		{
			Refresh();
			yearFormatter.Update(timeToWait?.years);
			return yearFormatter.ToString();
		}

		/// <returns>
		/// String representing days till burn.
		/// </returns>
		public string getDayValue()
		{
			Refresh();
			dayFormatter.Update(timeToWait?.days);
			return dayFormatter.ToString();
		}

		/// <returns>
		/// String representing hours till burn.
		/// </returns>
		public string getHourValue()
		{
			Refresh();
			hourFormatter.Update(timeToWait?.hours);
			return hourFormatter.ToString();
		}

		/// <returns>
		/// String representing minutes till burn.
		/// </returns>
		public string getMinuteValue()
		{
			Refresh();
			minuteFormatter.Update(timeToWait?.minutes);
			return minuteFormatter.ToString();
		}

		/// <returns>
		/// String representing seconds till burn.
		/// </returns>
		public string getSecondValue()
		{
			Refresh();
			secondFormatter.Update(timeToWait?.seconds);
			return secondFormatter.ToString();
		}

		/// <returns>
		/// String representing delta V of the burn.
		/// </returns>
		public string getDeltaV()
		{
			Refresh();
			double? dv = (model.planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV)
				? model.ejectionBurn?.totalDeltaV
				: model.ejectionBurn.totalDeltaV + model.planeChangeBurn.totalDeltaV;
			deltaVFormatter.Update(dv);
			return deltaVFormatter.ToString();
		}

		/// <returns>
		/// String representing minutes of burn duration.
		/// </returns>
		public string getBurnDurationMinutesValue()
		{
			Refresh();
			durationMinuteFormatter.Update(duration?.totalMinutes);
			return duration == null ? LoadingText
				: durationMinuteFormatter.ToString();
		}

		/// <returns>
		/// String representing seconds of burn duration.
		/// </returns>
		public string getBurnDurationSecondsValue()
		{
			Refresh();
			durationSecondFormatter.Update(duration?.seconds);
			durationTotalSecondFormatter.Update(duration?.totalSeconds);
			return duration == null ? LoadingText
				: duration.totalSeconds < 1 ? durationTotalSecondFormatter.ToString()
				: durationSecondFormatter.ToString();
		}

	}

}
