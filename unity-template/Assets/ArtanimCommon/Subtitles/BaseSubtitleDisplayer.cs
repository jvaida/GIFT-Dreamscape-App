using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

	public abstract class BaseSubtitleDisplayer : MonoBehaviour
	{
		/// <summary>
		/// Method called by the SubtitleController to show a subtitle. The given subtitle argument is already translated
		/// (if needed) and does not have to be translated by the displayer.
		/// </summary>
		/// <param name="subtitle">Subtitle to display</param>
		public abstract void ShowSubtitle(string subtitle);

		/// <summary>
		/// Called by the SubtitleController to hide the subtitle.
		/// </summary>
		/// <param name="keepSubtitleBackground">Flag indicating if the subitle background should be kept visible</param>
		public abstract void HideSubtitle(bool keepSubtitleBackground);

		/// <summary>
		/// Return a unique id used for this displayer.
		/// Null value is only used for default displayer configured in the experience settings or the SDK defaults.
		/// </summary>
		/// <returns></returns>
		public abstract string GetDisplayerId();
	}
}