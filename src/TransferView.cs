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

		private TransferModel model { get; set; }
		private double lastUniversalTime { get; set; }
		private DateTimeParts timeToWait { get; set; }
		private AstrogationView.ResetCallback resetCallback { get; set; }

		private void CreateLayout()
		{
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];

				// Skip columns that require an active vessel if we don't have one
				if (col.vesselSpecific && FlightGlobals.ActiveVessel == null) {
					continue;
				}

				// Skip columns that require maneuver nodes if they're not unlocked
				if (col.requiresPatchedConics && !patchedConicsUnlocked()) {
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
							col.contentStyle, "Create maneuver", CreateManeuvers));
						break;

					case ContentEnum.WarpToBurnButton:
						AddChild(iconButton(warpIcon,
							col.contentStyle, "Warp to window", WarpToBurn));
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
			bool modelNeedsUIUpdate = model.Refresh();
			double now = Math.Floor(Planetarium.GetUniversalTime());

			if (modelNeedsUIUpdate) {
				// We have a new ejection burn, so we might need a totally new view
				// because the sort could be wrong now.
				resetCallback();
				return true;
			} else if (lastUniversalTime != now && model.ejectionBurn != null) {
				timeToWait = new DateTimeParts(model.ejectionBurn.atTime - Planetarium.GetUniversalTime());
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

		private const string LoadingText = "---";

		/// <returns>
		/// String representing years till burn.
		/// </returns>
		public string getYearValue()
		{
			Refresh();
			if (timeToWait == null) {
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
			if (timeToWait == null) {
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
			if (timeToWait == null) {
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
			if (timeToWait == null) {
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
			if (timeToWait == null) {
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

		/// <summary>
		/// Turn this transfer's burns into user visible maneuver nodes.
		/// This is the behavior for the maneuver node icon.
		/// </summary>
		public void CreateManeuvers()
		{
			if (FlightGlobals.ActiveVessel != null) {

				// Remove all maneuver nodes because they'd conflict with the ones we're about to add
				ClearManeuverNodes();

				if (Settings.Instance.AutoTargetDestination) {
					// Switch to target mode, targeting the destination body
					FlightGlobals.fetch.SetVesselTarget(model.destination);
				}

				// Create a maneuver node for the ejection burn
				model.ejectionBurn.ToActiveManeuver();

				if (Settings.Instance.GeneratePlaneChangeBurns) {
					if (model.planeChangeBurn == null) {
						DbgFmt("Calculating plane change on the fly");
						model.CalculatePlaneChangeBurn();
					}

					if (model.planeChangeBurn != null) {
						model.planeChangeBurn.ToActiveManeuver();
					} else {
						DbgFmt("No plane change found");
					}
				} else {
					DbgFmt("Plane changes disabled");
				}

				if (Settings.Instance.AutoEditEjectionNode) {
					// Open the initial node for fine tuning
					model.ejectionBurn.EditNode();
				} else if (Settings.Instance.AutoEditPlaneChangeNode) {
					if (model.planeChangeBurn != null) {
						model.planeChangeBurn.EditNode();
					}
				}

				if (Settings.Instance.AutoFocusDestination) {
					if (model.HaveEncounter()) {
						// Move the map to the target for fine-tuning if we have an encounter
						FocusMap(model.destination);
					} else if (model.transferParent != null) {
						// Otherwise focus on the parent of the transfer orbit so we can get an encounter
						FocusMap(model.transferParent);
					}
				}
			}
		}

		/// <summary>
		/// Warp to (near) the burn.
		/// Since you usually need to start burning before the actual node,
		/// we use some simple padding logic to determine how far to warp.
		/// If you're more than five minutes from the burn, then we warp
		/// to that five minute mark. This should allow for most of the long burns.
		/// If you're closer than five minutes from the burn, then we warp
		/// right up to the moment of the actual burn.
		/// </summary>
		public void WarpToBurn()
		{
			DbgFmt("Attempting to warp to burn from {0} to {1}", Planetarium.GetUniversalTime(), model.ejectionBurn.atTime);
			if (Planetarium.GetUniversalTime() < model.ejectionBurn.atTime - BURN_PADDING ) {
				DbgFmt("Warping to burn minus offset");
				TimeWarp.fetch.WarpTo(model.ejectionBurn.atTime - BURN_PADDING);
			} else if (Planetarium.GetUniversalTime() < model.ejectionBurn.atTime) {
				DbgFmt("Already within offset; warping to burn");
				TimeWarp.fetch.WarpTo(model.ejectionBurn.atTime);
			} else {
				DbgFmt("Can't warp to the past!");
			}
		}
	}

}
