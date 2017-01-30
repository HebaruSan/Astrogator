using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;

	/// An object representing a maneuver, but without creating an
	/// actual maneuver node, so we can store the data and use it later.
	public class BurnModel {

		/// <value>
		/// Maneuver node created from this burn, if any.
		/// </value>
		public ManeuverNode node { get; private set; }

		/// <summary>
		/// The UT of the burn.
		/// </summary>
		public double atTime { get; private set; }

		/// <summary>
		/// Prograde burn component in m/s.
		/// </summary>
		public double prograde { get; private set; }

		/// <summary>
		/// Normal burn component in m/s.
		/// </summary>
		public double normal { get; private set; }

		/// <summary>
		/// Radial burn component in m/s.
		/// </summary>
		public double radial { get; private set; }

		/// <summary>
		/// Magnitude of the dV vector.
		/// </summary>
		public double totalDeltaV { get; private set; }

		/// <summary>
		/// Construct a burn object with the given parameters.
		/// </summary>
		/// <param name="t">Time of burn</param>
		/// <param name="pro">Prograde component</param>
		/// <param name="nor">Normal component</param>
		/// <param name="rad">Radial component</param>
		public BurnModel(double t, double pro, double nor, double rad)
		{
			atTime = t;
			prograde = pro;
			normal = nor;
			radial = rad;
			totalDeltaV = Math.Sqrt(prograde * prograde + normal * normal + radial * radial);
		}

		/// <summary>
		/// Generate a visible maneuver using this object's parameters.
		/// </summary>
		public ManeuverNode ToActiveManeuver()
		{
			DbgFmt("Activating maneuver");
			node = null;
			if (FlightGlobals.ActiveVessel != null
					&& FlightGlobals.ActiveVessel.patchedConicSolver != null) {
				node = FlightGlobals.ActiveVessel.patchedConicSolver.AddManeuverNode(atTime);
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
		/// Remove a previously added maneuver node representing this burn.
		/// </summary>
		public void RemoveNode()
		{
			if (node != null) {
				node.RemoveSelf();
				node = null;
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
