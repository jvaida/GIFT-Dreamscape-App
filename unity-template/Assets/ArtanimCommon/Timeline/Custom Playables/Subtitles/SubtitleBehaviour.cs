using Artanim;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;

namespace Artanim
{
	public class SubtitleBehaviour : PlayableBehaviour
	{
		public string Subtitle;
		public bool IsTextId = true;
		public bool KeepSubtitleBackground;
		public bool ForceDisplay = false;

		private BaseSubtitleDisplayer Displayer;
		private bool SubtitleWasPlayed;

		public override void OnGraphStart(Playable playable)
		{
			var duration = playable.GetDuration();

			if (Mathf.Approximately((float)duration, 0f))
			{
				throw new UnityException("Cannot have a zero duration");
			}
		}

		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			if(SubtitleController.Instance)
			{
				//Debug.LogFormat("Behaviour pause: {0}", Text);
				SubtitleController.Instance.HideSubtitle(KeepSubtitleBackground, displayer: Displayer);
				SubtitleWasPlayed = false;
			}
		}
		
		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			if (!Displayer)
			{
				//Setup translated audio source if available
				Displayer = playerData as BaseSubtitleDisplayer;
			}

			if (SubtitleController.Instance && info.weight == 1f && !SubtitleWasPlayed)
			{
				SubtitleController.Instance.ShowSubtitle(
					Subtitle,
					isTextId: IsTextId,
					displayDuration: 0,
					keepSubtitleBackground: KeepSubtitleBackground,
					forceDisplay: ForceDisplay,
					displayer: Displayer);

				SubtitleWasPlayed = true;
			}
		}
	}

}