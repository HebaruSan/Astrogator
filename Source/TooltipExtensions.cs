using UnityEngine;
using KSP.UI.TooltipTypes;

namespace Astrogator {

	/// <summary>
	/// Utilities to simplify use of tooltips with DialogGUI*-based UIs.
	/// </summary>
	public static class TooltipExtensions {

		private static readonly Tooltip_TitleAndText titleAndTextTooltipPrefab = AssetBase.GetPrefab<Tooltip_TitleAndText>("Tooltip_TitleAndText");

		/// <summary>
		/// Create a tooltip object for a given GameObject, containing both
		/// a title and a subtitle.
		/// </summary>
		/// <param name="gameObj">GameObject to which we want to add a tooltip</param>
		/// <param name="title">Highlighted text for the tooltip</param>
		/// <param name="text">Less emphasized text for the tooltip</param>
		public static void SetTooltip(this GameObject gameObj, string title, string text)
		{
			if (gameObj != null) {
				TooltipController_TitleAndText tt = (gameObj?.GetComponent<TooltipController_TitleAndText>() ?? gameObj?.AddComponent<TooltipController_TitleAndText>());
				if (tt != null) {
					tt.prefab      = titleAndTextTooltipPrefab;
					tt.titleString = title;
					tt.textString  = text;
				}
			}
		}

		private static readonly Tooltip_Text textTooltipPrefab = AssetBase.GetPrefab<Tooltip_Text>("Tooltip_Text");

		/// <summary>
		/// Create a tooltip object for a given GameObject, containing just one simple string.
		/// </summary>
		/// <param name="gameObj">GameObject to which we want to add a tooltip</param>
		/// <param name="tooltip">The text to show in the tooltip</param>
		/// <returns>
		/// True if we are able to create the tooltip, false otherwise
		/// </returns>
		public static bool SetTooltip(this GameObject gameObj, string tooltip)
		{
			if (gameObj != null) {
				TooltipController_Text tt = (gameObj.GetComponent<TooltipController_Text>() ?? gameObj.AddComponent<TooltipController_Text>());
				if (tt != null) {
					tt.textString = tooltip;
					tt.prefab     = textTooltipPrefab;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Set up callbacks to activate a tooltip for a DialogGUI* object later.
		/// DialogGUI* objects don't have GameObjects until they're displayed,
		/// and you need that to add a tooltip, so we need an asynchronous strategy.
		/// </summary>
		/// <param name="gb">DialogGUI* object that needs a tooltip</param>
		/// <returns>
		/// The same object that was passed in
		/// </returns>
		public static DialogGUIBase DeferTooltip(DialogGUIBase gb)
		{
			if (gb.tooltipText != "") {
				gb.OnUpdate = () => {
					if (gb.uiItem != null
							&& gb.uiItem.SetTooltip(gb.tooltipText)) {
						gb.OnUpdate = () => {};
					}
				};
			}
			return gb;
		}

	}

}
