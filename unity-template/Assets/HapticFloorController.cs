using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Artanim.Utils;
using System.Collections.Generic;
using System.Collections;
using Dreamscape;


public class HapticFloorController : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(VibrateAll());   
    }

    private IEnumerator VibrateAll()
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

        //turn all panels on
        foreach (HapticAudio_main.AudioChannel ac in main.AudioChannelsConfiguration.AudioChannels)
        {
            Debug.Log("setting channels to zero gain" + ac.channelName_1 + ac.channelName_2);
            //stereo gain two per each panel
            ac.gain_1 = 1.0f;
            ac.gain_2 = 1.0f;
        }


    }
}
