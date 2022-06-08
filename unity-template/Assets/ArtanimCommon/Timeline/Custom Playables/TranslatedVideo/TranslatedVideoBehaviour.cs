using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace Artanim
{
	public class TranslatedVideoBehaviour : PlayableBehaviour
	{

		public VideoClip VideoClip;
		public string ResourcePath;


		public bool Mute = false;
		public bool Loop = true;
		public double ClipInTime = 0.0;


		private TranslatedVideoPlayer TranslatedVideoPlayer;
		private bool ClipWasPlayed;

		private bool IsDataValid
		{
			get
			{
				return VideoClip && TranslatedVideoPlayer;
			}
		}

		public void PrepareVideo()
		{
			if (!IsDataValid)
				return;

			TranslatedVideoPlayer.VideoPlayer.targetCameraAlpha = 0.0f;

			if (TranslatedVideoPlayer.VideoPlayer.clip != VideoClip)
				StopVideo();

			TranslatedVideoPlayer.VideoPlayer.source = VideoSource.VideoClip;
			TranslatedVideoPlayer.VideoPlayer.clip = VideoClip;
			TranslatedVideoPlayer.VideoPlayer.playOnAwake = false;
			TranslatedVideoPlayer.VideoPlayer.waitForFirstFrame = true;

			for (ushort i = 0; i < VideoClip.audioTrackCount; ++i)
			{
				if (TranslatedVideoPlayer.VideoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
					TranslatedVideoPlayer.VideoPlayer.SetDirectAudioMute(i, Mute || !Application.isPlaying);
				else if (TranslatedVideoPlayer.VideoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
				{
					AudioSource audioSource = TranslatedVideoPlayer.VideoPlayer.GetTargetAudioSource(i);
					if (audioSource != null)
						audioSource.mute = Mute || !Application.isPlaying;
				}
			}

			TranslatedVideoPlayer.VideoPlayer.loopPointReached += LoopPointReached;
			TranslatedVideoPlayer.VideoPlayer.time = ClipInTime;
			TranslatedVideoPlayer.VideoPlayer.Prepare();
			TranslatedVideoPlayer.VideoPlayer.Stop();
		}

		void LoopPointReached(VideoPlayer vp)
		{
		}

		public override void PrepareFrame(Playable playable, FrameData info)
		{
			//Debug.LogErrorFormat("PrepareFrame Clip={0}", VideoClip.name);
			if (!IsDataValid)
				return;

			if (!Application.isPlaying)
				SyncVideoToPlayable(playable);
		}

		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			//Debug.LogErrorFormat("OnBehaviourPlay Clip={0}, Player={1}", VideoClip.name, TranslatedVideoPlayer ? TranslatedVideoPlayer.name : "null");
			PrepareVideo();

			if (!IsDataValid)
				return;
		}

		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			//Debug.LogErrorFormat("OnBehaviourPause Clip={0}", videoClip.name);
			if (!IsDataValid)
				return;

			PauseVideo();
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			//Debug.LogErrorFormat("ProcessFrame: Clip={0}, Weight={1}", VideoClip.name, info.weight);

			//Get reference to track data
			if (!TranslatedVideoPlayer)
			{
				//Setup translated audio source if available
				TranslatedVideoPlayer = playerData as TranslatedVideoPlayer;
				PrepareVideo();
			}

			if (!IsDataValid)
				return;
			
			TranslatedVideoPlayer.VideoPlayer.targetCameraAlpha = info.weight;

			if (info.weight > 0 && !TranslatedVideoPlayer.VideoPlayer.isPlaying)
			{
				//Debug.LogErrorFormat("Playing {0} on {1}", AudioClip.name, AudioSource.name);
				PlayVideo();
			}
		}

		public override void OnGraphStop(Playable playable)
		{
			//Debug.LogErrorFormat("OnGraphStop Clip={0}", VideoClip.name);
			if (!Application.isPlaying)
				StopVideo();
		}

		public override void OnPlayableDestroy(Playable playable)
		{
			//Debug.LogErrorFormat("OnPlayableDestroy Clip={0}", VideoClip.name);
			StopVideo();
		}

		public void PlayVideo()
		{
			//Debug.LogErrorFormat("PlayVideo Clip={0}", VideoClip.name);
			if (!IsDataValid)
				return;

			if(!ClipWasPlayed)
			{
				TranslatedVideoPlayer.Play(VideoClip, ResourcePath, Loop);
				ClipWasPlayed = true;
			}
			

			if (!Application.isPlaying)
				PauseVideo();
		}

		public void PauseVideo()
		{
			//Debug.LogErrorFormat("PauseVideo Clip={0}", VideoClip.name);
			if (!IsDataValid)
				return;

			TranslatedVideoPlayer.Pause();
		}

		public void StopVideo()
		{
			//Debug.LogErrorFormat("StopVideo Clip={0}", VideoClip.name);
			if (!IsDataValid)
				return;

			TranslatedVideoPlayer.Stop();
			ClipWasPlayed = false;
		}

		private void SyncVideoToPlayable(Playable playable)
		{
			//Debug.LogErrorFormat("SyncVideoToPlayable Clip={0}", VideoClip.name);
			if (!IsDataValid)
				return;

			TranslatedVideoPlayer.VideoPlayer.time = (ClipInTime + (playable.GetTime() * TranslatedVideoPlayer.VideoPlayer.playbackSpeed)) % TranslatedVideoPlayer.VideoPlayer.clip.length;
		}
	}
}