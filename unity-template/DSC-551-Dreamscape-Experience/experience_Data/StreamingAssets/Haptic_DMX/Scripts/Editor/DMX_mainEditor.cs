using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Collections;

namespace Dreamscape
{

	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(DMX_main))]
	public class DMX_mainEditor : Editor
	{
		SerializedProperty defaultDmxConfigFile;
		void OnEnable()
		{
			defaultDmxConfigFile = serializedObject.FindProperty("defaultDmxConfigFile");
		}
		public override void OnInspectorGUI()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();
			{
				var dmx_main = target as DMX_main;
				if (GUILayout.Button("Save to DMX config file."))
				{
					// parentScript.BuildObject();
					dmx_main.SaveToDmxPatchXML();
				}
				// GUILayout.FlexibleSpace();
				if (GUILayout.Button("Restore from DMX config file."))
				{
					// parentScript.BuildObject();
					dmx_main.RestoreFromDmxPatchXML();
					GUILayout.FlexibleSpace();
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(defaultDmxConfigFile, new GUIContent("Default Config File:"));

			DrawDefaultInspector();

			serializedObject.ApplyModifiedProperties();

			if (GUI.changed)
			{
				Debug.Log("DMX_mainEditor.OnInspectorGUI - changed");
			}
		}
	}

}