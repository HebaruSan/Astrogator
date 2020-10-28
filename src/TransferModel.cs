using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;
using KSP.Localization;

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
			origin      = org;
			destination = dest;
		}

		/// <summary>
		/// The body we're transferring to.
		/// </summary>
		public ITargetable   destination          { get; private set; }

		/// <summary>
		/// The SOI that we're aiming at, possibly an ancestor of our ultimate
		/// destination if the user targeted a distant moon.
		/// </summary>
		public ITargetable   transferDestination  { get; private set; }

		/// <summary>
		/// The reference body of the transfer portion of our route.
		/// </summary>
		public CelestialBody transferParent       { get; private set; }

		/// <summary>
		/// True if the transfer portion of this trajectory is retrograde, false otherwise.
		/// So for a retrograde Kerbin orbit, this is true for Mun and false for Duna.
		/// </summary>
		public bool          retrogradeTransfer   { get; private set; }

		/// <summary>
		/// The body we're transferring from.
		/// </summary>
		public ITargetable   origin               { get; private set; }

		/// <summary>
		/// Representation of the initial burn to start the transfer.
		/// </summary>
		public BurnModel     ejectionBurn         { get; private set; }

		/// <summary>
		/// Representation of the burn to change into the destination's orbital plane.
		/// </summary>
		public BurnModel     planeChangeBurn      { get; private set; }

		/// <summary>
		/// Number of seconds to complete this burn for current vessel
		/// </summary>
		public double?       ejectionBurnDuration { get; private set; }

		private BurnModel PlotCaptureBurn(Orbit currentOrbit)
		{
			double now = Planetarium.GetUniversalTime();
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
				return null;
			}
		}

		private BurnModel PlotTransferBurn(Orbit currentOrbit, bool retrograde)
		{
			// Normal prograde orbits
			double now = Planetarium.GetUniversalTime();
			// This is how fast the angle between the planets changes
			// Positive if destination is slower, negative if current is slower
			double phaseAnglePerSecond =
				  (Tau / transferDestination.GetOrbit().period)
				- (Tau / currentOrbit.period) * (retrograde ? -1 : 1);
			if (phaseAnglePerSecond == 0) {
				// Can't launch to surface-stationary target because
				// we'll never reach a good relative phase angle for it.
				// Ideally we'd check <Epsilon here, but that risks breaking
				// normal transfers to asteroids, since they have very low
				// relative angular velocities.
				return null;
			}
			// We'll search a time span this wide for the best burn time
			double searchInterval = 0.5 * Math.PI / Math.Abs(phaseAnglePerSecond);

			// Start searching immediately, will look at the next PI/2 and so on if not found
			double ejectionBurnTime = BurnTimeSearch(
				currentOrbit, transferDestination.GetOrbit(),
				now, now + searchInterval
			);
			double arrivalTime = ejectionBurnTime + TransferTravelTime(
				currentOrbit, transferDestination.GetOrbit(), ejectionBurnTime
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
		}

		private BurnModel PlotLaunchToTransfer()
		{
			// Launch to transfer, basically the same as normal
			// except we use the planet's rotation instead of starting orbit.
			// Assumes a gravity turn directly into Hohmann transfer.

			// Give us 30deg extra to compensate for launch delays
			const double LAUNCH_FUDGE_FACTOR = Tau / 12;

			double now = Planetarium.GetUniversalTime();
			bool haveVessel = (origin.GetVessel() != null);
			CelestialBody body = origin as CelestialBody ?? origin.GetOrbit().referenceBody;
			bool atHome = (body == FlightGlobals.GetHomeBody());
			bool haveLongitude = haveVessel || atHome;
			double targetRadius = GoodLowOrbitRadius(body);

			if (transferDestination == null) {
				FindIntermediateDestination(body, null);
				if (transferDestination == null) {
					DbgFmt("Failed to find transfer destination");
					return null;
				}
			}

			if (haveLongitude) {
				double optimalPhaseAngle = clamp(-LAUNCH_FUDGE_FACTOR + Math.PI * (
					1 - Math.Pow(
						(targetRadius + transferDestination.GetOrbit().semiMajorAxis)
							/ (2 * transferDestination.GetOrbit().semiMajorAxis),
						1.5)
				));

				double startingLongitude =
					haveVessel ? origin.GetVessel().longitude
					: atHome ? SpaceCenter.Instance.Longitude
					: 0;

				double currentPhaseAngle = clamp(
					  AbsolutePhaseAngle(transferDestination.GetOrbit(), now)
					- AbsolutePhaseAngle(body, now, startingLongitude));

				double angleToMakeUp = currentPhaseAngle - optimalPhaseAngle;

				double phaseAnglePerSecond =
					  (Tau / transferDestination.GetOrbit().period)
					- (body.rotates ? (Tau / body.rotationPeriod) : 0);

				if (angleToMakeUp > 0 && phaseAnglePerSecond > 0)
					angleToMakeUp -= Tau;
				if (angleToMakeUp < 0 && phaseAnglePerSecond < 0)
					angleToMakeUp += Tau;

				if (phaseAnglePerSecond == 0) {
					// Can't launch to surface-stationary target because
					// we'll never reach a good relative phase angle for it.
					// Ideally we'd check <Epsilon here, but that risks breaking
					// normal transfers to asteroids, since they have very low
					// relative angular velocities.
					return null;
				}

				double timeTillBurn = Math.Abs(angleToMakeUp / phaseAnglePerSecond);
				double ejectionBurnTime = now + timeTillBurn;
				double arrivalTime = ejectionBurnTime + 0.5 * OrbitalPeriod(
					transferDestination.GetOrbit().referenceBody,
					transferDestination.GetOrbit().semiMajorAxis,
					targetRadius
				);

				return new BurnModel(
					ejectionBurnTime,
					DeltaVToOrbit(body)
					+ BurnToNewAp(
						body,
						RadiusAtTime(transferDestination.GetOrbit(), arrivalTime)
							- 0.25 * SphereOfInfluence(transferDestination),
						targetRadius
					)
				);
			} else {
				// If we're at a body with no launch pad, we can't time the maneuver
				return new BurnModel(
					null,
					DeltaVToOrbit(body)
					+ BurnToNewAp(
						body,
						transferDestination.GetOrbit().semiMajorAxis
							- 0.25 * SphereOfInfluence(transferDestination),
						targetRadius
					)
				);
			}
		}

		private BurnModel PlotReturnToParentBurn(Orbit currentOrbit)
		{
			double now = Planetarium.GetUniversalTime();
			DbgFmt("Returning null time");
			return new BurnModel(null, BurnToNewPe(
				currentOrbit,
				now,
				GoodLowOrbitRadius(currentOrbit.referenceBody)
			));
		}

		private BurnModel PlotEjectionBurn(Orbit currentOrbit, bool fakeOrbit = false)
		{
			double now = Planetarium.GetUniversalTime();
			BurnModel outerBurn = GenerateEjectionBurnFromOrbit(ParentOrbit(currentOrbit));
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

				if (fakeOrbit && outerBurn.atTime == null) {
					// We have absolutely no basis for a time, so don't fake it.
					return new BurnModel(
						null,
						BurnToEscape(
							currentOrbit.referenceBody,
							currentOrbit.ApR,
							outerBurn.totalDeltaV
						)
					);
				} else {
					// Either the current orbit is real, or the outer burn constrains us,
					// so we can use those times.
					double burnTime = outerBurn.atTime ?? now;

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
							burnTime
						)
					);
				}

			} else {
				DbgFmt("No outer burn found");
				return null;
			}
		}

		private BurnModel PlotLaunchToEjection()
		{
			DbgFmt("Launching to ejection");

			double now = Planetarium.GetUniversalTime();
			bool haveVessel = (origin.GetVessel() != null);
			CelestialBody body = origin as CelestialBody ?? origin.GetOrbit().referenceBody;
			double targetRadius = GoodLowOrbitRadius(body);
			bool atHome = (body == FlightGlobals.GetHomeBody());
			bool haveLongitude = haveVessel || atHome;

			if (!body.rotates) {
				// A non-rotating body means we never get any closer to the
				// point where we want to escape. No calculation possible.
				// (This also lets us sidestep a divide by zero risk.)
				return null;
			}

			// This will give us a burn with the right delta V from low orbit.
			// The time will be now plus the time it takes to get from the absolute
			// reference direction to the burn at the orbital speed of fakeOrbit.
			Orbit fakeOrbit = new Orbit(0, 0, targetRadius, 0, 0, 0, 0, body);
			// This variable prevents infinite recursion between a body and fake orbits around it.
			BurnModel ejection = GenerateEjectionBurnFromOrbit(fakeOrbit, true);
			DbgFmt("Ejection time null: {0}", (ejection.atTime == null));

			if (haveLongitude && ejection.atTime != null) {
				DbgFmt("Using real longitude and ejection time");
				double startingLongitude =
					haveVessel ? origin.GetVessel().longitude
					: atHome ? SpaceCenter.Instance.Longitude
					: 0;

				// Now we figure out where the vessel (or KSC) will be at the time of that burn.
				double currentPhaseAngle = AbsolutePhaseAngle(
					body,
					ejection.atTime ?? now,
					startingLongitude
				);

				// This will tell us approximately where our ship should be to launch
				double targetAbsolutePhaseAngle = AbsolutePhaseAngle(fakeOrbit, ejection.atTime ?? now);

				// This tells us how fast the body rotates.
				// Note that we already have our divide-by-zero guard above.
				double phaseAnglePerSecond = Tau / body.rotationPeriod;

				// Now we adjust the original burn time to account for the planet rotating
				// into position for us.
				double burnTime = (ejection.atTime ?? now) + clamp(targetAbsolutePhaseAngle - currentPhaseAngle) / phaseAnglePerSecond;

				// Finally, generate the real burn if it seems OK.
				if (burnTime < now) {
					return null;
				} else {
					return new BurnModel(
						burnTime,
						ejection.prograde + DeltaVToOrbit(body)
					);
				}
			} else {
				DbgFmt("Longitude or ejection time missing, using outer burn time");
				// If we're at a body with no launch pad, we can't time the maneuver,
				// so just use what we got from the outer burn.
				return new BurnModel(
					ejection.atTime,
					ejection.prograde + DeltaVToOrbit(body)
				);
			}

		}

		private BurnModel FindIntermediateDestination(CelestialBody currentRefBody, Orbit currentOrbit)
		{
			// These need to be reset before the below loop to avoid messing things up
			transferParent = null;
			transferDestination = null;

			// If you want to go somewhere deep inside another SOI, we will
			// just aim at whatever ancestor we can see.
			// So Kerbin -> Laythe would be the same as Kerbin -> Jool.
			for (ITargetable b = StartBody(destination), prevBody = null;
					b != null;
					prevBody = b, b = ParentBody(b)) {

				if (currentRefBody == b as CelestialBody) {
					// Note which body is boss of the patch where we transfer

					if (prevBody == null) {
						// Return to LKO burn from Mun/Minmus
						DbgFmt("Found intermediate recursive origin {0} in destination ancestors; calculating return burn", TheName(currentRefBody));

						return PlotReturnToParentBurn(currentOrbit);

					} else {

						transferParent = b as CelestialBody;
						transferDestination = prevBody;
						DbgFmt("Found transfer patch, parent: {0}, destination: {1}",
							TheName(transferParent), TheName(transferDestination));
						break;
					}
				}
			}
			DbgFmt("No intermediate destination found");
			return null;
		}

		private BurnModel GenerateEjectionBurnFromOrbit(Orbit currentOrbit, bool fakeOrbit = false)
		{
			DbgFmt("Looking for a route from {0} to {1}, via {2}", TheName(origin), TheName(destination), TheName(currentOrbit.referenceBody));

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
			} else if (currentOrbit.eccentricity > 1.0) {
				return PlotCaptureBurn(currentOrbit);
			} else {

				BurnModel b = FindIntermediateDestination(currentOrbit.referenceBody, currentOrbit);
				if (b != null) {
					// If that function generated a burn, that means this destination
					// is a "return to parent" scenario.
					// Otherwise we have to continue calculating.
					DbgFmt("Got return to parent burn");
					return b;
				}

				// The above function sets this if we're in the transfer patch.
				if (transferDestination != null) {
					// Base case - calculate a simple Hohmann transfer

					retrogradeTransfer = (currentOrbit.GetRelativeInclination(transferDestination.GetOrbit()) > 90f);
					return PlotTransferBurn(currentOrbit, retrogradeTransfer);

				} else {
					// Recursive case - get an orbit from the parent body and adjust it for ejection from here
					DbgFmt("Direct route to {0} not found, recursing through parent {1}", TheName(destination), TheName(currentOrbit.referenceBody));

					return PlotEjectionBurn(currentOrbit, fakeOrbit);
				}
			}
		}

		private BurnModel GenerateEjectionBurn(Orbit currentOrbit)
		{
			if (currentOrbit == null) {
				return null;

			// Are we:
			// 1. With a vessel, on a solid surface; or
			// 2. Without a vessel, at a body with a solid surface
			} else if (Landed(origin) || solidBodyWithoutVessel(origin)) {

				// Are we:
				// 3. Aiming at a target in the same SOI
				if (SameSOITransfer(origin, destination)) {
					return PlotLaunchToTransfer();

				} else {
					// We are aiming at a target in a different SOI
					return PlotLaunchToEjection();
				}

			} else {
				// We are an orbiting vessel
				return GenerateEjectionBurnFromOrbit(currentOrbit);
			}
		}

		/// <summary>
		/// Calculate the time and delta V of the burn needed to transfer.
		/// </summary>
		public void CalculateEjectionBurn()
		{
			ejectionBurn = GenerateEjectionBurn(origin?.GetOrbit());
			GetDuration();

			if (planeChangeBurn != null && ejectionBurn != null) {
				if (planeChangeBurn.atTime < ejectionBurn.atTime) {
					DbgFmt("Resetting plane change burn because it's too early now");
					planeChangeBurn = null;
				}
			}
		}

		/// <summary>
		/// Calculate ejection burn duration
		/// </summary>
		public void GetDuration()
		{
			ejectionBurnDuration = ejectionBurn?.Duration(origin?.GetVessel()?.VesselDeltaV);
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
								double planeTime = TimeOfPlaneChange(o, transferDestination.GetOrbit(), ejectionBurn.atTime ?? 0, out ascendingNode);

								DbgFmt("Pinpointed plane change for {0}", transferParent.GetName());

								if (planeTime > 0 && planeTime > ejectionBurn.atTime) {
									Vector3d dv = DeltaVToMatchPlanes(
										o, transferDestination.GetOrbit(), planeTime);
									if (Math.Abs(dv.magnitude) > 0.1) {
										planeChangeBurn = new BurnModel(planeTime, dv);
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
						ScreenFmt(Localizer.Format("astrogator_adjustManeuversMessage"));
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
		/// You want to start burning before the actual node, so
		/// we use some simple padding logic to determine how far to warp.
		/// If you're more than half-duration-plus-one-minute from the burn, then we warp
		/// to that point. This should allow players to orient the craft and start burn on time.
		/// If you're closer than that but further than half the burn time, we warp to half the burn time.
		/// Otherwise we warp right up to the moment of the actual burn.
		/// If you're _already_ warping, cancel the warp (suggested by Kottabos).
		/// </summary>
		public void WarpToBurn()
		{
			if (TimeWarp.CurrentRate > 1) {
				DbgFmt("Warp button clicked while already in warp, cancelling warp");
				TimeWarp.fetch?.CancelAutoWarp();
				TimeWarp.SetRate(0, false);
			} else if (ejectionBurn.atTime == null) {
				DbgFmt("Can't warp to null time");
			} else {
				DbgFmt("Attempting to warp to burn from {0} to {1}", Planetarium.GetUniversalTime(), ejectionBurn.atTime);
				double unpaddedTime = (ejectionBurn.atTime ?? 0) - GameSettings.DELTAV_BURN_PERCENTAGE * (ejectionBurnDuration ?? 0);
				double paddedTime   = unpaddedTime - BURN_PADDING;
				if (Planetarium.GetUniversalTime() < paddedTime) {
					DbgFmt("Warping to burn minus half burn duration minus one minute");
					TimeWarp.fetch.WarpTo(paddedTime);
				} else if (Planetarium.GetUniversalTime() < unpaddedTime) {
					DbgFmt("Warping to burn minus half burn duration");
					TimeWarp.fetch.WarpTo(unpaddedTime);
				} else if (Planetarium.GetUniversalTime() < (ejectionBurn.atTime ?? 0)) {
					DbgFmt("Already within offset; warping to burn");
					TimeWarp.fetch.WarpTo(ejectionBurn.atTime ?? 0);
				} else {
					DbgFmt("Can't warp to the past!");
				}
			}
		}

	}

}
