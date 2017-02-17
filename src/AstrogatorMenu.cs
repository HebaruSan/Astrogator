using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

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
			timeToWait = new List<DateTimeParts>();
			cursorTransfer = 0;
			CalculateEjectionBurns();
		}

		[KSPField]
		public int buttonUp = 0;

		[KSPField]
		public int buttonDown = 1;

		[KSPField]
		public int buttonEnter = 2;

		private AstrogationModel    model             { get; set; }
		private List<DateTimeParts> timeToWait        { get; set; }
		private double              lastUniversalTime { get; set; }
		private int                 cursorTransfer    { get; set; }
		private bool                cursorMoved       { get; set; }
		private string              menu              { get; set; }
		private int?                activeButton      { get; set; }

		public string ShowMenu(int columns, int rows)
		{
			if (Refresh() || cursorMoved) {
				StringBuilder sb = new StringBuilder();
				sb.Append(centerString(" " + AstrogationView.DisplayName + " ", columns, '-'));
				sb.Append(Environment.NewLine);
				sb.Append(centerString(String.Format("Transfers from {0}", TheName(model.origin)), columns));
				sb.Append(Environment.NewLine);
				sb.Append(Environment.NewLine);

				// [#rrggbbaa]
				sb.AppendFormat("[#22ff22ff]{0,-9} {1,20} {2,9}",
					"Transfer", "Time Till Burn", "Δv");

				// Wrap the cursor around the edges now because it only tells us dimensions here.
				while (cursorTransfer < 0) {
					cursorTransfer += model.transfers.Count;
				}
				while (cursorTransfer >= model.transfers.Count) {
					cursorTransfer -= model.transfers.Count;
				}
				// TODO - handle multiple pages of transfers

				for (int i = 0; i < model.transfers.Count && i < rows - 4; ++i) {
					if (model?.transfers[i]?.ejectionBurn != null) {
						sb.Append(Environment.NewLine);

						string destLabel = CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(TheName(model.transfers[i].destination));

						sb.AppendFormat("{0,2}[#22ff22ff]{1,-7}[#ffffffff] {2,4} {3,4} {4,2} {5,3} {6,3} {7,9}",
							(cursorTransfer == i ? "> " : "  "),
							(destLabel.Length > 7 ? destLabel.Substring(0, 7) : destLabel),
							TimePieceString("{0}y", timeToWait[i].years,   timeToWait[i].needYears),
							TimePieceString("{0}d", timeToWait[i].days,    timeToWait[i].needDays),
							TimePieceString("{0}h", timeToWait[i].hours,   timeToWait[i].needHours),
							TimePieceString("{0}m", timeToWait[i].minutes, timeToWait[i].needMinutes),
							TimePieceString("{0}s", timeToWait[i].seconds, true),
							FormatSpeed(
								(model.transfers[i].planeChangeBurn == null || !Settings.Instance.AddPlaneChangeDeltaV)
									? model.transfers[i].ejectionBurn.totalDeltaV
									: (model.transfers[i].ejectionBurn.totalDeltaV + model.transfers[i].planeChangeBurn.totalDeltaV),
								Settings.Instance.DisplayUnits));
					}
				}
				menu = sb.ToString();
				cursorMoved = false;
			}
			return menu;
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
				CreateManeuvers();
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
				for (int i = 0; i < model.transfers.Count; ++i) {

					model.transfers[i].Refresh();
					if (model.transfers[i].ejectionBurn != null) {
						timeToWait[i] = new DateTimeParts(model.transfers[i].ejectionBurn.atTime - Planetarium.GetUniversalTime());
					}

				}
				lastUniversalTime = now;
				return true;
			}
			return false;
		}

		private void CalculateEjectionBurns()
		{
			for (int i = 0; i < model.transfers.Count; ++i) {
				timeToWait.Add(null);
				try {
					model.transfers[i].CalculateEjectionBurn();
				} catch (Exception ex) {
					DbgExc("Problem with load of ejection burn", ex);
				}
			}
		}

		private void CalculatePlaneChangeBurns()
		{
			if (Settings.Instance.GeneratePlaneChangeBurns
					&& Settings.Instance.AddPlaneChangeDeltaV) {

				for (int i = 0; i < model.transfers.Count; ++i) {
					try {
						model.transfers[i].CalculatePlaneChangeBurn();
					} catch (Exception ex) {
						DbgExc("Problem with background load of plane change burn", ex);

						// If a route calculation crashes, it can leave behind a temporary node.
						ClearManeuverNodes();
					}
				}
			}
		}

		/// <summary>
		/// Turn this transfer's burns into user visible maneuver nodes.
		/// This is the behavior for the maneuver node icon.
		/// </summary>
		private void CreateManeuvers()
		{
			TransferModel tr = model.transfers[cursorTransfer];

			if (FlightGlobals.ActiveVessel != null) {

				// Remove all maneuver nodes because they'd conflict with the ones we're about to add
				ClearManeuverNodes();

				if (Settings.Instance.AutoTargetDestination) {
					// Switch to target mode, targeting the destination body
					FlightGlobals.fetch.SetVesselTarget(tr.destination);
				}

				// Create a maneuver node for the ejection burn
				tr.ejectionBurn.ToActiveManeuver();

				if (Settings.Instance.GeneratePlaneChangeBurns) {
					if (tr.planeChangeBurn == null) {
						DbgFmt("Calculating plane change on the fly");
						tr.CalculatePlaneChangeBurn();
					}

					if (tr.planeChangeBurn != null) {
						tr.planeChangeBurn.ToActiveManeuver();
					} else {
						DbgFmt("No plane change found");
					}
				} else {
					DbgFmt("Plane changes disabled");
				}

				if (Settings.Instance.AutoEditEjectionNode) {
					// Open the initial node for fine tuning
					tr.ejectionBurn.EditNode();
				} else if (Settings.Instance.AutoEditPlaneChangeNode) {
					if (tr.planeChangeBurn != null) {
						tr.planeChangeBurn.EditNode();
					}
				}

				if (Settings.Instance.AutoFocusDestination) {
					if (tr.HaveEncounter()) {
						// Move the map to the target for fine-tuning if we have an encounter
						FocusMap(tr.destination);
					} else if (tr.transferParent != null) {
						// Otherwise focus on the parent of the transfer orbit so we can get an encounter
						// Try to explain why this is happening with a screen message
						ScreenFmt("Adjust maneuvers to establish encounter");
						FocusMap(tr.transferParent, tr.transferDestination);
					}
				}

				if (Settings.Instance.AutoSetSAS
						&& FlightGlobals.ActiveVessel != null
						&& FlightGlobals.ActiveVessel.Autopilot.CanSetMode(VesselAutopilot.AutopilotMode.Maneuver)) {
					try {
						if (FlightGlobals.ActiveVessel.Autopilot.Enabled) {
							FlightGlobals.ActiveVessel.Autopilot.SetMode(VesselAutopilot.AutopilotMode.Maneuver);
						} else {
							FlightGlobals.ActiveVessel.Autopilot.Enable(VesselAutopilot.AutopilotMode.Maneuver);
						}
					} catch (Exception ex) {
						DbgExc("Problem setting SAS to maneuver mode", ex);
					}
				}
			}
		}

	}

}
