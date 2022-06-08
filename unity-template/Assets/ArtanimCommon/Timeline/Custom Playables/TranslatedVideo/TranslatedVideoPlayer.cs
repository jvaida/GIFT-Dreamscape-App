using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Artanim
{
	[AddComponentMenu("Artanim/Translated Video Player")]
	[RequireComponent(typeof(VideoPlayer))]
	[ExecuteInEditMode]
	public class TranslatedVideoPlayer : NetworkSyncedBehaviour
	{
		private VideoPlayer _VideoPlayer;
		public VideoPlayer VideoPlayer
		{
			get
			{
				if (!_VideoPlayer)
					_VideoPlayer = GetComponent<VideoPlayer>();
				return _VideoPlayer;
			}
		}

		private string CurrentSourceClipPath;

		#region Unity events

		private void OnEnable()
		{
			TextService.OnLanguageChanged += Instance_OnLanguageChanged;

			// Can't access NetworkInterface if not playing
			if (Application.isPlaying)
				NetworkInterface.Instance.Subscribe<PlayVideo>(OnPlayVideo);
		}

		private void OnDisable()
		{
			TextService.OnLanguageChanged -= Instance_OnLanguageChanged;

			// Can't access NetworkInterface if not playing
			if(Application.isPlaying)
				NetworkInterface.SafeUnsubscribe<PlayVideo>(OnPlayVideo);
		}

		private void Instance_OnLanguageChanged(string language)
		{
			CurrentSourceClipPath = null;
		}

		#endregion

		#region Network events

		private void OnPlayVideo(PlayVideo args)
		{
			if (NeedTrigger(args.ObjectId))
			{
				switch (args.PlayMode)
				{
					case PlayVideo.EPlayMode.Play:
						Play(args.ClipResource, args.ClipResourcePath, loop: args.Loop);
						break;
					case PlayVideo.EPlayMode.Stop:
						Stop();
						break;
					case PlayVideo.EPlayMode.Pause:
						Pause();
						break;
				}
			}
		}

		#endregion

		#region Public interface

		/// <summary>
		/// 
		/// </summary>
		/// <param name="videoClip"></param>
		/// <param name="resourcePath"></param>
		/// <param name="loop"></param>
		public void Play(VideoClip videoClip, string resourcePath = "", bool loop = false, bool syncToSession = false)
		{
			if (syncToSession)
			{
				if (NetworkInterface.Instance.IsServer && ValidateObjectId())
				{
					//Send sync event
					NetworkInterface.Instance.SendMessage(new PlayVideo
					{
						ObjectId = ObjectId,
						PlayMode = PlayVideo.EPlayMode.Play,
						ClipResource = videoClip.name,
						ClipResourcePath = resourcePath,
						Loop = loop,
					});
				}
			}
			else
			{
				//Play normal
				Debug.LogFormat("Playing video clip: clip: {0}, resourcePath={1}, loop={2}", videoClip.name, resourcePath, loop);
				PrepareVideoClip(videoClip, resourcePath, loop);
				VideoPlayer.Play();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="videoClipName"></param>
		/// <param name="resourcePath"></param>
		/// <param name="loop"></param>
		/// <param name="syncToSession"></param>
		public void Play(string videoClipName, string resourcePath = "", bool loop = false, bool syncToSession = false)
		{
			if(syncToSession)
			{
				if(NetworkInterface.Instance.IsServer && ValidateObjectId())
				{
					//Send sync event
					NetworkInterface.Instance.SendMessage(new PlayVideo
					{
						ObjectId = ObjectId,
						PlayMode = PlayVideo.EPlayMode.Play,
						ClipResource = videoClipName,
						ClipResourcePath = resourcePath,
						Loop = loop,
					});
				}
			}
			else
			{
				//Play normal
				Debug.LogFormat("Playing video clip: clip: {0}, resourcePath={1}, loop={2}", videoClipName, resourcePath, loop);
				PrepareVideoClip(videoClipName, resourcePath, loop);
				VideoPlayer.Play();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop(bool syncToSession = false)
		{
			if(syncToSession && NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				NetworkInterface.Instance.SendMessage(new PlayVideo
				{
					ObjectId = ObjectId,
					PlayMode = PlayVideo.EPlayMode.Stop,
				});
			}
			else
			{
				Debug.LogFormat("Stopping video clip: clip: {0}", VideoPlayer.clip ? VideoPlayer.clip.name : "");
				VideoPlayer.Stop();
				CurrentSourceClipPath = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Pause(bool syncToSession = false)
		{
			if (syncToSession && NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				NetworkInterface.Instance.SendMessage(new PlayVideo
				{
					ObjectId = ObjectId,
					PlayMode = PlayVideo.EPlayMode.Pause,
				});
			}
			else
			{
				Debug.LogFormat("Pausing video clip: clip: {0}", VideoPlayer.clip ? VideoPlayer.clip.name : "");
				VideoPlayer.Pause();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="videoClip"></param>
		/// <param name="resourcePath"></param>
		/// <param name="loop"></param>
		public void PrepareVideoClip(VideoClip videoClip, string resourcePath = "", bool loop=false)
		{
			if (videoClip)
			{
				var clipPath = Path.Combine(resourcePath, videoClip.name);

				Debug.LogFormat("Preparing video clip: current={0}, new={1}", CurrentSourceClipPath, clipPath);

				//Prepare a new one?
				if (clipPath != CurrentSourceClipPath)
				{
					VideoPlayer.clip = ResourceUtils.GetPlayerLanguageResource(videoClip.name, resourcePath, videoClip);
					VideoPlayer.isLooping = loop;

					CurrentSourceClipPath = clipPath;

					//Debug.LogErrorFormat("Changed clip to {0}", VideoPlayer.clip.name);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="videoClip"></param>
		/// <param name="resourcePath"></param>
		/// <param name="loop"></param>
		public void PrepareVideoClip(string videoClipName, string resourcePath = "", bool loop = false)
		{
			if (!string.IsNullOrEmpty(videoClipName))
			{
				var clipPath = Path.Combine(resourcePath, videoClipName.Trim());

				//Prepare a new one?
				if (clipPath != CurrentSourceClipPath)
				{
					VideoPlayer.clip = ResourceUtils.GetPlayerLanguageResource<VideoClip>(videoClipName, resourcePath);
					VideoPlayer.isLooping = loop;

					CurrentSourceClipPath = clipPath;

					//Debug.LogErrorFormat("Changed clip to {0}", VideoPlayer.clip.name);
				}
			}
		}

		#endregion
	}
}