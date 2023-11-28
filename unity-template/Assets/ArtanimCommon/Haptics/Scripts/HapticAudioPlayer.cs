using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
	/// <summary>
	/// Parameters to create an HapticAudioPlayer
	/// </summary>
	public struct HapticAudioPlayerSettings
    {
		public string WaveName;
		public AudioClip Clip;
		public float Volume;
		public bool IsMuted;
		public float Duration;
		public bool LoopClip;
		public bool IsPersistent;
		public bool AlwaysPlay;
		public Bounds? Bounds;
		public string[] MutedElements;

		public string DisplayName
        {
			get { return Clip != null ? Clip.name : (WaveName != null ? WaveName : string.Empty); }
		}
	}

	/// <summary>
	/// Plays a sound clip or a generated wave to the haptic elements
	/// </summary>
	public class HapticAudioPlayer : MonoBehaviour
	{
		HapticAudioPlayerSettings _settings;
		int _destroyOnFrame;

		object _soundLock = new object();
		Location.AudioHaptics.Sound _sound;

		HapticAudioSource _hapticAudioSource;
		float _endOfLife;

		public delegate void SendSoundBufferHandler(float[] data, int sampleRate);

		#region Public properties

		/// <summary>
		/// Sound volume (can be animated)
		/// </summary>
		public float Volume
        {
			get { return _settings.Volume; }
			set { if (_settings.Volume != value) UpdateVolume(value); }
        }

		/// <summary>
		/// Whether or not to mute the sound (can be animated)
		/// </summary>
		public bool IsMuted
		{
			get { return _settings.IsMuted; }
			set { if (_settings.IsMuted != value) UpdateIsMuted(value); }
		}

		/// <summary>
		/// Whether or not it will auto-destroy if KeepAlive() wasn't called on the previous frame
		/// </summary>
		public bool IsPersistent
		{
			get { return _settings.IsPersistent; }
        }

		/// <summary>
		/// Whether or not bounds can be set (it's not possible once the clip has started playing)
		/// </summary>
		public bool CanSetBounds
        {
			get { return _sound == null; } // See SetBounds
        }

		#endregion

		#region Public methods

		/// <summary>
		/// Instantiate the script with the given settings on the specified game object
		/// </summary>
		/// <param name="gameObject">Game object on which to instantiate this script</param>
		/// <param name="settings">Player settings</param>
		/// <returns>The script instance</returns>
		public static HapticAudioPlayer AddToGameObject(GameObject gameObject, HapticAudioPlayerSettings settings)
        {
			if (gameObject == null)
			{
				throw new System.ArgumentNullException("gameObject");
			}
			if (gameObject.GetComponent<HapticAudioPlayer>() != null)
			{
				throw new System.InvalidOperationException("AudioPlayer already present on " + gameObject.name);
			}

			if (ConfigService.VerboseSdkLog) Debug.LogFormat(
				"<color=magenta>AudioPlayer ({0}): initializing with WaveName={1}, Clip={2}, Volume={3}, IsMuted={4}, Duration={5}, LoopClip={6}, IsPersistent={7}, AlwaysPlay={8}, Bounds={9}, DisabledElements={10}</color>",
				settings.DisplayName, settings.WaveName, settings.Clip, settings.Volume, settings.IsMuted, settings.Duration, settings.LoopClip, settings.IsPersistent,
				settings.AlwaysPlay, settings.Bounds.HasValue ? settings.Bounds.Value.ToString() : "<none>",
				(settings.MutedElements != null && settings.MutedElements.Length > 0) ? string.Join(", ", settings.MutedElements.Select(e => e.ToString()).ToArray()) : "<none>");

			var audioPlayer = gameObject.AddComponent<HapticAudioPlayer>();
			audioPlayer._settings = settings;
			if (settings.Duration > 0)
            {
				audioPlayer._endOfLife = Time.time + settings.Duration;
			}

			if (!settings.IsPersistent)
			{
				audioPlayer.KeepAlive();
			}

			// Create source to feed sound
			if (string.IsNullOrEmpty(settings.WaveName))
			{
				audioPlayer._hapticAudioSource = HapticAudioSource.AddToGameObject(gameObject, settings.Clip, settings.LoopClip, audioPlayer.PushAudioBuffer);
			}
			else
			{
				WaveGenerator.AddToGameObject(gameObject, settings.WaveName, audioPlayer.PushAudioBuffer);
			}

			return audioPlayer;
		}

		/// <summary>
		/// Specify bounds to limit on which elements the sound will be played
		/// </summary>
		/// <param name="bounds">Bounds in physical coordinates (pod)</param>
		/// <remarks> Not dynamic at the moment, it can only be set before the sound is first played
		/// but the native library could be changed to allow updating which haptics elements are active
		/// </remarks>
		public void ChangedBounds(Bounds bounds)
        {
			if (_settings.Bounds != bounds)
            {
				if (ConfigService.VerboseSdkLog) Debug.LogFormat(
					"<color=magenta>AudioPlayer ({0}): bounds={1}{2}</color>", _settings.DisplayName, bounds, _sound != null ? "" : " (sound not yet created)");

				_settings.Bounds = bounds;
			}
		}

		/// <summary>
		/// Change the sound volume for a given player
		/// The player's volume is multiplied with the sound volume
		/// </summary>
		/// <param name="player">RuntimePlayer for which to change the volume</param>
		/// <param name="volume">New volume</param>
		/// <remarks>This works only if sound is played on all elements (this a restriction on the native library)</remarks>
		public void SetPlayerVolume(RuntimePlayer player, float volume)
		{
			if (player == null)
            {
				throw new System.ArgumentNullException("player");
            }

			var ctrl = HapticsController.AudioDevicesController;
			if (ctrl != null)
			{
				var hapticPlayer = ctrl.GetHapticPlayer(player);
				if (hapticPlayer != null)
                {
					if (_sound == null)
                    {
						CreateSound();
					}
					_sound.SetPlayerGain(hapticPlayer, volume);
				}
				else
				{
					Debug.LogErrorFormat("Can't set player haptic volume because haptic player was not found . PlayerId={0}", player.Player.ComponentId);
				}
			}
			else
			{
				Debug.LogErrorFormat("Can't set player haptic volume because AudioDevicesController is either missing or disabled. PlayerId={0}", player.Player.ComponentId);
			}
		}

		/// <summary>
		/// If the sound is not persistent, call this method every frame otherwise the script will self-destroy
		/// </summary>
		public void KeepAlive()
		{
			_destroyOnFrame = Time.frameCount + 1;
		}

		#endregion

		#region Internals

		void UpdateVolume(float volume)
        {
			_settings.Volume = volume;
			if (_sound != null)
			{
				_sound.Gain = volume;
			}
		}

		void UpdateIsMuted(bool isMuted)
        {
			if (ConfigService.VerboseSdkLog) Debug.LogFormat(
				"<color=magenta>AudioPlayer ({0}): muted={1}{2}</color>", _settings.DisplayName, isMuted, _sound != null ? "" : " (sound not yet created)");

			_settings.IsMuted = isMuted;
			if (_sound != null)
			{
				_sound.IsMuted = isMuted;
			}
        }

		void PushAudioBuffer(float[] data, int sampleRate)
		{
			lock (_soundLock)
            {
				if (_sound != null)
				{
					_sound.PushBuffer(data, 1, sampleRate);
				}
			}
		}

		void CreateSound()
        {
			var ctrl = HapticsController.AudioDevicesController;
			if (ctrl != null)
			{
				lock (_soundLock)
				{
					// Get active elements
					bool hasMutedElements = (_settings.MutedElements != null) && (_settings.MutedElements.Length > 0);
					IEnumerable<Location.AudioHaptics.Element> elements = !hasMutedElements ? null :
						ctrl.AllElements.Where(e => !_settings.MutedElements.Contains(e.Name));

					// Note: we can't have both elements and players
					_sound = _settings.AlwaysPlay && !hasMutedElements && !_settings.Bounds.HasValue
						? ctrl.CreateGlobalSound(_settings.DisplayName, ignoreMuteWhenUnoccupiedFlag: true)
						: ctrl.CreateSound(_settings.DisplayName, elements, _settings.Bounds, (elements != null) || _settings.Bounds.HasValue ? null : ctrl.AllPlayers, _settings.AlwaysPlay);
					_sound.Gain = _settings.Volume;
					_sound.IsMuted = _settings.IsMuted;
				}
			}
			else
			{
				Debug.LogError("Can't create haptic sound because AudioDevicesController is either missing or disabled");
			}
		}

        #endregion

        #region Unity messages

        void LateUpdate()
		{
			if (_sound == null)
            {
				// For now we create the sounds as late as possible because the bounds can't be updated after creation
				CreateSound();
			}

			bool clipFinishedPlaying = _hapticAudioSource && (!_hapticAudioSource.IsPlaying);
			bool destroyThisFrame = (!_settings.IsPersistent) && (_destroyOnFrame > 0) && (Time.frameCount >= _destroyOnFrame);
			bool expired = (_endOfLife > 0) && (_endOfLife < Time.time);
			if (clipFinishedPlaying || destroyThisFrame || expired)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat(
					"<color=magenta>AudioPlayer ({0}): auto destroying, clipFinishedPlaying={1}, destroyThisFrame={2}, expired={3}</color>",
					_settings.DisplayName, clipFinishedPlaying, destroyThisFrame, expired);
				Object.Destroy(gameObject);
			}
		}

		void OnDisable()
        {
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=magenta>AudioPlayer ({0}): disabling</color>", _settings.DisplayName);

			lock (_soundLock)
			{
				if (_sound != null)
				{
					_sound.Dispose();
					_sound = null;
				}
			}
		}

        #endregion
    }
}