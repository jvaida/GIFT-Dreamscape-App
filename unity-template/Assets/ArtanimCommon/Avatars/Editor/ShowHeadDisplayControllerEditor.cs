using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomEditor(typeof(ShowHeadDisplayController))]
	[CanEditMultipleObjects]
    public class ShowHeadDisplayControllerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            //Setup
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Add All Renderers"))
            {
                foreach(var dc in targets.OfType<ShowHeadDisplayController>())
                    AddAllRenderers(dc);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddAllRenderers(ShowHeadDisplayController displayController)
        {
            if(displayController)
            {
                displayController.AvatarVisuals = displayController.GetComponentsInChildren<Renderer>().Select(r => r.gameObject).ToArray();
            }
        }
    }
}