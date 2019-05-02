using System;
using System.Collections.Generic;

namespace Astrogator {

	/// <summary>
	/// Format an infrequently changing int value with minimal garbage
	/// </summary>
	public class TidyFormatter<ValType> where ValType : struct {

		/// <summary>
		/// Initialize the object
		/// </summary>
		/// <param name="fmt">Function to format a value</param>
		/// <param name="needsRecalcFunc">Function to check whether a value needs reformatting</param>
		public TidyFormatter(Func<ValType?, string> fmt, Func<ValType?, ValType?, bool> needsRecalcFunc = null)
		{
			doFormat    = fmt;
			needsRecalc = needsRecalcFunc
				?? ((a, b) => !EqualityComparer<ValType?>.Default.Equals(a, b));
		}

		/// <summary>
		/// Receive a new value and do any needed processing
		/// </summary>
		/// <param name="newVal">New value of this field</param>
		/// <returns>
		/// Returns
		/// </returns>
		public void Update(ValType? newVal)
		{
			if (needsRecalc(newVal, prevVal)) {
				formatted = doFormat(newVal);
				prevVal   = newVal;
			}
		}

		/// <summary>
		/// Return the current formatted value
		/// </summary>
		/// <returns>
		/// The last return value of doFormat when needsRecalc was true
		/// </returns>
		public override string ToString()
		{
			return formatted;
		}

		private Func<ValType?, string>         doFormat;
		private Func<ValType?, ValType?, bool> needsRecalc;

		private ValType? prevVal   = null;
		private string   formatted = "";
	}

}
