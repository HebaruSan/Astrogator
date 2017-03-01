using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;
	using static PhysicsTools;

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
				RowWidth, 10,
				0, wrenchPadding,
				TextAnchor.UpperRight,
				new DialogGUILabel(getMessage, notificationStyle, true, true),
				iconButton(settingsIcon, settingsStyle, "Settings", toggleSettingsVisible)
			));
			if (Settings.Instance.ShowSettings) {
				AddChild(new SettingsView(resetCallback));
			}
		}

		private AstrogationModel model  { get; set; }
		private PopupDialog      dialog { get; set; }

		/// <summary>
		/// Type of function pointer used to request a re-creation of the UI.
		/// This is needed because the DialogGUI* functions don't allow us to
		/// make dynamic chnages to a UI beyond changing a label's text.
		/// </summary>
		public delegate void ResetCallback(bool resetModel = false);

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

		private void toggleSettingsVisible()
		{
			Settings.Instance.ShowSettings = !Settings.Instance.ShowSettings;
			resetCallback();
		}

		/// <summary>
		/// The user-facing name for this mod.
		/// Use Astrogator.Name for filenames, internal representations, CKAN, etc.
		/// </summary>
		public const string DisplayName = "Astrogator";

		/// <summary>
		/// UI object representing the top row of the table
		/// </summary>
		private DialogGUIHorizontalLayout ColumnHeaders { get; set; }

		private string columnSortIndicator(ColumnDefinition col)
		{
			return col.sortKey != Settings.Instance.TransferSort ? ""
				: Settings.Instance.DescendingSort ? " ↓"
				: " ↑";
		}

		private void createHeaders()
		{
			ColumnHeaders = new DialogGUIHorizontalLayout();
			for (int i = 0; i < Columns.Length; ++i) {
				ColumnDefinition col = Columns[i];
				// Skip columns that require an active vessel if we don't have one
				if (col.vesselSpecific && FlightGlobals.ActiveVessel == null) {
					continue;
				}
				if (col.requiresPatchedConics
						&& (!patchedConicsUnlocked() || model.notOrbiting)) {
					continue;
				}
				float width = 0;
				for (int span = 0; span < col.headerColSpan; ++span) {
					width += Columns[i + span].width;
				}
				if (width > 0) {
					// Add in the spacing gaps that got left out from colspanning
					width += (col.headerColSpan - 1) * spacing;
					if (col.header != "") {
						ColumnHeaders.AddChild(headerButton(
							col.header + columnSortIndicator(col),
							col.headerStyle, "Sort", width, rowHeight, () => {
								SortClicked(col.sortKey);
							}
						));
					} else {
						ColumnHeaders.AddChild(new DialogGUISpace(width));
					}
				}
			}
			AddChild(ColumnHeaders);
		}

		private void SortClicked(SortEnum which)
		{
			if (Settings.Instance.TransferSort == which) {
				Settings.Instance.DescendingSort = !Settings.Instance.DescendingSort;
			} else {
				Settings.Instance.TransferSort = which;
				Settings.Instance.DescendingSort = false;
			}
			resetCallback();
		}

		private List<TransferModel> SortTransfers(AstrogationModel m, SortEnum how, bool descend)
		{
			List<TransferModel> transfers = new List<TransferModel>(m.transfers);
			switch (how) {
				case SortEnum.Name:
					transfers.Sort((a, b) =>
						a?.destination?.GetName().CompareTo(b?.destination?.GetName()) ?? 0);
					break;
				case SortEnum.Position:
					// Use the natural/default ordering in the model
					break;
				case SortEnum.Time:
					transfers.Sort((a, b) =>
						a?.ejectionBurn?.atTime?.CompareTo(b?.ejectionBurn?.atTime ?? 0) ?? 0);
					break;
				case SortEnum.DeltaV:
					transfers.Sort((a, b) =>
						a?.ejectionBurn?.totalDeltaV.CompareTo(b?.ejectionBurn?.totalDeltaV) ?? 0);
					break;
				default:
					DbgFmt("Bad sort argument: {0}", how.ToString());
					break;
			}
			if (descend) {
				transfers.Reverse();
			}
			return transfers;
		}

		private void createRows()
		{
			List<TransferModel> transfers = SortTransfers(
				model,
				Settings.Instance.TransferSort,
				Settings.Instance.DescendingSort
			);
			for (int i = 0; i < transfers.Count; ++i) {
				AddChild(new TransferView(transfers[i], resetCallback));
			}
		}

		private bool ErrorCondition {
			get {
				return model == null
					|| model.transfers.Count == 0
					|| model.ErrorCondition;
			}
		}

		private string subTitle {
			get {
				if (model != null) {
					if (model.hyperbolicOrbit) {
						if (model.inbound) {
							return string.Format(
								"{0} is on an escape trajectory.\nCapture to see more transfers.",
								TheName(model.origin)
							);
						} else {
							return string.Format(
								"{0} is on an escape trajectory.\nCapture to see transfers.",
								TheName(model.origin)
							);
						}
					} else if (model.badInclination) {
						return string.Format(
							"Inclination is {0:0.0}°, accuracy too low past {1:0.}°",
							AngleFromEquatorial(model.origin.GetOrbit().inclination * Mathf.Deg2Rad) * Mathf.Rad2Deg,
							AstrogationModel.maxInclination * Mathf.Rad2Deg
						);
					} else if (model.transfers.Count == 0) {
						return "No transfers available";
					} else if (Landed(model.origin) || solidBodyWithoutVessel(model.origin)) {
						CelestialBody b = model.origin as CelestialBody;
						if (b == null) {
							b = model.origin.GetOrbit().referenceBody;
						}
						return string.Format(
							"Transfers from {0}\n(Launch ~{1})",
							TheName(model.origin),
							FormatSpeed(DeltaVToOrbit(b), Settings.Instance.DisplayUnits)
						);
					} else {
						return string.Format("Transfers from {0}", TheName(model.origin));
					}
				} else {
					return "Internal error: Model not found";
				}
			}
		}

		private string getMessage()
		{
			if (model.ActiveEjectionBurn != null
					&& Settings.Instance.TranslationAdjust
					&& FlightGlobals.ActiveVessel != null
					&& !FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS]) {
				return "Use translation controls to adjust nodes";
			} else {
				return "";
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
			if (dialog == null) {
				dialog = PopupDialog.SpawnPopupDialog(
					mainWindowAnchorMin,
					mainWindowAnchorMax,
					new MultiOptionDialog(
						subTitle,
						DisplayName + " " + versionString,
						skinToUse,
						geometry,
						this
					),
					false,
					skinToUse,
					false
				);
			}
			return dialog;
		}

		/// <summary>
		/// Close the popup.
		/// </summary>
		public void Dismiss()
		{
			if (dialog != null) {
				Vector3 rt = dialog.RTrf.position;
				DbgFmt("Coordinates at window close: {0}", rt.ToString());
				DbgFmt("Screen dimensions at window close: {0}x{1}", Screen.width, Screen.height);
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
