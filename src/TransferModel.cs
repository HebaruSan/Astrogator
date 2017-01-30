using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;

	/// An object representing everything we need to know about a particular transfer.
	public class TransferModel {

		/// <summary>
		/// The body we're transferring to.
		/// </summary>
		public CelestialBody destination     { get; private set; }

		/// <summary>
		/// The body we're transferring from.
		/// </summary>
		public CelestialBody origin          { get; private set; }

		/// <summary>
		/// The vessel we're flying, if any.
		/// </summary>
		public Vessel        vessel          { get; private set; }

		/// <summary>
		/// Representation of the initial burn to start the transfer.
		/// </summary>
		public BurnModel     ejectionBurn    { get; private set; }

		/// <summary>
		/// Representation of the burn to change into the destination's orbital plane.
		/// </summary>
		public BurnModel     planeChangeBurn { get; private set; }

		/// <summary>
		/// Construct a model object.
		/// </summary>
		public TransferModel(CelestialBody org, CelestialBody dest, Vessel v)
		{
			origin = org;
			destination = dest;
			vessel = v;
		}

		private BurnModel GenerateEjectionBurn(Orbit currentOrbit)
		{
			if (currentOrbit == null || destination == null) {
				// Sanity check just in case something unexpected happens.
				return null;

			} else if (currentOrbit.referenceBody == destination.referenceBody) {
				// Our normal recursive base case - just a normal transfer

				double optimalPhaseAngle = clamp(Math.PI * (
					1 - Math.Pow(
						(currentOrbit.semiMajorAxis + destination.orbit.semiMajorAxis)
							/ (2 * destination.orbit.semiMajorAxis),
						1.5)
				));

				// How many radians the phase angle increases or decreases by each second
				double phaseAnglePerSecond =
					  (Tau / destination.orbit.period)
					- (Tau / currentOrbit.period);

				double currentPhaseAngle = clamp(
					Mathf.Deg2Rad * (
						  destination.orbit.LAN
						+ destination.orbit.argumentOfPeriapsis
						- currentOrbit.LAN
						- currentOrbit.argumentOfPeriapsis
					)
					+ destination.orbit.trueAnomaly
					- currentOrbit.trueAnomaly
				);

				// This whole section borrowed from Kerbal Alarm Clock; thanks, TriggerAu!
				double angleToMakeUp = currentPhaseAngle - optimalPhaseAngle;
				if (angleToMakeUp > 0 && phaseAnglePerSecond > 0)
					angleToMakeUp -= Tau;
				if (angleToMakeUp < 0 && phaseAnglePerSecond < 0)
					angleToMakeUp += Tau;

				double timeTillBurn = Math.Abs(angleToMakeUp / phaseAnglePerSecond);
				double ejectionBurnTime = Planetarium.GetUniversalTime() + timeTillBurn;
				double arrivalTime = ejectionBurnTime + 0.5 * OrbitalPeriod(
					destination.orbit.referenceBody,
					destination.orbit.semiMajorAxis,
					currentOrbit.semiMajorAxis
				);

				if (currentOrbit.semiMajorAxis < destination.orbit.semiMajorAxis) {
					return new BurnModel(
						ejectionBurnTime,
						BurnToNewAp(
							currentOrbit,
							ejectionBurnTime,
							RadiusAtTime(destination.orbit, arrivalTime)
								- 0.25 * destination.sphereOfInfluence
						),
						0, 0
					);
				} else {
					return new BurnModel(
						ejectionBurnTime,
						BurnToNewPe(
							currentOrbit,
							ejectionBurnTime,
							RadiusAtTime(destination.orbit, arrivalTime)
								+ 0.25 * destination.sphereOfInfluence
						),
						0, 0
					);
				}

			} else {
				// Recursive case - get an orbit from the parent body and adjust it for ejection from here

				BurnModel outerBurn = GenerateEjectionBurn(ParentOrbit(currentOrbit));
				if (outerBurn != null) {

					// Temporary hack: make exits slightly smoother by going 5 minutes earlier.
					// Eventually we'll use a real hyperbolic orbit calculation here.
					const double EJECTION_FUDGE_FACTOR = -5 * 60;

					if (outerBurn.prograde < 0) {
						// Adjust maneuverTime to day side
						return new BurnModel(
							EJECTION_FUDGE_FACTOR + TimeAtNextNoon(
								currentOrbit.referenceBody.orbit,
								currentOrbit,
								outerBurn.atTime),
							BurnToEscape(
								currentOrbit.referenceBody,
								currentOrbit,
								outerBurn.totalDeltaV,
								outerBurn.atTime),
							0, 0
						);
					} else {
						// Adjust maneuverTime to night side
						return new BurnModel(
							EJECTION_FUDGE_FACTOR + TimeAtNextMidnight(
								currentOrbit.referenceBody.orbit,
								currentOrbit,
								outerBurn.atTime),
							BurnToEscape(
								currentOrbit.referenceBody,
								currentOrbit,
								outerBurn.totalDeltaV,
								outerBurn.atTime),
							0, 0
						);
					}
				}
				return outerBurn;
			}
		}

		/// <summary>
		/// Calculate the time and delta V of the burn needed to transfer.
		/// </summary>
		public void CalculateEjectionBurn()
		{
			if (vessel != null) {
				ejectionBurn = GenerateEjectionBurn(vessel.orbit);
			} else if (origin != null) {
				ejectionBurn = GenerateEjectionBurn(origin.orbit);
			}
		}

		/// <summary>
		/// Calculate the time and delta V of the burn needed to change planes.
		/// </summary>
		public void CalculatePlaneChangeBurn()
		{
			if (FlightGlobals.ActiveVessel == vessel
					&& vessel != null
					&& vessel.patchedConicSolver != null
					&& vessel.patchedConicSolver.maneuverNodes != null) {

				if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
					if (Settings.Instance.DeleteExistingManeuvers) {
						ClearManeuverNodes();
					} else {
						return;
					}
				}

				DbgFmt("Temporarily activating ejection burn to {0}", destination.theName);

				if (ejectionBurn != null) {
					ManeuverNode eNode = ejectionBurn.ToActiveManeuver();

					DbgFmt("Activated ejection burn to {0}", destination.theName);

					if (eNode != null) {

						if (eNode.nextPatch == null) {
							DbgFmt("This node goes nowhere.");
						}

						// Find the orbit patch that intersects the target orbit
						for (Orbit o = eNode.nextPatch; o != null; o = NextPatch(o)) {
							// Skip the patches that are in the wrong SoI
							if (o.referenceBody == destination.orbit.referenceBody) {

								DbgFmt("Identified matching reference body for {0}", destination.theName);

								// Find the AN or DN
								double planeTime = TimeOfPlaneChange(o, destination.orbit, ejectionBurn.atTime);

								DbgFmt("Pinpointed plane change for {0}", destination.theName);

								if (planeTime > 0 && planeTime > ejectionBurn.atTime) {
									double magnitude = FindPlaneChangeMagnitude(o, destination.orbit, planeTime);
									// Don't bother to create tiny maneuver nodes
									if (magnitude > 0.05) {
										// Add a maneuver node to change planes
										planeChangeBurn = new BurnModel(planeTime, 0,
											magnitude, 0);
									} else {
										planeChangeBurn = null;
									}

									DbgFmt("Transmitted correction burn for {0}", destination.theName);

									// Stop looping through orbit patches since we found what we want
									break;
								}
							} else {
								DbgFmt("Skipping a patch with the wrong parent body");
							}
						}
					} else {
						DbgFmt("Ejection burn existed but generated a null node");
					}

					// Clean up the node since we're just doing calculations, not intending to set things up for the user
					ejectionBurn.RemoveNode();
					DbgFmt("Released completed transfer to {0}", destination.theName);
				} else {
					DbgFmt("Ejection burn is missing somehow");
				}
			} else {
				DbgFmt("Can't do a plane change without a vessel");
			}
		}

		/// <summary>
		/// Check whether the current vessel currently has an encounter with this transfer's destination.
		/// Assumes that all of our maneuver nodes have been placed.
		/// </summary>
		/// <returns>
		///
		/// </returns>
		public bool HaveEncounter()
		{
			if (FlightGlobals.ActiveVessel == vessel
					&& vessel != null
					&& vessel.patchedConicSolver != null
					&& vessel.patchedConicSolver.maneuverNodes != null) {

				for (Orbit o = ejectionBurn.node.nextPatch; o != null; o = NextPatch(o)) {
					if (o.referenceBody == destination) {
						return true;
					}
				}
			}
			return false;
		}

		/// Returns true if UI needs an update
		public bool Refresh()
		{
			if (ejectionBurn != null) {
				if (ejectionBurn.atTime < Planetarium.GetUniversalTime()) {
					CalculateEjectionBurn();
					CalculatePlaneChangeBurn();
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}
	}

}
