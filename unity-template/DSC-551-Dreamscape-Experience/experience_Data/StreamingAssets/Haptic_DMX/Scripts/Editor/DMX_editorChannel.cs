using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Dreamscape
{

	public static class DMX_editorDevice
	{
		public static void Show(SerializedProperty dmxDevice/*, SerializedProperty UseGameObjectNameAsDeviceName*/)
		{
			//string oldValue = dmxDevice.FindPropertyRelative("deviceName").stringValue;
			//bool useGO = UseGameObjectNameAsDeviceName.boolValue;

			//EditorGUILayout.PropertyField(dmxDevice.FindPropertyRelative("deviceName"),
			//                              new GUIContent("Unique Device Name"));
			//if (useGO) dmxDevice.FindPropertyRelative("deviceName").stringValue = oldValue;


			DMX_editorList.Show(dmxDevice.FindPropertyRelative("dmxChannel"));
		}
	}

	public static class DMX_editorChannel
	{
		public static void Show(SerializedProperty DmxChannel)
		{
			SerializedProperty name = DmxChannel.FindPropertyRelative("name");
			string nname = name.stringValue;
			EditorGUILayout.PropertyField(DmxChannel.FindPropertyRelative("channel"), new GUIContent(nname));
		}
	}



	// -------------------------------------------------------------------------------


	public static class Audio_editorDevice
	{
		public static void Show(SerializedProperty AudioChannels, SerializedProperty AudioChannelsSize,
			SerializedProperty useDefaultDevConfig, SerializedProperty audioDeviceWorkingList,
			HapticAudio_main parentScript, SerializedProperty running)
		{
			AudioChannels.isExpanded = EditorGUILayout.Foldout(AudioChannels.isExpanded, "Audio Channels");

			if (AudioChannels.isExpanded)
			{
				GUI.enabled = !running.boolValue;
				EditorGUILayout.PropertyField(useDefaultDevConfig, new GUIContent("Use runtime config"));
				EditorGUI.indentLevel += 1;
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Use local devices."))
					{
						bool rv = EditorUtility.DisplayDialog("WARNING:",
							"Using the local devices on your current machine may cause you to loose the current device settings. \n\n" +
							"Be sure to 'Restore dev config' before checking this version in.",
							"Ok, proceed", "Cancel");
						if (rv)
						{
							parentScript.UpdateLocalDevices();
						}

					}
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Restore dev config."))
					{
						// parentScript.BuildObject();
						parentScript.RestoreFromXmlConfig();
					}
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Save dev config."))
					{
						// parentScript.BuildObject();
						parentScript.SaveToXmlConfig();
					}
					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.PropertyField(AudioChannelsSize, new GUIContent("Size"));


				GUI.enabled = true;

				for (int i = 0; i < AudioChannels.arraySize; i++)
				{
					Audio_editorChannel.Show(AudioChannels.GetArrayElementAtIndex(i), audioDeviceWorkingList, parentScript);
				}
				EditorGUI.indentLevel -= 1;
			}
		}
	}

	public static class Audio_editorChannel
	{
		public static void Show(SerializedProperty AudioChannel, SerializedProperty audioDeviceWorkingList, HapticAudio_main parentScript)
		{
			SerializedProperty deviceListIndex = AudioChannel.FindPropertyRelative("deviceListIndex");
			SerializedProperty audioDeviceName = AudioChannel.FindPropertyRelative("audioDeviceName");
            SerializedProperty panelName1 = AudioChannel.FindPropertyRelative("panelName_1");
            SerializedProperty panelName2 = AudioChannel.FindPropertyRelative("panelName_2");
            SerializedProperty channelName_1 = AudioChannel.FindPropertyRelative("channelName_1");
			SerializedProperty channelName_2 = AudioChannel.FindPropertyRelative("channelName_2");
			SerializedProperty isExpanded = AudioChannel.FindPropertyRelative("isExpanded");
			SerializedProperty gain_1 = AudioChannel.FindPropertyRelative("gain_1");
			SerializedProperty gain_2 = AudioChannel.FindPropertyRelative("gain_2");
			//SerializedProperty Collider_1 = AudioChannel.FindPropertyRelative("Collider_1");
			//SerializedProperty Collider_2 = AudioChannel.FindPropertyRelative("Collider_2");
			//SerializedProperty audioDeviceNames = audioDeviceWorkingList.FindPropertyRelative("audioDeviceNames");

			string title = channelName_1.stringValue + ", " + channelName_2.stringValue;
			if (title.Length == 0) title = "Un Assigned";
			//EditorGUILayout.PropertyField(AudioChannel, new GUIContent(title), false);

			isExpanded.boolValue = EditorGUILayout.Foldout(isExpanded.boolValue, title);
			if (isExpanded.boolValue)
			{
				EditorGUI.indentLevel += 1;
				//EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.PropertyField(channelName_1, new GUIContent("Chan Nm 1"), false, GUILayout.MinWidth(100));
					EditorGUILayout.PropertyField(channelName_2, new GUIContent("Chan Nm 2"), false, GUILayout.MinWidth(100));

					int dIndex = deviceListIndex.intValue;
					string currentDeviceName = audioDeviceName.stringValue;
					// List<string> devList = parentScript.audioDeviceWorkingList.audioDeviceNames;
					List<string> devList = new List<string>();
					// int size = audioDeviceWorkingList.FindPropertyRelative("audioDeviceNames.Array.size").intValue;
					int size = audioDeviceWorkingList.FindPropertyRelative("audioDeviceNames.Array.size").intValue;
					for (int i = 0; i < size; i++)
					{
						string prop = audioDeviceWorkingList.FindPropertyRelative(string.Format("audioDeviceNames.Array.data[{0}]", i)).stringValue;
						devList.Add(prop);
					}
					// yourProp.arraySize
					//yourProp.GetArrayElementAtIndex
					//List<string> devList = audioDeviceNames.
					int lenDevList = devList.Count;
					if (lenDevList > 0)
					{
						for (int i = 0; i < lenDevList; i++)
						{
							if (currentDeviceName == devList[i])
							{
								dIndex = i;
								break;
							}
						}
						if (dIndex >= lenDevList) dIndex = 0;

						dIndex = EditorGUILayout.Popup("Dev Nm", dIndex, devList.ToArray(), GUILayout.MinWidth(100));
						deviceListIndex.intValue = dIndex;
						audioDeviceName.stringValue = devList[dIndex];

					}
                    EditorGUILayout.PropertyField(panelName1, new GUIContent("Collider Panel 1"), false, GUILayout.MinWidth(100));
                    EditorGUILayout.PropertyField(panelName2, new GUIContent("Collider Panel 2"), false, GUILayout.MinWidth(100));
                    EditorGUILayout.Slider(gain_1, 0.0f, 1.0f, new GUIContent("Gain Ch #1"));
					EditorGUILayout.Slider(gain_2, 0.0f, 1.0f, new GUIContent("Gain Ch #1"));
					//EditorGUILayout.PropertyField(Collider_1, new GUIContent("Panel #1"), false, GUILayout.MinWidth(100));
					//EditorGUILayout.PropertyField(Collider_2, new GUIContent("Panel #2"), false, GUILayout.MinWidth(100));
				}
				//EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel -= 1;
			}
			//audioMain.selectedDevice = devList[_choiceIndex];
			// EditorGUILayout.PropertyField(AudioChannel.FindPropertyRelative("channelName"), new GUIContent("ccc"),true);
		}
	}

	/*
	Rect r = EditorGUILayout.BeginHorizontal("Button");
			if (GUI.Button(r, GUIContent.none))
				Debug.Log("I got pressed");
			GUILayout.Label("I'm inside the button");
			GUILayout.Label("So am I");
			EditorGUILayout.EndHorizontal();
			*/

}