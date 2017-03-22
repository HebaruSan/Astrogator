using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using KSP.Localization;

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

		public Astrogator()
			: base()
		{
			model  = new AstrogationModel();
			loader = new AstrogationLoadBehaviorette(model, ResetViewBackground);
		}

		private bool VesselMode { get; set; }

		/// <summary>
		/// Machine-readable name for this mod.
		/// Use this for directory/file names, etc.
		/// Use AstrogationView.DisplayName for user visible text.
		/// (Yes, they are the same at the moment. Don't worry about that.)
		/// </summary>
		public const string Name = "Astrogator";

		/// <summary>
		/// This is called at creation
		/// </summary>
		public void Start()
		{
			// This event fires when KSP is ready for mods to add toolbar buttons
			GameEvents.onGUIApplicationLauncherReady.Add(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncher);

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Add(TrackingStationTargetChanged);

			// Reset the view when we take off or land, etc.
			GameEvents.onVesselSituationChange.Add(OnSituationChanged);

			// This event fires on SOI change
			if (FlightGlobals.ActiveVessel != null) {
				VesselMode = true;

				OrbitDriver orbDrv = FlightGlobals.ActiveVessel.GetOrbitDriver();
				orbDrv.OnReferenceBodyChange += SOIChanged;
			}
		}

		/// <summary>
		/// This is called at destroy
		/// </summary>
		public void OnDisable()
		{
			// Tear down the window (saves the position as a side effect)
			HideMainWindow(false);

			// Save the persistent attributes to our settings file
			Settings.Instance.Save();

			if (loader != null) {
				// Tell the loader we don't need data anymore;
				// prevents new threads from starting
				loader.OnDisplayClosed();

				// Clean up the timer and thread
				loader.Dispose();
			}

			// The "dead" copy of our object will re-add itself if we don't unsubscribe to this!
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Remove(TrackingStationTargetChanged);

			// Reset the view when we take off or land, etc.
			GameEvents.onVesselSituationChange.Remove(OnSituationChanged);

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
			}

			// Auto open the window if it was open the last time we ran
			if (Settings.Instance.MainWindowVisible) {
				launcher.SetTrue();
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
		private void onAppLaunchHover()
		{
			DbgFmt("Hovered over");

			if (tooltip == null) {
				tooltip = TooltipView.AppLauncherTooltip(
					Localization.Format("astrogator_mainTitle"),
					Localization.Format("astrogator_mainTooltip"),
					launcher);
			}
			tooltip.Show();
		}

		/// <summary>
		/// React to the user de-hovering the app launcher by hiding the tooltip.
		/// </summary>
		private void onAppLaunchHoverOut()
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
		private void onAppLaunchToggleOn()
		{
			// TryStartLoad aborts if no displays are open, so we don't have to track that from every event handler
			loader.OnDisplayOpened();

			// Begin loading, open window when partially complete, refresh it when fully complete, also open on abort.
			loader.TryStartLoad(
				(ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
				() => { needViewOpen = true; },
				() => { needViewOpen = true; },
				() => { needViewOpen = true; }
			);
		}

		/// <summary>
		/// This is called when they click our toolbar button again
		/// </summary>
		private void onAppLaunchToggleOff()
		{
			DbgFmt("Returning to hangar");
			HideMainWindow(true);

			// Tell the loader the window is closed so it can stop processing
			loader.OnDisplayClosed();
		}

		#endregion App launcher

		#region Main window

		private AstrogationModel            model         { get; set; }
		private AstrogationLoadBehaviorette loader        { get; set; }
		private AstrogationView             view          { get; set; }
		private bool                        needViewOpen  { get; set; }

		/// <summary>
		/// Open the main window listing transfers.
		/// </summary>
		private void ShowMainWindow()
		{
			DbgFmt("Deploying main window");

			Settings.Instance.MainWindowVisible = true;
			if (view == null) {
				view = new AstrogationView(model, ResetView, () => { launcher.SetFalse(true); });
				DbgFmt("View mated to booster");
			}
			view.Show();

			DbgFmt("Deployed main window");
		}

		/// <summary>
		/// Close the main window.
		/// </summary>
		private void HideMainWindow(bool userInitiated)
		{
			if (view != null) {
				view.Dismiss();
				view = null;
			}
			if (userInitiated) {
				// If we close the window because we're exiting, don't set the setting.
				Settings.Instance.MainWindowVisible = false;

				// Now that the transfers and the settings are separate,
				// it doesn't make sense to still show the settings if they click the X.
				Settings.Instance.ShowSettings = false;
			}
		}

		#endregion Main window

		private void ResetView(bool resetModel = false)
		{
			if (resetModel) {
				loader.TryStartLoad(
					model.origin ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
					null, ResetViewBackground, null);
			} else if (view != null) {
				HideMainWindow(false);
				ShowMainWindow();
			}
		}

		/// <summary>
		/// Unity completely freaks out, sometimes with a hard crash,
		/// if you try to open a window from a background thread when it's not ready.
		/// So instead we'll just make a note and do it in the next Update() call.
		/// </summary>
		private void ResetViewBackground()
		{
			needViewOpen = true;
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
					if (n.attachedGizmo != null) {
						n.attachedGizmo.DeltaV = n.DeltaV;
						try {
							n.OnGizmoUpdated(n.DeltaV, burn.atTime ?? 0);
						} catch (Exception ex) {
							DbgExc("Problem updating gizmo", ex);
						}
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
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.up);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.down);
					}
				}
			}, {
				GameSettings.TRANSLATE_DOWN, (AstrogationModel m) => {
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.down);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.up);
					}
				}
			}, {
				GameSettings.TRANSLATE_LEFT, (AstrogationModel m) => {
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.right);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.left);
					}
				}
			}, {
				GameSettings.TRANSLATE_RIGHT, (AstrogationModel m) => {
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.left);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.right);
					}
				}
			}
		};

		private delegate void AxisCallback(AstrogationModel m, double axisValue);

		// Also adjust nodes with the joystick/controller translation axes
		private Dictionary<AxisBinding, AxisCallback> axes = new Dictionary<AxisBinding, AxisCallback>() {
			{
				GameSettings.AXIS_TRANSLATE_X, (AstrogationModel m, double axisValue) => {
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.left, axisValue);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.right, axisValue);
					}
				}
			}, {
				GameSettings.AXIS_TRANSLATE_Y, (AstrogationModel m, double axisValue) => {
					if (m.ActiveTransfer?.retrogradeTransfer ?? false) {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.up, axisValue);
					} else {
						AdjustManeuver(m.ActivePlaneChangeBurn ?? m.ActiveEjectionBurn, Vector3d.down, axisValue);
					}
				}
			}, {
				GameSettings.AXIS_TRANSLATE_Z, (AstrogationModel m, double axisValue) => {
					AdjustManeuver(m.ActiveEjectionBurn, 0.1 * Vector3d.back, axisValue);
				}
			}
		};

		/// <summary>
		/// Called by the framework for each UI tick.
		/// </summary>
		public void Update()
		{
			// Open the window if a background thread asked us to.
			if (needViewOpen) {
				HideMainWindow(false);
				ShowMainWindow();
				needViewOpen = false;
			}

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
					DbgFmt("OnTargetChanged");
					loader.TryStartLoad(
						model.origin ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
						null, ResetViewBackground, null);
				}
			}
		}

		private OrbitModel prevOrbit { get; set; }
		private bool OrbitChanged()
		{
			return VesselMode
				&& model.origin != null
				&& !model.notOrbiting
				&& (prevOrbit == null
					|| !prevOrbit.Equals(FlightGlobals.ActiveVessel.orbit));
		}

		private void OnOrbitChanged()
		{
			loader.TryStartLoad(
				model.origin ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
				null, ResetViewBackground, null);
		}

		private void OnSituationChanged(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> e)
		{
			if (model != null && view != null && e.host == FlightGlobals.ActiveVessel) {
				DbgFmt("Situation of {0} changed from {1} to {2}", TheName(e.host), e.from, e.to);
				loader.TryStartLoad(
					e.host ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
					null, ResetViewBackground, null);
			}
		}

		/// <summary>
		/// React to the sphere of influence of the vessel changing by resetting the model and view.
		/// </summary>
		private void SOIChanged(CelestialBody newBody)
		{
			if (model != null && view != null) {
				DbgFmt("Entered {0}'s sphere of influence", TheName(newBody));
				// The old list no longer applies because reachable bodies depend on current SOI
				loader.TryStartLoad(
					model.origin ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
					null, ResetViewBackground, null);
			}
		}

		/// <summary>
		/// React to the tracking station focus changing by resetting the model and view.
		/// Note that we have to make sure there's no active vessel to avoid doing this
		/// in flight mode's map view.
		/// </summary>
		private void TrackingStationTargetChanged(MapObject target)
		{
			if (!VesselMode
					&& model != null
					&& target != null) {

				DbgFmt("Tracking station changed target to {0}", target);
				loader.TryStartLoad(
					(ITargetable)target.vessel ?? (ITargetable)target.celestialBody ?? (ITargetable)FlightGlobals.ActiveVessel ?? (ITargetable)FlightGlobals.getMainBody(),
					null, ResetViewBackground, null
				);
			}
		}

		private void CheckIfNodesDisappeared()
		{
			model?.CheckIfNodesDisappeared();
		}
	}

}
