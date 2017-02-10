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
		/// <param name="org">Body or vessel to start at</param>
		public AstrogationModel(ITargetable org)
		{
			origin = org;
			transfers = new List<TransferModel>();

			if (!ErrorCondition) {
				CreateTransfers(org);
			}
		}

		/// <summary>
		/// The vessel or body that we're starting from.
		/// </summary>
		public ITargetable origin { get; private set; }

		/// <summary>
		/// Transfers to calculate and show in the window.
		/// </summary>
		public List<TransferModel> transfers { get; private set; }

		/// <summary>
		/// The inclination past which we refuse to do any calculations.
		/// Most of our approximations and heuristics work best at equatorial
		/// and get less accurate rapidly.
		/// </summary>
		public const double maxInclination = Tau / 12.0;

		/// <summary>
		/// True if there's a reason we can't calculate transfers, false if everything is OK.
		/// </summary>
		public bool ErrorCondition {
			get { return badInclination || notOrbiting; }
		}

		/// <summary>
		/// True if the vessel's inclination is too big to be worth bothering.
		/// </summary>
		public bool badInclination {
			get {
				// Orbit.inclination is in degrees
				// A bad inclination is one that is closer to Tau/4 than the limit is
				return origin?.GetOrbit() != null
					&& AngleFromEquatorial(origin.GetOrbit().inclination * Mathf.Deg2Rad) > maxInclination;
			}
		}

		/// <summary>
		/// True if the craft is in a retrograde orbit.
		/// </summary>
		public bool retrogradeOrbit {
			get {
				return origin?.GetOrbit() != null
					&& Math.Abs(origin.GetOrbit().inclination * Mathf.Deg2Rad) > 0.25 * Tau;
			}
		}

		/// <summary>
		/// True if the craft is on a hyperbolic trajectory.
		/// </summary>
		public bool hyperbolicOrbit {
			get { return origin?.GetOrbit()?.eccentricity > 1.0; }
		}

		/// <summary>
		/// True if the craft is sitting on a surface (solid or liquid) rather than on an orbit.
		/// </summary>
		public bool notOrbiting {
			get {
				Vessel vessel = origin.GetVessel();
				return vessel != null
					&& (vessel.situation == Vessel.Situations.PRELAUNCH
						|| vessel.situation == Vessel.Situations.LANDED
						|| vessel.situation == Vessel.Situations.SPLASHED);
			}
		}

		/// <summary>
		/// Re-initialize a model object for the given origin objects.
		/// </summary>
		/// <param name="org">Body or vessel to start at</param>
		public void Reset(ITargetable org)
		{
			origin = org;
			transfers = new List<TransferModel>();

			if (!ErrorCondition) {
				CreateTransfers(org);
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

		private void CreateTransfers(ITargetable start)
		{
			DbgFmt("Fabricating transfers around {0}", TheName(start));

			bool foundTarget = false;

			CelestialBody first = start.GetOrbit()?.referenceBody,
				targetBody = FlightGlobals.fetch.VesselTarget as CelestialBody;

			for (CelestialBody b = first, toSkip = start as CelestialBody;
					b != null;
					toSkip = b, b = ParentBody(b)) {

				DbgFmt("Checking transfers around {0}", b.theName);

				int numBodies = b.orbitingBodies.Count;
				for (int i = 0; i < numBodies; ++i) {
					CelestialBody satellite = b.orbitingBodies[i];
					if (satellite != toSkip) {
						DbgFmt("Allocating transfer to {0}", satellite.theName);
						transfers.Add(new TransferModel(origin, satellite));

						if (satellite == targetBody) {
							DbgFmt("Found target as satellite");
							foundTarget = true;
						}
					}
				}
				DbgFmt("Exhausted transfers around {0}", b.theName);

				if (toSkip == targetBody && targetBody != null) {
					DbgFmt("Found target as ancestor");
					foundTarget = true;
				}
			}

			if (!foundTarget
					&& FlightGlobals.ActiveVessel != null
					&& FlightGlobals.fetch.VesselTarget != null) {
				DbgFmt("Allocating transfer to {0}", FlightGlobals.fetch.VesselTarget.GetName());
				transfers.Insert(0, new TransferModel(origin, FlightGlobals.fetch.VesselTarget));
			}

			DbgFmt("Shipping completed transfers");
		}

		/// <summary>
		/// Check whether the user opened any manuever node editing gizmos since the last tick.
		/// There doesn't seem to be event-based notification for this, so we just have to poll.
		/// </summary>
		public void CheckIfNodesDisappeared()
		{
			for (int i = 0; i < transfers.Count; ++i) {
				transfers[i].CheckIfNodesDisappeared();
			}
		}

		/// <summary>
		/// Find the transfer that currently has an ejection burn instantiated as a real maneuver node, if any.
		/// </summary>
		public TransferModel ActiveTransfer {
			get {
				for (int i = 0; i < transfers.Count; ++i) {
					if (transfers[i].ejectionBurn?.node != null) {
						return transfers[i];
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Find the ejection burn that's currently instantiated as a real maneuver node, if any.
		/// </summary>
		public BurnModel ActiveEjectionBurn {
			get { return ActiveTransfer?.ejectionBurn; }
		}

		/// <summary>
		/// Find the plane change burn that's currently instantiated as a real maneuver node, if any.
		/// </summary>
		public BurnModel ActivePlaneChangeBurn {
			get { return ActiveTransfer?.planeChangeBurn; }
		}
	}

}
