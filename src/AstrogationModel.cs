using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;

	/// Container for all the transfers we want to know about.
	public class AstrogationModel {

		/// <summary>
		/// Construct a model object for the given origin objects.
		/// </summary>
		/// <param name="b">Body to start at, overridden by v</param>
		/// <param name="v">Vessel to start at</param>
		public AstrogationModel(CelestialBody b = null, Vessel v = null)
		{
			body = b;
			vessel = v;
			transfers = new List<TransferModel>();

			if (!badInclination) {
				CreateTransfers(b, v);
			}
		}

		/// <summary>
		/// Transfers to calculate and show in the window.
		/// </summary>
		public List<TransferModel> transfers { get; private set; }

		/// <summary>
		/// Origin body for all the current transfers.
		/// </summary>
		public CelestialBody body { get; private set; }

		/// <summary>
		/// Vessel for calculating transfers.
		/// </summary>
		public Vessel vessel { get; private set; }

		/// <summary>
		/// The inclination past which we refuse to do any calculations.
		/// Most of our approximations and heuristics work best at equatorial
		/// and get less accurate rapidly.
		/// </summary>
		public const double maxInclination = Tau / 12.0;

		/// <summary>
		/// True if the vessel's inclination is too big to be worth bothering.
		/// </summary>
		public bool badInclination {
			get {
				// Orbit.inclination is in degrees
				// A bad inclination is one that is closer to Tau/4 than the limit is
				return vessel?.orbit != null
					&& Math.Abs(0.25 * Tau - Math.Abs(vessel.orbit.inclination * Mathf.Deg2Rad)) < 0.25 * Tau - maxInclination;
			}
		}

		/// <summary>
		/// True if the craft is in a retrograde orbit.
		/// </summary>
		public bool retrogradeOrbit {
			get {
				return vessel != null
					&& Math.Abs(vessel.orbit.inclination * Mathf.Deg2Rad) > 0.25 * Tau;
			}
		}

		/// <summary>
		/// True if the craft is on a hyperbolic trajectory.
		/// </summary>
		public bool hyperbolicOrbit {
			get {
				return vessel?.GetOrbit().eccentricity > 1.0;
			}
		}

		/// <summary>
		/// True if the craft is sitting on a surface (solid or liquid) rather than on an orbit.
		/// </summary>
		public bool notOrbiting {
			get {
				return vessel != null
					&& (vessel.situation == Vessel.Situations.PRELAUNCH
						|| vessel.situation == Vessel.Situations.LANDED
						|| vessel.situation == Vessel.Situations.SPLASHED);
			}
		}

		/// <summary>
		/// Re-initialize a model object for the given origin objects.
		/// </summary>
		/// <param name="b">Body to start at, overridden by v</param>
		/// <param name="v">Vessel to start at</param>
		public void Reset(CelestialBody b = null, Vessel v = null)
		{
			body = b;
			vessel = v;
			transfers = new List<TransferModel>();

			if (!badInclination) {
				CreateTransfers(b, v);
			}
		}

		/// <returns>
		/// Description of the transfers contained in this model.
		/// </returns>
		public string OriginDescription {
			get {
				if (vessel != null) {
					return vessel.GetName();
				} else if (body != null) {
					return body.theName;
				} else {
					return "";
				}
			}
		}

		/// <summary>
		/// Check whether a destination is already in the table.
		/// Intended to avoid unnecessary UI resets.
		/// </summary>
		/// <param name="target">Destination to look for</param>
		/// <returns>
		/// True if in table, false otherwise.
		/// </returns>
		public bool HasDestination(ITargetable target)
		{
			for (int i = 0; i < transfers.Count; ++i) {
				if (transfers[i].destination == target) {
					return true;
				}
			}
			return false;
		}

		private void CreateTransfers(CelestialBody body, Vessel vessel)
		{
			DbgFmt("Fabricating transfers");

			bool foundTarget = false;
			int discoveryOrder = 0;

			CelestialBody origin = StartBody(body, vessel);
			CelestialBody targetBody = FlightGlobals.fetch.VesselTarget as CelestialBody;

			for (CelestialBody b = origin, toSkip = null;
					b != null;
					toSkip = b, b = ParentBody(b)) {

				// Skip the first body unless we can actually transfer to its children
				// (i.e., we have a vessel)
				if (vessel != null || toSkip != null) {
					DbgFmt("Checking transfers around {0}", b.theName);

					int numBodies = b.orbitingBodies.Count;
					for (int i = 0; i < numBodies; ++i) {
						CelestialBody satellite = b.orbitingBodies[i];
						if (satellite != toSkip) {
							DbgFmt("Allocating transfer to {0}", satellite.theName);
							transfers.Add(new TransferModel(origin, satellite, vessel, ++discoveryOrder));

							if (satellite == targetBody) {
								DbgFmt("Found target as satellite");
								foundTarget = true;
							}
						}
					}
					DbgFmt("Exhausted transfers around {0}", b.theName);
				}

				if (toSkip == targetBody && targetBody != null) {
					DbgFmt("Found target as toSkip");
					foundTarget = true;
				}
			}

			if (!foundTarget
					&& FlightGlobals.ActiveVessel != null
					&& FlightGlobals.fetch.VesselTarget != null) {
				DbgFmt("Allocating transfer to {0}", FlightGlobals.fetch.VesselTarget.GetName());
				transfers.Insert(0, new TransferModel(origin, FlightGlobals.fetch.VesselTarget, vessel, -1));
			}

			DbgFmt("Shipping completed transfers");
		}

		/// <summary>
		/// Check whether the user opened any manuever node editing gizmos since the last tick.
		/// There doesn't seem to be event-based notification for this, so we just have to poll.
		/// </summary>
		public void CheckIfNodesDisappeared()
		{
			if (transfers != null) {
				for (int i = 0; i < transfers.Count; ++i) {
					transfers[i].CheckIfNodesDisappeared();
				}
			}
		}

		/// <summary>
		/// Find the ejection burn that's currently instantiated as a real maneuver node, if any.
		/// </summary>
		public BurnModel ActiveEjectionBurn {
			get {
				for (int i = 0; i < transfers.Count; ++i) {
					if (transfers[i].ejectionBurn?.node != null) {
						return transfers[i].ejectionBurn;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Find the plane change burn that's currently instantiated as a real maneuver node, if any.
		/// </summary>
		public BurnModel ActivePlaneChangeBurn {
			get {
				for (int i = 0; i < transfers.Count; ++i) {
					if (transfers[i].planeChangeBurn?.node != null) {
						return transfers[i].planeChangeBurn;
					}
				}
				return null;
			}
		}
	}

}
