using System;
using UnityEngine;

namespace Astrogator {

	using static ViewTools;

	/// <summary>
	/// Wrapper around DialogGUIToggle using a DialogGUILabel to wrap the text
	/// </summary>
	public class WrappingToggle : DialogGUIHorizontalLayout {

		/// <summary>
		/// Construct a toggle button with wrapping label
		/// </summary>
		/// <param name="set">Function to call when user toggles the toggle</param>
		/// <param name="labelText">Text to show next to the toggle button</param>
		/// <param name="selected">Function that returns true if the toggle should be on, false if off</param>
		/// <param name="width">Horizontal space for the toggle, text will wrap if it's wider than this</param>
		/// <param name="height">Vertical space for the toggle, default is auto size</param>
		public WrappingToggle(Func<bool> set, string labelText, Callback<bool> selected, float width, float height = -1)
			: base(width, height, 0, settingsPadding, TextAnchor.MiddleLeft)
		{
			AddChild(new DialogGUIToggle(set, "", selected, toggleImageWidth, height));
			AddChild(new DialogGUILabel(labelText, width - toggleImageWidth, height) {
				guiStyle = toggleStyle,
				textLabelOptions = wordWrap,
			});
		}

		private static float toggleImageWidth = 24;

		private static DialogGUILabel.TextLabelOptions wordWrap = new DialogGUILabel.TextLabelOptions() {
			OverflowMode = TMPro.TextOverflowModes.Overflow
		};

	}

}
