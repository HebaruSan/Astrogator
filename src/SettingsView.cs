using System;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static ViewTools;
	using static Language;

	/// <summary>
	/// A GUI object allowing the user to edit the settings.
	/// </summary>
	public class SettingsView : DialogGUIVerticalLayout {

		private const string docsURL = "https://github.com/HebaruSan/Astrogator/blob/master/README.md#settings";

		/// <summary>
		/// Construct a GUI object that allows the user to edit the settings.
		/// </summary>
		public SettingsView(AstrogationView.ResetCallback reset)
			: base(
				mainWindowMinWidth, 10,
				mainWindowSpacing,  settingsPadding,
				TextAnchor.UpperLeft
			)
		{
			resetCallback = reset;

			try {

				AddChild(headerButton(
					manualLink,
					linkStyle, manualLinkTooltip, RowWidth, rowHeight,
					() => { Application.OpenURL(docsURL); }
				));

				AddChild(LabelWithStyleAndSize(
					settingsSectionHeader,
					midHdrStyle,
					mainWindowMinWidth, rowHeight
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.GeneratePlaneChangeBurns,
					planeChangeBurnsSetting,
					(bool b) => { Settings.Instance.GeneratePlaneChangeBurns = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AddPlaneChangeDeltaV,
					addChangeBurnsSetting,
					(bool b) => {
						Settings.Instance.AddPlaneChangeDeltaV = b;
						// Only need to reload if we don't already have the plane change values
						if (b) {
							resetCallback(true);
						}
					}
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.DeleteExistingManeuvers,
					autoDeleteNodesSetting,
					(bool b) => { Settings.Instance.DeleteExistingManeuvers = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.ShowTrackedAsteroids,
					asteroidsSetting,
					(bool b) => { Settings.Instance.ShowTrackedAsteroids = b; resetCallback(true); }
				));

				AddChild(LabelWithStyleAndSize(
					maneuverCreationHeader,
					midHdrStyle,
					mainWindowMinWidth, rowHeight
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AutoTargetDestination,
					autoTargetDestSetting,
					(bool b) => { Settings.Instance.AutoTargetDestination = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AutoFocusDestination,
					autoFocusDestSetting,
					(bool b) => { Settings.Instance.AutoFocusDestination = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AutoEditEjectionNode,
					autoEditEjecSetting,
					(bool b) => { Settings.Instance.AutoEditEjectionNode = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AutoEditPlaneChangeNode,
					autoEditPlaneChgSetting,
					(bool b) => { Settings.Instance.AutoEditPlaneChangeNode = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.AutoSetSAS,
					autoSetSASSetting,
					(bool b) => { Settings.Instance.AutoSetSAS = b; }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.TranslationAdjust,
					adjustNodesSetting,
					(bool b) => { Settings.Instance.TranslationAdjust = b; }
				));

				AddChild(LabelWithStyleAndSize(
					unitsHeader,
					midHdrStyle,
					mainWindowMinWidth, rowHeight
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.DisplayUnits == DisplayUnitsEnum.Metric,
					metricSetting,
					(bool b) => { if (b) Settings.Instance.DisplayUnits = DisplayUnitsEnum.Metric; resetCallback(false); }
				));

				AddChild(new DialogGUIToggle(
					() => Settings.Instance.DisplayUnits == DisplayUnitsEnum.UnitedStatesCustomary,
					imperialSetting,
					(bool b) => { if (b) Settings.Instance.DisplayUnits = DisplayUnitsEnum.UnitedStatesCustomary; resetCallback(false); }
				));

			} catch (Exception ex) {
				DbgExc("Problem constructing settings view", ex);
			}
		}

		private AstrogationView.ResetCallback resetCallback { get; set; }

	}

}
