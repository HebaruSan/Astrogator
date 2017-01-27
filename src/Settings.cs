using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static ViewTools;

	/// <summary>
	/// Wrapper around ConfigNode for our .settings file.
	/// </summary>
	public class Settings {
		private ConfigNode config { get; set; }
		private static string filename = FilePath(Astrogator.Name + ".settings");

		private const string
			MainWindowPositionKey = "MainWindowPosition",
			MainWindowVisibleKey = "MainWindowVisible";

		/// <summary>
		/// Singleton instance for our settings object.
		/// We don't want multiple copies of this floating around clobbering one another.
		/// </summary>
		public static Settings Instance = new Settings();

		private Settings()
		{
			if (System.IO.File.Exists(filename)) {
				config = ConfigNode.Load(filename);
			} else {
				config = new ConfigNode("SETTINGS");
			}
		}

		/// <summary>
		/// Save current settings to disk.
		/// </summary>
		public bool Save()
		{
			return config.Save(filename);
		}

		/// <summary>
		/// Screen position of the main window.
		/// </summary>
		public Vector2 MainWindowPosition {
			get {
				Vector2 pos = new Vector2(0.98f, 0.95f);
				config.TryGetValue(MainWindowPositionKey, ref pos);
				return pos;
			}
			set {
				if (config.HasValue(MainWindowPositionKey)) {
					config.SetValue(MainWindowPositionKey, value);
				} else {
					config.AddValue(MainWindowPositionKey, value);
				}
			}
		}

		/// <summary>
		/// Whether main window is visible or not.
		/// </summary>
		public bool MainWindowVisible {
			get {
				bool visible = false;
				config.TryGetValue(MainWindowVisibleKey, ref visible);
				return visible;
			}
			set {
				if (config.HasValue(MainWindowVisibleKey)) {
					config.SetValue(MainWindowVisibleKey, value);
				} else {
					config.AddValue(MainWindowVisibleKey, value);
				}
			}
		}
	}

}
