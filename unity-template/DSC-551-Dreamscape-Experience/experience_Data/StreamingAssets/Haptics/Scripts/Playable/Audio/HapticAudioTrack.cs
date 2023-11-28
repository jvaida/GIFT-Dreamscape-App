using Artanim.Haptics;
using Artanim.Haptics.UnityPlayable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim // Keep in Artanim namespace for user simplicity in timeline editor
{
    [TrackClipType(typeof(HapticAudio))]
    [TrackBindingType(typeof(AudioDeviceTarget))]
    [TrackColor(0.6f, 0.2f, 0.2f)]
    public class HapticAudioTrack : TrackAsset
    {
    }
}
