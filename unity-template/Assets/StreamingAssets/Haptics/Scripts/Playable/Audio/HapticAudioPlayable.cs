using Artanim.Haptics.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim.Haptics.UnityPlayable
{
    public class HapticAudioPlayable : PlayableBehaviour
    {
        public string WaveName;
        public AudioClip Clip;
        public float Volume;
        public AnimationCurve VolumeCurve;
        public bool LoopClip;
        public bool Persistent;
        public bool AlwaysPlay;
        public string[] Elements;

        HapticAudioPlayer _player;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Application.isPlaying && HapticsController.Instance)
            {
                _player = HapticsController.Instance.CreateAudioPlayer(new HapticAudioPlayerSettings
                {
                    WaveName = WaveName,
                    Clip = Clip,
                    Volume = Volume,
                    Duration = (float)playable.GetDuration(),
                    LoopClip = LoopClip,
                    IsPersistent = Persistent,
                    AlwaysPlay = AlwaysPlay,
                    MutedElements = Elements,
                });
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (_player == null)
                return;

            // https://forum.unity.com/threads/code-example-how-to-detect-the-end-of-the-playable-clip.659617/
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                var duration = playable.GetDuration();
                var time = playable.GetTime();
                var delta = info.deltaTime;

                if ((time + delta) <= duration)
                {
                    var target = playerData as AudioDeviceTarget;

                    // Update volume
                    float volume = Volume;
                    if (VolumeCurve != null)
                    {
                        volume *= VolumeCurve.Evaluate((float)(time / duration));
                    }
                    _player.Volume = volume;

                    // Update colliders (for now they can only be changed before first update)
                    if (_player.CanSetBounds && target && target.HasCollider)
                    {
                        _player.ChangedBounds(target.Bounds);
                    }

                    // Keep alive
                    _player.KeepAlive();
                }
            }
        }
    }
}
