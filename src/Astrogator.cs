using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using KSP.UI.Screens;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;
	using static ViewTools;

	// We speak American in this house, young lady!
	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// KSPAddon can only apply one scene per class; this is the one for the flight scene.
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class FlightAstrogator          : Astrogator { }

	/// KSPAddon can only apply one scene per class; this is the one for the tracking station scene.
	[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
	public class TrackingStationAstrogator : Astrogator { }

	/// KSPAddon can only apply one scene per class; this is the one for the KSC scene.
	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class SpaceCenterAstrogator     : Astrogator { }

	/// Our main plugin behavior.
	public class Astrogator : MonoBehavior {

		private bool VesselMode { get; set; }

		/// <summary>
		/// Machine-readable name for this mod.
		/// Use this for directory/file names, etc.
		/// Use AstrogationView.DisplayName for user visible text.
		/// (Yes, they are the same at the moment. Don't worry about that.)
		/// </summary>
		public const string Name = "Astrogator";

		/// <summary>
		/// Text to be shown in the tooltip to explain what this mod does.
		/// </summary>
		public const string Description = "Summary of transfer windows of reachable bodies";

		/// This is called at creation
		public void Start()
		{
			// This event fires when KSP is ready for mods to add toolbar buttons
			GameEvents.onGUIApplicationLauncherReady.Add(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncher);

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Add(TrackingStationTargetChanged);

			// This is called in the flight scene when the vessel is fully loaded.
			// We need that to be able to calculate plane changes.
			GameEvents.onFlightReady.Add(OnFlightReady);

			// This event fires on SOI change
			if (FlightGlobals.ActiveVessel != null) {
				VesselMode = true;

				OrbitDriver orbDrv = FlightGlobals.ActiveVessel.GetOrbitDriver();
				orbDrv.OnReferenceBodyChange += SOIChanged;
			}
		}

		/// This is called at destroy
		public void OnDisable()
		{
			// Tear down the window (saves the position as a side effect)
			HideMainWindow(false);

			// Save the persistent attributes to our settings file
			Settings.Instance.Save();

			// The "dead" copy of our object will re-add itself if we don't unsubscribe to this!
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Remove(TrackingStationTargetChanged);

			// This is called in the flight scene when the vessel is fully loaded.
			// We need that to be able to calculate plane changes.
			GameEvents.onFlightReady.Remove(OnFlightReady);

			// The launcher destroyed event doesn't always fire when we need it (?)
			RemoveLauncher();

			// This event fires on SOI change
			if (VesselMode) {
				OrbitDriver orbDrv = FlightGlobals.ActiveVessel.GetOrbitDriver();
				orbDrv.OnReferenceBodyChange -= SOIChanged;
			}
		}

		#region App launcher

		private ApplicationLauncherButton launcher { get; set; }

		private void AddLauncher()
		{
			if (ApplicationLauncher.Ready && launcher == null)
			{
				const ApplicationLauncher.AppScenes VisibleInScenes =
					ApplicationLauncher.AppScenes.FLIGHT |
					ApplicationLauncher.AppScenes.MAPVIEW |
					ApplicationLauncher.AppScenes.TRACKSTATION |
					ApplicationLauncher.AppScenes.SPACECENTER;

				launcher = ApplicationLauncher.Instance.AddModApplication(
					onAppLaunchToggleOn, onAppLaunchToggleOff,
					onAppLaunchHover,    onAppLaunchHoverOut,
					null, null,
					VisibleInScenes,
					AppIcon);

				// Auto open the window if it was open the last time we ran
				if (visible) {
					launcher.SetTrue();
				}
			}
		}

		private void RemoveLauncher()
		{
			if (launcher != null) {
				ApplicationLauncher.Instance.RemoveModApplication(launcher);
				launcher = null;
			}
		}

		private TooltipView tooltip { get; set; }

		/// <summary>
		/// React to user hovering over the app launcher by showing the tooltip.
		/// </summary>
		public void onAppLaunchHover()
		{
			DbgFmt("Hovered over");

			if (tooltip == null) {
				tooltip = TooltipView.AppLauncherTooltip(
					AstrogationView.DisplayName,
					Description,
					launcher);
			}
			tooltip.Show();
		}

		/// <summary>
		/// React to the user de-hovering the app launcher by hiding the tooltip.
		/// </summary>
		public void onAppLaunchHoverOut()
		{
			DbgFmt("Unhovered");

			if (tooltip != null) {
				tooltip.Dismiss();
				tooltip = null;
			}
		}

		/// <summary>
		/// This is called when they click our toolbar button
		/// </summary>
		public void onAppLaunchToggleOn()
		{
			DbgFmt("Ready for action");
			if (model == null) {
				StartLoadingModel((ITargetable)FlightGlobals.ActiveVessel
					?? (ITargetable)FlightGlobals.getMainBody());
			}
			ShowMainWindow();
		}

		/// <summary>
		/// This is called when they click our toolbar button again
		/// </summary>
		public void onAppLaunchToggleOff()
		{
			DbgFmt("Returning to hangar");
			HideMainWindow();
		}

		#endregion App launcher

		private bool flightReady { get; set; }

		#region Background loading

		private void OnFlightReady()
		{
			flightReady = true;
			if (Settings.Instance.GeneratePlaneChangeBurns
					&& Settings.Instance.AddPlaneChangeDeltaV) {
				StartLoadingModel(model.origin);
				ResetView();
			}
		}

		private void StartLoadingModel(ITargetable origin, bool fromScratch = false)
		{
			// Set up the very basics of the model so the view has something to display during load
			if (fromScratch || model == null) {
				DbgFmt("Assembling model");
				model = new AstrogationModel(origin);
				DbgFmt("Model assembled");
			} else {
				model.Reset(origin);
			}

			// Do the easy calculations in the foreground so the view can sort properly right away
			CalculateEjectionBurns();

			DbgFmt("Delegating load to background");

			BackgroundWorker bgworker = new BackgroundWorker();
			bgworker.DoWork += bw_LoadModel;
			bgworker.RunWorkerCompleted += bw_DoneLoadingModel;
			bgworker.RunWorkerAsync();

			DbgFmt("Launched background");
		}

		private static readonly object bgLoadMutex = new object();

		private void bw_LoadModel(object sender, DoWorkEventArgs e)
		{
			lock (bgLoadMutex) {
				DbgFmt("Beginning background model load");
				CalculatePlaneChangeBurns();
				DbgFmt("Finished background model load");
			}
		}

		private void CalculateEjectionBurns()
		{
			// Blast through the ejection burns so the popup has numbers ASAP
			for (int i = 0; i < model.transfers.Count; ++i) {
				try {
					model.transfers[i].CalculateEjectionBurn();
				} catch (Exception ex) {
					DbgExc("Problem with load of ejection burn", ex);
				}
			}
		}

		private void CalculatePlaneChangeBurns()
		{
			if (flightReady
					&& Settings.Instance.GeneratePlaneChangeBurns
					&& Settings.Instance.AddPlaneChangeDeltaV) {
				for (int i = 0; i < model.transfers.Count; ++i) {
					try {
						Thread.Sleep(200);
						model.transfers[i].CalculatePlaneChangeBurn();
					} catch (Exception ex) {
						DbgExc("Problem with background load of plane change burn", ex);

						// If a route calculation crashes, it can leave behind a temporary node.
						ClearManeuverNodes();
					}
				}
			}
		}

		private void bw_DoneLoadingModel(object sender, RunWorkerCompletedEventArgs e)
		{
			DbgFmt("Background load complete");
		}

		#endregion Background loading

		#region Main window

		private AstrogationModel model    { get; set; }
		private AstrogationView  view     { get; set; }

		private static bool visible {
			get {
				return Settings.Instance.MainWindowVisible;
			}
			set {
				Settings.Instance.MainWindowVisible = value;
			}
		}

		/// <summary>
		/// Open the main window listing transfers.
		/// </summary>
		public void ShowMainWindow()
		{
			DbgFmt("Deploying main window");

			visible = true;
			if (view == null) {
				view = new AstrogationView(model, ResetView);
				DbgFmt("View mated to booster");
			}
			view.Show();

			DbgFmt("Deployed main window");
		}

		/// <summary>
		/// Close the main window.
		/// </summary>
		public void HideMainWindow(bool userInitiated = true)
		{
			if (view != null) {
				view.Dismiss();
				view = null;
			}
			if (userInitiated) {
				// If we close the window because we're exiting, don't set the setting.
				visible = false;
			}
		}

		#endregion Main window

		private void ResetView()
		{
			if (view != null) {
				HideMainWindow();
				ShowMainWindow();
			}
		}

		private static void AdjustManeuver(BurnModel burn, Vector3d direction, double fraction = 1.0)
		{
			const double DELTA_V_INCREMENT_LARGE = 0.5,
				DELTA_V_INCREMENT_SMALL = 0.01;

			if (burn != null && FlightGlobals.ActiveVessel != null) {

				ManeuverNode n = burn?.node;
				if (n != null) {
					if (GameSettings.MODIFIER_KEY.GetKey()) {
						n.DeltaV += DELTA_V_INCREMENT_SMALL * fraction * direction;
					} else {
						n.DeltaV += DELTA_V_INCREMENT_LARGE * fraction * direction;
					}
					n.solver.UpdateFlightPlan();
				}
			}
		}

		private delegate void KeyPressedCallback(AstrogationModel m);

		// Adjust our nodes using the RCS translation controls if RCS is turned off
		private Dictionary<KeyBinding, KeyPressedCallback> keys = new Dictionary<KeyBinding, KeyPressedCallback>() {
			{
				GameSettings.TRANSLATE_FWD, (AstrogationModel m) => {
					AdjustManeuver(m.ActiveEjectionBurn, 0.1 * Vector3d.forward);
				}
			}, {
				GameSettings.TRANSLATE_BACK, (AstrogationModel m) => {
					AdjustManeuver(m.ActiveEjectionBurn, 0.1 * Vector3d.back);
				}
			}, {
				GameSettings.TRANSLATE_UP, (AstrogationModel m) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.up);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.down);
					}
				}
			}, {
				GameSettings.TRANSLATE_DOWN, (AstrogationModel m) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.down);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.up);
					}
				}
			}, {
				GameSettings.TRANSLATE_LEFT, (AstrogationModel m) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.right);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.left);
					}
				}
			}, {
				GameSettings.TRANSLATE_RIGHT, (AstrogationModel m) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.left);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.right);
					}
				}
			}
		};

		private delegate void AxisCallback(AstrogationModel m, double axisValue);

		// Also adjust nodes with the joystick/controller translation axes
		private Dictionary<AxisBinding, AxisCallback> axes = new Dictionary<AxisBinding, AxisCallback>() {
			{
				GameSettings.AXIS_TRANSLATE_X, (AstrogationModel m, double axisValue) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.left, axisValue);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.right, axisValue);
					}
				}
			}, {
				GameSettings.AXIS_TRANSLATE_Y, (AstrogationModel m, double axisValue) => {
					if (m.retrogradeOrbit) {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.up, axisValue);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.down, axisValue);
					}
				}
			}, {
				GameSettings.AXIS_TRANSLATE_Z, (AstrogationModel m, double axisValue) => {
					AdjustManeuver(m.ActivePlaneChangeBurn, Vector3d.back, axisValue);
				}
			}
		};

		/// <summary>
		/// Called by the framework for each UI tick.
		/// </summary>
		public void Update()
		{
			CheckIfNodesDisappeared();

			if (Settings.Instance.TranslationAdjust
					&& model != null
					&& FlightGlobals.ActiveVessel != null
					&& !FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS]) {

				foreach (KeyValuePair<KeyBinding, KeyPressedCallback> k in keys) {
					if (k.Key.GetKey()) {
						k.Value(model);
					}
				}
				foreach (KeyValuePair<AxisBinding, AxisCallback> a in axes) {
					double val = a.Key.GetAxis();
					if (val != 0) {
						a.Value(model, val);
					}
				}
			}
		}

		/// <summary>
		/// Called by the framework for each physics tick.
		/// </summary>
		public void FixedUpdate()
		{
			if (VesselMode) {
				// Check for changes in vessel's orbit

				if (OrbitChanged()) {
					OnOrbitChanged();
					prevOrbit = new OrbitModel(FlightGlobals.ActiveVessel.orbit);
				}

				if (TargetChanged()) {
					OnTargetChanged();
					prevTarget = FlightGlobals.fetch.VesselTarget;
				}

				if (SituationChanged()) {
					OnSituationChanged();
					prevSituation = FlightGlobals.ActiveVessel.situation;
				}
			}
		}

		private ITargetable prevTarget { get; set; }
		private bool TargetChanged()
		{
			return VesselMode
				&& prevTarget != FlightGlobals.fetch.VesselTarget;
		}

		private void OnTargetChanged()
		{
			// Refresh the model so it can reflect the latest target data
			if (model != null) {
				if (!model.HasDestination(FlightGlobals.fetch.VesselTarget)) {
					DbgFmt("Reloading model and view on target change");
					StartLoadingModel(model.origin);
					ResetView();
				}
			}
		}

		private OrbitModel prevOrbit { get; set; }
		private bool OrbitChanged()
		{
			return VesselMode
				&& (prevOrbit == null
					|| !prevOrbit.Equals(FlightGlobals.ActiveVessel.orbit));
		}

		private void OnOrbitChanged()
		{
			if (prevOrbit == null) {
				DbgFmt("No previous orbit.");
			} else {
				DbgFmt(prevOrbit.ComparisonDescription(FlightGlobals.ActiveVessel.orbit));
			}

			if (model != null) {

				// Just recalculate the ejection burns since those are relatively simple
				for (int i = 0; i < model.transfers.Count; ++i) {
					try {
						model.transfers[i].CalculateEjectionBurn();
					} catch (Exception ex) {
						DbgExc("Problem after orbit change", ex);
					}
				}
			}
		}

		private Vessel.Situations prevSituation { get; set; }
		private bool SituationChanged()
		{
			return prevSituation != FlightGlobals.ActiveVessel.situation;
		}

		private void OnSituationChanged()
		{
			if (model != null && view != null) {
				StartLoadingModel(FlightGlobals.ActiveVessel);
				ResetView();
			}
		}

		/// <summary>
		/// React to the sphere of influence of the vessel changing by resetting the model and view.
		/// </summary>
		public void SOIChanged(CelestialBody newBody)
		{
			DbgFmt("Entered {0}'s sphere of influence", newBody.theName);

			if (model != null && view != null) {
				// The old list no longer applies because reachable bodies depend on current SOI
				StartLoadingModel(FlightGlobals.ActiveVessel);
				ResetView();
			}
		}

		/// <summary>
		/// React to the tracking station focus changing by resetting the model and view.
		/// Note that we have to make sure there's no active vessel to avoid doing this
		/// in flight mode's map view.
		/// </summary>
		public void TrackingStationTargetChanged(MapObject target)
		{
			if (!VesselMode
					&& model != null
					&& target != null) {

				DbgFmt("Tracking station changed target to {0}", target);
				StartLoadingModel((ITargetable)target.vessel
					?? (ITargetable)target.celestialBody);
				ResetView();
			}
		}

		private void CheckIfNodesDisappeared()
		{
			model?.CheckIfNodesDisappeared();
		}
	}

}
