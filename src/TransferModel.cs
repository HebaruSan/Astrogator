using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;
	using static KerbalTools;

	/// An object representing everything we need to know about a particular transfer.
	public class TransferModel {

		// XXX - Some of this needs to be re-worked.
		// We cannot currently handle a craft orbiting Laythe that wants to get home
		// to Kerbin, because we assume there will only be one main grandparent
		// and zero or one initial parents.

		// Therefore, I am not going to add ///XML to this yet.

		// Maybe we need to split this into multiple derived classes, one per scenario.
		// Cases:
		//   1. LKO -> Mun/Minmus (vessel, start SOI contains dest SOI)
		//   2. LKO -> Jool (vessel, start SOI's parent SOI contains dest SOI)
		//   3. Laythe orbit -> Kerbin (vessel, start SOI's grandparent SOI contains dest SOI)
		//   4. Kerbin -> Jool (no vessel, start SOI's parent SOI contains dest SOI)
		//   5. Laythe -> Kerbin (no vessel, start SOI's grandparent SOI contains dest SOI)
		//   6. Mun -> LKO (vessel, start SOI is contained within dest SOI)
		//   7. Launch / landed (vessel, not a real orbit)
		//   8. KSC (no vessel, act like it's a launch)

		public Orbit originOrbit { get; private set; }
		public CelestialBody origin { get; private set; }
		public CelestialBody destination { get; private set; }
		public Vessel vessel { get; private set; }
		public BurnModel ejectionBurn { get; private set; }
		public BurnModel planeChangeBurn { get; private set; }

		public double OptimalPhaseAngle { get; private set; }
		public double PhaseAnglePerSecond { get; private set; }

		public TransferModel(CelestialBody org, CelestialBody dest, Vessel v)
		{
			origin = org;
			destination = dest;
			vessel = v;

			DbgFmt("Charting transfer to {0}", dest.theName);

			if (origin != null) {
				originOrbit = origin.orbit;
			} else if (v != null) {
				originOrbit = vessel.orbit;
			} else {
				// No ship or planet; try to simulate an orbit based on launching from KSC?
				// Could be a low circular orbit 30deg behind.
			}

			if (originOrbit != null) {
				OptimalPhaseAngle = clamp(Math.PI * (
					1 - Math.Pow(
						(originOrbit.semiMajorAxis + destination.orbit.semiMajorAxis)
							/ (2 * destination.orbit.semiMajorAxis),
						1.5)
				));

				// How many radians the phase angle increases or decreases by each second
				PhaseAnglePerSecond =
					   (Tau / destination.orbit.period)
					 - (Tau / originOrbit.period);
			}
		}

		public void UpdateManeuvers()
		{
			DbgFmt("Specifying maneuvers to {0}", destination.theName);

			if (originOrbit != null) {

				// Longitude of Ascending Node: Angle between an absolute direction and the ascending node.
				// Argument of Periapsis: Angle between ascending node and periapsis.
				// True Anomaly: Angle between periapsis and current location of the body.
				// Sum: Angle describing absolute location of the body.
				// Only the True Anomaly is in radians by default.
				double currentPhaseAngle = clamp(
					Mathf.Deg2Rad * (
						  destination.orbit.LAN
						+ destination.orbit.argumentOfPeriapsis
						- originOrbit.LAN
						- originOrbit.argumentOfPeriapsis
					)
					+ destination.orbit.trueAnomaly
					- originOrbit.trueAnomaly
				);

				// This whole section borrowed from Kerbal Alarm Clock; thanks, TriggerAu!
				double angleToMakeUp = currentPhaseAngle - OptimalPhaseAngle;
				if (angleToMakeUp > 0 && PhaseAnglePerSecond > 0)
					angleToMakeUp -= Tau;
				if (angleToMakeUp < 0 && PhaseAnglePerSecond < 0)
					angleToMakeUp += Tau;

				double timeTillBurn = Math.Floor(Math.Abs(angleToMakeUp / PhaseAnglePerSecond));

				if (origin != null) {
					// We are starting from a sub-SoI of the transfer, so we can't just burn at the designated time.

					double mu = destination.referenceBody.gravParameter,
						r1 = originOrbit.semiMajorAxis,
						r2 = destination.orbit.semiMajorAxis;

					if (vessel != null) {
						// If we have a vessel, then we can burn when it's at the right part of its orbit

						// Temporary hack: make exits slightly smoother by going 5 minutes earlier.
						// Eventually we'll use a real hyperbolic orbit calculation here.
						const double EJECTION_FUDGE_FACTOR = -5 * 60;

						if (destination.orbit.semiMajorAxis < originOrbit.semiMajorAxis) {
							double speedAtInfinity = Math.Abs(
								Math.Sqrt(mu / r2) * (1 - Math.Sqrt(2 * r1 / (r1 + r2)))
							);
							// Adjust maneuverTime to day side
							ejectionBurn = new BurnModel(
								EJECTION_FUDGE_FACTOR + TimeAtNextNoon(
									originOrbit,
									vessel.orbit,
									Planetarium.GetUniversalTime() + timeTillBurn),
								BurnToEscape(
									origin,
									vessel.orbit,
									speedAtInfinity,
									Planetarium.GetUniversalTime() + timeTillBurn),
								0, 0
							);
						} else {
							double speedAtInfinity = Math.Abs(
								Math.Sqrt(mu / r1) * (Math.Sqrt(2 * r2 / (r1 + r2)) - 1)
							);
							// Adjust maneuverTime to night side
							ejectionBurn = new BurnModel(
								EJECTION_FUDGE_FACTOR + TimeAtNextMidnight(
									originOrbit,
									vessel.orbit,
									Planetarium.GetUniversalTime() + timeTillBurn),
								BurnToEscape(
									origin,
									vessel.orbit,
									speedAtInfinity,
									Planetarium.GetUniversalTime() + timeTillBurn),
								0, 0
							);
						}
					} else {
						// Without a vessel, we just burn when the orbits align

						if (destination.orbit.semiMajorAxis < originOrbit.semiMajorAxis) {
							ejectionBurn = new BurnModel(
								Planetarium.GetUniversalTime() + timeTillBurn,
								Math.Sqrt(mu / r2) * (1 - Math.Sqrt(2 * r1 / (r1 + r2))),
								0, 0
							);
						} else {
							ejectionBurn = new BurnModel(
								Planetarium.GetUniversalTime() + timeTillBurn,
								Math.Sqrt(mu / r1) * (Math.Sqrt(2 * r2 / (r1 + r2)) - 1),
								0, 0
							);
						}
					}

				} else {

					// Already in the destination sphere of influence
					if (originOrbit.semiMajorAxis < destination.orbit.semiMajorAxis) {
						ejectionBurn = new BurnModel(Planetarium.GetUniversalTime() + timeTillBurn,
							BurnToNewAp(originOrbit, Planetarium.GetUniversalTime() + timeTillBurn, destination), 0, 0);
					} else {
						ejectionBurn = new BurnModel(Planetarium.GetUniversalTime() + timeTillBurn,
							BurnToNewPe(originOrbit, Planetarium.GetUniversalTime() + timeTillBurn, destination), 0, 0);
					}

				}
			} else {
				DbgFmt("Punting on this orbit because we lack a craft");
				// TODO: Do something useful for KSC and tracking station
				ejectionBurn = new BurnModel(
					Planetarium.GetUniversalTime() + destination.orbit.period,
					0, 0, 0);
			}

			if (FlightGlobals.ActiveVessel == vessel
					&& vessel != null
					&& vessel.patchedConicSolver != null
					&& vessel.patchedConicSolver.maneuverNodes != null
					&& vessel.patchedConicSolver.maneuverNodes.Count == 0) {

				DbgFmt("Temporarily activating ejection burn to {0}", destination.theName);

				if (ejectionBurn != null) {
					ManeuverNode eNode = ejectionBurn.ToActiveManeuver();

					DbgFmt("Activated ejection burn to {0}", destination.theName);

					if (eNode != null) {

						if (eNode.nextPatch == null) {
							DbgFmt("This node goes nowhere.");
						}

						// Find the orbit patch that intersects the target orbit
						for (Orbit o = eNode.nextPatch; o != null; o = o.nextPatch) {
							// Skip the patches that are in the wrong SoI
							if (o.referenceBody == destination.orbit.referenceBody) {

								DbgFmt("Identified matching reference body for {0}", destination.theName);

								// Find the AN or DN
								double planeTime = TimeOfPlaneChange(o, destination.orbit, ejectionBurn.atTime);

								DbgFmt("Pinpointed plane change for {0}", destination.theName);

								if (planeTime > 0 && planeTime > ejectionBurn.atTime) {
									// Add a maneuver node to change planes
									planeChangeBurn = new BurnModel(planeTime, 0,
										FindPlaneChangeMagnitude(o, destination.orbit, planeTime), 0);

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

		/// Returns true if UI needs an update
		public bool Refresh() {
			if (ejectionBurn != null) {
				if (ejectionBurn.atTime < Planetarium.GetUniversalTime()) {
					UpdateManeuvers();
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
