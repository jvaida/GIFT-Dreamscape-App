using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim.Haptics.UnityPlayable
{
    public class DmxDevice : PlayableAsset
    {
        [Range(0, 1)]
        [UnityEngine.Serialization.FormerlySerializedAs("Speed")]
        public float Value = 1f;

        [UnityEngine.Serialization.FormerlySerializedAs("SpeedCurve")]
        public AnimationCurve NormalizedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DmxDevicePlayable>.Create(graph);

            var fan = playable.GetBehaviour();
            fan.Value = Value;
            fan.NormalizedCurve = NormalizedCurve;

            return playable;
        }
    }
}
