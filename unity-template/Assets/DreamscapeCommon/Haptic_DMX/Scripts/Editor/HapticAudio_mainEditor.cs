using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Dreamscape
{

	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(HapticAudio_main))]
	public class HapticAudio_mainEditor : Editor
	{
		// The imported function
		[DllImport("Dll1", EntryPoint = "GetDeviceNames")]
		unsafe private static extern void GetDeviceNames(byte* a);

		[DllImport("Dll1", EntryPoint = "UpdateDeviceNames")]
		unsafe private static extern void UpdateDeviceNames(byte* a);


		//int _choiceIndex = 0;

		SerializedProperty inUnityGain;
		SerializedProperty outputGain;
		SerializedProperty AudioChannels;
		SerializedProperty AudioChannelsSize;
		SerializedProperty useDefaultDevConfig;
		SerializedProperty audioDeviceWorkingList;
		SerializedProperty AudioChannelsConfiguration;

		void OnEnable()
		{
			// Setup the SerializedProperties.
			//inUnityGain = serializedObject.FindProperty("inUnityGain");
			//outputGain = serializedObject.FindProperty("outputGain");

			AudioChannelsSize = serializedObject.FindProperty("AudioChannelsSize");
			useDefaultDevConfig = serializedObject.FindProperty("useDefaultDevConfig");

			AudioChannelsConfiguration = serializedObject.FindProperty("AudioChannelsConfiguration");
			audioDeviceWorkingList = AudioChannelsConfiguration.FindPropertyRelative("audioDeviceWorkingList");
			inUnityGain = AudioChannelsConfiguration.FindPropertyRelative("inUnityGain");
			outputGain = AudioChannelsConfiguration.FindPropertyRelative("outputGain");
			AudioChannels = AudioChannelsConfiguration.FindPropertyRelative("AudioChannels");
		}

		public override void OnInspectorGUI()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();
			//DrawDefaultInspector();
			var audioMain = target as HapticAudio_main;

			// Show the slider for the speed value.
			EditorGUILayout.Slider(inUnityGain, 0f, 1f, new GUIContent("In Unity Gain"));
			EditorGUILayout.Slider(outputGain, 0f, 1f, new GUIContent("ButtKicker Gain"));

			// Show the channel information.
			SerializedProperty running = serializedObject.FindProperty("running");
			Audio_editorDevice.Show(AudioChannels, AudioChannelsSize, useDefaultDevConfig, audioDeviceWorkingList, audioMain, running);

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// This code gets a list of all the audio output devices currently on the system
		/// by calling an external dll
		/// </summary>

		//// [StructLayout(LayoutKind.Sequential, Pack = 2)]
		//public unsafe struct deviceNames
		//{
		//    public fixed byte name[100 * 32];
		//}
		//public static deviceNames devNamGrp;

	}

}