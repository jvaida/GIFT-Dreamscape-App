using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim.Haptics.Editor
{
    [CustomEditor(typeof(HapticAudioEffect))]
    public class HapticAudioEffectEditor : HapticAudioBaseEditor
    {
        SerializedProperty _waveName;
        SerializedProperty _clip;
        SerializedProperty _volume;
        SerializedProperty _muted;
        SerializedProperty _duration;
        SerializedProperty _loopClip;
        SerializedProperty _persistent;
        SerializedProperty _alwaysPlay;
        SerializedProperty _target;
        SerializedProperty _mutedElements;

        enum DevicesSelection { Elements = 1, Target = 2 };

        DevicesSelection _devSelection;

        protected override SerializedProperty ClipProperty { get { return _clip; } }

        protected override SerializedProperty WaveNameProperty { get { return _waveName; } }

        void OnEnable()
        {
            _waveName = serializedObject.FindProperty("WaveName");
            _clip = serializedObject.FindProperty("Clip");
            _volume = serializedObject.FindProperty("Volume");
            _muted = serializedObject.FindProperty("Muted");
            _duration = serializedObject.FindProperty("Duration");
            _loopClip = serializedObject.FindProperty("LoopClip");
            _persistent = serializedObject.FindProperty("Persistent");
            _alwaysPlay = serializedObject.FindProperty("AlwaysPlay");
            _target = serializedObject.FindProperty("Target");
            _mutedElements = serializedObject.FindProperty("MutedElements");
        }

        protected override void UpdateGui()
        {
            EditorGUILayout.PropertyField(_volume);
            EditorGUILayout.PropertyField(_muted);
            EditorGUILayout.PropertyField(_duration);
            if (AudioSource == AudioType.Clip)
            {
                EditorGUILayout.PropertyField(_loopClip);
            }
            EditorGUILayout.PropertyField(_persistent);
            EditorGUILayout.PropertyField(_alwaysPlay);

            // Devices selection
            if (_devSelection == 0)
            {
                _devSelection = _target.objectReferenceValue ? DevicesSelection.Target : DevicesSelection.Elements;
            }
            var newDevSelection = (DevicesSelection)EditorGUILayout.EnumPopup("Devices Selection", _devSelection);
            if (newDevSelection != _devSelection)
            {
                _devSelection = newDevSelection;
                if (_devSelection == DevicesSelection.Target)
                {
                    _mutedElements.ClearArray();
                }
                else
                {
                    _target.objectReferenceValue = null;
                }
            }

            if (_devSelection == DevicesSelection.Target)
            {
                EditorGUILayout.PropertyField(_target);
            }
            else if (_devSelection == DevicesSelection.Elements)
            {
                // Elements (tiles)
                ShowElementsGuiLayout(_mutedElements);
            }
        }
    }
}
