using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static PhysicsTools;

	/// <summary>
	/// A bunch of calculations based on Keplerian orbits and so forth.
	///
	/// Some of these have user-visible side effects, such as creating and deleting
	/// several maneuver nodes rapidly! Examine before using.
	/// </summary>
	public static class PhysicsTools {

		/// <summary>
		/// Nice to avoid having to multiply π by 2.
		/// I prefer using radians over degrees when possible.
		/// </summary>
		public const double Tau = 2.0 * Math.PI;

		/// <summary>
		/// How long before a burn-minus-half-duration to time warp.
		/// Should give player time to orient craft to maneuver node / prograde.
		/// </summary>
		public const double BURN_PADDING = 1 * 60;

		/// <summary>
		/// Force an angle to be within 0 and Tau (2π)
		/// </summary>
		/// <param name="angle">Angle in radians to clamp</param>
		/// <param name="minAllowed">Minimum angle for clamp range, default 0</param>
		/// <returns>
		/// An angle in [minAllowed,minAllowed+Tau] that has
		/// the same sin and cos as the param.
		/// </returns>
		public static double clamp(double angle, double minAllowed = 0)
		{
			while (angle > minAllowed + Tau) {
				angle -= Tau;
			}
			while (angle < minAllowed) {
				angle += Tau;
			}
			return angle;
		}

		/// <summary>
		/// Calculate a user-friendly inclination value, the angle between
		/// the orbit and the equator. Never negative, never > π/2.
		/// </summary>
		/// <param name="inclination">The true inclination of the orbit in radians</param>
		/// <returns>
		/// Angle between orbit and equator in radians.
		/// </returns>
		public static double AngleFromEquatorial(double inclination)
			=> 0.25 * Tau - Math.Abs(0.25 * Tau - Math.Abs(inclination));

		/// <summary>
		/// Estimate the delta V needed for the given vessel to get to orbit around the given body.
		/// </summary>
		/// <param name="body">Body to launch from</param>
		/// <returns>
		/// Delta V it would take to launch from vessel's current position to a comfortable orbit.
		/// </returns>
		public static double DeltaVToOrbit(CelestialBody body)
		{
			// http://forum.kerbalspaceprogram.com/index.php?/topic/144538-delta-v-calculations-accuracy/&do=findComment&comment=2692269
			// Gravity loss: Integral<time>(ignition time, burnout time, parent.GeeASL * Math.Sin(pitch))
			//   pitch = π at t=0, π/2 by 10 km, and 0 by the end
			// Drag loss: Integral<time>(ignition time, burnout time, drag force * mass)
			//   mass = wetMass - t * fuelMass / totalLaunchTime
			//   drag force = 0 in space, more lower, proportional to speed

			// I can't solve integrals on the fly, so we'll just add linear correction factors.
			// Oddly, our uncorrected values overshoot for bodies without atmospheres, so subtract
			// a correction factor based on surface gravity.
			double gravCorrection = -200 * body.GeeASL;

			// Now add another linear correction factor based on atmospheric pressure at sea level.
			// This brings Kerbin's launch dV very close to correct, and everything else but Duna
			// to within 20%. Duna is a 30% undershoot.
			double atmoCorrection = body.atmosphere ? 12 * body.atmospherePressureSeaLevel : 0;

			double targetRadius = GoodLowOrbitRadius(body);
			return SpeedAtPeriapsis(body, targetRadius, body.Radius)
				- EquatorialRotationSpeed(body)
				+ SpeedAtPeriapsis(body, targetRadius, targetRadius)
				- SpeedAtApoapsis(body, targetRadius, body.Radius)
				+ gravCorrection + atmoCorrection;
		}

		/// <summary>
		/// Calculate how much speed an object on the surface at the equator
		/// receives from the rotation of the body.
		/// </summary>
		/// <param name="body">The rotating body</param>
		/// <returns>
		/// Speed in m/s
		/// </returns>
		public static double EquatorialRotationSpeed(CelestialBody body)
			=> (!body.rotates || body.rotationPeriod == 0)
				? 0
				: Tau * body.Radius / body.rotationPeriod;

		/// <summary>
		/// Calculate the offset from the hidden "reference direction"
		/// of the given longitude on body b at the given time.
		/// </summary>
		/// <param name="b">The body whose surface we're concerned about</param>
		/// <param name="time">UT at which to calculate the angle</param>
		/// <param name="longitude">Longitude of the point on the surface we care about</param>
		/// <returns>
		/// Angle in radians. Can be compared to AbsolutePhaseAngle for an orbit.
		/// </returns>
		public static double AbsolutePhaseAngle(CelestialBody b, double time, double longitude)
		=> (!b.rotates || b.rotationPeriod == 0)
			? Mathf.Deg2Rad * (b.initialRotation + longitude)
			: Mathf.Deg2Rad * (b.initialRotation + longitude)
					+ (Tau / b.rotationPeriod) * time;

		/// <summary>
		/// Calculate a phase angle for the given orbit at the given time
		/// </summary>
		/// <param name="o">Orbit to consider</param>
		/// <param name="t">Time when we want to know the phase angle</param>
		/// <returns>
		/// An angle in radians that can be compared to similar values of other satellites.
		/// </returns>
		public static double AbsolutePhaseAngle(Orbit o, double t)
		{
			// Longitude of Ascending Node: Angle between an absolute direction and the ascending node.
			// Argument of Periapsis: Angle between ascending node and periapsis.
			// True Anomaly: Angle between periapsis and current location of the body.
			// Sum: Angle describing absolute location of the body.
			// Only the True Anomaly is in radians by default.

			// https://upload.wikimedia.org/wikipedia/commons/e/eb/Orbit1.svg
			// The ArgOfPer and TruAno move you the normal amount at Inc=0,
			// zero at Inc=π/2, and backwards at Inc=π.
			// That's similar to a cosine, except that it doesn't really make
			// sense to bias the angle towards the LAN when ArgPe+TruAno=π/2.
			double cosInc = o.inclination < 90f ? 1 : -1;
			return clamp(
				Mathf.Deg2Rad * (
					  o.LAN
					+ o.argumentOfPeriapsis * cosInc)
				+ o.TrueAnomalyAtUT(t) * cosInc);
		}

		/// <summary>
		/// Calculate distance from parent body to satellite at a given absolute phase angle.
		/// Absolute phase angle means the angle between the body and the absolute
		/// reference direction. This concept allows us to compare angles for bodies
		/// with different LAN / AoP.
		/// </summary>
		/// <param name="o">Orbit of body to consider</param>
		/// <param name="phaseAngle">Angle in radians between body and the absolute reference direction</param>
		/// <returns>
		/// Distance in meters from parent body to satellite when
		/// absolute phase angle has the given value.
		/// </returns>
		private static double RadiusAtAbsolutePhaseAngle(Orbit o, double phaseAngle)
		{
			double cosInc = o.inclination < 90f ? 1 : -1;
			return o.RadiusAtTrueAnomaly(
				phaseAngle - Mathf.Deg2Rad * (
					  o.LAN
					+ o.argumentOfPeriapsis * cosInc));
		}

		/// <summary>
		/// Calculate the angle at arrival between a craft on a Hohmann transfer
		/// and its destination, if the craft departs at the given time.
		///
		/// We use the orbital radii of the starting body at the start time
		/// and the destination body on the opposite side of the parent (180°)
		/// to determine the transfer orbit, which in turn determines the travel time,
		/// which we use to compare the position of the craft and the destination
		/// at arrival.
		///
		/// This function will be called in a tight loop, so it should be fast.
		/// </summary>
		/// <param name="origOrb">Orbit of starting body</param>
		/// <param name="destOrb">Orbit of destination body</param>
		/// <param name="transTime">UT at departure</param>
		/// <returns>
		/// Angle at arrival in radians between the destination body
		/// and a craft on a Hohmann transfer from the starting body.
		/// Values will be in [-π, π] to allow meaningful comparisons of
		/// values near zero.
		/// </returns>
		private static double ArrivalPhaseAngleDifference(Orbit origOrb, Orbit destOrb, double transTime)
		{
			double arrivalPhaseAngle    = AbsolutePhaseAngle(origOrb, transTime) + Math.PI;
			double arrivalTime          = transTime + TransferTravelTime(
				origOrb, destOrb, transTime, arrivalPhaseAngle);
			double destArrivePhaseAngle = AbsolutePhaseAngle(destOrb, arrivalTime);
			return clamp(arrivalPhaseAngle - destArrivePhaseAngle, -Math.PI);
		}

		/// <summary>
		/// Calculate how long it takes to perform a Hohmann transfer from
		/// one orbiting body to another, starting at a given time.
		///
		/// We use half the orbital period of an orbit touching the starting
		/// orbit at the given time and the destination orbit at the opposite
		/// point.
		/// </summary>
		/// <param name="origOrb">Orbit of starting body</param>
		/// <param name="destOrb">Orbit of destination body</param>
		/// <param name="transTime">UT at departure</param>
		/// <param name="arrivalPhaseAngle">Absolute phase angle at arrival, if already calculated; if null, will be computed from the other params</param>
		/// <returns>
		/// Time in seconds to travel from one orbit to the other,
		/// starting at the specified time.
		/// </returns>
		public static double TransferTravelTime(Orbit origOrb, Orbit destOrb, double transTime, double? arrivalPhaseAngle = null)
			=> 0.5 * OrbitalPeriod(
				destOrb.referenceBody,
				RadiusAtTime(origOrb, transTime),
				RadiusAtAbsolutePhaseAngle(
					destOrb,
					arrivalPhaseAngle ?? (AbsolutePhaseAngle(origOrb, transTime) + Math.PI)));

		/// <summary>
		/// Find a point where a continuous function is zero.
		/// We use the absolute simplest bisection method,
		/// because it does not require us to know the derivative:
		/// https://en.wikipedia.org/wiki/Root-finding_algorithm#Bisection_method
		/// </summary>
		/// <param name="f">Function for which to find a root</param>
		/// <param name="min">Minimum value of domain to search</param>
		/// <param name="max">Maximum value of domain to search</param>
		/// <param name="epsilon">How close the return value should be to the root, default 0.0001</param>
		/// <param name="rangeEpsilon">Maximum value allowed for final slope, used to detect discontinuities, default 0.1</param>
		/// <returns>
		/// A value t such that f(t) ≈ 0.
		/// If no such value is found in the interval: double.NaN.
		/// If the function has a sign-changing discontinuity in the range: double.NaN.
		/// </returns>
		private static double FindRoot(Func<double, double> f, double min, double max, double epsilon = 0.0001, double rangeEpsilon = 0.1)
		{
			double minVal = f(min), maxVal = f(max);
			if (Math.Sign(minVal) == Math.Sign(maxVal)) {
				// Can't search if both negative or both positive.
				// Well, we could try, but it would be sheer luck if it worked,
				// since there's no systematic way to pick better bounds.
				return double.NaN;
			} else {
				double mid = 0.5 * (min + max);
				// Zoom in on a change in sign
				while (max - min > epsilon) {
					double midVal = f(mid);

					if (Math.Sign(minVal) == Math.Sign(midVal)) {
						// Min and mid have same sign
						// Replace min with mid
						min    = mid;
						minVal = midVal;
					} else {
						// Mid and max have same sign
						// Replace max with mid
						max    = mid;
						maxVal = midVal;
					}
					mid = 0.5 * (min + max);
				}
				// We have found the position of a change in sign.
				// However, only half of such changes are legitimate zeroes;
				// the other half are where the relative phase angle overflows
				// from π to -π or vice versa.
				// Accept the former and reject the latter.
				return Math.Abs(maxVal - minVal) < rangeEpsilon
					? mid
					// Reject bad solutions where we home in on the transition from π to -π
					: double.NaN;
			}
		}

		/// <summary>
		/// Calculate the time in a given range when a craft on the given starting orbit
		/// should leave on a Hohmann transfer to reach a body on the destination orbit.
		/// </summary>
		/// <param name="origOrb">Orbit of starting body</param>
		/// <param name="destOrb">Orbit of destination body</param>
		/// <param name="minTime">Start of search range</param>
		/// <param name="maxTime">End of search range</param>
		/// <returns>
		/// Departure time with the closest approach at arrival.
		/// If there's no closest approach in the interval: double.NaN.
		/// </returns>
		private static double OptimalTransferTime(Orbit origOrb, Orbit destOrb, double minTime, double maxTime)
			=> FindRoot(
				(double t) => ArrivalPhaseAngleDifference(origOrb, destOrb, t),
				minTime,
				maxTime);

		/// <summary>
		/// Evaluate our burn time solver over successive intervals
		/// until a solution is found.
		/// </summary>
		/// <param name="origOrb">Orbit of starting body</param>
		/// <param name="destOrb">Orbit of destination body</param>
		/// <param name="searchStart">Minimum of the first interval to search</param>
		/// <param name="searchEnd">Maximum of the first interval to search</param>
		/// <returns>
		/// Return value description
		/// </returns>
		public static double BurnTimeSearch(Orbit origOrb, Orbit destOrb, double searchStart, double searchEnd)
		{
			// Now we have a very rough approximation for the burn time.
			// To do better, we define a function that calculates how close we are at arrival,
			// then use the bisection method to solve for its root.
			// Since that function requires a range, use the initial estimate to seed the
			// range to search, and try later ranges until a solution is found.
			double searchInterval = searchEnd - searchStart;
			while (true) {
				double adjustedBurnTime = OptimalTransferTime(
					origOrb, destOrb, searchStart, searchEnd
				);
				if (!double.IsNaN(adjustedBurnTime)) {
					return adjustedBurnTime;
				} else {
					searchStart = searchEnd;
					searchEnd += searchInterval;
				}
			}
		}

		/// <summary>
		/// Calculate the time around closeToTime when the given landed vessel
		/// will reach the given phase angle through the rotation of the body.
		/// NOTE: Not yet completed!
		/// </summary>
		/// <param name="b">Body on which v is landed</param>
		/// <param name="v">Vessel to track</param>
		/// <param name="angle">Desired phase angle for the vessel</param>
		/// <param name="closeToTime">Target time around which to search for actual time</param>
		/// <returns>
		/// Time when vessel reaches the phase angle.
		/// </returns>
		public static double TimeAtSurfacePhaseAngle(CelestialBody b, Vessel v, double angle, double closeToTime)
		{
			double anglePerSecond = Tau / b.rotationPeriod;
			double phaseAngleAtGivenTime = 0;
			double angleToMakeUp = clamp(angle - phaseAngleAtGivenTime);
			double timeToMakeUp = angleToMakeUp / anglePerSecond;
			return closeToTime + timeToMakeUp;
		}

		/// <summary>
		/// Calculate ejection angle of a hyperbolic orbit with the given parameters
		/// </summary>
		/// <param name="parent">Parent body we're escaping</param>
		/// <param name="periapsis">Radius at closest point to parent</param>
		/// <param name="speedAtInfinity">Desired relative escape velocity</param>
		/// <returns>
		/// Angle in radians between parent-prograde and the periapsis
		/// </returns>
		public static double EjectionAngle(CelestialBody parent, double periapsis, double speedAtInfinity)
		{
			// Vinf ^ 2
			double speed2 = speedAtInfinity * speedAtInfinity;

			// https://en.wikipedia.org/wiki/Hyperbolic_trajectory#Hyperbolic_excess_velocity
			// Negative; more thrust = closer to zero
			double semiMajorAxis = -parent.gravParameter / speed2;

			// http://www.bogan.ca/orbits/kepler/orbteqtn.html
			// Standard orbital parameter describing how curved this orbit is
			// >1; more thrust = bigger.
			double eccentricity = 1.0 - periapsis / semiMajorAxis;

			// https://en.wikipedia.org/wiki/Hyperbolic_trajectory#Angle_between_approach_and_departure
			// http://www.rapidtables.com/math/trigonometry/arccos/arccos-graph.png
			// Angle between departure vector and vector opposite the periapsis point
			// (half the angle between injection and ejection asymptotes)
			// More thrust = closer to π/2 (from above)
			// Less thrust = closer to π (from below)
			double theta = Math.Acos(-1.0 / eccentricity);

			// Angle between parent's prograde and the vessel's prograde at burn
			// Should be between π/2 and π
			return 0.75 * Tau - theta;
		}

		/// <returns>
		/// Magnitude of velocity needed to escape a body with a given speed.
		/// Uses traditional physics calculation involving "speed at infinity",
		/// which is not 100% applicable due to Spheres of Influence being finite.
		/// </returns>
		/// <param name="parent">The body we're trying to escape</param>
		/// <param name="radiusAtBurn">The distance from parent's center at which to determine the velocity</param>
		/// <param name="speedAtInfinity">The desired speed left over after we escape</param>
		public static double SpeedToEscape(CelestialBody parent, double radiusAtBurn, double speedAtInfinity)
			=> Math.Sqrt(2.0 * parent.gravParameter / radiusAtBurn + speedAtInfinity * speedAtInfinity);

		/// <returns>
		/// Magnitude of velocity needed to escape a body with a given speed.
		/// Takes into account the size of the SOI rather than assuming it's infinite.
		/// Should converge to above function as sphere of influence -> infinity.
		/// Derived directly from the vis viva equation.
		/// https://en.wikipedia.org/wiki/Hyperbolic_trajectory#Velocity
		/// </returns>
		/// <param name="parent">The body we're trying to escape</param>
		/// <param name="periapsis">The distance from parent's center at which to determine the velocity</param>
		/// <param name="speedAtSOI">The desired speed left over after we escape</param>
		public static double SpeedToExitSOI(CelestialBody parent, double periapsis, double speedAtSOI)
			=> Math.Sqrt(
				2.0 * parent.gravParameter * (parent.sphereOfInfluence - periapsis)
				/ (parent.sphereOfInfluence * periapsis)
				+ speedAtSOI * speedAtSOI);

		/// <returns>
		/// Delta V needed to escape a body with various constraints.
		/// </returns>
		/// <param name="parent">The body we're escaping</param>
		/// <param name="fromOrbit">The orbit of the craft that's escaping</param>
		/// <param name="speedAtInfinity">The desired speed left over after we escape</param>
		/// <param name="burnTime">The time when we want to execute the burn</param>
		public static double BurnToEscape(CelestialBody parent, Orbit fromOrbit, double speedAtInfinity, double burnTime)
		{
			Vector3d preBurnVelocity, preBurnPosition;
			fromOrbit.GetOrbitalStateVectorsAtUT(burnTime, out preBurnPosition, out preBurnVelocity);
			// GetOrbitalSpeedAtUT seems to be unreliable and not agree with the velocity magnitude.
			double preBurnRadius = preBurnPosition.magnitude,
				preBurnSpeed = preBurnVelocity.magnitude;
			return SpeedToExitSOI(parent, preBurnRadius, speedAtInfinity) - preBurnSpeed;
		}

		/// <summary>
		/// Delta V needed to escape a body at a given speed from a circular orbit
		/// </summary>
		/// <param name="parent">Body from which to escape</param>
		/// <param name="periapsis">Radius of starting orbit</param>
		/// <param name="speedAtInfinity">Desired velocity at SOI exit</param>
		/// <returns>
		/// Speed change in m/s needed
		/// </returns>
		public static double BurnToEscape(CelestialBody parent, double periapsis, double speedAtInfinity)
		=> SpeedToExitSOI(parent, periapsis, speedAtInfinity)
			- SpeedAtPeriapsis(parent, periapsis, periapsis);

		/// <summary>
		/// Calculate the absolute time when a satellite will be a given angle away from
		/// its local midnight position.
		/// </summary>
		/// <param name="parentOrbit">Orbit of the parent body</param>
		/// <param name="satOrbit">Orbit of the satellite around the parent body</param>
		/// <param name="minTime">Absolute time to use as a minimum for the calculation</param>
		/// <param name="angle">0 for exactly midnight, π/2 for parent-prograde, π for exactly noon, 3π/2 for parent-retrograde</param>
		/// <returns>
		/// Absolute time when craft is in desired position
		/// </returns>
		public static double TimeAtAngleFromMidnight(Orbit parentOrbit, Orbit satOrbit, double minTime, double angle)
		{
			double satTrueAnomaly;
			if (satOrbit.GetRelativeInclination(parentOrbit) < 90f) {
				satTrueAnomaly = clamp(
					Mathf.Deg2Rad * (
						  parentOrbit.LAN
						+ parentOrbit.argumentOfPeriapsis
						- satOrbit.LAN
						- satOrbit.argumentOfPeriapsis)
					+ parentOrbit.TrueAnomalyAtUT(minTime)
					+ angle);
			} else {
				satTrueAnomaly = clamp(
					Mathf.Deg2Rad * (
						- parentOrbit.LAN
						- parentOrbit.argumentOfPeriapsis
						+ satOrbit.LAN
						- satOrbit.argumentOfPeriapsis)
					- parentOrbit.TrueAnomalyAtUT(minTime)
					+ angle
					+ Math.PI);
			}
			double nextTime = satOrbit.GetUTforTrueAnomaly(satTrueAnomaly, Planetarium.GetUniversalTime());
			int numOrbits = (int)Math.Ceiling((minTime - nextTime) / satOrbit.period);
			return nextTime + numOrbits * satOrbit.period;
		}

		/// <summary>
		/// How fast would a ship with the given Ap and Pe around the given body
		/// be moving at its Pe?
		/// </summary>
		/// <param name="parent">Body around which we're orbiting</param>
		/// <param name="apoapsis">Maximum radius of orbit</param>
		/// <param name="periapsis">Minimum radius of orbit</param>
		/// <returns>
		/// Magnitude of velocity in m/s
		/// </returns>
		public static double SpeedAtPeriapsis(CelestialBody parent, double apoapsis, double periapsis)
			=> Math.Sqrt(
				parent.gravParameter * (
					2.0 / periapsis - 2.0 / (apoapsis + periapsis)));

		/// <summary>
		/// How fast would a ship with the given Ap and Pe around the given body
		/// be moving at its Ap?
		/// </summary>
		/// <param name="parent">Body around which we're orbiting</param>
		/// <param name="apoapsis">Maximum radius of orbit</param>
		/// <param name="periapsis">Minimum radius of orbit</param>
		/// <returns>
		/// Magnitude of velocity in m/s
		/// </returns>
		public static double SpeedAtApoapsis(CelestialBody parent, double apoapsis, double periapsis)
			=> Math.Sqrt(
				parent.gravParameter * (
					2.0 / apoapsis - 2.0 / (apoapsis + periapsis)));

		/// <returns>
		/// Distance from center of parent body at a given time.
		/// </returns>
		/// <param name="o">Orbit to analyze</param>
		/// <param name="t">Time at which to calculate the distance</param>
		public static double RadiusAtTime(Orbit o, double t)
		{
			Vector3d pos, v;
			o.GetOrbitalStateVectorsAtUT(t, out pos, out v);
			return pos.magnitude;
		}

		/// <returns>
		/// Magnitude of velocity at a given time.
		/// </returns>
		/// <param name="o">Orbit to analyze</param>
		/// <param name="t">Time at which to calculate the speed</param>
		public static double SpeedAtTime(Orbit o, double t)
		{
			Vector3d pos, v;
			o.GetOrbitalStateVectorsAtUT(t, out pos, out v);
			return v.magnitude;
		}

		/// <returns>
		/// Period of an orbit with the given characteristics.
		/// </returns>
		/// <param name="parent">Body around which to orbit</param>
		/// <param name="apoapsis">Greatest distance from center of parent</param>
		/// <param name="periapsis">Smallest distance from center of parent</param>
		public static double OrbitalPeriod(CelestialBody parent, double apoapsis, double periapsis)
		{
			double r = 0.5 * (apoapsis + periapsis);
			return Tau * Math.Sqrt(r * r * r / parent.gravParameter);
		}

		/// Determine the burn needed to enter an orbit at burnTime, with a periaps at
		/// current altitude in fromOrbit and apoaps at destination body.
		/// This pretty well nails Mun and Minmus transfers every time.
		public static double BurnToNewAp(Orbit fromOrbit, double burnTime, double newApoapsis)
		{
			Vector3d preBurnVelocity, preBurnPosition;
			fromOrbit.GetOrbitalStateVectorsAtUT(burnTime, out preBurnPosition, out preBurnVelocity);
			// GetOrbitalSpeedAtUT seems to be unreliable and not agree with the velocity magnitude.
			double preBurnRadius = preBurnPosition.magnitude,
				preBurnSpeed = preBurnVelocity.magnitude;
			double postBurnSpeed = SpeedAtPeriapsis(fromOrbit.referenceBody, newApoapsis, preBurnRadius);
			return postBurnSpeed - preBurnSpeed;
		}

		/// <summary>
		/// Delta V needed to go from a circular orbit to one with a given higher apoapsis
		/// </summary>
		/// <param name="b">Body we're orbiting</param>
		/// <param name="newApoapsis">Apoapsis to which to raise</param>
		/// <param name="preBurnRadius">Radius of circular orbit prior to burn</param>
		/// <returns>
		/// Speed change in m/s needed
		/// </returns>
		public static double BurnToNewAp(CelestialBody b, double newApoapsis, double preBurnRadius)
		{
			double preBurnSpeed = SpeedAtPeriapsis(b, preBurnRadius, preBurnRadius);
			double postBurnSpeed = SpeedAtPeriapsis(b, newApoapsis, preBurnRadius);
			return postBurnSpeed - preBurnSpeed;
		}

		/// Determine the burn needed to enter an orbit at burnTime, with an apoaps at
		/// current altitude in fromOrbit and periaps at destination body.
		/// Just like the previous function but for when you're starting higher.
		public static double BurnToNewPe(Orbit fromOrbit, double burnTime, double newPeriapsis)
		{
			Vector3d preBurnVelocity, preBurnPosition;
			fromOrbit.GetOrbitalStateVectorsAtUT(burnTime, out preBurnPosition, out preBurnVelocity);
			// GetOrbitalSpeedAtUT seems to be unreliable and not agree with the velocity magnitude.
			double preBurnRadius = preBurnPosition.magnitude,
				preBurnSpeed = preBurnVelocity.magnitude;
			double postBurnSpeed = SpeedAtApoapsis(fromOrbit.referenceBody, preBurnRadius, newPeriapsis);
			return postBurnSpeed - preBurnSpeed;
		}

		/// Return the UT of the AN or DN, whichever is sooner
		public static double TimeOfPlaneChange(Orbit currentOrbit, Orbit targetOrbit, double minTime, out bool ascending)
		{
			double ascTime  = currentOrbit.TimeOfAscendingNode(targetOrbit, minTime);
			double descTime = currentOrbit.TimeOfDescendingNode(targetOrbit, minTime);
			if (ascTime > minTime && ascTime < descTime) {
				ascending = true;
				return ascTime;
			} else {
				ascending = false;
				return descTime;
			}
		}

		/// <summary>
		/// Calculate the delta V required to change planes from o to target at time burnUT.
		/// Borrowed from MechJeb.
		/// </summary>
		/// <param name="currentOrbit">Starting orbit</param>
		/// <param name="target">Destination orbit</param>
		/// <param name="burnUT">Time to burn</param>
		/// <returns>
		/// Delta V in m/s, in (radial, normal, prograde) format
		/// </returns>
		public static Vector3d DeltaVToMatchPlanes(Orbit currentOrbit, Orbit target, double burnUT)
		{
			Vector3d desiredHorizontal =
				(currentOrbit.GetRelativeInclination(target) < 90f)
				? Vector3d.Cross(target.SwappedOrbitNormal(), currentOrbit.Up(burnUT))
				: Vector3d.Cross(currentOrbit.Up(burnUT), target.SwappedOrbitNormal());
			Vector3d actualHorizontalVelocity = Vector3d.Exclude(
				currentOrbit.Up(burnUT), currentOrbit.SwappedOrbitalVelocityAtUT(burnUT));
			Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
			Vector3d theBurn = desiredHorizontalVelocity - actualHorizontalVelocity;
			Vector3d normalizedOrbitalV = currentOrbit.SwappedOrbitalVelocityAtUT(burnUT).normalized;
			// Convert from absolute coordinates to (radial, normal, prograde)
			return new Vector3d(
				Vector3d.Dot(Vector3d.Exclude(normalizedOrbitalV, currentOrbit.Up(burnUT)).normalized, theBurn),
				Vector3d.Dot(-currentOrbit.SwappedOrbitNormal(), theBurn),
				Vector3d.Dot(normalizedOrbitalV, theBurn));
		}

	}

	#region Orbit extensions a la r4m0n

	// Borrowed from KAC's borrowing from r4m0n's MJ plugin.
	// We need this to be able to calculate the AN and DN.

	internal static class MuUtils
	{
		// acosh(x) = log(x + sqrt(x^2 - 1))
		public static double Acosh(double x)
			=> Math.Log(x + Math.Sqrt(x * x - 1));
	}

	internal static class VectorExtensions
	{
		/// <summary>
		/// Return the vector with the Y and Z components exchanged
		/// Borrowed from MechJeb
		/// </summary>
		/// <param name="v">Input vector</param>
		/// <returns>
		/// Vector equivalent to (v.x, v.z, v.y)
		/// </returns>
		public static Vector3d SwapYZ(this Vector3d v)
			=> new Vector3d(v.x, v.z, v.y);
	}

	/// <summary>
	/// Extensions to calculate the AN and DN.
	/// Borrowed from KAC's borrowing from r4m0n's MJ plugin.
	/// </summary>
	public static class MuMech_OrbitExtensions
	{

		/// <summary>
		///  Normalized vector pointing radially outward from the planet
		/// Borrowed from MechJeb
		/// </summary>
		/// <param name="o">Orbit from which to plot the vector</param>
		/// <param name="UT">Time at which to plot the vector</param>
		/// <returns>
		/// Outward radial vector matching the given parameters.
		/// </returns>
		public static Vector3d Up(this Orbit o, double UT)
			=> o.SwappedRelativePositionAtUT(UT).normalized;

		/// <summary>
		/// Get orbital velocity, transformed to work with world space
		/// Borrowed from MechJeb
		/// </summary>
		/// <param name="o">Orbit</param>
		/// <param name="UT">Time at which to get the velocity</param>
		/// <returns>
		/// Orbital velocity vector with Y and Z swapped to make sense.
		/// </returns>
		public static Vector3d SwappedOrbitalVelocityAtUT(this Orbit o, double UT)
			=> o.getOrbitalVelocityAtUT(UT).SwapYZ();

		/// <summary>
		/// Get position of orbiting entity relative to its parent body at a given time,
		/// transformed to work with world space
		/// Borrowed from MechJeb
		/// </summary>
		/// <param name="o">Orbit of entity</param>
		/// <param name="UT">Time at which to get the position</param>
		/// <returns>
		/// Return value description
		/// </returns>
		public static Vector3d SwappedRelativePositionAtUT(this Orbit o, double UT)
			=> o.getRelativePositionAtUT(UT).SwapYZ();

		// These "Swapped" functions translate preexisting Orbit class functions into world space.
		// The Orbit class uses a coordinate system in which the Y and Z coordinates are swapped.

		/// <summary>
		/// Normalized vector perpendicular to the orbital plane
		/// convention: as you look down along the orbit normal, the satellite revolves counterclockwise
		/// </summary>
		/// <param name="o">An orbit</param>
		/// <returns>Normal vector of the orbit in world space</returns>
		public static Vector3d SwappedOrbitNormal(this Orbit o)
			=> -o.GetOrbitNormal().SwapYZ().normalized;

		/// <summary>
		/// Returns the next time at which a will cross its ascending node with b.
		/// For elliptical orbits this is a time between UT and UT + a.period.
		/// For hyperbolic orbits this can be any time, including a time in the past if
		/// the ascending node is in the past.
		/// NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
		/// occurs at a true anomaly that a does not actually ever attain
		/// </summary>
		/// <param name="a">Our orbit</param>
		/// <param name="b">The other orbit</param>
		/// <param name="UT">Minimum time of AN</param>
		/// <returns>Time of AN</returns>
		public static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT)
			=> a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);

		/// <summary>
		/// Returns the next time at which a will cross its descending node with b.
		/// For elliptical orbits this is a time between UT and UT + a.period.
		/// For hyperbolic orbits this can be any time, including a time in the past if
		/// the descending node is in the past.
		/// NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
		/// occurs at a true anomaly that a does not actually ever attain
		/// </summary>
		/// <param name="a">Our orbit</param>
		/// <param name="b">The other orbit</param>
		/// <param name="UT">Minimum time of DN</param>
		/// <returns>Time of DN</returns>
		public static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT)
			=> a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);

		// Mean motion is rate of increase of the mean anomaly
		private static double MeanMotion(this Orbit o)
			=> Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));

		// The mean anomaly of the orbit.
		// For elliptical orbits, the value return is always between 0 and Tau
		// For hyperbolic orbits, the value can be any number.
		private static double MeanAnomalyAtUT(this Orbit o, double UT)
			=> o.eccentricity < 1
				? clamp(o.meanAnomalyAtEpoch + o.MeanMotion() * (UT - o.epoch))
				: o.meanAnomalyAtEpoch + o.MeanMotion() * (UT - o.epoch);

		// The next time at which the orbiting object will reach the given mean anomaly.
		// For elliptical orbits, this will be a time between UT and UT + o.period
		// For hyperbolic orbits, this can be any time, including a time in the past, if
		// the given mean anomaly occurred in the past
		private static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT)
		{
			double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
			double meanDifference = meanAnomaly - currentMeanAnomaly;
			if (o.eccentricity < 1)
				meanDifference = clamp(meanDifference);
			return UT + meanDifference / o.MeanMotion();
		}

		// True anomaly (in a's orbit) at which a crosses its ascending node
		// with b's orbit.
		private static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
			=> a.TrueAnomalyFromVector(Vector3d.Cross(b.SwappedOrbitNormal(), a.SwappedOrbitNormal()));

		// True anomaly (in a's orbit) at which a crosses its descending node
		// with b's orbit.
		private static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b)
			=> a.TrueAnomalyFromVector(Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal()));

		// Converts a direction, specified by a Vector3d, into a true anomaly.
		// The vector is projected into the orbital plane and then the true anomaly is
		// computed as the angle this vector makes with the vector pointing to the periapsis.
		private static double TrueAnomalyFromVector(this Orbit o, Vector3d vec)
			=> clamp(o.GetTrueAnomalyOfZupVector(vec));

		// Originally by Zool, revised by The_Duck
		// Converts a true anomaly into an eccentric anomaly.
		// For elliptical orbits this returns a value between 0 and Tau
		// For hyperbolic orbits the returned value can be any number.
		// NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
		// past the true anomaly of the asymptote) then an ArgumentException is thrown
		private static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
		{
			double e = o.eccentricity;
			double cosTA = Math.Cos(trueAnomaly);
			if (e < 1)
			{
				// Elliptical orbits
				double cosE = (e + cosTA) / (1 + e * cosTA);
				double sinE = Math.Sqrt(1 - (cosE * cosE));
				if (trueAnomaly > Math.PI)
					sinE *= -1;

				return clamp(Math.Atan2(sinE, cosE));
			}
			else
			{
				// Hyperbolic orbits
				double coshE = (e + cosTA) / (1 + e * cosTA);
				if (coshE < 1)
					throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + o.eccentricity);

				double E = MuUtils.Acosh(coshE);
				if (trueAnomaly > Math.PI)
					E *= -1;

				return E;
			}
		}

		// Originally by Zool, revised by The_Duck
		// Converts an eccentric anomaly into a mean anomaly.
		// For an elliptical orbit, the returned value is between 0 and Tau
		// For a hyperbolic orbit, the returned value is any number
		private static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E)
			=> o.eccentricity < 1
				// Elliptical orbits
				? clamp(E - (o.eccentricity * Math.Sin(E)))
				// Hyperbolic orbits
				: (o.eccentricity * Math.Sinh(E)) - E;

		// NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
		// large enough that it never attains the given true anomaly
		private static double TimeOfTrueAnomaly(this Orbit o, double trueAnomaly, double UT)
			=> o.UTAtMeanAnomaly(
				o.GetMeanAnomalyAtEccentricAnomaly(
					o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)),
				UT);

		// End code copied from Kerbal Alarm Clock
	}

	#endregion Orbit extensions a la r4m0n
}
