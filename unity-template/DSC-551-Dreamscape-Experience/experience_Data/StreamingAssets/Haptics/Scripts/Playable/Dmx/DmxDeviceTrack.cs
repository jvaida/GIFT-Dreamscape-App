using Artanim.Haptics;
using Artanim.Haptics.UnityPlayable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Artanim // Keep in Artanim namespace for user simplicity in timeline editor
{
    [TrackClipType(typeof(DmxDevice))]
    [TrackBindingType(typeof(DmxDeviceTarget))]
    [TrackColor(0.2f, 0.2f, 0.6f)]
    public class DmxDeviceTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<DmxDeviceMixer>.Create(graph, inputCount);
        }
    }
}
