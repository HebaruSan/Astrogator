using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;

	/// An object representing everything we need to know about a particular transfer.
	public class TransferModel {

		/// <summary>
		/// Construct a model object.
		/// </summary>
		public TransferModel(CelestialBody org, ITargetable dest, Vessel v)
		{
			origin = org;
			destination = dest;
			vessel = v;
		}

		/// <summary>
		/// The body we're transferring to.
		/// </summary>
		public ITargetable   destination     { get; private set; }

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

		private BurnModel GenerateEjectionBurn(Orbit currentOrbit)
		{
			if (currentOrbit == null || destination == null) {
				// Sanity check just in case something unexpected happens.
				return null;

			} else {
				// If you want to go somewhere deep inside another SOI, we will
				// just aim at whatever ancestor we can see.
				// So Kerbin -> Laythe would be the same as Kerbin -> Jool.
				ITargetable immediateDestination = null;
				for (ITargetable b = StartBody(destination), prevBody = null;
						b != null;
						prevBody = b, b = ParentBody(b)) {

					if (currentOrbit.referenceBody == b as CelestialBody) {
						immediateDestination = prevBody;
						break;
					}
				}

				if (origin == immediateDestination as CelestialBody) {
					return null;
				}

				if (immediateDestination != null) {
					// Our normal recursive base case - just a normal transfer

					double optimalPhaseAngle = clamp(Math.PI * (
						1 - Math.Pow(
							(currentOrbit.semiMajorAxis + immediateDestination.GetOrbit().semiMajorAxis)
								/ (2 * immediateDestination.GetOrbit().semiMajorAxis),
							1.5)
					));

					// How many radians the phase angle increases or decreases by each second
					double phaseAnglePerSecond =
						  (Tau / immediateDestination.GetOrbit().period)
						- (Tau / currentOrbit.period);

					double currentPhaseAngle = clamp(
						Mathf.Deg2Rad * (
							  immediateDestination.GetOrbit().LAN
							+ immediateDestination.GetOrbit().argumentOfPeriapsis
							- currentOrbit.LAN
							- currentOrbit.argumentOfPeriapsis
						)
						+ immediateDestination.GetOrbit().trueAnomaly
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
						immediateDestination.GetOrbit().referenceBody,
						immediateDestination.GetOrbit().semiMajorAxis,
						currentOrbit.semiMajorAxis
					);

					if (currentOrbit.semiMajorAxis < immediateDestination.GetOrbit().semiMajorAxis) {
						return new BurnModel(
							ejectionBurnTime,
							BurnToNewAp(
								currentOrbit,
								ejectionBurnTime,
								RadiusAtTime(immediateDestination.GetOrbit(), arrivalTime)
									- 0.25 * SphereOfInfluence(immediateDestination)
							),
							0, 0
						);
					} else {
						return new BurnModel(
							ejectionBurnTime,
							BurnToNewPe(
								currentOrbit,
								ejectionBurnTime,
								RadiusAtTime(immediateDestination.GetOrbit(), arrivalTime)
									+ 0.25 * SphereOfInfluence(immediateDestination)
							),
							0, 0
						);
					}

				} else {
					// Recursive case - get an orbit from the parent body and adjust it for ejection from here

					BurnModel outerBurn = GenerateEjectionBurn(ParentOrbit(currentOrbit));
					if (outerBurn != null) {

						double angleOffset = outerBurn.prograde < 0
							? 0
							: -Math.PI;

						// The angle, position, and time are interdependent.
						// So we seed them with parameters from the outer burn, then
						// cross-seed them with each other this many times.
						// Cross your fingers and hope this converges to the right answer.
						const int iterations = 6;

						double burnTime = outerBurn.atTime;

						try {
							for (int i = 0; i < iterations; ++i) {
								DbgFmt("Burn at ({1}): {0}", burnTime, i);
								double ejectionAngle = EjectionAngle(
									currentOrbit.referenceBody,
									RadiusAtTime(currentOrbit, burnTime),
									outerBurn.totalDeltaV);
								burnTime = TimeAtAngleFromMidnight(
									currentOrbit.referenceBody.orbit,
									currentOrbit,
									burnTime,
									ejectionAngle + angleOffset);
							}
						} catch (Exception ex) {
							DbgFmt("Problem with ejection calc: {0}\n{1}",
								ex.Message, ex.StackTrace);
						}
						DbgFmt("Final burn time: {0}", burnTime);

						return new BurnModel(
							burnTime,
							BurnToEscape(
								currentOrbit.referenceBody,
								currentOrbit,
								outerBurn.totalDeltaV,
								burnTime),
							0, 0
						);
					}
					return outerBurn;
				}
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
			} else {
				ejectionBurn = null;
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

				DbgFmt("Temporarily activating ejection burn to {0}", destination.GetName());

				if (ejectionBurn != null) {
					ManeuverNode eNode = ejectionBurn.ToActiveManeuver();

					DbgFmt("Activated ejection burn to {0}", destination.GetName());

					if (eNode != null) {

						if (eNode.nextPatch == null) {
							DbgFmt("This node goes nowhere.");
						}

						// Find the orbit patch that intersects the target orbit
						for (Orbit o = eNode.nextPatch; o != null; o = NextPatch(o)) {
							// Skip the patches that are in the wrong SoI
							if (o.referenceBody == destination.GetOrbit().referenceBody) {

								DbgFmt("Identified matching reference body for {0}", destination.GetName());

								// Find the AN or DN
								bool ascendingNode;
								double planeTime = TimeOfPlaneChange(o, destination.GetOrbit(), ejectionBurn.atTime, out ascendingNode);

								DbgFmt("Pinpointed plane change for {0}", destination.GetName());

								if (planeTime > 0 && planeTime > ejectionBurn.atTime) {
									double magnitude = PlaneChangeDeltaV(o, destination.GetOrbit(), planeTime, ascendingNode);
									// Don't bother to create tiny maneuver nodes
									if (Math.Abs(magnitude) > 0.05) {
										// Add a maneuver node to change planes
										planeChangeBurn = new BurnModel(planeTime, 0,
											magnitude, 0);
										DbgFmt("Transmitted correction burn for {0}: {1}", destination.GetName(), magnitude);
									} else {
										planeChangeBurn = null;
										DbgFmt("No plane change needed for {0}", destination.GetName());
									}

									// Stop looping through orbit patches since we found what we want
									break;
								} else {
									DbgFmt("Plane change burn would be before the ejection burn, skipping");
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
					DbgFmt("Released completed transfer to {0}", destination.GetName());
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
					if (o.referenceBody == destination as CelestialBody) {
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
