using System;
using UnityEngine;
using KSP;

namespace Astrogator {

	/// Tools to help with debugging.
	/// They'd be module-level global variables if C# allowed that.
	public static class DebugTools {

		private static readonly object debugMutex = new object();

		/// <summary>
		/// Add a formattable string to the debug output.
		/// Automatically prepends the mod name and a timestamp.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void DbgFmt(string format, params object[] args)
		{
			string formattedMessage = string.Format(format, args);

			lock (debugMutex) {
				Debug.Log(string.Format(
					"[{0} {1:000.000}] {2}",
					AstrogationView.DisplayName,
					Time.realtimeSinceStartup,
					formattedMessage
				));
			}
		}

		/// <summary>
		/// Log a debug message about an Exception
		/// </summary>
		/// <param name="description">Explanation of the context in which the exception was raised</param>
		/// <param name="ex">The exception to log</param>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void DbgExc(string description, Exception ex) {
			DbgFmt(
				"{0}: {1}\n{2}",
				description,
				ex.Message,
				ex.StackTrace
			);
		}

		/// <summary>
		/// Show a formattable string to the user.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void ScreenFmt(string format, params object[] args)
		{
			ScreenMessages.PostScreenMessage(
				string.Format(format, args)
			);
		}

	}
}
