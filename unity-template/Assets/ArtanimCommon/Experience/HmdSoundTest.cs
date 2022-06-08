using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    [RequireComponent(typeof(AudioSource))]
    public class HmdSoundTest : MonoBehaviour
    {
        // Ambulance frequencies
        readonly float _leftFrequency = 700;
        readonly float _rightFrequency = 1000;

        // Push volume a bit
        readonly float _volume = 1.2f;

        // Pulse period
        readonly float _pulsePeriod = 2;

        // Sound wave settings
        readonly int _sampleRate = 44100;
        AudioSource _audioSource;
        float _sampleTime;

        // Some cached values
        float _tInc, _halfPulsePeriod;

        void Start()
        {
            _tInc = 1f / _sampleRate;
            _halfPulsePeriod = 0.5f * _pulsePeriod;
        }

        void OnEnable()
        {
            _sampleTime = 0;

            _audioSource = GetComponent<AudioSource>();
            _audioSource.Play();
        }

        void OnDisable()
        {
            _audioSource.Stop();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                bool left = _sampleTime < _halfPulsePeriod;
                data[i] = left ? _volume * Sine(_leftFrequency, _sampleTime) : 0f;
                if (channels == 2)
                {
                    data[i + 1] = left ? 0 : _volume * Sine(_rightFrequency, _sampleTime);
                }

                _sampleTime = (_sampleTime + _tInc) % _pulsePeriod;
            }
        }

        static float Sine(float frequency, float time)
        {
            return Mathf.Sin(2 * Mathf.PI * frequency * time);
        }
    }
}
