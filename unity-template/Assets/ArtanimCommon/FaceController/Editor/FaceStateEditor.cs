using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomEditor(typeof(FaceState))]
    [CanEditMultipleObjects]
    public class FaceStateEditor : Editor
    {
        private Vector2 BlendShapesScroll;

        public override void OnInspectorGUI()
        {
            if (serializedObject.targetObject == null)
                return;

            serializedObject.Update();

            //Open in editor
            if (GUILayout.Button("Open in editor"))
            {
                FaceStateEditorWindow.Open(serializedObject.targetObject as FaceState);
            }

            DrawDefaultInspector();

            //Blendshapes, no blendshape editing on multiple objects
            if (targets.Length == 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Blendshapes", EditorStyles.boldLabel);

                BlendShapesScroll = EditorGUILayout.BeginScrollView(BlendShapesScroll);
                {
                    EditorGUILayout.BeginVertical();
                    {
                        var bsValues = serializedObject.FindProperty("BlendShapeValues");
                        for (var i = 0; i < bsValues.arraySize; ++i)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                var bsValue = bsValues.GetArrayElementAtIndex(i);
                                EditorGUILayout.LabelField(bsValue.FindPropertyRelative("Name").stringValue, GUILayout.Width(EditorGUIUtility.labelWidth));
                                EditorGUILayout.PropertyField(bsValue.FindPropertyRelative("Value"), new GUIContent());

                                bsValue.FindPropertyRelative("Value").floatValue = bsValue.FindPropertyRelative("Value").floatValue;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnDestroy()
        {
            RemoveUnusedBlendShapes();
        }

        public void RenderWithAllBlendshapes(IEnumerable<string> allBlendShapes)
        {
            var faceState = serializedObject.targetObject as FaceState;
            if(faceState)
            {
                var toCreate = new List<BlendShapeValue>();
                foreach(var bsName in allBlendShapes)
                {
                    var bsValue = faceState.BlendShapeValues.FirstOrDefault(b => b.Name == bsName);
                    if (bsValue == null)
                    {
                        //Create one
                        toCreate.Add(new BlendShapeValue
                        {
                            Name = bsName,
                            Value = 0f,
                        });
                    }
                }

                foreach(var b in toCreate)
                {
                    faceState.BlendShapeValues.Add(b);
                }

                //Sort
                faceState.BlendShapeValues = faceState.BlendShapeValues.OrderBy(b => b.Name).ToList();
            }

            //Render normal
            OnInspectorGUI();
        }

        public void RemoveUnusedBlendShapes()
        {
            if(target && serializedObject != null && serializedObject.targetObject != null)
            {
                var faceState = serializedObject.targetObject as FaceState;
                if (faceState)
                {
                    var toRemove = new List<BlendShapeValue>();
                    foreach (var blendShapeValue in faceState.BlendShapeValues)
                    {
                        if (blendShapeValue.Value == 0f)
                            toRemove.Add(blendShapeValue);
                    }

                    foreach (var b in toRemove)
                        faceState.BlendShapeValues.Remove(b);
                }
            }
        }

    }
}