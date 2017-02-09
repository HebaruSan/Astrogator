using System.Linq;
using KSP;

namespace Astrogator {

	using static DebugTools;

	/// Shared low level tools for dealing with KSP APIs.
	public static class KerbalTools {

		/// <summary>
		/// Make the map view focus move to the given body.
		/// Borrowed from Precise Node.
		/// </summary>
		/// <param name="destination">The body to focus</param>
		public static void FocusMap(ITargetable destination)
		{
			MapView.MapCamera.SetTarget(
				PlanetariumCamera.fetch.targets.Find(
					mapObj => mapObj.celestialBody != null
						&& mapObj.celestialBody.Equals(destination)
				)
			);
		}

		/// <summary>
		/// Check whether maneuver nodes are available in the current game mode.
		/// Borrowed from MechJeb.
		/// </summary>
		public static bool patchedConicsUnlocked()
		{
			return GameVariables.Instance.GetOrbitDisplayMode(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)) == GameVariables.OrbitDisplayMode.PatchedConics;
		}

		/// <summary>
		/// Choose an radius for an orbit around the given body.
		/// </summary>
		/// <param name="body">The body for which to suggest a radius</param>
		/// <returns>
		/// About 10km above the atmosphere or the minimum safe distance.
		/// </returns>
		public static double GoodLowOrbitRadius(CelestialBody body)
		{
			// Allow enough distance that we are "safely" in the desired zone
			const double ALTITUDE_PADDING = 10000;
			if (body.atmosphere) {
				// For a body with an atmosphere, assume we'll be just above it.
				return body.Radius + body.atmosphereDepth + ALTITUDE_PADDING;
			} else {
				// Otherwise we go just above the minimum safe distance.
				return body.Radius + body.minOrbitalDistance + ALTITUDE_PADDING;
			}
		}

		/// <summary>
		/// Wrapper around CelestialBody.sphereOfInfluence to support ITargetable
		/// </summary>
		/// <param name="target">Body or vessel to check</param>
		/// <returns>
		/// Sphere of influence radius for bodies, 0 otherwise
		/// </returns>
		public static double SphereOfInfluence(ITargetable target)
		{
			CelestialBody b = target as CelestialBody;
			return b?.sphereOfInfluence ?? 0.0;
		}

		/// <summary>
		/// Wrapper around CelestialBody.theName to support other targets as well.
		/// </summary>
		/// <param name="target">Body or vessel to check</param>
		/// <returns>
		/// Name of target, possibly with "the" in front
		/// </returns>
		public static string TheName(ITargetable target)
		{
			CelestialBody b = target as CelestialBody;
			return b?.theName ?? target?.GetName() ?? "NULL";
		}

		/// <summary>
		/// Determine a starting point for finding destination bodies.
		/// </summary>
		/// <param name="target">Body or vessel to use</param>
		public static ITargetable StartBody(ITargetable target = null)
		{
			return target ?? FlightGlobals.GetHomeBody();
		}

		/// <summary>
		/// Return the body to search for destinations next after the parameter.
		/// Essentially a wrapper around CelestialBody.referenceBody to make it
		/// return null for the sun instead of itself.
		/// </summary>
		/// <param name="target">Previous target we searched</param>
		public static CelestialBody ParentBody(ITargetable target)
		{
			if (target.GetOrbit() == null || target.GetOrbit().referenceBody == target as CelestialBody) {
				return null;
			} else {
				return target.GetOrbit().referenceBody;
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
		public static Orbit ParentOrbit(Orbit currentOrbit)
		{
			if (currentOrbit.referenceBody.orbit == currentOrbit) {
				return null;
			} else {
				return currentOrbit.referenceBody.orbit;
			}
		}

		/// <summary>
		/// Get the next part of an orbital path that goes across spheres of influence.
		/// A wrapper around Orbit.nextPatch that doesn't crash on FINAL orbits.
		/// </summary>
		/// <param name="currentPatch">The orbit from which we wish to advance</param>
		/// <returns>
		/// Next orbit if any, otherwise null.
		/// </returns>
		public static Orbit NextPatch(Orbit currentPatch)
		{
			if (currentPatch == null || currentPatch.patchEndTransition == Orbit.PatchTransitionType.FINAL) {
				return null;
			} else {
				// This raises a null reference exception for a FINAL orbit.
				return currentPatch.nextPatch;
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
