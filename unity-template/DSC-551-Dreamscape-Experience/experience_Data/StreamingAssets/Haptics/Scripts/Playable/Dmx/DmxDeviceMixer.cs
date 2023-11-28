using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim.Haptics.UnityPlayable
{
    public class DmxDeviceMixer : PlayableBehaviour
    {
        // NOTE: This function is called at runtime and edit time. Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying)
                return;

            var target = playerData as DmxDeviceTarget;
            if (!target)
                return;

            // https://forum.unity.com/threads/code-example-how-to-detect-the-end-of-the-playable-clip.659617/
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                float total = 0f;

                var duration = playable.GetDuration();
                var time = playable.GetTime();
                var delta = info.deltaTime;

                if ((time + delta) >= duration)
                {
                    target.Muted = true;
                    // Done
                }
                else
                {
                    // Update
                    int inputCount = playable.GetInputCount();
                    for (int i = 0; i < inputCount; i++)
                    {
                        var inputPlayable = (ScriptPlayable<DmxDevicePlayable>)playable.GetInput(i);
                        var device = inputPlayable.GetBehaviour();
                        float value = playable.GetInputWeight(i) * device.Value;
                        if (device.NormalizedCurve != null)
                        {
                            value *= device.NormalizedCurve.Evaluate((float)(inputPlayable.GetTime() / inputPlayable.GetDuration()));
                        }
                        total += value;
                    }
                    target.Muted = false;
                }

                target.Value = total;
            }
        }
    }
}
