using Artanim.Haptics.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
    public class TestHapticsTriggers : MonoBehaviour
    {
        const string _waveName = "ProductionTest";

        public float SingleFanTestDuration = 4;
        public float SingleAudioTestDuration = 4;

        public void TestDmxDevices()
        {
            StartCoroutine(TestDmxDevicesAsync());
        }

        public void TestAudioDevices()
        {
            StartCoroutine(TestAudioDevicesAsync());
        }

        IEnumerator TestDmxDevicesAsync()
        {
            if (HapticsController.DmxDevicesController)
            {
                Debug.Log("Testing all haptic DMX devices");

                foreach (var fan in HapticsController.DmxDevicesController.AllDevices)
                {
                    Debug.Log("Testing DMX device: " + fan.Name);

                    HapticsController.DmxDevicesController.SetDeviceValue(fan.Name, 1);
                    yield return new WaitForSecondsRealtime(SingleFanTestDuration);

                    HapticsController.DmxDevicesController.SetDeviceValue(fan.Name, 0);
                    yield return new WaitForSecondsRealtime(0.5f);
                }

                Debug.Log("Done testing all haptic DMX devices");
            }
        }

        IEnumerator TestAudioDevicesAsync()
        {
            if (HapticsController.AudioDevicesController)
            {
                Debug.Log("Testing all haptic audio devices");

                AudioWavesConfig.AddWave(_waveName, Enumerable.Range(2, 5).Select(i => new HarmonicConfig { Frequency = 15 * i, Volume = 1 }).ToArray());

                foreach (var elem in HapticsController.AudioDevicesController.AllElements)
                {
                    Debug.Log("Testing audio element: " + elem.Name);

                    var audioPlayer = HapticsController.Instance.CreateAudioPlayer(new HapticAudioPlayerSettings
                    {
                        WaveName = _waveName,
                        Volume = 1f,
                        IsPersistent = true,
                        AlwaysPlay = true,
                        MutedElements = HapticsController.AudioDevicesController.AllElements.Except(new[] { elem }).Select(e => e.Name).ToArray(),
                    });

                    yield return new WaitForSecondsRealtime(SingleAudioTestDuration);

                    if (audioPlayer)
                    {
                        Destroy(audioPlayer.gameObject);
                    }
                }
            }

            Debug.Log("Done testing all haptic audio devices");
        }
    }
}