using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;

namespace Artanim
{
	public class TranslatedVideoClip : PlayableAsset
	{
		[Tooltip("Video clip to be played.")]
		public VideoClip Clip;

		[Tooltip("Mute the video audio.")]
		[SerializeField, NotKeyable]
		public bool Mute = false;

		[Tooltip("Loop the video playback.")]
		[SerializeField, NotKeyable]
		public bool Loop = true;

		[Tooltip("Time withing the video to start the playback.")]
		[SerializeField, NotKeyable]
		public double ClipInTime = 0.0;

		[Tooltip("Path within the resources folder used to search for translated object")]
		[SerializeField, NotKeyable]
		public string ResourcePath;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<TranslatedVideoBehaviour>.Create(graph);
			var behaviour = playable.GetBehaviour();

			behaviour.VideoClip = Clip;
			behaviour.ResourcePath = ResourcePath;

			behaviour.Mute = Mute;
			behaviour.Loop = Loop;
			behaviour.ClipInTime = ClipInTime;

			return playable;
		}
		
	}
}