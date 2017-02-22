using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;

	/// <summary>
	/// Wrapper around ConfigNode for our .settings file.
	/// </summary>
	public class Settings {

		private Settings()
		{
			DbgFmt("Initializing settings object");
			if (System.IO.File.Exists(filename)) {
				DbgFmt("Loading settings from {0}", filename);
				try {
					config = ConfigNode.Load(filename);
				} catch (Exception ex) {
					DbgExc("Problem loading settings", ex);
				}
			}
			// Generate a default settings object even if it tries and fails to load, so at least it won't crash
			if (config == null) {
				DbgFmt("Creating settings object from scratch");
				config = new ConfigNode(RootKey);
			}
			DbgFmt("Is settings object ready? {0}", (config != null));
		}

		private ConfigNode config { get; set; }
		private static string filename = FilePath(Astrogator.Name + ".settings", false);

		/// <summary>
		/// We don't want multiple copies of this floating around clobbering one another.
		/// </summary>
		/// <value>
		/// Singleton instance for our settings object.
		/// </value>
		public static Settings Instance { get; private set; } = new Settings();

		private const string
			RootKey                     = "SETTINGS",
			MainWindowPositionKey       = "MainWindowPosition",
			MainWindowVisibleKey        = "MainWindowVisible",

			ShowSettingsKey             = "ShowSettings",
			TransferSortKey             = "TransferSort",
			DescendingSortKey           = "DescendingSort",

			DisplayUnitsKey             = "DisplayUnits",
			ShowTrackedAsteroidsKey     = "ShowTrackedAsteroids",

			GeneratePlaneChangeBurnsKey = "GeneratePlaneChangeBurns",
			AddPlaneChangeDeltaVKey     = "AddPlaneChangeDeltaV",
			DeleteExistingManeuversKey  = "DeleteExistingManeuvers",

			AutoTargetDestinationKey    = "AutoTargetDestination",
			AutoFocusDestinationKey     = "AutoFocusDestination",
			AutoEditEjectionNodeKey     = "AutoEditEjectionNode",
			AutoEditPlaneChangeNodeKey  = "AutoEditPlaneChangeNode",
			AutoSetSASKey               = "AutoSetSAS",
			TranslationAdjustKey        = "TranslationAdjust";

		/// <summary>
		/// Save current settings to disk.
		/// </summary>
		public bool Save()
		{
			try {
				DbgFmt("Attempting to save settings to {0}", filename);
				return config.Save(filename);
			} catch (Exception ex) {
				DbgExc("Failed to save settings", ex);
			}
			return false;
		}

		private bool PositionOnScreen(Vector2 pos)
		{
			return pos.x > -1 && pos.x < 1 && pos.y > -1 && pos.y < 1;
		}

		/// <value>
		/// Screen position of the main window.
		/// </value>
		public Vector2 MainWindowPosition {
			get {
				Vector2 dflt = new Vector2(0.75f, 0.75f);
				Vector2 pos = GetValue(MainWindowPositionKey, dflt);
				if (PositionOnScreen(pos)) {
					return pos;
				} else {
					DbgFmt("Attempt to load invalid window position: {0}", pos.ToString());
					return dflt;
				}
			}
			set {
				if (PositionOnScreen(value)) {
					SetValue(MainWindowPositionKey, value);
				} else {
					DbgFmt("Attempt to save invalid window position: {0}", value.ToString());
				}
			}
		}

		/// <value>
		/// Whether main window is visible or not.
		/// </value>
		public bool MainWindowVisible {
			get { return GetValue(MainWindowVisibleKey, false); }
			set { SetValue(MainWindowVisibleKey, value); }
		}

		/// <value>
		/// Whether settings are visible or not.
		/// </value>
		public bool ShowSettings {
			get { return GetValue(ShowSettingsKey, false); }
			set { SetValue(ShowSettingsKey, value); }
		}

		/// <value>
		/// Whether to delete maneuvers in order to determine plane changes.
		/// False by default because this could be very disruptive.
		/// </value>
		public bool DeleteExistingManeuvers {
			get { return GetValue(DeleteExistingManeuversKey, false); }
			set { SetValue(DeleteExistingManeuversKey, value); }
		}

		/// <value>
		/// Whether to generate plane change maneuvers.
		/// On by default because otherwise the ejection maneuver may not be enough.
		/// </value>
		public bool GeneratePlaneChangeBurns {
			get { return GetValue(GeneratePlaneChangeBurnsKey, true); }
			set {
				SetValue(GeneratePlaneChangeBurnsKey, value);
				if (!value) {
					DeleteExistingManeuvers = false;
					AutoEditPlaneChangeNode = false;
					AddPlaneChangeDeltaV = false;
				}
			}
		}

		/// <value>
		/// Whether to include the delta V of plane change maneuvers in the display.
		/// Default to false because otherwise people might burn the total amount to eject.
		/// </value>
		public bool AddPlaneChangeDeltaV {
			get { return GetValue(AddPlaneChangeDeltaVKey, false); }
			set {
				SetValue(AddPlaneChangeDeltaVKey, value);
				if (value) {
					GeneratePlaneChangeBurns = true;
				}
			}
		}

		/// <value>
		/// Whether the destination should be set as target when creeating maneuvers.
		/// On by default because it's almost always what you'd want.
		/// </value>
		public bool AutoTargetDestination {
			get { return GetValue(AutoTargetDestinationKey, true); }
			set { SetValue(AutoTargetDestinationKey, value); }
		}

		/// <value>
		/// Whether the destination should be set as focus when creating maneuvers.
		/// On by default because it's convenient to be able to fine tune your arrival.
		/// Note that if our maneuvers don't give you an encounter, we'll focus
		/// the parent body of the transfer instead (usually Sun).
		/// </value>
		public bool AutoFocusDestination {
			get { return GetValue(AutoFocusDestinationKey, true); }
			set { SetValue(AutoFocusDestinationKey, value); }
		}

		/// <value>
		/// If true, creating a maneuver node will enable SAS and set it to maneuver mode.
		/// </value>
		public bool AutoSetSAS {
			get { return GetValue(AutoSetSASKey, true); }
			set { SetValue(AutoSetSASKey, value); }
		}

		/// <value>
		/// Whether to open the ejection maneuver node for editing upon creation.
		/// On by default because it's the first one you'll want to use for fine tuning.
		/// </value>
		public bool AutoEditEjectionNode {
			get { return GetValue(AutoEditEjectionNodeKey, true); }
			set {
				SetValue(AutoEditEjectionNodeKey, value);
				if (value) {
					AutoEditPlaneChangeNode = false;
				}
			}
		}

		/// <value>
		/// Whether to open the plane change maneuver node for editing upon creation.
		/// Off by default because usually you'd want to edit the ejection node instead.
		/// </value>
		public bool AutoEditPlaneChangeNode {
			get { return GetValue(AutoEditPlaneChangeNodeKey, false); }
			set {
				SetValue(AutoEditPlaneChangeNodeKey, value);
				if (value) {
					GeneratePlaneChangeBurns = true;
					AutoEditEjectionNode = false;
				}
			}
		}

		/// <summary>
		/// How to sort the table.
		/// </summary>
		public SortEnum TransferSort {
			get { return GetValue<SortEnum>(TransferSortKey, SortEnum.Position);}
			set { SetValue(TransferSortKey, value.ToString()); }
		}

		/// <summary>
		/// True if the sort should be largest value at the top, false otherwise.
		/// </summary>
		public bool DescendingSort {
			get { return GetValue(DescendingSortKey, false); }
			set { SetValue(DescendingSortKey, value); }
		}

		/// <summary>
		/// Unit system for display of physical quantities.
		/// </summary>
		public DisplayUnitsEnum DisplayUnits {
			get { return GetValue<DisplayUnitsEnum>(DisplayUnitsKey, DisplayUnitsEnum.Metric); }
			set { SetValue(DisplayUnitsKey, value.ToString()); }
		}

		/// <summary>
		/// True if tracked asteroids should be included in the list of transfers, false to leave them out.
		/// </summary>
		public bool ShowTrackedAsteroids {
			get { return GetValue(ShowTrackedAsteroidsKey, true); }
			set { SetValue(ShowTrackedAsteroidsKey, value); }
		}

		/// <summary>
		/// True to use the RCS translation controls to adjust generated maneuver nodes.
		/// Includes both the HNJIKL keys and the joystick/controller translation axes.
		/// Only applies when RCS is turned off!
		/// </summary>
		public bool TranslationAdjust {
			get { return GetValue(TranslationAdjustKey, true); }
			set { SetValue(TranslationAdjustKey, value); }
		}

		// We can't use generics here because ALL allowed types must have callable functions available,
		// C#'s generic constraints doesn't support "T must be one of any of the following types",
		// and KSP only defines these functions for certain specific types.
		private bool GetValue(string key, bool defaultVal)
		{
			bool val = defaultVal;
			config.TryGetValue(key, ref val);
			return val;
		}
		private void SetValue(string key, bool val)
		{
			if (config.HasValue(key)) {
				config.SetValue(key, val);
			} else {
				config.AddValue(key, val);
			}
		}
		private T GetValue<T>(string key, T defaultVal) where T : IConvertible
		{
			T val = defaultVal;
			string savedValue = "";
			if (config.TryGetValue(key, ref savedValue)) {
				val = ParseEnum<T>(savedValue, defaultVal);
			}
			return val;
		}
		private void SetValue(string key, string val)
		{
			if (config.HasValue(key)) {
				config.SetValue(key, val);
			} else {
				config.AddValue(key, val);
			}
		}
		private Vector2 GetValue(string key, Vector2 defaultVal)
		{
			Vector2 val = defaultVal;
			config.TryGetValue(key, ref val);
			return val;
		}
		private void SetValue(string key, Vector2 val)
		{
			if (config.HasValue(key)) {
				config.SetValue(key, val);
			} else {
				config.AddValue(key, val);
			}
		}

	}

}
