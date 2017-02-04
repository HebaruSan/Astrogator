using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;

	/// <summary>
	/// A DialogGUI* object that displays our app's data.
	/// Intended for embedding in a MultiOptionDialog.
	/// </summary>
	public class AstrogationView : DialogGUIVerticalLayout {

		/// <summary>
		/// Construct a view for the given model.
		/// </summary>
		/// <param name="m">Model object for which to make a view</param>
		/// <param name="reset">Function to call when the view needs to be re-initiated</param>
		public AstrogationView(AstrogationModel m, ResetCallback reset)
			: base(
				mainWindowMinWidth,
				mainWindowMinHeight,
				mainWindowSpacing,
				mainWindowPadding,
				TextAnchor.UpperCenter
			)
		{
			model = m;
			resetCallback = reset;

			if (!ErrorCondition) {
				createHeaders();
				createRows();
			}
			AddChild(new DialogGUIHorizontalLayout(
				mainWindowMinWidth, 10,
				mainWindowSpacing, mainWindowPadding,
				TextAnchor.UpperRight,
				new DialogGUIFlexibleSpace(),
				iconButton(settingsIcon, settingsStyle, "Settings", toggleSettingsVisible)
			));
			if (ShowSettings) {
				AddChild(new SettingsView());
			}
		}

		private AstrogationModel model { get; set; }
		private PopupDialog dialog { get; set; }

		/// <summary>
		/// Type of function pointer used to request a re-creation of the UI.
		/// This is needed because the DialogGUI* functions don't allow us to
		/// make dynamic chnages to a UI beyond changing a label's text.
		/// </summary>
		public delegate void ResetCallback();

		private ResetCallback resetCallback { get; set; }

		private static Rect geometry {
			get {
				Vector2 pos = Settings.Instance.MainWindowPosition;
				return new Rect(pos.x, pos.y, mainWindowMinWidth, mainWindowMinHeight);
			}
			set {
				Settings.Instance.MainWindowPosition = new Vector2(value.x, value.y);
			}
		}

		private bool ShowSettings {
			get {
				return Settings.Instance.ShowSettings;
			}
			set {
				Settings.Instance.ShowSettings = value;
			}
		}

		/// <summary>
		/// The user-facing name for this mod.
		/// Use Astrogator.Name for filenames, internal representations, CKAN, etc.
		/// </summary>
		public const string DisplayName = "Astrogator";

		/// <summary>
		/// UI object representing the top row of the table
		/// </summary>
		private static DialogGUIHorizontalLayout ColumnHeaders { get; set; }

		private void toggleSettingsVisible()
		{
			ShowSettings = !ShowSettings;
			resetCallback();
		}

		private void createHeaders()
		{
			if (ColumnHeaders == null) {
				ColumnHeaders = new DialogGUIHorizontalLayout();
				for (int i = 0; i < Columns.Length; ++i) {
					ColumnDefinition col = Columns[i];
					// Skip columns that require an active vessel if we don't have one
					if (!col.vesselSpecific || model.vessel != null) {
						int width = 0;
						for (int span = 0; span < col.headerColSpan; ++span) {
							width += Columns[i + span].width;
						}
						if (width > 0) {
							ColumnHeaders.AddChild(LabelWithStyleAndSize(col.header, col.headerStyle, width, rowHeight));
						}
					}
				}
			}
			AddChild(ColumnHeaders);
		}

		private void createRows()
		{
			for (int i = 0; i < model.transfers.Count; ++i) {
				AddChild(new TransferView(model.transfers[i]));
			}
		}

		private bool ErrorCondition {
			get {
				return model == null
					|| model.transfers.Count == 0
					|| model.badInclination
					|| model.hyperbolicOrbit
					|| model.notOrbiting
					|| model.retrogradeOrbit;
			}
		}

		private string subTitle {
			get {
				if (model != null) {
					if (model.hyperbolicOrbit) {
						return string.Format(
							"{0} is on a hyperbolic trajectory. Capture to see transfer info.",
							TheName(model.vessel)
						);
					} else if (model.notOrbiting) {
						return string.Format(
							"{0} is landed. Launch to orbit to see transfer info.",
							TheName(model.vessel)
						);
					} else if (model.retrogradeOrbit) {
						return string.Format(
							"Orbit is retrograde, must be prograde with inclination below {1:0.}°",
							Math.Abs(model.vessel.orbit.inclination),
							AstrogationModel.maxInclination * Mathf.Rad2Deg
						);
					} else if (model.badInclination) {
						return string.Format(
							"Inclination is {0:0.0}°, accuracy too low past {1:0.}°",
							Math.Abs(model.vessel.orbit.inclination),
							AstrogationModel.maxInclination * Mathf.Rad2Deg
						);
					} else if (model.transfers.Count == 0) {
						return "No transfers available";
					} else {
						return string.Format("Transfers from {0}", model.OriginDescription);
					}
				} else {
					return "Internal error: Model not found";
				}
			}
		}

		private UISkinDef skinToUse {
			get {
				if (!ErrorCondition) {
					return AstrogatorSkin;
				} else {
					return AstrogatorErrorSkin;
				}
			}
		}

		/// <summary>
		/// Launch a PopupDialog containing the view.
		/// Use Dismiss() to get rid of it.
		/// </summary>
		public PopupDialog Show()
		{
			return dialog = PopupDialog.SpawnPopupDialog(
				mainWindowAnchorMin,
				mainWindowAnchorMax,
				new MultiOptionDialog(
					subTitle,
					DisplayName,
					skinToUse,
					geometry,
					this
				),
				false,
				skinToUse,
				false
			);
		}

		/// <summary>
		/// Close the popup.
		/// </summary>
		public void Dismiss()
		{
			if (dialog != null) {
				Vector3 rt = dialog.RTrf.position;
				geometry = new Rect(
					rt.x / Screen.width  + 0.5f,
					rt.y / Screen.height + 0.5f,
					mainWindowMinWidth,
					mainWindowMinHeight
				);
				dialog.Dismiss();
				dialog = null;
			}
		}
	}

}
