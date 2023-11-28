using Artanim.Haptics.Internal;
using Artanim.Haptics.PodLayout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Artanim.Haptics.Editor
{
    public abstract class HapticAudioBaseEditor : UnityEditor.Editor
    {
        protected enum AudioType { Procedural = 1, Clip = 2 };

        protected AudioType AudioSource { get; private set; }
        string[] _waveNames;

        protected abstract SerializedProperty ClipProperty { get; }

        protected abstract SerializedProperty WaveNameProperty { get; }

        public override void OnInspectorGUI()
        {
            if (_waveNames == null)
            {
                var names = AudioWavesConfig.ReadWaveNames();
                if (names.Length == 0)
                {
                    _waveNames = new string[1] { "<invalid, empty or missing " + AudioWavesConfig.Filename + ">" };
                }
                else
                {
                    _waveNames = new string[] { "<none>" }.Concat(names).ToArray();
                }
            }

            serializedObject.Update();

            // Audio type
            if (AudioSource == 0)
            {
                AudioSource = string.IsNullOrEmpty(WaveNameProperty.stringValue) ? AudioType.Clip : AudioType.Procedural;
            }

            var newAudioSource = (AudioType)EditorGUILayout.EnumPopup("Source", AudioSource);
            if (newAudioSource != AudioSource)
            {
                AudioSource = newAudioSource;
                if (AudioSource == AudioType.Procedural)
                {
                    ClipProperty.objectReferenceValue = null;
                }
                else
                {
                    WaveNameProperty.stringValue = null;
                }
            }

            if (AudioSource == AudioType.Clip)
            {
                EditorGUILayout.PropertyField(ClipProperty);
            }
            else
            {
                int waveIndex = Mathf.Max(0, System.Array.IndexOf(_waveNames, WaveNameProperty.stringValue));
                int newWaveIndex = EditorGUILayout.Popup("Wave Name", waveIndex, _waveNames);
                if (newWaveIndex != waveIndex)
                {
                    WaveNameProperty.stringValue = newWaveIndex == 0 ? "" : _waveNames[newWaveIndex];
                }
            }

            UpdateGui();

            serializedObject.ApplyModifiedProperties();
        }

        protected abstract void UpdateGui();

        protected static void ShowElementsGuiLayout(SerializedProperty mutedElements)
        {
            EditorGUILayout.LabelField("Active tiles", EditorStyles.boldLabel);

            var podConfig = PodLayoutConfig.Instance;
            if ((podConfig != null) && (podConfig.Floor != null))
            {
                var mutedElementsPos = new List<Vector2Int>();
                for (int i = 0, iMax = mutedElements.arraySize; i < iMax; ++i)
                {
                    var str = mutedElements.GetArrayElementAtIndex(i).stringValue;
                    mutedElementsPos.Add(PodLayoutConfig.GetFloorTileIndex(podConfig.Floor.NameTemplate, str));
                }

                EditorGUILayout.LabelField("(back of the pod)", EditorStyles.miniLabel);
                EditorGUI.BeginChangeCheck();

                var newMutedElementsPos = new List<Vector2Int>();
                for (int z = podConfig.Floor.Dimensions.Depth - 1; z >= 0; --z)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int x = 0; x < podConfig.Floor.Dimensions.Width; ++x)
                    {
                        var pos = new Vector2Int(x, z);
                        string name = PodLayoutConfig.CreateFloorTileName(podConfig.Floor.NameTemplate, pos);
                        if (!EditorGUILayout.ToggleLeft(name, !mutedElementsPos.Contains(pos), GUILayout.MinWidth(60)))
                        {
                            newMutedElementsPos.Add(pos);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    var newMutedElems = newMutedElementsPos.Select(e => PodLayoutConfig.CreateFloorTileName(podConfig.Floor.NameTemplate, e)).ToArray();
                    mutedElements.arraySize = newMutedElems.Length;
                    for (int i = 0; i < newMutedElems.Length; ++i)
                    {
                        mutedElements.GetArrayElementAtIndex(i).stringValue = newMutedElems[i];
                    }
                }
                EditorGUILayout.LabelField("(entrance of the pod)", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Invalid or missing pod layout config!");
            }
        }
    }
}
