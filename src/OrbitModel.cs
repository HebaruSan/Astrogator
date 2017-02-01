using System;

namespace Astrogator {

	/// <summary>
	/// Simplified object representing an orbit, which we use
	/// to compare one orbit to another easily.
	/// </summary>
	public class OrbitModel {

		/// <summary>
		/// Construct one of our orbit objects from the given stock orbit.
		/// </summary>
		/// <param name="o">Orbit from which to copy orbital parameters</param>
		public OrbitModel(Orbit o)
		{
			SemiMajorAxis = o.semiMajorAxis;
			Eccentricity = o.eccentricity;
			Inclination = o.inclination;
			LongitudeOfAscendingNode = o.LAN;
			ArgumentOfPeriapsis = o.argumentOfPeriapsis;
		}

		private double SemiMajorAxis            { get; set; }
		private double Eccentricity             { get; set; }
		private double Inclination              { get; set; }
		private double LongitudeOfAscendingNode { get; set; }
		private double ArgumentOfPeriapsis      { get; set; }

		private static bool CloseEnough(double a, double b, double margin)
		{
			return Math.Abs(a - b) < margin;
		}

		/// <summary>
		/// Generate a string describing how the given orbit differs from this object's orbit.
		/// </summary>
		/// <param name="o">Orbit for comparison</param>
		/// <returns>
		/// A string stating whether the semimajor axis, eccentricity, inclination,
		/// longitude of ascending node, and/or argument of periapsis are different.
		/// </returns>
		public string ComparisonDescription(Orbit o)
		{
			string ret = "Differences:";

			// Meters
			if (!CloseEnough(SemiMajorAxis, o.semiMajorAxis, 1)) {
				ret += " sma";
			}

			// Dimensionless, 0=circular, 1=straight line up/down
			if (!CloseEnough(Eccentricity, o.eccentricity, 0.01)) {
				ret += " ecc";
			}

			// Degrees
			if (!CloseEnough(Inclination, o.inclination, 0.1)) {
				ret += " inc";
			}

			// Degrees
			if (!CloseEnough(LongitudeOfAscendingNode, o.LAN, 0.1)) {
				ret += " lan";
			}

			// Degrees
			if (!CloseEnough(ArgumentOfPeriapsis, o.argumentOfPeriapsis, 0.5)) {
				ret += " aop";
			}

			return ret;
		}

		/// <summary>
		/// Check whether the given orbit is different from this one.
		/// </summary>
		/// <param name="o">Orbit for comparison</param>
		/// <returns>
		/// True if they're the same or close enough, false otherwise.
		/// </returns>
		public bool Equals(Orbit o)
		{
			return CloseEnough(Eccentricity, o.eccentricity, 0.01)
				&& CloseEnough(SemiMajorAxis, o.semiMajorAxis, 1)
				&& CloseEnough(Inclination, o.inclination, 0.1)
				&& CloseEnough(LongitudeOfAscendingNode, o.LAN, 0.1)
				&& CloseEnough(ArgumentOfPeriapsis, o.argumentOfPeriapsis, 0.5);
		}
	}

}
