using UnityEngine;
using KSP.UI.Screens;

namespace Astrogator {

	using static DebugTools;
	using static ViewTools;

	/// A class for a simple tooltip popup, since I couldn't find a way to do that easily in Unity.
	class TooltipView : MultiOptionDialog {
		public PopupDialog dialog { get; set; }

		public TooltipView(string title, string description, Rect where)
			: base(
				description,
				" " + title + " ",
				AstrogatorSkin,
				where)
			{ }

		/// Use this wrapper to generate a tooltip for an app launcher button.
		public static TooltipView AppLauncherTooltip(
				string title, string description,
				ApplicationLauncherButton button)
		{
			Vector3 rt = button.GetAnchor();
			// It seems we need to convert from [[-w/2, w/2], [-h/2, h/2]] to [[0,1],[0,1]],
			// so I just divide and add 0.5. There's probably an API for this.
			return new TooltipView(
				title,
				description,
				new Rect(
					rt.x / Screen.width + 0.5f,
					rt.y / Screen.height + 0.5f,
					200,
					10
				)
			);
		}

		public void Show()
		{
			dialog = PopupDialog.SpawnPopupDialog(
				Vector2.right, // Anchor the lower right corner of the tooltip to the button
				Vector2.right,
				this,
				false,
				AstrogatorSkin,
				false
			);
		}

		public void Dismiss()
		{
			if (dialog != null) {
				dialog.Dismiss();
				dialog = null;
			}
		}
	}

}
