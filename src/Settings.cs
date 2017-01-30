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
			RootKey                     = "SETTINGS",
			MainWindowPositionKey       = "MainWindowPosition",
			MainWindowVisibleKey        = "MainWindowVisible",

			ShowSettingsKey             = "ShowSettings",

			GeneratePlaneChangeBurnsKey = "GeneratePlaneChangeBurns",
			AddPlaneChangeDeltaVKey     = "AddPlaneChangeDeltaV",
			DeleteExistingManeuversKey  = "DeleteExistingManeuvers",

			AutoTargetDestinationKey    = "AutoTargetDestination",
			AutoFocusDestinationKey     = "AutoFocusDestination",
			AutoEditEjectionNodeKey     = "AutoEditEjectionNode",
			AutoEditPlaneChangeNodeKey  = "AutoEditPlaneChangeNode";

		/// <summary>
		/// We don't want multiple copies of this floating around clobbering one another.
		/// </summary>
		/// <value>
		/// Singleton instance for our settings object.
		/// </value>
		public static Settings Instance { get; private set; } = new Settings();

		private Settings()
		{
			if (System.IO.File.Exists(filename)) {
				config = ConfigNode.Load(filename);
			} else {
				config = new ConfigNode(RootKey);
			}
		}

		/// <summary>
		/// Save current settings to disk.
		/// </summary>
		public bool Save()
		{
			return config.Save(filename);
		}

		/// <value>
		/// Screen position of the main window.
		/// </value>
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

	}

}
