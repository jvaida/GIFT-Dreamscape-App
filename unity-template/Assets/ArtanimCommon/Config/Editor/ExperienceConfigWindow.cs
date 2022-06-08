using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Artanim
{
	public class ExperienceConfigWindow : EditorWindow
	{
		private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header experience config.jpg";

		[MenuItem("Artanim/Experience Config...", priority = 1)]
		public static void Open()
		{
			var configWindow = (ExperienceConfigWindow)GetWindow(typeof(ExperienceConfigWindow), true, "Experience Config", true);
			configWindow.minSize = new Vector2(800f, 800f);
			configWindow.maxSize = new Vector2(800f, 1200f);
			configWindow.Show();
		}

		public ExperienceConfigSO ExperienceConfig;
		public ExperienceSettingsSO ExperienceSettings;
		private Editor ExperienceConfigEditor;
		private Editor ExperienceSettingsEditor;

		private bool ShowExperienceSettings = true;
		private Texture2D ImageHeader;

		private void Awake()
		{
			var mainFolder = EditorUtils.GetSDKAssetFolder();
			if (!string.IsNullOrEmpty(mainFolder))
			{
				ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));
			}
		}


		private void ReInitializeObjects()
		{
			//Experience config
			ExperienceConfig = ExperienceConfigSO.GetOrCreateConfig();
			ExperienceConfigEditor = Editor.CreateEditor(ExperienceConfig);

			//Experience settings
			ExperienceSettings = ExperienceSettingsSO.GetOrCreateSettings();
			ExperienceSettingsEditor = Editor.CreateEditor(ExperienceSettings);
		}
		
		private Vector2 _scrollPos = Vector2.zero;
		private void OnGUI()
		{
			//Unity invalidates all serialized objects when a scene is loaded in the editor causing the 
			//cached SO's to be invalid. If that happens, just recreate the objects from scratch.
			if (!ExperienceConfig || !ExperienceSettings)
			{
				ReInitializeObjects();
			}

			if (ImageHeader)
				GUILayout.Label(ImageHeader);

			RenderControls();
			RenderInfos();
			EditorGUILayout.Space();

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
			{
				//Experience Config
				ExperienceConfigEditor.OnInspectorGUI();

				//Experience Settings
				if (ShowExperienceSettings = RenderSectionFoldout(ShowExperienceSettings, "SDK Runtime Settings"))
					ExperienceSettingsEditor.OnInspectorGUI();
			}
			EditorGUILayout.EndScrollView();
		}

		private void RenderControls()
		{
			if (GUILayout.Button("Reload config"))
			{
				ExperienceConfig.ReloadConfig();
				ExperienceConfigEditor.serializedObject.Update();
			}

			if (GUILayout.Button("Save config"))
			{
				ExperienceConfig.SaveConfig();
			}
		}

		private void RenderInfos()
		{
			EditorGUILayout.HelpBox(string.Format("{0}\n{1}", ExperienceConfig.UserMessage, ExperienceSettings.UserMessage), MessageType.Info);
		}

		private bool RenderSectionFoldout(bool state, string header)
		{
			EditorGUILayout.Space();
			state = EditorGUILayout.Foldout(state, header);
			EditorGUILayout.Space();
			return state;
		}

	}
}