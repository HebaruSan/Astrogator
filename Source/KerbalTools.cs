using System;
using System.Linq;
using KSP;

namespace Astrogator {

	using static DebugTools;

	/// Shared low level tools for dealing with KSP APIs.
	public static class KerbalTools {

		/// <summary>
		/// Show a formattable string to the user.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		public static void ScreenFmt(string format, params object[] args)
		{
			ScreenMessages.PostScreenMessage(string.Format(format, args));
		}

		/// <summary>
		/// True if the craft is sitting on a surface (solid or liquid) rather than on an orbit.
		/// </summary>
		public static bool Landed(ITargetable t)
		{
			Vessel vessel = t?.GetVessel();
			return vessel != null
				&& (vessel.situation == Vessel.Situations.PRELAUNCH
					|| vessel.situation == Vessel.Situations.LANDED
					|| vessel.situation == Vessel.Situations.SPLASHED);
		}

		/// <summary>
		/// Check whether the target is a body with a solid surface
		/// </summary>
		/// <param name="t">Target to check</param>
		/// <returns>
		/// True if body with solid surface, false if non-body or no solid surface
		/// </returns>
		public static bool solidBodyWithoutVessel(ITargetable t)
		{
			CelestialBody b = t as CelestialBody;
			return b?.hasSolidSurface ?? false;
		}

		/// <summary>
		/// Check whether a burn from one target to another constitutes a same-SOI transfer
		/// </summary>
		/// <param name="start">The body from which we start</param>
		/// <param name="end">The body where we end up</param>
		/// <returns>
		/// True if same SOI transfer, false if an ejection angle is involved
		/// </returns>
		public static bool SameSOITransfer(ITargetable start, ITargetable end)
		{
			CelestialBody b1 = start as CelestialBody;
			if (b1 != null) {
				// 1. start is body,   end is body:   end's referenceBody must be start
				// 2. start is body,   end is vessel: N/A - bodies can't have targets
				return (b1 == end.GetOrbit().referenceBody);
			} else {
				// 3. start is vessel, end is vessel: Orbits must have same referenceBody
				// 4. start is vessel, end is body:   Orbits must have same referenceBody
				// This needs to treat solar orbit -> Laythe as same SOI!
				return AncestorsInclude(end.GetOrbit().referenceBody, start.GetOrbit().referenceBody);
			}
		}

		/// <summary>
		/// Check whether a given body is one of the ancestors of another
		/// </summary>
		/// <param name="child">The body of which to check the ancestors</param>
		/// <param name="ancestor">The body to look for in the list of ancestors</param>
		/// <returns>
		/// True if ancestor found, false otherwise
		/// </returns>
		public static bool AncestorsInclude(CelestialBody child, CelestialBody ancestor)
		{
			for (CelestialBody b = child; b != null; b = ParentBody(b)) {
				if (b == ancestor) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Check whether a vessel is a tracked asteroid.
		/// Derived from CustomAsteroids and RasterPropMonitor.
		/// </summary>
		/// <param name="v">The vessel to check</param>
		/// <returns>
		/// True if v is a tracked asteroid, false otherwise.
		/// </returns>
		public static bool IsTrackedAsteroid(Vessel v)
		{
			return v.vesselType == VesselType.SpaceObject
				&& (v.DiscoveryInfo?.HaveKnowledgeAbout(DiscoveryLevels.StateVectors) ?? false);
		}

		/// <summary>
		/// Make the map view focus move to the given body.
		/// Borrowed and modified from Precise Node.
		/// </summary>
		/// <param name="center">The body or vessel to put at the middle of the screen</param>
		/// <param name="edge">A body or vessel to keep barely visible when we zoom</param>
		public static void FocusMap(ITargetable center, ITargetable edge = null)
		{
			MapView.MapCamera.SetTarget(
				PlanetariumCamera.fetch.targets.Find(
					mapObj => mapObj.celestialBody != null
						&& mapObj.celestialBody.Equals(center)
				)
			);

			// Zoom the camera to facilitate the next step.
			if (edge != null) {
				// Without an encounter, the next step is establishing the encounter within the transfer SOI.
				SetMapZoom((float) edge.GetOrbit().semiMajorAxis);
			} else {
				// With an encounter, the next step is fine tuning within the destination SOI.
				CelestialBody b = center as CelestialBody;
				if (b != null) {
					SetMapZoom((float) b.sphereOfInfluence);
				}
			}
		}

		/// <summary>
		/// Zoom map view based on a desired screen width in meters.
		/// </summary>
		/// <param name="widthInMeters">Number of meters we'd like to be able to see across the screen</param>
		public static void SetMapZoom(float widthInMeters)
		{
			// Conversion factor determined empirically:

			//   Scenario        Distance  Width of orbit
			//   Initial zoom:     87,200             ???
			//   Moho:          1,452,381   5,263,138,304
			//   Kerbin:        3,596,042  13,599,840,256
			//   Jool:         19,683,170  68,773,560,320
			//   Eeloo:        34,688,480  90,118,820,000

			// Distance seems to be in km, so 1000 of this comes from that conversion.
			// The remaining 3 probably relate to field of view and radius vs diameter.
			const float CAMERA_DISTANCE_SCALING_FACTOR = 3000;

			MapView.MapCamera.SetDistance(widthInMeters / CAMERA_DISTANCE_SCALING_FACTOR);
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
		/// Check whether a vessel can be controlled,
		/// for example to decide whether to show maneuver creation icons
		/// </summary>
		/// <param name="v">The vessel to check</param>
		/// <returns>
		/// True if controllable, false otherwise
		/// </returns>
		public static bool vesselControllable(Vessel v)
		{
			return v != null && v.CurrentControlLevel == Vessel.ControlLevel.FULL;
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
		/// Name of target, including gender markers
		/// (Historically, this checked CelestialBody.theName, which included a lower case
		/// version of some names such as "the Mun". This was removed in the localization
		/// update, and now we only have "The Mun" regardless of where in the sentence
		/// the string will be used. This has been reported on the bug tracker:
		///   http://bugs.kerbalspaceprogram.com/issues/14314 )
		/// </returns>
		public static string TheName(ITargetable target)
		{
			return target?.GetDisplayName() ?? target?.GetName() ?? "NULL";
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
			try
			{
				while (solver.maneuverNodes.Count > 0) {
					solver.maneuverNodes.First().RemoveSelf();
				}
			}
			catch (IndexOutOfRangeException)
			{
				// This can be thrown if another thread clears it before us.
				// *Shrug*
			}
		}

		/// <returns>
		/// The full relative path from the main KSP folder to a given resource from this mod.
		/// </returns>
		/// <param name="filename">Name of file located in our plugin folder</param>
		/// <param name="GameDataRelative">True if the KSP/GameData portion of the path is assumed, false if we need to provide the full path</param>
		public static string FilePath(string filename, bool GameDataRelative = true)
		{
			if (GameDataRelative) {
				return string.Format("{0}/{1}", Astrogator.Name, filename);
			} else {
				return string.Format("{0}/{1}",
					System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					filename);
			}
		}

		/// <summary>Discard annoying exceptions</summary>
		/// <param name="func">The function to evaluate</param>
		/// <returns>
		///   The return value of func
		///   or the default of its return type (typically null)
		///   if it throws an exception
		/// </returns>
		public static T DefaultIfThrows<T>(Func<T> func)
		{
			try
			{
				return func();
			}
			catch
			{
				return default;
			}
		}

	}
}
