using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Astrogator {

	using static DebugTools;
	using static KerbalTools;
	using static ViewTools;
	using static PhysicsTools;
	using static Language;

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
		/// <param name="close">Function to call when the user clicks a close button</param>
		public AstrogationView(AstrogationModel m, ResetCallback reset, UnityAction close)
			: base(
				mainWindowMinWidth,
				mainWindowMinHeight,
				mainWindowSpacing,
				mainWindowPadding,
				TextAnchor.UpperCenter
			)
		{
			model         = m;
			resetCallback = reset;
			closeCallback = close;

			if (!ErrorCondition) {
				createHeaders();
				createRows();
			}
			AddChild(new DialogGUIHorizontalLayout(
				RowWidth, 10,
				0, wrenchPadding,
				TextAnchor.UpperRight,
				new DialogGUILabel(getMessage, notificationStyle, true, true),
				iconButton(settingsIcon, settingsStyle, settingsButtonTooltip, toggleSettingsVisible)
			));
			if (Settings.Instance.ShowSettings) {
				AddChild(new SettingsView(resetCallback));
			} else if (!ErrorCondition) {
				createHeaders();
				createRows();
				AddChild(new DialogGUIHorizontalLayout(
					RowWidth, 10,
					0, wrenchPadding,
					TextAnchor.UpperRight,
					new DialogGUILabel(getMessage, notificationStyle, true, true)
				));
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
		private UnityAction   closeCallback { get; set; }

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
		/// UI object representing the top row of the table
		/// </summary>
		private DialogGUIHorizontalLayout ColumnHeaders { get; set; }

		private string columnSortIndicator(ColumnDefinition col)
		{
			return col.sortKey != Settings.Instance.TransferSort ? ""
				: Settings.Instance.DescendingSort ? " ▼"
				: " ▲";
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
						&& (!patchedConicsUnlocked() || model.origin == null || model.notOrbiting)) {
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
							col.headerStyle, columnHeaderTooltip, width, rowHeight, () => {
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
					|| model.origin == null
					|| model.transfers.Count == 0
					|| model.ErrorCondition;
			}
		}

		private string subTitle {
			get {
				if (model != null) {
					if (model.origin == null) {
						return "Model's origin is null";
					} else if (model.hyperbolicOrbit) {
						if (model.inbound) {
							return string.Format(
								inboundHyperbolicWarning,
								TheName(model.origin)
							);
						} else {
							return string.Format(
								outboundHyperbolicError,
								TheName(model.origin)
							);
						}
					} else if (model.badInclination) {
						return string.Format(
							highInclinationError,
							AngleFromEquatorial(model.origin.GetOrbit().inclination * Mathf.Deg2Rad) * Mathf.Rad2Deg,
							AstrogationModel.maxInclination * Mathf.Rad2Deg
						);
					} else if (model.transfers.Count == 0) {
						return noTransfersError;
					} else if (Landed(model.origin) || solidBodyWithoutVessel(model.origin)) {
						CelestialBody b = model.origin as CelestialBody;
						if (b == null) {
							b = model.origin.GetOrbit().referenceBody;
						}
						return string.Format(
							launchSubtitle,
							TheName(model.origin),
							FormatSpeed(DeltaVToOrbit(b), Settings.Instance.DisplayUnits)
						);
					} else {
						return string.Format(normalSubtitle, TheName(model.origin));
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
				return translationControlsNotification;
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

		private UIStyle settingsToggleStyle {
			get {
				if (Settings.Instance.ShowSettings) {
					return backStyle;
				} else {
					return settingsStyle;
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
						mainTitle,
						subTitle,
						mainTitle + " " + versionString,
						skinToUse,
						geometry,
						this
					),
					false,
					skinToUse,
					false
				);

				// Add the close button in the upper right corner after the PopupDialog has been created.
				AddFloatingButton(
					dialog.transform,
					-mainWindowPadding.right - mainWindowSpacing, -mainWindowPadding.top,
					closeStyle,
					closeCallback
				);

				// Add the settings button next to the close button.
				// If the settings are visible it's a back '<' icon, otherwise a wrench+screwdriver.
				AddFloatingButton(
					dialog.transform,
					-mainWindowPadding.right - 3 * mainWindowSpacing - buttonIconWidth,
					-mainWindowPadding.top,
					settingsToggleStyle,
					toggleSettingsVisible
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
