using System;
using UnityEngine;
using KSP;

namespace Astrogator {

	/// Tools to help with debugging.
	/// They'd be module-level global variables if C# allowed that.
	public static class DebugTools {

		#if DEBUG
		private static readonly object debugMutex = new object();
		#endif

		/// <summary>
		/// Add a formattable string to the debug output.
		/// Automatically prepends the mod name and a timestamp.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		public static void DbgFmt(string format, params object[] args)
		{
			#if DEBUG

			string formattedMessage = string.Format(format, args);

			lock (debugMutex) {
				Debug.Log(string.Format(
					"[{0} {1:000.000}] {2}",
					AstrogationView.DisplayName,
					Time.realtimeSinceStartup,
					formattedMessage
				));
			}

			#endif
		}

		/// <summary>
		/// Show a formattable string to the user.
		/// </summary>
		/// <param name="format">String.Format format string</param>
		/// <param name="args">Parameters for the format string, if any</param>
		public static void ScreenFmt(string format, params object[] args)
		{
			#if DEBUG

			ScreenMessages.PostScreenMessage(
				string.Format(format, args)
			);

			#endif
		}

	}
}
