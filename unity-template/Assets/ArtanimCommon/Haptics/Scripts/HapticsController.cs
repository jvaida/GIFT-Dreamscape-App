using Artanim.Haptics.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics
{
    public class HapticsController : SingletonBehaviour<HapticsController>
    {
        static DmxDevicesController _dmxCtrl;
        static AudioDevicesController _audioCtrl;

        /// <summary>
        /// Whether or not DMX is enabled
        /// </summary>
        public static bool IsDmxEnabled
        {
            get { return _dmxCtrl && _dmxCtrl.enabled; }
        }

        /// <summary>
        /// Returns the DmxDevicesController instance if created
        /// </summary>
        public static DmxDevicesController DmxDevicesController
        {
            get { return IsDmxEnabled ? _dmxCtrl : null; }
        }

        /// <summary>
        /// Whether or not audio is enabled
        /// </summary>
        public static bool IsAudioEnabled
        {
            get { return _audioCtrl && _audioCtrl.enabled; }
        }

        /// <summary>
        /// Returns the AudioDevicesController instance if created
        /// </summary>
        public static AudioDevicesController AudioDevicesController
        {
            get { return IsAudioEnabled ? _audioCtrl : null; }
        }

        /// <summary>
        /// Instantiate an AudioPlayer under this game object using the given settings
        /// </summary>
        /// <param name="settings">The AudioPlayer settings</param>
        /// <returns>An AudioPlayer instance if audio is enabled, otherwise null</returns>
        public HapticAudioPlayer CreateAudioPlayer(HapticAudioPlayerSettings settings)
        {
            if (IsAudioEnabled)
            {
                var go = new GameObject("AudioPlayer - " + settings.DisplayName);
                go.transform.parent = transform;
                return HapticAudioPlayer.AddToGameObject(go, settings);
            }
            return null;
        }

        /// <summary>
        /// Stop all haptic effects parented to this game object
        /// </summary>
        public void StopAllEffects()
        {
            if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=magenta>HapticsController: stopping all audio effects (found {0})</color>", transform.childCount);
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        void OnEnable()
        {
            if (ConfigService.VerboseSdkLog) Debug.Log("<color=magenta>HapticsController: enabled</color>");
            GameController.Instance.OnLeftSession += Instance_OnLeftSession;

            _dmxCtrl = GetComponent<DmxDevicesController>();
            _audioCtrl = GetComponent<AudioDevicesController>();
        }

        void OnDisable()
        {
            StopAllEffects();

            _dmxCtrl = null;
            _audioCtrl = null;

            if (ConfigService.VerboseSdkLog) Debug.Log("<color=magenta>HapticsController: disabled</color>");
            var gameController = GameController.Instance;
            if (gameController)
            {
                gameController.OnLeftSession -= Instance_OnLeftSession;
            }
        }

        void Instance_OnLeftSession()
        {
            StopAllEffects();
        }
    }
}
