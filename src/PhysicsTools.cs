using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static PhysicsTools;

	/// A bunch of calculations based on Keplerian orbits and so forth.
	///
	/// Some of these have user-visible side effects, such as creating and deleting
	/// several maneuver nodes rapidly! Examine before using.
	public static class PhysicsTools {

		/// Nice to avoid having to multiply PI by 2.
		/// I prefer using radians over degrees when possible.
		public const double Tau = 2.0 * Math.PI;

		/// How long before a burn to abort time warp by default
		public const double BURN_PADDING = 5 * 60;

		/// <summary>
		/// Force an angle to be within 0 and Tau (2PI)
		/// </summary>
		/// <param name="angle">Angle in radians to clamp</param>
		/// <returns>
		/// An angle in [0,2PI] that has the same sin and cos as the param.
		/// </returns>
		public static double clamp(double angle)
		{
			while (angle > Tau) {
				angle -= Tau;
			}
			while (angle < 0) {
				angle += Tau;
			}
			return angle;
		}

		/// <returns>
		/// Magnitude of velocity needed to escape a body with a given speed.
		/// </returns>
		/// <param name="parent">The body we're trying to escape</param>
		/// <param name="radiusAtBurn">The distance from parent's center at which to determine the velocity</param>
		/// <param name="speedatInfinity">The desired speed left over after we escape</param>
		public static double SpeedToEscape(CelestialBody parent, double radiusAtBurn, double speedatInfinity)
		{
			return Math.Sqrt(2.0 * parent.gravParameter / radiusAtBurn + speedatInfinity * speedatInfinity);
		}

		/// <returns>
		/// Delta V needed to escape a body with various constraints.
		/// </returns>
		/// <param name="parent">The body we're escaping</param>
		/// <param name="fromOrbit">The orbit of the craft that's escaping</param>
		/// <param name="speedatInfinity">The desired speed left over after we escape</param>
		/// <param name="burnTime">The time when we want to execute the burn</param>
		public static double BurnToEscape(CelestialBody parent, Orbit fromOrbit, double speedatInfinity, double burnTime)
		{
			Vector3d preBurnVelocity, preBurnPosition;
			fromOrbit.GetOrbitalStateVectorsAtUT(burnTime, out preBurnPosition, out preBurnVelocity);
			// GetOrbitalSpeedAtUT seems to be unreliable and not agree with the velocity magnitude.
			double preBurnRadius = preBurnPosition.magnitude,
				preBurnSpeed = preBurnVelocity.magnitude;

			return SpeedToEscape(parent, preBurnRadius, speedatInfinity) - preBurnSpeed;
		}

		/// Given a parent body orbit, a satellite orbit, and a start time,
		/// calculate when the satellite passes the opposite side of the parent from the grandparent.
		/// We do this by solving for when the phase angle between the two orbits is zero.
		public static double TimeAtNextMidnight(Orbit parentOrbit, Orbit satOrbit, double minTime)
		{
			double satTrueAnomaly = clamp(
				Mathf.Deg2Rad * (
					  parentOrbit.LAN
					+ parentOrbit.argumentOfPeriapsis
					- satOrbit.LAN
					- satOrbit.argumentOfPeriapsis
				)
				+ parentOrbit.TrueAnomalyAtUT(minTime)
			);

			double nextTime = satOrbit.GetUTforTrueAnomaly(satTrueAnomaly, satOrbit.period);
			int numOrbits = (int)Math.Ceiling((minTime - nextTime) / satOrbit.period);
			return nextTime + numOrbits * satOrbit.period;
		}

		/// Similar to above, except rotated around to the day side.
		public static double TimeAtNextNoon(Orbit parentOrbit, Orbit satOrbit, double minTime)
		{
			double satTrueAnomaly = clamp(
				Mathf.Deg2Rad * (
					  parentOrbit.LAN
					+ parentOrbit.argumentOfPeriapsis
					- satOrbit.LAN
					- satOrbit.argumentOfPeriapsis
				)
				+ parentOrbit.TrueAnomalyAtUT(minTime)
				+ Math.PI
			);
			double nextTime = satOrbit.GetUTforTrueAnomaly(satTrueAnomaly, satOrbit.period);
			int numOrbits = (int)Math.Ceiling((minTime - nextTime) / satOrbit.period);
			return nextTime + numOrbits * satOrbit.period;
		}

		/// How fast would a ship with the given Ap and Pe around the given body
		/// be moving at its Pe?
		public static double SpeedAtPeriapsis(CelestialBody parent, double apoapsis, double periapsis)
		{
			return Math.Sqrt(
				parent.gravParameter * (
					2.0 / periapsis - 2.0 / (apoapsis + periapsis)
				)
			);
		}

		/// How fast would a ship with the given Ap and Pe around the given body
		/// be moving at its Ap?
		public static double SpeedAtApoapsis(CelestialBody parent, double apoapsis, double periapsis)
		{
			return Math.Sqrt(
				parent.gravParameter * (
					2.0 / apoapsis - 2.0 / (apoapsis + periapsis)
				)
			);
		}

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
		public static double TimeOfPlaneChange(Orbit currentOrbit, Orbit targetOrbit, double minTime)
		{
			double ascTime = currentOrbit.TimeOfTrueAnomaly(currentOrbit.AscendingNodeTrueAnomaly(targetOrbit), minTime),
			 	descTime = currentOrbit.TimeOfTrueAnomaly(currentOrbit.DescendingNodeTrueAnomaly(targetOrbit), minTime);
			if (ascTime > minTime && ascTime < descTime) {
				return ascTime;
			} else {
				return descTime;
			}
		}

		/// Figure out the value of the AN or DN after the given burn.
		/// Creates and deletes a temporary maneuver node which the user can see!
		/// This is in degrees, not radians.
		public static double inclinationAfterBurn(Orbit targetOrbit, double nodeTime, double deltaV)
		{
			DbgFmt("Probing inclination after burning {0}", deltaV.ToString());

			ManeuverNode node = FlightGlobals.ActiveVessel.patchedConicSolver.AddManeuverNode(nodeTime);
			node.DeltaV = new Vector3d(0, deltaV, 0);
			node.nodeRotation = Quaternion.identity;
			FlightGlobals.ActiveVessel.patchedConicSolver.UpdateFlightPlan();
			Orbit postBurnOrbit = node.nextPatch;
			double inclination = postBurnOrbit.GetRelativeInclination(targetOrbit);
			node.RemoveSelf();
			FlightGlobals.ActiveVessel.patchedConicSolver.UpdateFlightPlan();

			DbgFmt("Inclination weighs in at {0}", inclination.ToString());

			return inclination;
		}

		/// Return the slope of the inclination/deltaV curve at the given point, so
		/// we can find the minimum.
		/// Creates and deletes TWO temporary maneuver nodes which the user can see!
		/// Degrees per m/s
		public static double inclinationSlope(Orbit targetOrbit, double nodeTime, double deltaV)
		{
			const double epsilon = 0.01;

			double inc1 = inclinationAfterBurn(targetOrbit, nodeTime, deltaV - epsilon),
				inc2 = inclinationAfterBurn(targetOrbit, nodeTime, deltaV + epsilon);

			return (inc2 - inc1) / (epsilon + epsilon);
		}

		/// Given a pre-burn orbit and a destination orbit, return the delta V needed to
		/// change from one plane to the other.
		/// Creates and deletes MANY temporary maneuver nodes which the user can see!
		///
		/// Binary search for the zero point of the inclination as a function of delta V.
		/// Uses a small epsilon value to calculate the slope of the function.
		/// There may be a way to calculate this directly, but I don't know what it is.
		/// I believe this algorithm is named after some guy.
		public static double FindPlaneChangeMagnitude(Orbit currentOrbit, Orbit targetOrbit, double nodeTime)
		{
			DbgFmt("Crunching orbits for plane change");

			// We assume without checking that burning 1000 m/s south will give you
			// a big descending node, and 1000 m/s north will give you a big ascending node.
			const double tolerance = 0.01;
			const double epsilon = 0.01;
			const double goodEnoughInclination = 0.05; // (degrees)
			double minDeltaV = -1000,
				maxDeltaV = 1000;

			for (int numReps = 0; numReps < 100; ++numReps) {

				double midDeltaV = 0.5 * (minDeltaV + maxDeltaV);

				double inc1 = inclinationAfterBurn(targetOrbit, nodeTime, midDeltaV - epsilon);

				if (inc1 < goodEnoughInclination) {
					return midDeltaV - epsilon;
				}

				double inc2 = inclinationAfterBurn(targetOrbit, nodeTime, midDeltaV + epsilon);

				if (inc2 < goodEnoughInclination) {
					return midDeltaV + epsilon;
				}

				double midSlope = (inc2 - inc1) / (epsilon + epsilon);

				if (midSlope < 0) {
					// Zoom in on the right segment
					minDeltaV = midDeltaV;
				} else if (midSlope > 0) {
					// Zoom in on the left segment
					maxDeltaV = midDeltaV;
				} else {
					// Found it exactly (very unlikely)
					DbgFmt("Found good plane change");
					return midDeltaV;
				}

				if (maxDeltaV - minDeltaV < tolerance) {
					DbgFmt("Found good enough plane change");
					return midDeltaV;
				}
			}

			// If we didn't find anything within our target tolerance,
			// just use the middle point of the remaining search space.
			DbgFmt("Gave up looking for good plane change");
			return 0.5 * (minDeltaV + maxDeltaV);
		}

	}

	#region Orbit extensions a la r4m0n

	// Borrowed from KAC's borrowing from r4m0n's MJ plugin.
	// We need this to be able to calculate the AN and DN.

	internal static class MuUtils
	{
		//acosh(x) = log(x + sqrt(x^2 - 1))
		internal static double Acosh(double x)
		{
			return Math.Log(x + Math.Sqrt(x * x - 1));
		}

		//keeps angles in the range 0 to 360
		internal static double ClampDegrees360(double angle)
		{
			angle = angle % 360.0;
			if (angle < 0) return angle + 360.0;
			else return angle;
		}

		//keeps angles in the range -180 to 180
		internal static double ClampDegrees180(double angle)
		{
			angle = ClampDegrees360(angle);
			if (angle > 180) angle -= 360;
			return angle;
		}

		internal static double ClampRadiansTwoPi(double angle)
		{
			angle = angle % Tau;
			if (angle < 0) return angle + Tau;
			else return angle;
		}
	}

	internal static class VectorExtensions
	{
		internal static Vector3d Reorder(this Vector3d vector, int order)
		{
			switch (order)
			{
				case 123:
					return new Vector3d(vector.x, vector.y, vector.z);
				case 132:
					return new Vector3d(vector.x, vector.z, vector.y);
				case 213:
					return new Vector3d(vector.y, vector.x, vector.z);
				case 231:
					return new Vector3d(vector.y, vector.z, vector.x);
				case 312:
					return new Vector3d(vector.z, vector.x, vector.y);
				case 321:
					return new Vector3d(vector.z, vector.y, vector.x);
			}
			throw new ArgumentException("Invalid order", "order");
		}
	}

	internal static class MuMech_OrbitExtensions
	{
		//can probably be replaced with Vector3d.xzy?
		internal static Vector3d SwapYZ(Vector3d v)
		{
			return v.Reorder(132);
		}

		//
		// These "Swapped" functions translate preexisting Orbit class functions into world
		// space. For some reason, Orbit class functions seem to use a coordinate system
		// in which the Y and Z coordinates are swapped.
		//

		///normalized vector perpendicular to the orbital plane
		///convention: as you look down along the orbit normal, the satellite revolves counterclockwise
		internal static Vector3d SwappedOrbitNormal(this Orbit o)
		{
			return -SwapYZ(o.GetOrbitNormal()).normalized;
		}

		///mean motion is rate of increase of the mean anomaly
		internal static double MeanMotion(this Orbit o)
		{
			return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
		}

		///The mean anomaly of the orbit.
		///For elliptical orbits, the value return is always between 0 and 2pi
		///For hyperbolic orbits, the value can be any number.
		internal static double MeanAnomalyAtUT(this Orbit o, double UT)
		{
			double ret = o.meanAnomalyAtEpoch + o.MeanMotion() * (UT - o.epoch);
			if (o.eccentricity < 1) ret = MuUtils.ClampRadiansTwoPi(ret);
			return ret;
		}

		///The next time at which the orbiting object will reach the given mean anomaly.
		///For elliptical orbits, this will be a time between UT and UT + o.period
		///For hyperbolic orbits, this can be any time, including a time in the past, if
		///the given mean anomaly occurred in the past
		internal static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT)
		{
			double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
			double meanDifference = meanAnomaly - currentMeanAnomaly;
			if (o.eccentricity < 1) meanDifference = MuUtils.ClampRadiansTwoPi(meanDifference);
			return UT + meanDifference / o.MeanMotion();
		}

		///Gives the true anomaly (in a's orbit) at which a crosses its ascending node
		///with b's orbit.
		///The returned value is always between 0 and 360.
		internal static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
		{
			Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal());
			return a.TrueAnomalyFromVector(vectorToAN);
		}

		///Gives the true anomaly (in a's orbit) at which a crosses its descending node
		///with b's orbit.
		///The returned value is always between 0 and 360.
		internal static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b)
		{
			return MuUtils.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180);
		}

		///Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
		///or southwards, if o is west-moving.
		///The returned value is always between 0 and 360.
		internal static double AscendingNodeEquatorialTrueAnomaly(this Orbit o)
		{
			Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up, o.SwappedOrbitNormal());
			return o.TrueAnomalyFromVector(vectorToAN);
		}

		///Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
		///or northwards, if o is west-moving.
		///The returned value is always between 0 and 360.
		internal static double DescendingNodeEquatorialTrueAnomaly(this Orbit o)
		{
			return MuUtils.ClampDegrees360(o.AscendingNodeEquatorialTrueAnomaly() + 180);
		}

		///For hyperbolic orbits, the true anomaly only takes on values in the range
		/// (-M, M) for some M. This function computes M.
		internal static double MaximumTrueAnomaly(this Orbit o)
		{
			if (o.eccentricity < 1) return 180;
			else return 180 / Math.PI * Math.Acos(-1 / o.eccentricity);
		}

		///Returns whether a has an ascending node with b. This can be false
		///if a is hyperbolic and the would-be ascending node is within the opening
		///angle of the hyperbola.
		internal static bool AscendingNodeExists(this Orbit a, Orbit b)
		{
			return Math.Abs(MuUtils.ClampDegrees180(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
		}

		///Returns whether a has a descending node with b. This can be false
		///if a is hyperbolic and the would-be descending node is within the opening
		///angle of the hyperbola.
		internal static bool DescendingNodeExists(this Orbit a, Orbit b)
		{
			return Math.Abs(MuUtils.ClampDegrees180(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
		}

		///Returns whether o has an ascending node with the equator. This can be false
		///if o is hyperbolic and the would-be ascending node is within the opening
		///angle of the hyperbola.
		internal static bool AscendingNodeEquatorialExists(this Orbit o)
		{
			return Math.Abs(MuUtils.ClampDegrees180(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
		}

		///Returns whether o has a descending node with the equator. This can be false
		///if o is hyperbolic and the would-be descending node is within the opening
		///angle of the hyperbola.
		internal static bool DescendingNodeEquatorialExists(this Orbit o)
		{
			return Math.Abs(MuUtils.ClampDegrees180(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
		}

		///Converts a direction, specified by a Vector3d, into a true anomaly.
		///The vector is projected into the orbital plane and then the true anomaly is
		///computed as the angle this vector makes with the vector pointing to the periapsis.
		///The returned value is always between 0 and 360.
		internal static double TrueAnomalyFromVector(this Orbit o, Vector3d vec)
		{
			Vector3d projected = Vector3d.Exclude(o.SwappedOrbitNormal(), vec);
			Vector3d vectorToPe = SwapYZ(o.eccVec);
			double angleFromPe = Math.Abs(Vector3d.Angle(vectorToPe, projected));

			//If the vector points to the infalling part of the orbit then we need to do 360 minus the
			//angle from Pe to get the true anomaly. Test this by taking the the cross product of the
			//orbit normal and vector to the periapsis. This gives a vector that points to center of the
			//outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
			//during the infalling part of the orbit.
			if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(o.SwappedOrbitNormal(), vectorToPe))) < 90)
			{
				return angleFromPe;
			}
			else
			{
				return 360 - angleFromPe;
			}
		}

		///Originally by Zool, revised by The_Duck
		///Converts a true anomaly into an eccentric anomaly.
		///For elliptical orbits this returns a value between 0 and 2pi
		///For hyperbolic orbits the returned value can be any number.
		///NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
		///past the true anomaly of the asymptote) then an ArgumentException is thrown
		internal static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
		{
			double e = o.eccentricity;
			trueAnomaly = MuUtils.ClampDegrees360(trueAnomaly);
			trueAnomaly = trueAnomaly * (Math.PI / 180);

			if (e < 1) //elliptical orbits
			{
				double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
				double sinE = Math.Sqrt(1 - (cosE * cosE));
				if (trueAnomaly > Math.PI) sinE *= -1;

				return MuUtils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
			}
			else  //hyperbolic orbits
			{
				double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
				if (coshE < 1) throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + o.eccentricity);

				double E = MuUtils.Acosh(coshE);
				if (trueAnomaly > Math.PI) E *= -1;

				return E;
			}
		}

		///Originally by Zool, revised by The_Duck
		///Converts an eccentric anomaly into a mean anomaly.
		///For an elliptical orbit, the returned value is between 0 and 2pi
		///For a hyperbolic orbit, the returned value is any number
		internal static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E)
		{
			double e = o.eccentricity;
			if (e < 1) //elliptical orbits
			{
				return MuUtils.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
			}
			else //hyperbolic orbits
			{
				return (e * Math.Sinh(E)) - E;
			}
		}

		///Converts a true anomaly into a mean anomaly (via the intermediate step of the eccentric anomaly)
		///For elliptical orbits, the output is between 0 and 2pi
		///For hyperbolic orbits, the output can be any number
		///NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
		///past the true anomaly of the asymptote) then an ArgumentException is thrown
		internal static double GetMeanAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
		{
			return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
		}

		///NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
		///large enough that it never attains the given true anomaly
		internal static double TimeOfTrueAnomaly(this Orbit o, double trueAnomaly, double UT)
		{
			return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
		}

		///Returns the next time at which a will cross its ascending node with b.
		///For elliptical orbits this is a time between UT and UT + a.period.
		///For hyperbolic orbits this can be any time, including a time in the past if
		///the ascending node is in the past.
		///NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
		///occurs at a true anomaly that a does not actually ever attain
		internal static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT)
		{
			return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
		}

		///Returns the next time at which a will cross its descending node with b.
		///For elliptical orbits this is a time between UT and UT + a.period.
		///For hyperbolic orbits this can be any time, including a time in the past if
		///the descending node is in the past.
		///NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
		///occurs at a true anomaly that a does not actually ever attain
		internal static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT)
		{
			return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
		}

		///Returns the next time at which the orbiting object will cross the equator
		///moving northward, if o is east-moving, or southward, if o is west-moving.
		///For elliptical orbits this is a time between UT and UT + o.period.
		///For hyperbolic orbits this can by any time, including a time in the past if the
		///ascending node is in the past.
		///NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
		///"ascending node" occurs at a true anomaly that o does not actually ever attain.
		internal static double TimeOfAscendingNodeEquatorial(this Orbit o, double UT)
		{
			return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
		}

		///Returns the next time at which the orbiting object will cross the equator
		///moving southward, if o is east-moving, or northward, if o is west-moving.
		///For elliptical orbits this is a time between UT and UT + o.period.
		///For hyperbolic orbits this can by any time, including a time in the past if the
		///descending node is in the past.
		///NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
		///"descending node" occurs at a true anomaly that o does not actually ever attain.
		internal static double TimeOfDescendingNodeEquatorial(this Orbit o, double UT)
		{
			return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
		}

		// End code copied from Kerbal Alarm Clock

		/// This one is from KerbalEngineer.Extensions.OrbitExtensions
		/// Returns a value in degrees, not radians
		public static double GetRelativeInclination(this Orbit orbit, Orbit target)
		{
			return Vector3d.Angle(orbit.GetOrbitNormal(), target.GetOrbitNormal());
		}
	}

	#endregion Orbit extensions a la r4m0n
}
