using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Artanim.Haptics.UnityPlayable
{
    /// <summary>
    /// PlayableAsset that plays an haptic audio effect
    /// You can also use an HapticsAudioEffect behavior
    /// </summary>
    public class HapticAudio : PlayableAsset
    {
        [Tooltip("The generated sound to play, listed in generated_waves_config.xml")]
        public string WaveName;

        [Tooltip("The audio clip to play")]
        public AudioClip Clip;

        [Range(0, 10)]
        public float Volume = 1f;
        public AnimationCurve VolumeCurve = AnimationCurve.Constant(0, 1, 1);

        public bool LoopClip;

        [Tooltip("Whether or not the effect should continue playing if the timeline is destroyed before finishing playing")]
        public bool Persistent;

        [Tooltip("Whether or not the effect should play only for elements where there is a player")]
        public bool AlwaysPlay;

        [Tooltip("Elements (audio devices) on which the effect shouldn't play")]
        public string[] MutedElements;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<HapticAudioPlayable>.Create(graph);

            var floor = playable.GetBehaviour();
            floor.WaveName = WaveName;
            floor.Clip = Clip;
            floor.Volume = Volume;
            floor.VolumeCurve = VolumeCurve;
            floor.LoopClip = LoopClip;
            floor.Persistent = Persistent;
            floor.AlwaysPlay = AlwaysPlay;
            floor.Elements = MutedElements;

            return playable;
        }
    }
}
