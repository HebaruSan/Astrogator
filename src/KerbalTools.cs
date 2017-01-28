using System.Linq;

namespace Astrogator {

	using static DebugTools;

	/// Shared low level tools for dealing with KSP APIs.
	public static class KerbalTools {

		/// <summary>
		/// Make the map view focus move to the given body.
		/// Borrowed from Precise Node.
		/// </summary>
		/// <param name="destination">The body to focus</param>
		public static void FocusMap(CelestialBody destination)
		{
			MapView.MapCamera.SetTarget(
				PlanetariumCamera.fetch.targets.Find(
					mapObj => mapObj.celestialBody != null
						&& mapObj.celestialBody.Equals(destination)
				)
			);
		}

		/// <summary>
		/// Determine a starting point for finding destination bodies.
		/// </summary>
		/// <param name="b">The body to use, overridden by v</param>
		/// <param name="v">Vessel to use, parent body used if passed</param>
		public static CelestialBody StartBody(CelestialBody b = null, Vessel v = null)
		{
			if (v != null) {
				// Vessel scenes: flight, map, tracking station (?)
				DbgFmt("Starting loop with vessel parent, {0}", v.mainBody.theName);
				return v.mainBody;
			} else if (b != null) {
				DbgFmt("Starting loop with body from parameter, {0}", b.theName);
				return b;
			} else {
				// Non-vessel scenes: KSC
				DbgFmt("Starting loop with overall home body, {0}", FlightGlobals.GetHomeBody().theName);
				return FlightGlobals.GetHomeBody();
			}
		}

		/// <summary>
		/// Return the body to search for destinations next after the parameter.
		/// Essentially a wrapper around CelestialBody.referenceBody to make it
		/// return null for the sun instead of itself.
		/// </summary>
		/// <param name="currentBody">Previous body we searched</param>
		public static CelestialBody ParentBody(CelestialBody currentBody) {
			if (currentBody.referenceBody == currentBody) {
				// Sun has itself as its own referenceBody, but null is more convenient for us
				return null;
			} else {
				return currentBody.referenceBody;
			}
		}

		/// <summary>
		/// Find the orbit that contains the given orbit.
		/// Wrapper around Orbit.referenceBody that returns
		/// null for the sun instead of the sun.
		/// </summary>
		/// <param name="currentOrbit">Orbit where we're starting</param>
		/// <returns>
		/// Parent orbit or null.
		/// </returns>
		public static Orbit ParentOrbit(Orbit currentOrbit) {
			if (currentOrbit.referenceBody.orbit == currentOrbit) {
				return null;
			} else {
				return currentOrbit.referenceBody.orbit;
			}
		}

		/// <summary>
		/// Delete all the maneuver nodes for the active vessel.
		/// </summary>
		public static void ClearManeuverNodes()
		{
			PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
			while (solver.maneuverNodes.Count > 0) {
				solver.maneuverNodes.First().RemoveSelf();
			}
		}

	}
}
