using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;

	/// An object representing a maneuver, but without creating an
	/// actual maneuver node, so we can store the data and use it later.
	public class BurnModel {

		/// <summary>
		/// Construct a burn object with the given parameters.
		/// </summary>
		/// <param name="t">Time of burn</param>
		/// <param name="pro">Prograde component</param>
		/// <param name="nor">Normal component</param>
		/// <param name="rad">Radial component</param>
		public BurnModel(double? t, double pro, double nor = 0, double rad = 0)
		{
			atTime      = t;
			prograde    = pro;
			normal      = nor;
			radial      = rad;
			totalDeltaV = Math.Sqrt(prograde * prograde + normal * normal + radial * radial);
		}

		/// <summary>
		/// Construct a burn object from a time and a vector.
		/// </summary>
		/// <param name="t">Time of burn</param>
		/// <param name="dv">3 dimensional vector describing the burn, should be in (radial, normal, prograde) format.</param>
		public BurnModel(double? t, Vector3d dv)
		{
			atTime      = t;
			radial      = dv.x;
			normal      = dv.y;
			prograde    = dv.z;
			totalDeltaV = Math.Sqrt(prograde * prograde + normal * normal + radial * radial);
		}

		/// <value>
		/// Maneuver node created from this burn, if any.
		/// </value>
		public ManeuverNode node  { get; private set; }

		/// <summary>
		/// The UT of the burn.
		/// If null, then the burn can happen anytime (as when launching from an indeterminate position).
		/// </summary>
		public double? atTime     { get; private set; }

		/// <summary>
		/// Prograde burn component in m/s.
		/// </summary>
		public double prograde    { get; private set; }

		/// <summary>
		/// Normal burn component in m/s.
		/// </summary>
		public double normal      { get; private set; }

		/// <summary>
		/// Radial burn component in m/s.
		/// </summary>
		public double radial      { get; private set; }

		/// <summary>
		/// Magnitude of the dV vector.
		/// </summary>
		public double totalDeltaV { get; private set; }

		/// <summary>
		/// Generate a visible maneuver using this object's parameters.
		/// </summary>
		public ManeuverNode ToActiveManeuver()
		{
			DbgFmt("Activating maneuver");
			node = null;
			if (FlightGlobals.ActiveVessel?.patchedConicSolver != null && atTime != null) {
				node = FlightGlobals.ActiveVessel.patchedConicSolver.AddManeuverNode(atTime ?? 0);
			}
			if (node != null) {
				DbgFmt("Maneuver activated");
				// DeltaV Vector: (Radial out, Normal up, Prograde)
				node.DeltaV = new Vector3d(radial, normal, prograde);
				DbgFmt("Delta vee filled in");
				node.nodeRotation = Quaternion.identity;
				DbgFmt("Updating flight plan");
				FlightGlobals.ActiveVessel.patchedConicSolver.UpdateFlightPlan();
				DbgFmt("Flight plan revised");

				return node;
			} else {
				return null;
			}
		}

		/// <summary>
		/// Calculate the burn time for a given vessel and delta V amount
		/// </summary>
		/// <param name="dvCalc">Stock delta V object from a vessel</param>
		/// <returns>
		/// null if not ready yet;
		/// PositiveInfinity if not enough fuel to do the burn;
		/// NaN if we can't burn at all;
		/// otherwise number of seconds required for the burn
		/// </returns>
		public double? Duration(VesselDeltaV dvCalc)
		{
			if (dvCalc != null && totalDeltaV > 0) {
				if (!dvCalc.IsReady) {
					return null;
				} else if (totalDeltaV > dvCalc.TotalDeltaVActual) {
					return double.PositiveInfinity;
				} else {
					double remaining = totalDeltaV;
					double t         = 0;
					for (int i = 0; i < dvCalc.OperatingStageInfo.Count; ++i) {
						DeltaVStageInfo stg = dvCalc.OperatingStageInfo[i];
						double exhVel = stg.ispActual * EarthGeeASL;
						if (remaining >= stg.deltaVActual) {
							// We need to expend this whole stage, so just add its complete time
							remaining -= stg.deltaVActual;
							t         += stg.stageBurnTime;
						} else {
							// We only need part of this stage, so appeal to Tsiolkovsky
							t += exhVel
								* stg.startMass
								* (1.0 - Math.Exp(-remaining / exhVel))
								/ stg.thrustActual;
							break;
						}
					}
					return t;
				}
			} else {
				// No such burn
				return double.NaN;
			}
		}

		/// <summary>
		/// Check whether the user opened this manuever node's editing gizmo since the last tick.
		/// There doesn't seem to be event-based notification for this, so we just have to poll.
		/// </summary>
		public void CheckIfNodeDisappeared()
		{
			if (node != null) {
				if (!FlightGlobals.ActiveVessel?.patchedConicSolver?.maneuverNodes.Contains(node) ?? true) {
					NodeDeleted();
				}
			}
		}

		/// <summary>
		/// When a maneuver node is deleted, release our reference to it.
		/// </summary>
		public void NodeDeleted()
		{
			DbgFmt("Our node was deleted, release it");

			if (node != null) {
				node = null;
			}
		}

		/// <summary>
		/// Remove a previously added maneuver node representing this burn.
		/// </summary>
		public void RemoveNode()
		{
			if (node != null) {
				node.RemoveSelf();
				NodeDeleted();
			}
		}

		/// <summary>
		/// Pop open the maneuver node editing widget for this burn.
		/// Note that any changes the user makes will NOT affect this object!
		/// We are storing the *initial* burn info only, and any tweaks the
		/// user makes are the user's own business.
		/// </summary>
		public void EditNode()
		{
			if (node != null && FlightGlobals.ActiveVessel != null) {
				node.AttachGizmo(MapView.ManeuverNodePrefab,
					FlightGlobals.ActiveVessel.patchedConicRenderer);
			}
		}
	}
}
