using UnityEditor;
using UnityEngine;

namespace Dreamscape
{

	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(DMX_device))]
	public class DMX_deviceEditor : Editor
	{
		public SerializedProperty speed;
		public SerializedProperty dmxDevice;
		//SerializedProperty subDevices;
		public SerializedProperty slaveDevices;
		public SerializedProperty deviceName;
		//public SerializedProperty UseGameObjectNameAsDeviceName;

		public SerializedProperty buttonText_useingGameObjecName;
		public SerializedProperty buttonText_notUseingGameObjecName;

		void OnEnable()
		{
			// Setup the SerializedProperties.
			speed = serializedObject.FindProperty("speed");
			dmxDevice = serializedObject.FindProperty("dmxDevice");
			slaveDevices = serializedObject.FindProperty("slaveDevices");
			deviceName = dmxDevice.FindPropertyRelative("deviceName");
			//UseGameObjectNameAsDeviceName = serializedObject.FindProperty("UseGameObjectNameAsDeviceName");

			buttonText_useingGameObjecName = serializedObject.FindProperty("buttonText_useingGameObjecName");
			buttonText_notUseingGameObjecName = serializedObject.FindProperty("buttonText_notUseingGameObjecName");
		}


		public override void OnInspectorGUI()
		{
			// Update the serializedProperty 
			serializedObject.Update();
			Color currentColor = GUI.color;
			Color contentColor = GUI.contentColor;

			// Show the slider for the speed value.
			EditorGUILayout.Slider(speed, 0, 1.0f, new GUIContent("Value"));

			string dn = dmxDevice.FindPropertyRelative("deviceName").stringValue;
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.LabelField("Device Name", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
				EditorGUILayout.SelectableLabel(dn, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
			}
			EditorGUILayout.EndHorizontal();

			GUI.color = currentColor;
			GUI.contentColor = contentColor;

			// Show the channel information.
			DMX_editorDevice.Show(dmxDevice/*, UseGameObjectNameAsDeviceName*/);

			// Show the sub Devices, and slave Devices.
			//EditorGUILayout.PropertyField(subDevices, true);
			EditorGUILayout.PropertyField(slaveDevices, true);

			serializedObject.ApplyModifiedProperties();
		}
	}

}