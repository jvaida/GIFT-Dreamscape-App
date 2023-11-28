using Artanim.Haptics.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics
{
	/// <summary>
	/// Behaviour that plays an haptic audio effect
	/// You can also use a Timeline's Haptic Audio Track
	/// </summary>
	public class HapticAudioEffect : MonoBehaviour
	{
		[Tooltip("The generated sound to play, listed in generated_waves_config.xml")]
		public string WaveName;

		[Tooltip("The audio clip to play")]
		public AudioClip Clip;

		[Tooltip("Can be animated")]
		[Range(0, 10)]
		public float Volume = 1f;
		[Tooltip("Can be animated")]
		public bool Muted;

		[Tooltip("If left to 0 when playing a clip, it will stop when the clip is finished playing")]
		public float Duration;

        public bool LoopClip;

		[Tooltip("Whether or not the effect should continue playing if this behavior is destroyed before finishing playing")]
		public bool Persistent;

		[Tooltip("Whether or not the effect should play only for elements where there is a player")]
		public bool AlwaysPlay;

		[Tooltip("Use colliders to select which audio devices to target")]
		public AudioDeviceTarget Target;

		[Tooltip("Elements (audio devices) on which the effect shouldn't play")]
		public string[] MutedElements;

		HapticAudioPlayer _player;

		public void SetPlayerVolume(RuntimePlayer player, float volume)
		{
			if (HapticsController.Instance)
			{
				if (_player == null)
	            {
						Debug.LogWarning("Calling SetPlayerVolume on disabled HapticAudioEffect");
	            }
				else
	            {
					_player.SetPlayerVolume(player, volume);
				}
			}
		}

		void OnEnable()
		{
			if (HapticsController.Instance)
            {
				_player = HapticsController.Instance.CreateAudioPlayer(new HapticAudioPlayerSettings
				{
					WaveName = WaveName,
					Clip = Clip,
					Volume = Volume,
					IsMuted = Muted,
					Duration = Duration,
					LoopClip = LoopClip,
					IsPersistent = Persistent,
					AlwaysPlay = AlwaysPlay,
					Bounds = (Target && Target.HasCollider) ? Target.Bounds : new Bounds?(),
					MutedElements = MutedElements,
				});
			}
		}

		void OnDisable()
		{
			_player = null;
		}

		// Update is called once per frame
		void Update()
		{
			if (_player != null)
            {
				_player.Volume = Volume;
				_player.IsMuted = Muted;
				// Update colliders (for now they can only be changed before first update)
				if (_player.CanSetBounds && Target && Target.HasCollider)
				{
					_player.ChangedBounds(Target.Bounds);
				}
				_player.KeepAlive();
			}
		}
	}
}