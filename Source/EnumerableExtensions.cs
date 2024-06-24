using System;
using System.Linq;
using System.Collections.Generic;

namespace Astrogator {

	/// <summary>
	/// Going to extra effort to be lazy
	/// </summary>
	public static class EnumerableExtensions
	{

		/// <summary>
		/// Return the first element in a sequence matching a predicate,
		/// or the the first element at all if none match.
		/// </summary>
		/// <typeparam name="T">The type of elements in the sequence</typeparam>
		/// <param name="source">The sequence to scan, may be checked twice</param>
		/// <param name="predicate">The predicate to check per element</param>
		/// <returns></returns>
		public static T FirstOrDefaultOrFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
			where T : class
			=> source.FirstOrDefault(predicate) ?? source.FirstOrDefault();

	}

}
