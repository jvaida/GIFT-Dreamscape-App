using Artanim.Haptics.UnityPlayable;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim.Haptics.Editor
{
    [CustomEditor(typeof(HapticAudio))]
    public class PlayableHapticAudioEditor : HapticAudioBaseEditor
    {
        SerializedProperty _waveName;
        SerializedProperty _clip;
        SerializedProperty _volume;
        SerializedProperty _volumeCurve;
        SerializedProperty _loopClip;
        SerializedProperty _persistent;
        SerializedProperty _alwaysPlay;
        SerializedProperty _mutedElements;

        protected override SerializedProperty ClipProperty { get { return _clip; } }

        protected override SerializedProperty WaveNameProperty { get { return _waveName; } }

        void OnEnable()
        {
            _waveName = serializedObject.FindProperty("WaveName");
            _clip = serializedObject.FindProperty("Clip");
            _volume = serializedObject.FindProperty("Volume");
            _volumeCurve = serializedObject.FindProperty("VolumeCurve");
            _loopClip = serializedObject.FindProperty("LoopClip");
            _persistent = serializedObject.FindProperty("Persistent");
            _alwaysPlay = serializedObject.FindProperty("AlwaysPlay");
            _mutedElements = serializedObject.FindProperty("MutedElements");
        }

        protected override void UpdateGui()
        {
            EditorGUILayout.PropertyField(_volume);
            EditorGUILayout.PropertyField(_volumeCurve);
            if (AudioSource == AudioType.Clip)
            {
                EditorGUILayout.PropertyField(_loopClip);
            }
            EditorGUILayout.PropertyField(_persistent);
            EditorGUILayout.PropertyField(_alwaysPlay);

            // Elements (tiles)
            EditorGUILayout.Space();
            ShowElementsGuiLayout(_mutedElements);
        }
    }
}
