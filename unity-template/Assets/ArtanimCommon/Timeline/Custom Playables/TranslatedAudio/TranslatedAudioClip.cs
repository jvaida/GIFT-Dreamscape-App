using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim
{
	public class TranslatedAudioClip : AudioPlayableAsset
	{
		[Tooltip("Path within the resources folder used to search for translated object")]
		public string ResourcePath;

        [Tooltip("Specifies if the audio clip weight should be applied to the audio source volume. Default is false, enabling timeline fading.")]
        public bool IgnoreAudioClipWeight = false;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			//Debug.LogErrorFormat("TranslatedAudioClip.CreatePlayable: {0}", clip ? clip.GetType().Name : "null");

			var playable = ScriptPlayable<TranslatedAudioBehaviour>.Create(graph);
			var behaviour = playable.GetBehaviour();

			behaviour.AudioClip = clip;
			behaviour.Loop = (clipCaps & ClipCaps.Looping) == ClipCaps.Looping;
			behaviour.ResourcePath = ResourcePath;
            behaviour.IgnoreAudioClipWeight = IgnoreAudioClipWeight;
			
			return playable;
		}

	}

}