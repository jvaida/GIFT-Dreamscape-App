using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Artanim.Utils;
using System.Collections.Generic;
using System.Collections;
using Dreamscape;



    public class HapticAudioFloorTest : MonoBehaviour
    {
        [Range(0.1f, 10f)]
        public float DurationPerTile = 5f;
        [Range(0f, 1f)]
        public float AudioGain = 1f;

        void Start()
        {
            StartCoroutine(RunPanelTest());
        }

        private IEnumerator RunPanelTest()
        {
            HapticAudio_main main = null;

            while (!main)
            {
                main = HapticAudio_main.Instance;
                yield return new WaitForEndOfFrame();
            }   
            FloorBlinker blinker = FindObjectOfType<FloorBlinker>();

            if (blinker)
                blinker.BlinkFloor();
            foreach (HapticAudio_main.AudioChannel ac in main.AudioChannelsConfiguration.AudioChannels)
            {
                Debug.Log("setting channels to zero gain " + ac.channelName_1 + ac.channelName_2);
                ac.gain_1 = 0.0f;
                ac.gain_2 = 0.0f;
            }
            while (true)
            {
                foreach (HapticAudio_main.AudioChannel ac in main.AudioChannelsConfiguration.AudioChannels)
                {
                    ac.gain_1 = AudioGain;
                    yield return new WaitForSeconds(DurationPerTile);
                    ac.gain_1 = 0.0f;
                    if (blinker)
                        blinker.BlinkNextFloor();
                    ac.gain_2 = AudioGain;
                    yield return new WaitForSeconds(DurationPerTile);
                    ac.gain_2 = 0.0f;
                    if (blinker)
                        blinker.BlinkNextFloor();
                }
            }
        }
    }

