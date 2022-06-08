using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim
{

	[Serializable]
	public class SubtitleClip : PlayableAsset, ITimelineClipAsset
	{
		public SubtitleBehaviour Template = new SubtitleBehaviour();

		[Tooltip("Subtitle or textId to display")]
        [SerializeField, NotKeyable]
		public string Subtitle;

		[Tooltip("Indicates if the given subtitle is a textId to be translated")]
        [SerializeField, NotKeyable]
		public bool IsTextId = true;

		[Tooltip("Should the subtitle background stay visible after the displayDuration")]
        [SerializeField, NotKeyable]
		public bool KeepSubtitleBackground;

		[Tooltip("Forces to display the subtitle even if the player does not see subtitles")]
        [SerializeField, NotKeyable]
		public bool ForceDisplay = false;

		public ClipCaps clipCaps
		{
			get
			{
				return ClipCaps.Blending;
			}
		}

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			//Create clip instance
			ScriptPlayable<SubtitleBehaviour> playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);
			SubtitleBehaviour playableBehaviour = playable.GetBehaviour();

			playableBehaviour.Subtitle = Subtitle;
			playableBehaviour.IsTextId = IsTextId;
			playableBehaviour.KeepSubtitleBackground = KeepSubtitleBackground;
			playableBehaviour.ForceDisplay = ForceDisplay;

			return playable;
		}

	}
}