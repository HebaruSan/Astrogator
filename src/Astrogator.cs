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
	public class FlightAstrogator : Astrogator { }

	/// KSPAddon can only apply one scene per class; this is the one for the tracking station scene.
	[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
	public class TrackingStationAstrogator : Astrogator { }

	/// KSPAddon can only apply one scene per class; this is the one for the KSC scene.
	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class SpaceCenterAstrogator : Astrogator { }

	/// Our main plugin behavior.
	public class Astrogator : MonoBehavior {

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

			// This event fires on SOI change
			if (FlightGlobals.ActiveVessel != null) {
				OrbitDriver orbDrv = FlightGlobals.ActiveVessel.GetOrbitDriver();
				orbDrv.OnReferenceBodyChange += SOIChanged;
			}
		}

		/// This is called at destroy
		public void OnDisable()
		{
			Settings.Instance.Save();

			// The "dead" copy of our object will re-add itself if we don't unsubscribe to this!
			GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncher);

			// This event fires when KSP wants mods to remove their toolbar buttons
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncher);

			// This event fires when switching focus in the tracking station
			GameEvents.onPlanetariumTargetChanged.Remove(TrackingStationTargetChanged);

			// The launcher destroyed event doesn't always fire when we need it (?)
			RemoveLauncher();

			// This event fires on SOI change
			if (FlightGlobals.ActiveVessel != null) {
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
				StartLoadingModel(FlightGlobals.getMainBody(), FlightGlobals.ActiveVessel);
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

		#region Background loading

		private void StartLoadingModel(CelestialBody b = null, Vessel v = null, bool fromScratch = false)
		{
			if (fromScratch || model == null) {
				DbgFmt("Assembling model");
				model = new AstrogationModel(b, v);
				DbgFmt("Model assembled");
			} else {
				model.Reset(b, v);
			}

			// Avoid running multiple background jobs at the same time
			if (bw == null) {

				DbgFmt("Delegating load to background");

				bw = new BackgroundWorker();
				bw.DoWork += bw_LoadModel;
				bw.RunWorkerCompleted += bw_DoneLoadingModel;
				bw.RunWorkerAsync();

				DbgFmt("Launched background");
			}
		}

		private const int UPDATE_DELAY_MS = 100;

		private void bw_LoadModel(object sender, DoWorkEventArgs e)
		{
			DbgFmt("Beginning background model load");

			for (int i = 0; i < model.transfers.Count; ++i) {
				try {
					// It looks like we can't activate maneuver nodes right away(?).
					// Wait half a second once we're in the background.
					Thread.Sleep(UPDATE_DELAY_MS);

					model.transfers[i].UpdateManeuvers();
				} catch (Exception ex) {
					DbgFmt("Problem with background load: {0}\n{1}",
						ex.Message, ex.StackTrace);
					ScreenFmt("Problem with background load: {0}\n{1}",
						ex.Message, ex.StackTrace);

					// If a route calculation crashes, it can leave behind a temporary node.
					ClearManeuverNodes();
				}
			}
			DbgFmt("Finished background model load");
		}

		private void bw_DoneLoadingModel(object sender, RunWorkerCompletedEventArgs e)
		{
			DbgFmt("Background load complete");
			bw = null;
		}

		#endregion Background loading

		#region Main window

		private AstrogationModel model { get; set; }
		private AstrogationView view { get; set; }
		private BackgroundWorker bw { get; set; }

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
				view = new AstrogationView(model);
				DbgFmt("View mated to booster");
			}
			view.Show();

			DbgFmt("Deployed main window");
		}

		/// <summary>
		/// Close the main window.
		/// </summary>
		public void HideMainWindow()
		{
			if (view != null) {
				view.Dismiss();
				view = null;
			}
			visible = false;
		}

		#endregion Main window

		private void ResetView()
		{
			if (view != null) {
				HideMainWindow();
				ShowMainWindow();
			}
		}

		/// <summary>
		/// React to the sphere of influence of the vessel changing by resetting the model and view.
		/// </summary>
		public void SOIChanged(CelestialBody newBody)
		{
			DbgFmt("Entered {0}'s sphere of influence", newBody.theName);

			// The old list no longer applies because reachable bodies depend on current SOI
			StartLoadingModel(newBody, FlightGlobals.ActiveVessel);
			ResetView();
		}

		/// <summary>
		/// React to the tracking station focus changing by resetting the model and view.
		/// Note that we have to make sure there's no active vessel to avoid doing this
		/// in flight mode's map view.
		/// </summary>
		public void TrackingStationTargetChanged(MapObject target)
		{
			if (FlightGlobals.ActiveVessel == null
					&& model != null
					&& target != null) {

				DbgFmt("Tracking station changed target to {0}", target.ToString());
				StartLoadingModel(target.celestialBody, target.vessel);
				ResetView();
			}
		}
	}

}
