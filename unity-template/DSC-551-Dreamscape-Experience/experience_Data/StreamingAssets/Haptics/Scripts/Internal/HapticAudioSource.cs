using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
    [RequireComponent(typeof(AudioSource))]
    public class HapticAudioSource : MonoBehaviour
    {
        float[] _buffer;
        HapticAudioPlayer.SendSoundBufferHandler _pushSoundData;

        AudioSource _audioSource;
        bool _hasAudioMixedGroup;
        int _sampleRate;

        public bool IsPlaying
        {
            get { return _audioSource && _audioSource.isPlaying; }
        }

        public static HapticAudioSource AddToGameObject(GameObject gameObject, AudioClip clip, bool loop, HapticAudioPlayer.SendSoundBufferHandler sendSoundBuffer)
        {
            if (gameObject == null)
            {
                throw new System.ArgumentNullException("gameObject");
            }
            if (gameObject.GetComponent<AudioSource>() != null)
            {
                throw new System.InvalidOperationException("AudioSource already present on " + gameObject.name);
            }
            if (gameObject.GetComponent<HapticAudioSource>() != null)
            {
                throw new System.InvalidOperationException("HapticAudioSource already present on " + gameObject.name);
            }

            if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=magenta>HapticAudioSource: initializing for clip {0}</color>", clip ? clip.name : "<none>");

            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = loop;

            var hapticAudioSource = gameObject.AddComponent<HapticAudioSource>();
            hapticAudioSource._pushSoundData = sendSoundBuffer;
            return hapticAudioSource;
        }

        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 0; // 2D
            _audioSource.bypassEffects = false;
            _audioSource.bypassListenerEffects = true;
            _audioSource.bypassReverbZones = true;
            _hasAudioMixedGroup = _audioSource.outputAudioMixerGroup != null;

            if (_audioSource.clip == null)
            {
                Debug.LogError("HapticAudioSource: no clip assigned to haptic audio source: " + name);
                enabled = false;
            }
            else
            {
                _sampleRate = _audioSource.clip.frequency;

                if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=magenta>HapticAudioSource: playing clip {0} with rate of {1}</color>", _audioSource.clip.name, _sampleRate);
                _audioSource.Play();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_pushSoundData != null)
            {
                var bufferToSend = data;

                if (channels != 1)
                {
                    // Allocate buffer if needed
                    if ((_buffer == null) || (_buffer.Length != (data.Length / channels)))
                    {
                        _buffer = new float[data.Length / channels];
                    }

                    // Copy first channel only
                    for (int i = 0, j = 0; i < data.Length; i += channels, ++j)
                    {
                        _buffer[j] = data[i];
                    }

                    bufferToSend = _buffer;
                }

                _pushSoundData(bufferToSend, _sampleRate);
            }

            // Mute audio data if no mixer attached, so it doesn't play in the audio pipeline
            if (!_hasAudioMixedGroup)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = 0;
                }
            }
        }
    }
}
