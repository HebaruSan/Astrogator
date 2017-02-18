using System;
using System.Threading;
using System.ComponentModel;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;

	/// <summary>
	/// The logic of loading our model got too complicated to stay embedded in the main class.
	/// Also, we want to share it with the RasterPropMonitor display screen.
	/// This isn't a full "behavior" object, but it can be used by one to take care of a lot of tasks.
	///
	/// This class is responsible for loading the main model object and providing functions to
	/// refresh it as needed.
	/// It defers some operations to the background and keeps track of when it's OK to do that.
	///
	/// There's no point in calculating if the window isn't open, so we suppress calculations in that case.
	/// We also abort any load request that happens while another load is already in progress.
	/// We call it an "open display" so our Raster Prop Monitor widget can get in on the act.
	///
	/// We need to refresh the data when the orbit changes, but if you do a ten-minute burn,
	/// we can't churn the CPU constantly for that entire time.
	/// So we require that at least 5 seconds have passed since the last calculation.
	///
	/// However, if the user toggles the window rapidly, or switches focus in the tracking station,
	/// we need to calculate regardless of whether 5 seconds have passed!
	/// The same goes for if another calculation is in progress.
	///
	/// Burns can expire if their burn time elapses into the past, so we need to check them once per second.
	/// </summary>
	public class AstrogationLoadBehaviorette {

		/// <summary>
		/// Construct a loader object for the given model
		/// </summary>
		/// <param name="m">Model object for us to manage</param>
		/// <param name="unReqNotif">Function to call if we trigger our own refresh without being asked (usually because a burn expired)</param>
		public AstrogationLoadBehaviorette(AstrogationModel m, LoadDoneCallback unReqNotif)
		{
			model = m;
			unrequestedLoadNotification = unReqNotif;

			// Watch for expiring burns
			StartBurnTimePolling();
		}

		private AstrogationModel       model                       { get; set; }
		private bool                   loading                     { get; set; }
		private static readonly object bgLoadMutex = new object();
		private const double           minSecondsBetweenLoads = 5;
		private double                 lastUpdateTime              { get; set; }
		private LoadDoneCallback       unrequestedLoadNotification { get; set; }
		private int                    numOpenDisplays             { get; set; } = 0;

		/// <summary>
		/// Tell the loader that we are currently displaying the data.
		/// If it thinks we aren't, it won't calculate anything.
		/// </summary>
		public void OnDisplayOpened() { ++numOpenDisplays; }

		/// <summary>
		/// Tell the loader that we're closing a display.
		/// If it thinks they're all gone, it won't calculate anything.
		/// </summary>
		public void OnDisplayClosed() { --numOpenDisplays; }

		private bool AllowStart(ITargetable newOrigin) {
			return model != null
				&& numOpenDisplays > 0
				&& (
					// If you've switched origins, we have to update now
					newOrigin != model.origin
					// Otherwise we only update if there isn't already one in progress
					// and the minimum refresh interval has elapsed since the last one.
					|| (!loading && lastUpdateTime + minSecondsBetweenLoads < Planetarium.GetUniversalTime())
				);
		}

		/// <summary>
		/// Callback type for notifications of load completion or failure.
		/// </summary>
		public delegate void LoadDoneCallback();

		/// <summary>
		/// Request a refresh of the data, intended to be called by event handlers.
		/// Will often refuse to refresh to save CPU time!
		/// Note that the callbacks may be called from background jobs, and so they
		/// should never do any Unity UI manipulation (unless you like hard crashes).
		/// Setting member variables seems to be safe.
		/// This is supposed to be the single entry point when any other class needs to
		/// load data for the plug-in.
		/// </summary>
		/// <param name="newOrigin">Body for which to calculate; can override some throttling behaviors if it's different from the last one</param>
		/// <param name="partialLoaded">Function to call when we have enough data for a simple display, but not quite complete</param>
		/// <param name="fullyLoaded">Function to call on successful completion of the load</param>
		/// <param name="aborted">Function to call if we decide not to load</param>
		/// <returns>
		/// True if we kicked off an actual refresh, false otherwise.
		/// </returns>
		public bool TryStartLoad(ITargetable newOrigin, LoadDoneCallback partialLoaded, LoadDoneCallback fullyLoaded, LoadDoneCallback aborted)
		{
			if (newOrigin != null) {
				// 1. Check whether we should even do anything, if not call aborted() and return
				if (!AllowStart(newOrigin)) {
					if (aborted != null) {
						aborted();
					}
					return false;
				} else {
					// 2. Start background job
					DbgFmt("Starting background job");
					BackgroundWorker bgworker = new BackgroundWorker();
					bgworker.DoWork += Load;
					bgworker.RunWorkerAsync(new BackgroundParameters() {
						origin        = newOrigin,
						partialLoaded = partialLoaded,
						fullyLoaded   = fullyLoaded,
						aborted       = aborted
					});
					return true;
				}
			} else {
				DbgFmt("Somebody tried to load with a null origin");
				if (aborted != null) {
					aborted();
				}
				return false;
			}
		}

		private class BackgroundParameters {
			public ITargetable      origin        { get; set; }
			public LoadDoneCallback partialLoaded { get; set; }
			public LoadDoneCallback fullyLoaded   { get; set; }
			public LoadDoneCallback aborted       { get; set; }
		}

		/// <summary>
		/// Main driver for calculations.
		/// Always runs in a background thread!
		/// </summary>
		/// <param name="sender">Standard parameter from BackgroundWorker</param>
		/// <param name="e">Params from main thread, e.Argument is a BackgroundParameters object with our actual info in it</param>
		private void Load(object sender, DoWorkEventArgs e)
		{
			BackgroundParameters bp = e.Argument as BackgroundParameters;
			if (bp != null) {
				lock (bgLoadMutex) {
					loading = true;

					// 3. In background, load the first pass of stuff
					model.Reset(bp.origin);

					if (PlaneChangesEnabled) {

						// Ejection burns are relatively cheap to calculate and needed for the display to look good
						RecalculateEjections();

						// 4. Call partialLoaded() (window can open, view can sort)
						if (bp.partialLoaded != null) {
							bp.partialLoaded();
						}

						// 5. Load everything else
						RecalculatePlaneChanges();

						// 6. Call fullyLoaded() (sort again)
						if (bp.fullyLoaded != null) {
							bp.fullyLoaded();
						}

					} else {

						// Ejection burns are all we need
						RecalculateEjections();

						// 6. Call fullyLoaded() (sort again)
						if (bp.fullyLoaded != null) {
							bp.fullyLoaded();
						}

					}

					lastUpdateTime = Planetarium.GetUniversalTime();
					loading = false;
				}
			} else {
				if (bp.aborted != null) {
					bp.aborted();
				}
			}
		}

		/// <summary>
		/// Check once per second if any of the transfers are out of date.
		/// Note, this could mean we need to re-sort the view!
		/// That's what the unrequestedLoadNotification is for.
		/// </summary>
		private void StartBurnTimePolling()
		{
			System.Timers.Timer t = new System.Timers.Timer() { Interval = 1000 };
			t.Elapsed += BurnTimePoll;
			t.Start();
		}

		private void BurnTimePoll(object source, System.Timers.ElapsedEventArgs e)
		{
			if (numOpenDisplays > 0 && !loading) {
				bool found = false;
				lock (bgLoadMutex) {
					double now = Planetarium.GetUniversalTime();
					for (int i = 0; i < model.transfers.Count; ++i) {
						if (model.transfers[i].ejectionBurn != null
								&& model.transfers[i].ejectionBurn.atTime < now) {

							DbgFmt("Recalculating expired burn");
							found = true;

							model.transfers[i].CalculateEjectionBurn();
							if (PlaneChangesEnabled) {
								model.transfers[i].CalculatePlaneChangeBurn();
							}
						}
					}
				}
				// Tell the main behavior object to refresh the view if the time of any of the burns is changed
				if (found && unrequestedLoadNotification != null) {
					unrequestedLoadNotification();
				}
			}
		}

		private void RecalculateEjections()
		{
			for (int i = 0; i < model.transfers.Count; ++i) {
				try {
					model.transfers[i].CalculateEjectionBurn();
				} catch (Exception ex) {
					DbgExc("Problem with load of ejection burn", ex);
				}
			}
		}

		private bool PlaneChangesEnabled {
			get {
				return Settings.Instance.GeneratePlaneChangeBurns
						&& Settings.Instance.AddPlaneChangeDeltaV;
			}
		}

		private void RecalculatePlaneChanges()
		{
			if (PlaneChangesEnabled) {
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

	}

}
