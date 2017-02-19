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
		public TransferModel(ITargetable org, ITargetable dest)
		{
			origin = org;
			destination = dest;
		}

		/// <summary>
		/// The body we're transferring to.
		/// </summary>
		public ITargetable   destination         { get; private set; }

		/// <summary>
		/// The SOI that we're aiming at, possibly an ancestor of our ultimate
		/// destination if the user targeted a distant moon.
		/// </summary>
		public ITargetable   transferDestination { get; private set; }

		/// <summary>
		/// The reference body of the transfer portion of our route.
		/// </summary>
		public CelestialBody transferParent      { get; private set; }

		/// <summary>
		/// True if the transfer portion of this trajectory is retrograde, false otherwise.
		/// So for a retrograde Kerbin orbit, this is true for Mun and false for Duna.
		/// </summary>
		public bool          retrogradeTransfer  { get; private set; }

		/// <summary>
		/// The body we're transferring from.
		/// </summary>
		public ITargetable   origin              { get; private set; }

		/// <summary>
		/// Representation of the initial burn to start the transfer.
		/// </summary>
		public BurnModel     ejectionBurn        { get; private set; }

		/// <summary>
		/// Representation of the burn to change into the destination's orbital plane.
		/// </summary>
		public BurnModel     planeChangeBurn     { get; private set; }

		/// <summary>
		/// True if the craft is sitting on a surface (solid or liquid) rather than on an orbit.
		/// </summary>
		public bool Landed {
			get {
				Vessel vessel = origin.GetVessel();
				return vessel != null
					&& (vessel.situation == Vessel.Situations.PRELAUNCH
						|| vessel.situation == Vessel.Situations.LANDED
						|| vessel.situation == Vessel.Situations.SPLASHED);
			}
		}

		private BurnModel GenerateEjectionBurn(Orbit currentOrbit)
		{
			DbgFmt("Looking for a route from {0} to {1}, via {2}", TheName(origin), TheName(destination), TheName(currentOrbit.referenceBody));

			double now = Planetarium.GetUniversalTime();
			if (currentOrbit == null) {
				DbgFmt("Skipping transfer from null starting orbit.");
				// Sanity check just in case something unexpected happens.
				return null;

			} else if (destination == null) {
				DbgFmt("Skipping transfer to null destination.");
				// Sanity check just in case something unexpected happens.
				return null;

			} else if (destination.GetOrbit().eccentricity > 1) {
				DbgFmt("{0} is on an escape trajectory; bailing", TheName(destination));

				return null;

			} else if (Landed && currentOrbit == origin.GetOrbit()) {
				// TODO - Include space center scene here

				CelestialBody body = origin.GetOrbit().referenceBody;

				if (!body.rotates) {
					// A non-rotating body means we never get any closer to the
					// point where we want to escape. No calculation possible.
					// (This also lets us sidestep a divide by zero risk.)
					return null;
				}

				// This will give us a burn with the right delta V from low orbit.
				// The time will be now plus the time it takes to get from the absolute
				// reference direction to the burn at the orbital speed of fakeOrbit.
				double targetRadius = GoodLowOrbitRadius(body);
				Orbit fakeOrbit = new Orbit(0, 0, targetRadius, 0, 0, 0, 0, body);
				BurnModel ejection = GenerateEjectionBurn(fakeOrbit);

				// Now we figure out where the vessel (or KSC) will be at the time of that burn.
				double currentPhaseAngle = AbsolutePhaseAngle(
					body,
					ejection.atTime,
					origin.GetVessel()?.longitude ?? SpaceCenter.Instance.Longitude
				);

				// This will tell us approximately where our ship should be to launch
				double targetAbsolutePhaseAngle = AbsolutePhaseAngle(fakeOrbit, ejection.atTime);

				// This tells us how fast the body rotates.
				// Note that we already have our divide-by-zero guard above.
				double phaseAnglePerSecond = Tau / body.rotationPeriod;

				// Now we adjust the original burn time to account for the planet rotating
				// into position for us.
				// TODO - The Mun may have moved a significant distance during the offset!
				//        Adjust somehow.
				double burnTime = ejection.atTime + clamp(targetAbsolutePhaseAngle - currentPhaseAngle) / phaseAnglePerSecond;

				// Number of seconds to adjust burn time to account for launch
				const double LAUNCH_OFFSET = 0;

				// Finally, generate the real burn if it seems OK.
				if (burnTime + LAUNCH_OFFSET < now) {
					return null;
				} else {
					return new BurnModel(
						burnTime + LAUNCH_OFFSET,
						ejection.prograde + DeltaVToOrbit(body, origin)
					);
				}

			} else if (currentOrbit.eccentricity > 1.0) {

				if (currentOrbit.TrueAnomalyAtUT(now) < 0) {
					DbgFmt("Generating capture burn");

					double burnTime = currentOrbit.GetUTforTrueAnomaly(0, now),
						currentPeSpeed = currentOrbit.getOrbitalVelocityAtTrueAnomaly(0).magnitude;
					double periapsis = RadiusAtTime(currentOrbit, burnTime);
					double capturedPeSpeed = SpeedAtPeriapsis(
						currentOrbit.referenceBody, periapsis, periapsis);
					return new BurnModel(burnTime, capturedPeSpeed - currentPeSpeed);

				} else {
					DbgFmt("Too late for a decent capture burn");
				}
				return null;

			} else {

				// These need to be reset before the below loop to avoid messing things up
				transferParent = null;
				transferDestination = null;

				// If you want to go somewhere deep inside another SOI, we will
				// just aim at whatever ancestor we can see.
				// So Kerbin -> Laythe would be the same as Kerbin -> Jool.
				for (ITargetable b = StartBody(destination), prevBody = null;
						b != null;
						prevBody = b, b = ParentBody(b)) {

					if (currentOrbit.referenceBody == b as CelestialBody) {
						// Note which body is boss of the patch where we transfer

						if (prevBody == null) {
							// Return to LKO burn from Mun/Minmus
							DbgFmt("Found intermediate recursive origin {0} in destination ancestors; calculating return burn", TheName(currentOrbit.referenceBody));

							return new BurnModel(now, BurnToNewPe(
								currentOrbit,
								now,
								GoodLowOrbitRadius(currentOrbit.referenceBody)
							));

						} else {

							transferParent = b as CelestialBody;
							transferDestination = prevBody;
							DbgFmt("Found transfer patch, parent: {0}, destination: {1}",
								TheName(transferParent), TheName(transferDestination));
							break;
						}
					}
				}

				if (transferDestination != null) {
					// Our normal recursive base case - just a normal transfer

					// How many radians the phase angle increases or decreases by each second
					double phaseAnglePerSecond, angleToMakeUp;
					retrogradeTransfer = (currentOrbit.GetRelativeInclination(transferDestination.GetOrbit()) > 90f);

					if (!retrogradeTransfer) {

						// Normal prograde orbits
						double optimalPhaseAngle = clamp(Math.PI * (
							1 - Math.Pow(
								(currentOrbit.semiMajorAxis + transferDestination.GetOrbit().semiMajorAxis)
									/ (2 * transferDestination.GetOrbit().semiMajorAxis),
								1.5)
						));
						double currentPhaseAngle = clamp(
							  AbsolutePhaseAngle(transferDestination.GetOrbit(), now)
							- AbsolutePhaseAngle(currentOrbit, now));
						angleToMakeUp = currentPhaseAngle - optimalPhaseAngle;
						phaseAnglePerSecond =
							  (Tau / transferDestination.GetOrbit().period)
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
								(currentOrbit.semiMajorAxis + transferDestination.GetOrbit().semiMajorAxis)
									/ (2 * transferDestination.GetOrbit().semiMajorAxis),
								1.5)
						));
						// The phase angle always decreases by the sum of the angular velocities
						double currentPhaseAngle = Tau - clamp(
							  AbsolutePhaseAngle(transferDestination.GetOrbit(), now)
							- AbsolutePhaseAngle(currentOrbit, now));
						// Angle to make up is always positive
						angleToMakeUp = clamp(currentPhaseAngle - optimalPhaseAngle);
						phaseAnglePerSecond =
							  (Tau / transferDestination.GetOrbit().period)
							+ (Tau / currentOrbit.period);

					}

					double timeTillBurn = Math.Abs(angleToMakeUp / phaseAnglePerSecond);
					double ejectionBurnTime = now + timeTillBurn;
					double arrivalTime = ejectionBurnTime + 0.5 * OrbitalPeriod(
						transferDestination.GetOrbit().referenceBody,
						transferDestination.GetOrbit().semiMajorAxis,
						currentOrbit.semiMajorAxis
					);

					if (currentOrbit.semiMajorAxis < transferDestination.GetOrbit().semiMajorAxis) {
						return new BurnModel(
							ejectionBurnTime,
							BurnToNewAp(
								currentOrbit,
								ejectionBurnTime,
								RadiusAtTime(transferDestination.GetOrbit(), arrivalTime)
									- 0.25 * SphereOfInfluence(transferDestination)
							)
						);
					} else {
						return new BurnModel(
							ejectionBurnTime,
							BurnToNewPe(
								currentOrbit,
								ejectionBurnTime,
								RadiusAtTime(transferDestination.GetOrbit(), arrivalTime)
									+ 0.25 * SphereOfInfluence(transferDestination)
							)
						);
					}

				} else {
					// Recursive case - get an orbit from the parent body and adjust it for ejection from here

					DbgFmt("Direct route to {0} not found, recursing through parent {1}", TheName(destination), TheName(currentOrbit.referenceBody));
					BurnModel outerBurn = GenerateEjectionBurn(ParentOrbit(currentOrbit));
					if (outerBurn != null) {
						DbgFmt("Got route from {0}, calculating ejection", TheName(currentOrbit.referenceBody));

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
					} else {
						DbgFmt("No outer burn found");
						return null;
					}
				}
			}
		}

		/// <summary>
		/// Calculate the time and delta V of the burn needed to transfer.
		/// </summary>
		public void CalculateEjectionBurn()
		{
			if (origin != null) {
				ejectionBurn = GenerateEjectionBurn(origin.GetOrbit());
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
			if (FlightGlobals.ActiveVessel?.patchedConicSolver?.maneuverNodes != null
					&& transferDestination != null
					&& transferParent != null
					&& destination.GetOrbit().eccentricity < 1) {

				bool ejectionAlreadyActive = false;

				if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0) {
					if (Settings.Instance.DeleteExistingManeuvers) {

						ClearManeuverNodes();

					} else if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count == 1
							&& ejectionBurn.node != null) {

						ejectionAlreadyActive = true;

					} else {
						// At least one unrelated maneuver is active, and we're not allowed to delete them.
						// Can't activate ejection burn for calculation.
						return;
					}
				}

				if (ejectionBurn != null) {

					ManeuverNode eNode;
					if (ejectionAlreadyActive) {
						eNode = ejectionBurn.node;
					} else {
						DbgFmt("Temporarily activating ejection burn to {0}", destination.GetName());
						eNode = ejectionBurn.ToActiveManeuver();
						DbgFmt("Activated ejection burn to {0}", destination.GetName());
					}

					if (eNode != null) {

						if (eNode.nextPatch == null) {
							DbgFmt("This node goes nowhere.");
						}

						// Find the orbit patch that intersects the target orbit
						for (Orbit o = eNode.nextPatch; o != null; o = NextPatch(o)) {
							// Skip the patches that are in the wrong SoI
							if (o.referenceBody == transferParent) {

								DbgFmt("Identified matching reference body for {0}", transferParent.GetName());

								// Find the AN or DN
								bool ascendingNode;
								double planeTime = TimeOfPlaneChange(o, transferDestination.GetOrbit(), ejectionBurn.atTime, out ascendingNode);

								DbgFmt("Pinpointed plane change for {0}", transferParent.GetName());

								if (planeTime > 0 && planeTime > ejectionBurn.atTime) {
									double magnitude = PlaneChangeDeltaV(o, transferDestination.GetOrbit(), planeTime, ascendingNode);
									// Don't bother to create tiny maneuver nodes
									if (Math.Abs(magnitude) > 0.05) {
										// Add a maneuver node to change planes
										planeChangeBurn = new BurnModel(planeTime, 0,
											magnitude);
										DbgFmt("Transmitted correction burn for {0}: {1}", transferDestination.GetName(), magnitude);
									} else {
										planeChangeBurn = null;
										DbgFmt("No plane change needed for {0}", transferDestination.GetName());
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
			for (Orbit o = ejectionBurn?.node?.nextPatch; o != null; o = NextPatch(o)) {
				if (o.referenceBody == destination as CelestialBody) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Check whether the user opened any manuever node editing gizmos since the last tick.
		/// There doesn't seem to be event-based notification for this, so we just have to poll.
		/// </summary>
		public void CheckIfNodesDisappeared()
		{
			ejectionBurn?.CheckIfNodeDisappeared();
			planeChangeBurn?.CheckIfNodeDisappeared();
		}

		/// <summary>
		/// Turn this transfer's burns into user visible maneuver nodes.
		/// This is the behavior for the maneuver node icon.
		/// </summary>
		public void CreateManeuvers()
		{
			if (FlightGlobals.ActiveVessel != null) {

				// Remove all maneuver nodes because they'd conflict with the ones we're about to add
				ClearManeuverNodes();

				if (Settings.Instance.AutoTargetDestination) {
					// Switch to target mode, targeting the destination body
					FlightGlobals.fetch.SetVesselTarget(destination);
				}

				// Create a maneuver node for the ejection burn
				ejectionBurn.ToActiveManeuver();

				if (Settings.Instance.GeneratePlaneChangeBurns) {
					if (planeChangeBurn == null) {
						DbgFmt("Calculating plane change on the fly");
						CalculatePlaneChangeBurn();
					}

					if (planeChangeBurn != null) {
						planeChangeBurn.ToActiveManeuver();
					} else {
						DbgFmt("No plane change found");
					}
				} else {
					DbgFmt("Plane changes disabled");
				}

				if (Settings.Instance.AutoEditEjectionNode) {
					// Open the initial node for fine tuning
					ejectionBurn.EditNode();
				} else if (Settings.Instance.AutoEditPlaneChangeNode) {
					if (planeChangeBurn != null) {
						planeChangeBurn.EditNode();
					}
				}

				if (Settings.Instance.AutoFocusDestination) {
					if (HaveEncounter()) {
						// Move the map to the target for fine-tuning if we have an encounter
						FocusMap(destination);
					} else if (transferParent != null) {
						// Otherwise focus on the parent of the transfer orbit so we can get an encounter
						// Try to explain why this is happening with a screen message
						ScreenFmt("Adjust maneuvers to establish encounter");
						FocusMap(transferParent, transferDestination);
					}
				}

				if (Settings.Instance.AutoSetSAS
						&& FlightGlobals.ActiveVessel != null
						&& FlightGlobals.ActiveVessel.Autopilot.CanSetMode(VesselAutopilot.AutopilotMode.Maneuver)) {
					// The API for SAS is ... peculiar.
					// http://forum.kerbalspaceprogram.com/index.php?/topic/153420-enabledisable-autopilot/
					try {
						if (FlightGlobals.ActiveVessel.Autopilot.Enabled) {
							FlightGlobals.ActiveVessel.Autopilot.SetMode(VesselAutopilot.AutopilotMode.Maneuver);
						} else {
							DbgFmt("Not enabled, trying to enable");
							FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
							FlightGlobals.ActiveVessel.Autopilot.Enable(VesselAutopilot.AutopilotMode.Maneuver);
						}
					} catch (Exception ex) {
						DbgExc("Problem setting SAS to maneuver mode", ex);
					}
				}
			}
		}

		/// <summary>
		/// Warp to (near) the burn.
		/// Since you usually need to start burning before the actual node,
		/// we use some simple padding logic to determine how far to warp.
		/// If you're more than five minutes from the burn, then we warp
		/// to that five minute mark. This should allow for most of the long burns.
		/// If you're closer than five minutes from the burn, then we warp
		/// right up to the moment of the actual burn.
		/// If you're _already_ warping, cancel the warp (suggested by Kottabos).
		/// </summary>
		public void WarpToBurn()
		{
			if (TimeWarp.CurrentRate > 1) {
				DbgFmt("Warp button clicked while already in warp, cancelling warp");
				TimeWarp.fetch?.CancelAutoWarp();
				TimeWarp.SetRate(0, false);
			} else {
				DbgFmt("Attempting to warp to burn from {0} to {1}", Planetarium.GetUniversalTime(), ejectionBurn.atTime);
				if (Planetarium.GetUniversalTime() < ejectionBurn.atTime - BURN_PADDING ) {
					DbgFmt("Warping to burn minus offset");
					TimeWarp.fetch.WarpTo(ejectionBurn.atTime - BURN_PADDING);
				} else if (Planetarium.GetUniversalTime() < ejectionBurn.atTime) {
					DbgFmt("Already within offset; warping to burn");
					TimeWarp.fetch.WarpTo(ejectionBurn.atTime);
				} else {
					DbgFmt("Can't warp to the past!");
				}
			}
		}

	}

}
