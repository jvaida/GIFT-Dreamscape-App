using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    [RequireComponent(typeof(AudioSource))]
    public class MicAudioSource : MonoBehaviour
    {
        private bool Initialized = false;

        private AudioSource AudioSource
        {
            get { return GetComponent<AudioSource>(); }
        }

        void Start()
        {
            if (Microphone.devices.Length > 0)
                InitMicSource();
        }

        void Update()
        {
            if (Time.frameCount % 100 == 0)
            {
                var hasMicrophone = Microphone.devices.Length > 0;
                if (!Initialized && hasMicrophone)
                    InitMicSource();
                else if (Initialized && !hasMicrophone)
                    StopMicrophone();
            }
        }

        private void InitMicSource()
        {
            AudioSource.clip = Microphone.Start(null, true, 3, 22050);
            AudioSource.loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }
            AudioSource.Play();
            Initialized = true;
            Debug.Log("Microphone audio source started");
        }

        private void StopMicrophone()
        {
            Microphone.End(null);
            AudioSource.Stop();
            AudioSource.clip = null;
            Initialized = false;
            Debug.Log("Microphone audio source stopped");
        }
    }
}