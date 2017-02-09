using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
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
		private static string filename = FilePath(Astrogator.Name + ".settings");

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

			GeneratePlaneChangeBurnsKey = "GeneratePlaneChangeBurns",
			AddPlaneChangeDeltaVKey     = "AddPlaneChangeDeltaV",
			DeleteExistingManeuversKey  = "DeleteExistingManeuvers",

			AutoTargetDestinationKey    = "AutoTargetDestination",
			AutoFocusDestinationKey     = "AutoFocusDestination",
			AutoEditEjectionNodeKey     = "AutoEditEjectionNode",
			AutoEditPlaneChangeNodeKey  = "AutoEditPlaneChangeNode",
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

		/// <value>
		/// Screen position of the main window.
		/// </value>
		public Vector2 MainWindowPosition {
			get {
				Vector2 pos = new Vector2(0.75f, 0.75f);
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

		/// <value>
		/// Whether main window is visible or not.
		/// </value>
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

		/// <value>
		/// Whether settings are visible or not.
		/// </value>
		public bool ShowSettings {
			get {
				bool show = false;
				config.TryGetValue(ShowSettingsKey, ref show);
				return show;
			}
			set {
				if (config.HasValue(ShowSettingsKey)) {
					config.SetValue(ShowSettingsKey, value);
				} else {
					config.AddValue(ShowSettingsKey, value);
				}
			}
		}

		/// <value>
		/// Whether to delete maneuvers in order to determine plane changes.
		/// False by default because this could be very disruptive.
		/// </value>
		public bool DeleteExistingManeuvers {
			get {
				bool delete = false;
				config.TryGetValue(DeleteExistingManeuversKey, ref delete);
				return delete;
			}
			set {
				if (config.HasValue(DeleteExistingManeuversKey)) {
					config.SetValue(DeleteExistingManeuversKey, value);
				} else {
					config.AddValue(DeleteExistingManeuversKey, value);
				}
			}
		}

		/// <value>
		/// Whether to generate plane change maneuvers.
		/// On by default because otherwise the ejection maneuver may not be enough.
		/// </value>
		public bool GeneratePlaneChangeBurns {
			get {
				bool generate = true;
				config.TryGetValue(GeneratePlaneChangeBurnsKey, ref generate);
				return generate;
			}
			set {
				if (config.HasValue(GeneratePlaneChangeBurnsKey)) {
					config.SetValue(GeneratePlaneChangeBurnsKey, value);
				} else {
					config.AddValue(GeneratePlaneChangeBurnsKey, value);
				}
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
			get {
				bool include = false;
				config.TryGetValue(AddPlaneChangeDeltaVKey, ref include);
				return include;
			}
			set {
				if (config.HasValue(AddPlaneChangeDeltaVKey)) {
					config.SetValue(AddPlaneChangeDeltaVKey, value);
				} else {
					config.AddValue(AddPlaneChangeDeltaVKey, value);
				}
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
			get {
				bool autoTarget = true;
				config.TryGetValue(AutoTargetDestinationKey, ref autoTarget);
				return autoTarget;
			}
			set {
				if (config.HasValue(AutoTargetDestinationKey)) {
					config.SetValue(AutoTargetDestinationKey, value);
				} else {
					config.AddValue(AutoTargetDestinationKey, value);
				}
			}
		}

		/// <value>
		/// Whether the destination should be set as focus when creating maneuvers.
		/// On by default because it's convenient to be able to fine tune your arrival.
		/// Note that if our maneuvers don't give you an encounter, we'll focus
		/// the parent body of the transfer instead (usually Sun).
		/// </value>
		public bool AutoFocusDestination {
			get {
				bool autoFocus = true;
				config.TryGetValue(AutoFocusDestinationKey, ref autoFocus);
				return autoFocus;
			}
			set {
				if (config.HasValue(AutoFocusDestinationKey)) {
					config.SetValue(AutoFocusDestinationKey, value);
				} else {
					config.AddValue(AutoFocusDestinationKey, value);
				}
			}
		}

		/// <value>
		/// Whether to open the ejection maneuver node for editing upon creation.
		/// On by default because it's the first one you'll want to use for fine tuning.
		/// </value>
		public bool AutoEditEjectionNode {
			get {
				bool autoEdit = true;
				config.TryGetValue(AutoEditEjectionNodeKey, ref autoEdit);
				return autoEdit;
			}
			set {
				if (config.HasValue(AutoEditEjectionNodeKey)) {
					config.SetValue(AutoEditEjectionNodeKey, value);
				} else {
					config.AddValue(AutoEditEjectionNodeKey, value);
				}
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
			get {
				bool autoEdit = false;
				config.TryGetValue(AutoEditPlaneChangeNodeKey, ref autoEdit);
				return autoEdit;
			}
			set {
				if (config.HasValue(AutoEditPlaneChangeNodeKey)) {
					config.SetValue(AutoEditPlaneChangeNodeKey, value);
				} else {
					config.AddValue(AutoEditPlaneChangeNodeKey, value);
				}
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
			get {
				SortEnum ts = SortEnum.Position;
				string savedValue = "";
				if (config.TryGetValue(TransferSortKey, ref savedValue)) {
					ts = ParseEnum<SortEnum>(savedValue, SortEnum.Position);
				}
				return ts;
			}
			set {
				if (config.HasValue(TransferSortKey)) {
					config.SetValue(TransferSortKey, value.ToString());
				} else {
					config.AddValue(TransferSortKey, value.ToString());
				}
			}
		}

		/// <summary>
		/// True if the sort should be largest value at the top, false otherwise.
		/// </summary>
		public bool DescendingSort {
			get {
				bool descend = false;
				config.TryGetValue(DescendingSortKey, ref descend);
				return descend;
			}
			set {
				if (config.HasValue(DescendingSortKey)) {
					config.SetValue(DescendingSortKey, value);
				} else {
					config.AddValue(DescendingSortKey, value);
				}
			}
		}

		/// <summary>
		/// Unit system for display of physical quantities.
		/// </summary>
		public DisplayUnitsEnum DisplayUnits {
			get {
				DisplayUnitsEnum du = DisplayUnitsEnum.Metric;
				string savedValue = "";
				if (config.TryGetValue(DisplayUnitsKey, ref savedValue)) {
					du = ParseEnum<DisplayUnitsEnum>(savedValue, DisplayUnitsEnum.Metric);
				}
				return du;
			}
			set {
				if (config.HasValue(DisplayUnitsKey)) {
					config.SetValue(DisplayUnitsKey, value.ToString());
				} else {
					config.AddValue(DisplayUnitsKey, value.ToString());
				}
			}
		}

		/// <summary>
		/// True to use the RCS translation controls to adjust generated maneuver nodes.
		/// Includes both the HNJIKL keys and the joystick/controller translation axes.
		/// Only applies when RCS is turned off!
		/// </summary>
		public bool TranslationAdjust {
			get {
				bool ta = true;
				config.TryGetValue(TranslationAdjustKey, ref ta);
				return ta;
			}
			set {
				if (config.HasValue(TranslationAdjustKey)) {
					config.SetValue(TranslationAdjustKey, value);
				} else {
					config.AddValue(TranslationAdjustKey, value);
				}
			}
		}

	}

}
