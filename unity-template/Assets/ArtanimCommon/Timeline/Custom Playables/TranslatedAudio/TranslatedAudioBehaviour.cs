using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim
{
	public class TranslatedAudioBehaviour : PlayableBehaviour
	{
		public AudioClip AudioClip;
		public bool Loop;
		public string ResourcePath;
        public bool IgnoreAudioClipWeight = false;


        private TranslatedAudioSource TranslatedAudioSource;
		private bool ClipWasPlayed;

		public override void OnGraphStart(Playable playable)
		{
			base.OnGraphStart(playable);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
		{
            if (TranslatedAudioSource)
			{
				TranslatedAudioSource.Stop();
				ClipWasPlayed = false;
			}
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
            if (!TranslatedAudioSource)
			{
				//Setup translated audio source if available
				TranslatedAudioSource = playerData as TranslatedAudioSource;
			}

			if (info.weight > 0 && AudioClip && TranslatedAudioSource)
			{
				if (info.weight > 0)
				{
                    if(!IgnoreAudioClipWeight && TranslatedAudioSource.AudioSource.volume != info.weight)
                    {
                        //Debug.LogFormat("Playback Time={0}, Volume={1}", playable.GetTime(), info.weight);
                        TranslatedAudioSource.AudioSource.volume = info.weight;
                    }

                    //Already played this one?
                    if (!ClipWasPlayed && !TranslatedAudioSource.AudioSource.isPlaying)
					{
                        //Debug.LogErrorFormat("Playing {0} on {1}, clipIn={2}", AudioClip.name, TranslatedAudioSource.name, playable.GetTime());
                        TranslatedAudioSource.Play(AudioClip, ResourcePath, Loop, clipInTime: (float)playable.GetTime());
						ClipWasPlayed = true;
					}
				}
			}
		}

	}
}