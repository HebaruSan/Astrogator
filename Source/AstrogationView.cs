using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using KSP.Localization;

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
		/// <param name="close">Function to call when the user clicks a close button</param>
		public AstrogationView(AstrogationModel m, ResetCallback reset, UnityAction close)
			: base(
				FlightGlobals.ActiveVessel != null ? mainWindowMinWidthWithVessel : mainWindowMinWidthWithoutVessel,
				mainWindowMinHeight,
				mainWindowSpacing,
				mainWindowPadding,
				TextAnchor.UpperCenter
			)
		{
			model         = m;
			resetCallback = reset;
			closeCallback = close;

			int width = FlightGlobals.ActiveVessel != null ? RowWidthWithVessel : RowWidthWithoutVessel;

			if (Settings.Instance.ShowSettings) {
				AddChild(new SettingsView(resetCallback, width));
			} else if (!ErrorCondition) {
				createHeaders();
				createRows();
				AddChild(new DialogGUIHorizontalLayout(
					width, 10,
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
					: Settings.Instance.DescendingSort ? " v"
					: " ^";
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
				if (col.requiresPatchedConics && (
					!patchedConicsUnlocked()
						|| !vesselControllable(FlightGlobals.ActiveVessel)
						|| model.origin == null
						|| Landed(model.origin)
				)) {
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
							col.headerStyle, Localizer.Format("astrogator_columnHeaderTooltip"), width, -1, () => {
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

		private void createRows()
		{
			List<TransferModel> transfers = SortTransfers(
				model,
				Settings.Instance.TransferSort,
				Settings.Instance.DescendingSort
			);
			for (int i = 0; i < transfers.Count; ++i) {
				AddChild(new TransferView(transfers[i]));
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

		private string getMessage()
		{
			if (model.ActiveEjectionBurn != null
					&& Settings.Instance.TranslationAdjust
					&& FlightGlobals.ActiveVessel != null
					&& !FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS]) {
				return Localizer.Format("astrogator_translationControlsNotification");
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

		private string settingsToggleTooltip {
			get {
				if (Settings.Instance.ShowSettings) {
					return "astrogator_backButtonTooltip";
				} else {
					return "astrogator_settingsButtonTooltip";
				}
			}
		}

		private Rect geometry {
			get {
				// UI_SCALE is handled by calculating distance from this Y coordinate,
				// then scaling it, then converting back to position so as to
				// increase the minimum value.
				const float fakeTop = 1f;
				const float margin = 0.05f;

				Vector2 pos = Settings.Instance.MainWindowPosition;
				float sc = uiScale;
				// UI_SCALE chops off some of the left and right edges of the screen
				float leftEdge = (float)estimateLeftEdgeLogarithmic(sc);
				float rightEdge = 1f - leftEdge;
				float between = rightEdge - leftEdge;
				return new Rect(
					Math.Min(Math.Max(pos.x, leftEdge), rightEdge),
					// UI_SCALE chops off some of the bottom of the screen
					Math.Max(
						fakeTop - (fakeTop - margin) / sc,
						fakeTop - (fakeTop - pos.y)  / sc
					),
					FlightGlobals.ActiveVessel != null ? mainWindowMinWidthWithVessel : mainWindowMinWidthWithoutVessel,
					mainWindowMinHeight
				);
			}
			set { Settings.Instance.MainWindowPosition = value.position; }
		}

		private Rect currentGeometry {
			get {
				RectTransform[] inParents = dialog.GetComponentsInParent<RectTransform>();
				Rect    winRect   = inParents[0].rect;
				Rect    scrRect   = inParents[1].rect;
				Vector3 winLocPos = inParents[0].localPosition;
				return new Rect(
					(winLocPos.x - scrRect.x) / scrRect.width,
					(winLocPos.y - scrRect.y) / scrRect.height,
					FlightGlobals.ActiveVessel != null ? mainWindowMinWidthWithVessel : mainWindowMinWidthWithoutVessel,
					mainWindowMinHeight
				);
			}
		}

		/// <summary>
		/// Get the active UI scale setting.
		/// I've been seeing a bug in KSP 1.8.0 and 1.8.1 where stock settings,
		/// UI Scale included, aren't applied at startup unless I go into the
		/// settings and click Apply or Accept. To work around this, we check
		/// the rect transform associated with the active dialog first, only falling
		/// back to the setting if there isn't one.
		/// </summary>
		/// <returns>
		/// UI scale fraction (1 for 100%, 0.8 for 80%, 1.5 for 150%, etc.)
		/// </returns>
		private float uiScale {
			get {
				RectTransform[] inParents = dialog?.GetComponentsInParent<RectTransform>();
				return inParents?[1].localScale.x ?? GameSettings.UI_SCALE;
			}
		}

		/// <summary>
		/// MultiOptionDialog's Rect param expects strangely different values
		/// depending on the UI scale. If the scale is 100%, then (0.5,0.1) is at
		/// the bottom middle of the screen, but at 200% it is off the bottom of
		/// the screen (which is at Y=0.5). Similarly, (0.05,0.6) is at the left
		/// middle at 100%, but off the left edge at 200% (when the left boundary is
		/// around X=0.33). To make sure our window is visible, we need to account
		/// for this quirk, but I have not found anything provided by stock to help.
		///
		/// In desperation, I measured the values for the the left and right edges
		/// of the screen at various scales, plugged them into a spreadsheet,
		/// and solved for a logarithmic formula (R² ≈ 0.986). This allows us to
		/// guess whether a given coordinate will be on-screen when opening a dialog.
		///
		/// XXX: This might be resolution-dependent!
		/// </summary>
		/// <param name="scale">The current UI_SCALE setting</param>
		/// <returns>
		/// Value representing the left edge of the screen for MultiOptionDialog's
		/// rect parameter at the given UI scale
		/// </returns>
		private static double estimateLeftEdgeLogarithmic(double scale)
		{
			const double estimateCoeff = 0.407367910114971;
			const double estimateConst = 0.072429060773288;
			return estimateCoeff * Math.Log(scale) + estimateConst;
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
						Localizer.Format("astrogator_mainTitle"),
						ModelDescription(model),
						Localizer.Format("astrogator_mainTitle") + " " + versionString,
						skinToUse,
						geometry,
						this
					),
					false,
					skinToUse,
					false
				);

				// Save position and deactivate app launcher on Esc
				dialog.OnDismiss = onDismiss;

				// Add the close button in the upper right corner after the PopupDialog has been created.
				AddFloatingButton(
					dialog.transform,
					-mainWindowPadding.right - mainWindowSpacing, -mainWindowPadding.top,
					closeStyle,
					"astrogator_closeButtonTooltip",
					closeCallback
				);

				// Add the settings button next to the close button.
				// If the settings are visible it's a back '<' icon, otherwise a wrench+screwdriver.
				AddFloatingButton(
					dialog.transform,
					-mainWindowPadding.right - 3 * mainWindowSpacing - buttonIconWidth,
					-mainWindowPadding.top,
					settingsToggleStyle,
					settingsToggleTooltip,
					toggleSettingsVisible
				);
			}
			return dialog;
		}

		/// <summary>
		/// React to the user closing all dialogs with Esc,
		/// doesn't get called when dismissing programmatically
		/// </summary>
		private void onDismiss()
		{
			geometry = currentGeometry;
			dialog = null;
			if (closeCallback != null) {
				closeCallback();
			}
		}

		/// <summary>
		/// Close the popup.
		/// </summary>
		public void Dismiss()
		{
			if (dialog != null) {
				geometry = currentGeometry;
				dialog.Dismiss();
				dialog = null;
			}
		}
	}

}
