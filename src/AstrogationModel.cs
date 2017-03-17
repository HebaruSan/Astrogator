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
		/// We need to allow an empty object to be valid so we can load most of it in the background.
		/// </summary>
		public AstrogationModel()
		{
			transfers = new List<TransferModel>();
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
			get { return badInclination || (hyperbolicOrbit && !inbound); }
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
		/// Returns whether we're on an inbound hyperbolic orbit.
		/// Used to determine whether to provide a capture burn.
		/// </summary>
		public bool inbound {
			get {
				return hyperbolicOrbit && origin.GetOrbit().trueAnomaly < 0;
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
		public bool notOrbiting { get { return Landed(origin); } }

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

			if (hyperbolicOrbit && inbound) {

				// Just try to calculate a capture burn
				DbgFmt("Orbit is hyperbolic, creating transfer for capture");
				transfers.Add(new TransferModel(origin, origin.GetOrbit().referenceBody));

			} else {

				// Normal orbit, load up everything
				bool foundTarget = false;

				CelestialBody targetBody = FlightGlobals.fetch.VesselTarget as CelestialBody,
					first = start as CelestialBody;
				// If the starting point is a vessel or has no solid surface, then we can't
				// launch from it.
				if (first == null || !first.hasSolidSurface) {
					first = start.GetOrbit()?.referenceBody;
				}

				for (CelestialBody b = first, toSkip = start as CelestialBody;
						b != null;
						toSkip = b, b = ParentBody(b)) {

					DbgFmt("Checking transfers around {0}", TheName(b));

					// It's worth calculating return-from-satellite burns for Eve, Kerbin, and Duna,
					// but not for Jool or the Sun.
					if (b.hasSolidSurface && b != first) {
						DbgFmt("Adding return-to-parent transfer to {0}", TheName(b));
						transfers.Add(new TransferModel(origin, b));
					}

					int numBodies = b.orbitingBodies.Count;
					for (int i = 0; i < numBodies; ++i) {
						CelestialBody satellite = b.orbitingBodies[i];
						if (satellite != toSkip) {
							DbgFmt("Allocating transfer to {0}", TheName(satellite));
							transfers.Add(new TransferModel(origin, satellite));

							if (satellite == targetBody) {
								DbgFmt("Found target as satellite");
								foundTarget = true;
							}
						}
					}

					if (Settings.Instance.ShowTrackedAsteroids) {
						// Add any tracked asteroids in this SOI.
						// Insertion sort into bodies according to semiMajorAxis.
						for (int i = 0; i < FlightGlobals.Vessels.Count; ++i) {
							Vessel v = FlightGlobals.Vessels[i];

							if (v != start as Vessel && IsTrackedAsteroid(v) && v.GetOrbit()?.referenceBody == b) {

								// Loop past the end of the array to provide a chance to
								// append after the last entry.
								for (int t = 0; t < transfers.Count + 1; ++t) {
									if (t >= transfers.Count) {

										transfers.Add(new TransferModel(origin, v));
										if (v == targetBody) {
											foundTarget = true;
										}
										break;

									} else if (transfers[t].destination.GetOrbit().referenceBody == b
									&& (v.GetOrbit()?.semiMajorAxis ?? 0) < transfers[t].destination.GetOrbit().semiMajorAxis) {

										transfers.Insert(t, new TransferModel(origin, v));
										if (v == targetBody) {
											foundTarget = true;
										}
										break;
									}
								}
							}
						}
					}

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
			}
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
