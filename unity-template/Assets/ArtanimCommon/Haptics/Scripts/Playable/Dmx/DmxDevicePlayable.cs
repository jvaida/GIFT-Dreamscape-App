using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim.Haptics.UnityPlayable
{
    public class DmxDevicePlayable : PlayableBehaviour
    {
        public float Value;
        public AnimationCurve NormalizedCurve;
    }
}
