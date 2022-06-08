using UnityEngine;
using System.Collections;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;

namespace Artanim
{
	/// <summary>
	/// Network synced AudioSource.
	/// AudioClips can be played from indexed array or directly by passing a resource path.
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	[AddComponentMenu("Artanim/Network Sound")]
	public class NetworkSound : MonoBehaviour
	{
        [ObjectId]
		[Tooltip("ID this behavior reacts to.")]
		public string ObjectId;

		[Tooltip("Enable if the sound should also be played on the server side. Server side audio is muted by default in the config.")]
		public bool MuteOnServer = true;

		[Tooltip("List of audio clips which can be played by index.")]
		public AudioClip[] AudioClips;

		[Header("Auto play")]
		[Tooltip("Specify if the audio clips defined in AutoPlayIndex should be played at startup.")]
		public bool PlayOnAwake = false;

		[Tooltip("Index of the audio clip to be played at startup. This value only has an effect if PlayOnAwake is set.")]
		public int AutoPlayIndex = 0;

		[Tooltip("Specify if the audio clip played at startup should loop. This value only has an effect if PlayOnAwake is set.")]
		public bool AutoPlayLoop = false;

		[Header("Volume fading")]
		[Tooltip("Default fading time. 0 means no fading.")]
		public float DefaultFadeTime = 0f;

		private AudioSource AudioSource;

		private float DefaultVolume;

		private float FadeTime;
		private float CurrentFadeTime;
		private float FadeStartVolume;
		private float TargetVolume;

		void Start()
		{
			AudioSource = GetComponent<AudioSource>();
			DefaultVolume = AudioSource.volume;
			TargetVolume = DefaultVolume;

			//Setup audio source
			AudioSource.playOnAwake = false;
			AudioSource.loop = false;

			if(NetworkInterface.Instance.IsServer && PlayOnAwake)
			{
				//Delay the play message to allow clients to startup.
				Invoke("DelayedPlayOnAwake", 1f);
			}
		}

		void Update()
		{
			if(AudioSource.volume != TargetVolume)
			{
				CurrentFadeTime += Time.deltaTime;
				AudioSource.volume = Mathf.Lerp(FadeStartVolume, TargetVolume, CurrentFadeTime / FadeTime);

				//Stop
				if(AudioSource.volume == 0f)
				{
					AudioSource.Stop();
					AudioSource.loop = false;
				}
			}
		}

		void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<PlayAudio>(NetworkMessage_PlayAudio);
		}

		void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<PlayAudio>(NetworkMessage_PlayAudio);
		}

		#region Public interface

		/// <summary>
		/// Plays the clip at clipIndex once.
		/// </summary>
		/// <param name="clipIndex"></param>
		public void PlayOnce(int clipIndex)
		{
			SendPlayEvent(clipIndex, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="clipIndex"></param>
		/// <param name="fadeTime"></param>
		public void PlayOnce(int clipIndex, float fadeTime = 0f)
		{
			SendPlayEvent(clipIndex, false, fadeTime);
		}

		/// <summary>
		/// Plays the given audioResource from the resouce folder once.
		/// </summary>
		/// <param name="audioResource"></param>
		public void PlayOnce(string audioResource)
		{
			SendPlayEvent(audioResource, false);
		}

		/// <summary>
		/// Plays the clip at clipIndex looping. Call Stop() to stop looping sounds.
		/// </summary>
		/// <param name="clipIndex"></param>
		public void PlayLoop(int clipIndex)
		{
			SendPlayEvent(clipIndex, true);
		}

		/// <summary>
		/// Plays the given audioResource from the resource folder looping. Call Stop() to stop looping sounds.
		/// </summary>
		/// <param name="audioResource"></param>
		public void PlayLoop(string audioResource)
		{
			SendPlayEvent(audioResource, true);
		}

		/// <summary>
		/// Stops the current playback.
		/// </summary>
		public void Stop()
		{
			SendStopEvent();
		}

		/// <summary>
		/// Stops the current playback fading out.
		/// </summary>
		/// <param name="fadeTime"></param>
		public void Stop(float fadeTime)
		{
			SendStopEvent(fadeTime);
		}

		#endregion

		#region Network events

		private void NetworkMessage_PlayAudio(PlayAudio args)
		{
			if (args.ObjectId == ObjectId)
			{
				switch (args.PlayMode)
				{
					case PlayAudio.EPlayMode.Play:
						InternalPlay(args);
						break;
					case PlayAudio.EPlayMode.Stop:
						InternalStop(args);
						break;
				}
			}
		}

		#endregion

		#region Internals

		private void DelayedPlayOnAwake()
		{
			if(PlayOnAwake)
			{
				SendPlayEvent(AutoPlayIndex, AutoPlayLoop);
			}
		}

		private void SendPlayEvent(int clipIndex, bool loop, float fadeTime = 0f)
		{
			if(NetworkInterface.Instance.IsServer && GameController.Instance.CurrentSession != null)
			{
				if (AudioClips != null && clipIndex < AudioClips.Length && AudioClips[clipIndex])
				{
					if(!string.IsNullOrEmpty(ObjectId))
					{
						NetworkInterface.Instance.SendMessage(new PlayAudio
						{
							PlayMode = PlayAudio.EPlayMode.Play,
							ObjectId = ObjectId,
							Loop = loop,
							ClipIndex = clipIndex,
							FadeTime = fadeTime,
						});
					}
					else
					{
						Debug.LogWarningFormat("Cannot play audio with index={0} on NetworkSound ObjectId={1}. No ObjectId set.", clipIndex, ObjectId);
					}
				}
				else
				{
					Debug.LogWarningFormat("Cannot play audio with index={0} on NetworkSound ObjectId={1}. The given index is not in the array.", clipIndex, ObjectId);
				}
			}
			else
			{
				Debug.LogWarningFormat("Cannot play audio with index={0} on NetworkSound ObjectId={1}. I'm not a server and the ServerOnlyTrigger is set.", clipIndex, ObjectId);
			}
		}

		private void SendPlayEvent(string audioResource, bool loop)
		{
			if (NetworkInterface.Instance.IsServer)
			{
				if (!string.IsNullOrEmpty(audioResource))
				{
					if(!string.IsNullOrEmpty(ObjectId))
					{
						NetworkInterface.Instance.SendMessage(new PlayAudio
						{
							PlayMode = PlayAudio.EPlayMode.Play,
							ObjectId = ObjectId,
							Loop = loop,
							ClipResource = audioResource,
						});
					}
					else
					{
						Debug.LogWarningFormat("Cannot play audio with resource={0} on NetworkSound ObjectId={1}. No ObjectId set.", audioResource, ObjectId);
					}
				}
				else
				{
					Debug.LogWarningFormat("Cannot play audio with resource={0} on NetworkSound ObjectId={1}. The given resource is empty.", audioResource, ObjectId);
				}
			}
			else
			{
				Debug.LogWarningFormat("Cannot play audio with resource={0} on NetworkSound ObjectId={1}. I'm not a server and the ServerOnlyTrigger is set.", audioResource, ObjectId);
			}
		}

		private void SendStopEvent(float fadeTime = 0f)
		{
			if (NetworkInterface.Instance.IsServer)
			{
				NetworkInterface.Instance.SendMessage(new PlayAudio
				{
					ObjectId = ObjectId,
					PlayMode = PlayAudio.EPlayMode.Stop,
					FadeTime = fadeTime,
				});
			}
			else
			{
				Debug.LogWarningFormat("Cannot stop audio on NetworkSound ObjectId={0}. I'm not a server and the ServerOnlyTrigger is set.", ObjectId);
			}
		}

		private void InternalPlay(PlayAudio args)
		{
			if (NetworkInterface.Instance.IsClient || !MuteOnServer)
			{
				//Play sound
				if (!string.IsNullOrEmpty(args.ClipResource))
				{
					//Play from resource
					var clip = ResourceUtils.LoadResources<AudioClip>(args.ClipResource);

					if (clip)
					{
						Debug.LogFormat("NetworkSound play from resource: ObjectId={0}, clip={1}, loop={2}, fadeTime={3}", ObjectId, clip.name, args.Loop, args.FadeTime);
						if (args.Loop)
						{
							AudioSource.loop = true;
							AudioSource.clip = clip;
							AudioSource.Play();
						}
						else
						{
							AudioSource.loop = false;
							AudioSource.PlayOneShot(clip);
						}
					}
				}
				else if (args.ClipIndex > -1 && AudioClips != null && args.ClipIndex < AudioClips.Length)
				{
					//Play from index
					var clip = AudioClips[args.ClipIndex];
					Debug.LogFormat("NetworkSound play from index: ObjectId={0}, clip={1}, loop={2}, fadeTime={3}", ObjectId, clip.name, args.Loop, args.FadeTime);
					if (args.Loop)
					{
						AudioSource.loop = true;
						AudioSource.clip = clip;
						AudioSource.Play();
					}
					else
					{
						AudioSource.loop = true;
						AudioSource.PlayOneShot(clip);
					}
				}

				//Fading
				if (args.FadeTime != 0f || DefaultFadeTime != 0)
				{
					AudioSource.volume = 0f;
					FadeStartVolume = AudioSource.volume;
					FadeTime = args.FadeTime != 0f ? args.FadeTime : DefaultFadeTime;
					CurrentFadeTime = 0f;
					TargetVolume = DefaultVolume;
				}
				else
				{
					AudioSource.volume = DefaultVolume;
					TargetVolume = DefaultVolume;
				}
			}
		}

		private void InternalStop(PlayAudio args)
		{			
			Debug.LogFormat("NetworkSound stop: ObjectId={0}", ObjectId);

			//Fading
			if (args.FadeTime != 0f || DefaultFadeTime != 0f)
			{
				FadeTime = args.FadeTime != 0f ? args.FadeTime : DefaultFadeTime;
				CurrentFadeTime = 0f;
				FadeStartVolume = AudioSource.volume;
				TargetVolume = 0f;
			}
			else
			{
				AudioSource.Stop();
				AudioSource.loop = false;
			}
		}
		
		#endregion

	}

}