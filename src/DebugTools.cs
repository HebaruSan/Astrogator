using System;
using System.Diagnostics;
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
		[Conditional("DEBUG")]
		public static void DbgFmt(string format, params object[] args)
		{
			string formattedMessage = string.Format(format, args);

			lock (debugMutex) {
				UnityEngine.Debug.Log(string.Format(
					"[{0} {1:000.000}] {2}",
					AstrogationView.DisplayName,
					Time.realtimeSinceStartup,
					formattedMessage
				));
			}
		}

		/// <summary>
		/// Show a formattable string to the user.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		[Conditional("DEBUG")]
		public static void ScreenFmt(string format, params object[] args)
		{
			ScreenMessages.PostScreenMessage(
				string.Format(format, args)
			);
		}

	}
}
