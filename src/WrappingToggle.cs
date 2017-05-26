using System;
using UnityEngine;

namespace Astrogator {

	using static ViewTools;

	public class WrappingToggle : DialogGUIHorizontalLayout {

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
