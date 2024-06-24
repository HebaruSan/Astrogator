using System;
using System.Diagnostics;
using UnityEngine;
using KSP;
using KSP.Localization;

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
				MonoBehaviour.print($"[{Astrogator.Name} {Time.realtimeSinceStartup:000.000}] {formattedMessage}");
			}
		}

		/// <summary>
		/// Log a debug message about an Exception
		/// </summary>
		/// <param name="description">Explanation of the context in which the exception was raised</param>
		/// <param name="ex">The exception to log</param>
		[Conditional("DEBUG")]
		public static void DbgExc(string description, Exception ex) {
			DbgFmt(
				"{0}: {1}\n{2}",
				description,
				ex.Message,
				ex.StackTrace
			);
		}

		/// <summary>
		/// List the components associated with a Unityu GameObject
		/// </summary>
		/// <param name="gameObj">The object to examine</param>
		[Conditional("DEBUG")]
		public static void printComponentNames(GameObject gameObj)
		{
			Component[] comps = gameObj.GetComponents<Component>();
			for (int i = 0; i < comps.Length; ++i) {
				MonoBehaviour.print($"Component {i}: {comps[i].GetType()}");
			}
		}

		[Conditional("DEBUG")]
		private static void printPartModules(Part part)
		{
			for (int i = 0; i < part.Modules.Count; ++i) {
				MonoBehaviour.print($"Module {i}: {part.Modules[i].GetType()}");
			}
		}

		[Conditional("DEBUG")]
		private static void printResoures(Part part)
		{
			for (int i = 0; i < part.Resources.Count; ++i) {
				PartResource r = part.Resources[i];
				MonoBehaviour.print($"Resource {i}: {r.resourceName}");
			}
		}

	}
}
