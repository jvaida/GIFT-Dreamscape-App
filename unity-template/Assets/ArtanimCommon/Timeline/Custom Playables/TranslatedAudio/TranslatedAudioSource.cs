using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/Translated Audio Source")]
	[RequireComponent(typeof(AudioSource))]
	[ExecuteInEditMode]
	public class TranslatedAudioSource : NetworkSyncedBehaviour
	{
		private AudioSource _AudioSource;
		public AudioSource AudioSource
		{
			get
			{
				if (!_AudioSource)
					_AudioSource = GetComponent<AudioSource>();
				return _AudioSource;
			}
		}

		private string CurrentSourceClipPath;

		#region Unity events

		private void OnEnable()
		{
			TextService.OnLanguageChanged += Instance_OnLanguageChanged;

			// Can't access NetworkInterface if not playing
			if (Application.isPlaying)
				NetworkInterface.Instance.Subscribe<PlayAudio>(OnPlayAudio);
		}

		private void OnDisable() 
		{
			TextService.OnLanguageChanged -= Instance_OnLanguageChanged;

			// Can't access NetworkInterface if not playing
			if (Application.isPlaying)
				NetworkInterface.SafeUnsubscribe<PlayAudio>(OnPlayAudio);
		}

		private void Instance_OnLanguageChanged(string language)
		{
			CurrentSourceClipPath = null;
		}

		#endregion

		#region Network events

		private void OnPlayAudio(PlayAudio args)
		{
			if(NeedTrigger(args.ObjectId))
			{
				switch (args.PlayMode)
				{
					case PlayAudio.EPlayMode.Play:
						Play(args.ClipResource, args.ClipResourcePath, loop: args.Loop, clipInTime: args.ClipInTime);
						break;
					case PlayAudio.EPlayMode.Stop:
						Stop();
						break;
					case PlayAudio.EPlayMode.Pause:
						Pause();
						break;
				}
			}
		}

		#endregion

		#region Public interface

		/// <summary>
		/// Plays the given audio clips corresponding language specific clip.
		/// </summary>
		/// <param name="audioClip">Audio clip to be translated</param>
		/// <param name="resourcePath">The path within the Unity resource folder to search for the language specific clip.</param>
		/// <param name="loop">Plays the audio clip looping</param>
		/// <param name="syncToSession">Sync the play event to all session compoents. If set to true, this method call only works in the server.</param>
		public void Play(AudioClip audioClip, string resourcePath = "", bool loop=false, bool syncToSession = false, float clipInTime =0f)
		{
			if(syncToSession)
			{
				if (NetworkInterface.Instance.IsServer && ValidateObjectId())
				{
					//Send sync event
					NetworkInterface.Instance.SendMessage(new PlayAudio
					{
						ObjectId = ObjectId,
						PlayMode = PlayAudio.EPlayMode.Play,
						ClipResource = audioClip.name,
						ClipResourcePath = resourcePath,
						Loop = loop,
                        ClipInTime = clipInTime,
					});
				}
			}
			else
			{
				//Play normal
				Debug.LogFormat("Playing audio clip: clip: {0}, resourcePath={1}, loop={2}, clipInTime={3}", audioClip.name, resourcePath, loop, clipInTime);
				PrepareAudioClip(audioClip, resourcePath, loop);
                AudioSource.time = clipInTime;
				AudioSource.Play();
			}
		}

		/// <summary>
		/// Plays the given audio clips corresponding language specific clip using the clips name.
		/// </summary>
		/// <param name="audioClipName">Audio clip name to be translated</param>
		/// <param name="resourcePath">The path within the Unity resource folder to search for the language specific clip.</param>
		/// <param name="loop">Plays the audio clip looping</param>
		/// <param name="syncToSession">Sync the play event to all session compoents. If set to true, this method call only works in the server.</param>
		public void Play(string audioClipName, string resourcePath = "", bool loop = false, bool syncToSession = false, float clipInTime = 0f)
		{
			if (syncToSession && NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				//Send sync event
				NetworkInterface.Instance.SendMessage(new PlayAudio
				{
					ObjectId = ObjectId,
					PlayMode = PlayAudio.EPlayMode.Play,
					ClipResource = audioClipName,
					ClipResourcePath = resourcePath,
					Loop = loop,
                    ClipInTime = clipInTime,
                });
			}
			else
			{
				//Play normal
				Debug.LogFormat("Playing audio clip: clip: {0}, resourcePath={1}, loop={2}, clipInTime={3}", audioClipName, resourcePath, loop, clipInTime);
				PrepareAudioClip(audioClipName, resourcePath, loop);
                AudioSource.time = clipInTime;
                AudioSource.Play();
			}
				
		}

		/// <summary>
		/// Prepares the given audio clip to be played.
		/// This will search the translated version of the given clip in the Unity resource folder and set it to the AudioSource.
		/// </summary>
		/// <param name="audioClip">Audio clip to be translated</param>
		/// <param name="resourcePath">The path within the Unity resource folder to search for the language specific clip.</param>
		/// <param name="loop">Plays the audio clip looping</param>
		public void PrepareAudioClip(AudioClip audioClip, string resourcePath = "", bool loop = false)
		{
			if (audioClip)
			{
				var clipPath = Path.Combine(resourcePath, audioClip.name);

				//Prepare a new one?
				if (clipPath != CurrentSourceClipPath)
				{
					AudioSource.clip = ResourceUtils.GetPlayerLanguageResource(audioClip.name, resourcePath, audioClip);
					AudioSource.loop = loop;
					CurrentSourceClipPath = clipPath;
				}
			}
			else
			{
				Debug.LogWarning("Failed to prepare null audio clip.");
			}
		}

		/// <summary>
		/// Prepares the given audio clip to be played using its name.
		/// This will search the translated version of the given clip in the Unity resource folder and set it to the AudioSource.
		/// </summary>
		/// <param name="audioClipName">Audio clip name to be translated</param>
		/// <param name="resourcePath">The path within the Unity resource folder to search for the language specific clip.</param>
		/// <param name="loop">Plays the audio clip looping</param>
		public void PrepareAudioClip(string audioClipName, string resourcePath = "", bool loop = false)
		{
			if (!string.IsNullOrEmpty(audioClipName))
			{
				var clipPath = Path.Combine(resourcePath, audioClipName.Trim());

				//Prepare a new one?
				if (clipPath != CurrentSourceClipPath)
				{
					AudioSource.clip = ResourceUtils.GetPlayerLanguageResource<AudioClip>(audioClipName, resourcePath);
					AudioSource.loop = loop;
					CurrentSourceClipPath = clipPath;
				}
			}
			else
			{
				Debug.LogWarning("Failed to prepare null audio clip.");
			}
		}

		/// <summary>
		/// Stops the currently playing audio clip.
		/// </summary>
		/// <param name="syncToSession">Sync the stop even to all session components. If set to true, this method call only has an effect in the server</param>
		public void Stop(bool syncToSession = false)
		{
			if (syncToSession && NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				//Send sync event
				NetworkInterface.Instance.SendMessage(new PlayAudio
				{
					ObjectId = ObjectId,
					PlayMode = PlayAudio.EPlayMode.Stop,
				});
			}
			else
			{
				//Just stop
				Debug.LogFormat("Stopping audio clip: clip: {0}", AudioSource.clip ? AudioSource.clip.name : "");
				AudioSource.Stop();
			}
		}

		/// <summary>
		/// Pauses the currently playing audio clip.
		/// </summary>
		/// <param name="syncToSession">Sync the stop even to all session components. If set to true, this method call only has an effect in the server</param>
		public void Pause(bool syncToSession = false)
		{
			if (syncToSession && NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				//Send sync event
				NetworkInterface.Instance.SendMessage(new PlayAudio
				{
					ObjectId = ObjectId,
					PlayMode = PlayAudio.EPlayMode.Stop,
				});
			}
			else
			{
				//Just pause
				Debug.LogFormat("Pausing audio clip: clip: {0}", AudioSource.clip ? AudioSource.clip.name : "");
				AudioSource.Pause();
			}
		}

		#endregion


	}
}