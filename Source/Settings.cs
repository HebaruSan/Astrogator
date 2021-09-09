using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static ViewTools;

	using MonoBehavior = UnityEngine.MonoBehaviour;

	/// <summary>
	/// Wrapper around ConfigNode for our .settings file.
	/// </summary>
	public class Settings : MonoBehavior {

		private Settings()
		{
			ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(configNodeName);
			if (nodes.Length > 0) {
				for (int n = 0; n < nodes.Length; ++n) {
					ConfigNode.LoadObjectFromConfig(this, nodes[n]);
				}
			} else if (File.Exists(legacyPath)) {
				// ConfigNode.Load can return null if the file is empty,
				// and this crashes LoadObjectFromConfig.
				try {
					ConfigNode.LoadObjectFromConfig(this, ConfigNode.Load(legacyPath));
				} catch (Exception ex) {
					DbgExc("Failed to load settings file", ex);
				}
			}
		}

		private const           string configNodeName = "ASTROGATORSETTINGS";
		private static readonly string legacyPath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/{Astrogator.Name}.settings";

		/// <summary>
		/// Save current settings to disk.
		/// </summary>
		public void Save()
		{
			UrlDir.UrlFile settingsFile = null;
			UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs(configNodeName);
			if (configs.Length > 0) {
				// Already have a cfg file with settings, overwrite
				settingsFile = configs[0].parent;
				settingsFile.configs.Clear();
			} else {
				// Make a new file
				var modFolder = GameDatabase.Instance.root.children
					.First(d => d.type == UrlDir.DirectoryType.GameData)
					.GetDirectory(Astrogator.Name);
				settingsFile = new UrlDir.UrlFile(modFolder,
					new FileInfo($"{modFolder.path}/AstrogatorSettings.cfg"));
			}
			settingsFile.configs.Add(new UrlDir.UrlConfig(
				settingsFile,
				ConfigNode.CreateConfigFromObject(this, new ConfigNode(configNodeName))
			));
			settingsFile.SaveConfigs();
		}

		/// <summary>
		/// Default position for main window.
		/// Should be a value that works for all UI Scale values.
		/// </summary>
		public static readonly Vector2 defaultWindowPosition = new Vector2(0.5f, 0.7f);

		/// <summary>
		/// We don't want multiple copies of this floating around clobbering one another.
		/// </summary>
		/// <value>
		/// Singleton instance for our settings object.
		/// </value>
		public static Settings Instance { get; private set; } = new Settings();

		/// <value>
		/// Screen position of the main window.
		/// </value>
		[Persistent] public Vector2 MainWindowPosition = defaultWindowPosition;

		/// <value>
		/// Whether main window is visible or not.
		/// </value>
		[Persistent] public bool MainWindowVisible = false;

		/// <value>
		/// Whether settings are visible or not.
		/// </value>
		[Persistent] public bool ShowSettings = false;

		/// <value>
		/// Whether to delete maneuvers in order to determine plane changes.
		/// False by default because this could be very disruptive.
		/// </value>
		[Persistent] public bool DeleteExistingManeuvers = false;

		/// <value>
		/// Whether to generate plane change maneuvers.
		/// On by default because otherwise the ejection maneuver may not be enough.
		/// </value>
		[Persistent] public bool GeneratePlaneChangeBurns = true;

		/// <value>
		/// Whether to include the delta V of plane change maneuvers in the display.
		/// Default to false because otherwise people might burn the total amount to eject.
		/// </value>
		[Persistent] public bool AddPlaneChangeDeltaV = false;

		/// <value>
		/// Whether the destination should be set as target when creeating maneuvers.
		/// On by default because it's almost always what you'd want.
		/// </value>
		[Persistent] public bool AutoTargetDestination = true;

		/// <value>
		/// Whether the destination should be set as focus when creating maneuvers.
		/// On by default because it's convenient to be able to fine tune your arrival.
		/// Note that if our maneuvers don't give you an encounter, we'll focus
		/// the parent body of the transfer instead (usually Sun).
		/// </value>
		[Persistent] public bool AutoFocusDestination = true;

		/// <value>
		/// If true, creating a maneuver node will enable SAS and set it to maneuver mode.
		/// </value>
		[Persistent] public bool AutoSetSAS = true;

		/// <value>
		/// Whether to open the ejection maneuver node for editing upon creation.
		/// On by default because it's the first one you'll want to use for fine tuning.
		/// </value>
		[Persistent] public bool AutoEditEjectionNode = true;

		/// <value>
		/// Whether to open the plane change maneuver node for editing upon creation.
		/// Off by default because usually you'd want to edit the ejection node instead.
		/// </value>
		[Persistent] public bool AutoEditPlaneChangeNode = false;

		/// <summary>
		/// How to sort the table.
		/// </summary>
		[Persistent] public SortEnum TransferSort = SortEnum.Position;

		/// <summary>
		/// True if the sort should be largest value at the top, false otherwise.
		/// </summary>
		[Persistent] public bool DescendingSort = false;

		/// <summary>
		/// Unit system for display of physical quantities.
		/// </summary>
		[Persistent] public DisplayUnitsEnum DisplayUnits = DisplayUnitsEnum.Metric;

		/// <summary>
		/// True if tracked asteroids should be included in the list of transfers, false to leave them out.
		/// </summary>
		[Persistent] public bool ShowTrackedAsteroids = true;

		/// <summary>
		/// True to use the RCS translation controls to adjust generated maneuver nodes.
		/// Includes both the HNJIKL keys and the joystick/controller translation axes.
		/// Only applies when RCS is turned off!
		/// </summary>
		[Persistent] public bool TranslationAdjust = true;

	}

}
