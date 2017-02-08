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
		public TransferModel(CelestialBody org, ITargetable dest, Vessel v, int order)
		{
			origin = org;
			destination = dest;
			vessel = v;
			DiscoveryOrder = order;
		}

		/// <summary>
		/// The body we're transferring to.
		/// </summary>
		public ITargetable   destination     { get; private set; }

		/// <summary>
		/// The reference body of the transfer portion of our route.
		/// </summary>
		public CelestialBody transferParent  { get; private set; }

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
		/// Number representing the position of this row when sorted by position.
		/// </summary>
		public int           DiscoveryOrder  { get; private set; }

		private BurnModel GenerateEjectionBurn(Orbit currentOrbit)
		{
			if (currentOrbit == null || destination == null) {
				// Sanity check just in case something unexpected happens.
				return null;

			} else if (vessel != null
					&& (vessel.situation == Vessel.Situations.PRELAUNCH
						|| vessel.situation == Vessel.Situations.LANDED
						|| vessel.situation == Vessel.Situations.SPLASHED)) {

				DbgFmt("Delta V to orbit: {0}", DeltaVToOrbit(origin, vessel));

				return null;

			} else if (currentOrbit.eccentricity > 1.0) {

				DbgFmt("No point in trying to calculate on hyperbolic orbit.");

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
					// Trying to get to the start SOI or one of its sub-SOIs
					return null;
				}

				if (immediateDestination != null) {
					// Our normal recursive base case - just a normal transfer

					// Note which body is boss in the zone where we transfer
					transferParent = immediateDestination.GetOrbit().referenceBody;

					double now = Planetarium.GetUniversalTime();

					// How many radians the phase angle increases or decreases by each second
					double phaseAnglePerSecond, angleToMakeUp;
					if (currentOrbit.GetRelativeInclination(immediateDestination.GetOrbit()) < 90f) {

						// Normal prograde orbits
						double optimalPhaseAngle = clamp(Math.PI * (
							1 - Math.Pow(
								(currentOrbit.semiMajorAxis + immediateDestination.GetOrbit().semiMajorAxis)
									/ (2 * immediateDestination.GetOrbit().semiMajorAxis),
								1.5)
						));
						double currentPhaseAngle = clamp(
							  AbsolutePhaseAngle(immediateDestination.GetOrbit(), now)
							- AbsolutePhaseAngle(currentOrbit, now));
						angleToMakeUp = currentPhaseAngle - optimalPhaseAngle;
						phaseAnglePerSecond =
							  (Tau / immediateDestination.GetOrbit().period)
							- (Tau / currentOrbit.period);
						// This whole section borrowed from Kerbal Alarm Clock; thanks, TriggerAu!
						if (angleToMakeUp > 0 && phaseAnglePerSecond > 0)
							angleToMakeUp -= Tau;
						if (angleToMakeUp < 0 && phaseAnglePerSecond < 0)
							angleToMakeUp += Tau;

					} else {

						// Special logic needed for retrograde orbits
						// The phase angle is the opposite part of the unit circle
						double optimalPhaseAngle = Tau - clamp(Math.PI * (
							1 - Math.Pow(
								(currentOrbit.semiMajorAxis + immediateDestination.GetOrbit().semiMajorAxis)
									/ (2 * immediateDestination.GetOrbit().semiMajorAxis),
								1.5)
						));
						// The phase angle always decreases by the sum of the angular velocities
						double currentPhaseAngle = Tau - clamp(
							  AbsolutePhaseAngle(immediateDestination.GetOrbit(), now)
							- AbsolutePhaseAngle(currentOrbit, now));
						angleToMakeUp = clamp(currentPhaseAngle - optimalPhaseAngle);
						phaseAnglePerSecond =
							  (Tau / immediateDestination.GetOrbit().period)
							+ (Tau / currentOrbit.period);

					}

					double timeTillBurn = Math.Abs(angleToMakeUp / phaseAnglePerSecond);
					double ejectionBurnTime = now + timeTillBurn;
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
							)
						);
					} else {
						return new BurnModel(
							ejectionBurnTime,
							BurnToNewPe(
								currentOrbit,
								ejectionBurnTime,
								RadiusAtTime(immediateDestination.GetOrbit(), arrivalTime)
									+ 0.25 * SphereOfInfluence(immediateDestination)
							)
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
							DbgExc("Problem with ejection calc", ex);
						}

						return new BurnModel(
							burnTime,
							BurnToEscape(
								currentOrbit.referenceBody,
								currentOrbit,
								outerBurn.totalDeltaV,
								burnTime)
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

			if (planeChangeBurn != null && ejectionBurn != null) {
				if (planeChangeBurn.atTime < ejectionBurn.atTime) {
					DbgFmt("Resetting plane change burn because it's too early now");
					planeChangeBurn = null;
				}
			}
		}

		/// <summary>
		/// Calculate the time and delta V of the burn needed to change planes.
		/// </summary>
		public void CalculatePlaneChangeBurn()
		{
			if (FlightGlobals.ActiveVessel == vessel
					&& vessel?.patchedConicSolver?.maneuverNodes != null) {

				bool ejectionAlreadyActive = false;

				if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
					if (Settings.Instance.DeleteExistingManeuvers) {

						ClearManeuverNodes();

					} else if (vessel.patchedConicSolver.maneuverNodes.Count == 1
							&& ejectionBurn.node != null) {

						ejectionAlreadyActive = true;

					} else {
						// At least one unrelated maneuver is active, and we're not allowed to delete them.
						// Can't activate ejection burn for calculation.
						return;
					}
				}

				DbgFmt("Temporarily activating ejection burn to {0}", destination.GetName());

				if (ejectionBurn != null) {

					ManeuverNode eNode;
					if (ejectionAlreadyActive) {
						eNode = ejectionBurn.node;
					} else {
						eNode = ejectionBurn.ToActiveManeuver();
					}

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
											magnitude);
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

					if (!ejectionAlreadyActive) {
						// Clean up the node since we're just doing calculations, not intending to set things up for the user
						ejectionBurn.RemoveNode();
					}
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
					&& vessel?.patchedConicSolver?.maneuverNodes != null) {

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

					// Apply the same filters we do everywhere else to suppress phantom nodes
					if (Settings.Instance.GeneratePlaneChangeBurns
							&& Settings.Instance.AddPlaneChangeDeltaV) {

						try {
							CalculatePlaneChangeBurn();
						} catch (Exception ex) {
							DbgExc("Problem with plane change at expiration", ex);
							ClearManeuverNodes();
						}
					}
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		/// <summary>
		/// Check whether the user opened any manuever node editing gizmos since the last tick.
		/// There doesn't seem to be event-based notification for this, so we just have to poll.
		/// </summary>
		public void CheckForOpenGizmos()
		{
			ejectionBurn?.CheckForOpenGizmo();
			planeChangeBurn?.CheckForOpenGizmo();
		}
	}

}
