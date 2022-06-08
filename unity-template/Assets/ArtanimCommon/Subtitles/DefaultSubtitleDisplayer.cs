using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	public class DefaultSubtitleDisplayer : BaseSubtitleDisplayer
	{
		public GameObject PanelSubtitles;
		public Text TextSubtitle;
        public AlwaysOnTopCanvas AlwaysOnTopCanvas;
		public string DisplayerId;

		#region Unity events

		void Start()
		{
			if (PanelSubtitles)
				PanelSubtitles.SetActive(false);
		}

		#endregion

		#region BaseSubtitleDisplayer interface

		public override void ShowSubtitle(string subtitle)
		{
            //Place canvas on top of rendering
            if (AlwaysOnTopCanvas)
                AlwaysOnTopCanvas.IsOnTop = true;

			//Activate panel
			if (PanelSubtitles)
				PanelSubtitles.SetActive(true);

			//Set subtitle
			if (TextSubtitle)
			{
				TextSubtitle.gameObject.SetActive(true);
				TextSubtitle.text = subtitle;
			}
		}

		public override void HideSubtitle(bool keepSubtitleBackground)
		{
            //Reset canvas on top of rendering
            if (AlwaysOnTopCanvas)
                AlwaysOnTopCanvas.IsOnTop = false;

            //Clear subtitle
            if (TextSubtitle)
				TextSubtitle.text = string.Empty;

			//Hide text
			if (keepSubtitleBackground && TextSubtitle)
				TextSubtitle.gameObject.SetActive(false);
			//Hide panel
			else if (PanelSubtitles)
				PanelSubtitles.SetActive(false);
		}

		public override string GetDisplayerId()
		{
			return DisplayerId;
		}

		#endregion
	}
}