using Artanim;
using Artanim.Location.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Artanim
{

	[CustomEditor(typeof(ExperienceConfigSO))]
	public class ExperienceConfigSOEditor : Editor
	{
		private const int LIST_LABEL_WIDTH = 60;
		private const int LIST_ELEMENT_MARGIN = 10;

		private void Awake()
		{
			var experienceConfig = serializedObject.targetObject as ExperienceConfigSO;

			PrepareScenesList(experienceConfig);
			PrepareAvatarsList();
			PrepareExperiencePropertiesList();
            PrepareTrackedPropsList();
        }

		private bool ShowScenes = false;
		private bool ShowAvatars = false;
		private bool ShowProperties = false;
        private bool ShowTrackedProps = false;

		public override void OnInspectorGUI()
		{
            if (TrackedPropsList == null || ExperiencePropertiesList == null || AvatarsList == null || ScenesList == null)
                Awake();

			RenderHeader("Base Experience Settings");
			RenderBaseExperienceSettings();

			if (ShowScenes = RenderSectionFoldout(ShowScenes, "Scenes"))
				RenderScenesList();

			if (ShowAvatars = RenderSectionFoldout(ShowAvatars, "Avatars"))
				RenderAvatarsList();

			if (ShowProperties = RenderSectionFoldout(ShowProperties, "Experience Properties"))
				RenderExperiencePropertiesList();

            if(ShowTrackedProps = RenderSectionFoldout(ShowTrackedProps, "Tracked Props"))
			    RenderTrackedProps();

			serializedObject.ApplyModifiedProperties();
		}

		private void RenderBaseExperienceSettings()
		{
			//Experience name
			EditorGUILayout.PropertyField(serializedObject.FindProperty("ExperienceName"), true);

			//Allow add player in running session
			EditorGUILayout.PropertyField(serializedObject.FindProperty("AllowAddPlayerWhileRunning"));

			//Server FPS
			EditorGUILayout.IntSlider(serializedObject.FindProperty("ServerFPS"), 20, 300);

            //Limit client HMD FPS on not present
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LimitFPSOnHMDOffHead"));

            //Seated experience
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SeatedExperience"));

            //Teamspeak
            EditorGUILayout.PropertyField(serializedObject.FindProperty("VoiceChat"));
            GUI.enabled = serializedObject.FindProperty("VoiceChat").enumValueIndex == (int)ExperienceConfig.EVoiceChat.Teamspeak;
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TSMuteMic"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TSMuteAudio"));
                GUI.enabled = GUI.enabled && serializedObject.FindProperty("TSMuteAudio").boolValue;
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("TSMuteHostessAudio"));
                    EditorGUI.indentLevel -= 1;
                }
                GUI.enabled = true;
            }
            GUI.enabled = true;

            //Lipsync
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LipsyncMode"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("LipsyncAudioGain"));
            GUI.enabled = serializedObject.FindProperty("LipsyncMode").enumValueIndex == (int)ExperienceConfig.ELipsyncMode.Microphone;
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LipsyncSyncUpdatePeriod"));
            }
            GUI.enabled = true;

			//Haptics
			EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableNativeHaptics"));
		}

		private void RenderControls(ExperienceConfigSO config)
		{
			if (GUILayout.Button("Reload config"))
			{
				config.ReloadConfig();
				serializedObject.Update();
			}

			if (GUILayout.Button("Save config"))
			{
				config.SaveConfig();
				serializedObject.Update();
			}
		}

		private void RenderInfos()
		{
			EditorGUILayout.HelpBox(serializedObject.FindProperty("UserMessage").stringValue, MessageType.Info);
		}

		private bool RenderSectionFoldout(bool state, string header)
		{
			EditorGUILayout.Space();
			state = EditorGUILayout.Foldout(state, header);
			EditorGUILayout.Space();
			return state;
		}

		private void RenderHeader(string header)
		{
			EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
		}

		#region Scenes list

		private ReorderableList ScenesList;

		private void PrepareScenesList(ExperienceConfigSO config)
		{
			//Create reorderable list
			ScenesList = new ReorderableList(serializedObject, serializedObject.FindProperty("StartScenes"), true, true, true, true);
			ScenesList.drawElementCallback = (Rect rect, int index, bool isActivae, bool isFocused) =>
			{
				var element = ScenesList.serializedProperty.GetArrayElementAtIndex(index);
				var sceneName = element.FindPropertyRelative("SceneName");

				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;

				EditorGUI.LabelField(rect, sceneName.stringValue);
			};

			//List header callback
			ScenesList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Scenes"); };

			//List add dropdown callback
			ScenesList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
			{
			//Generate dropdown menu with all editor scenes but the SDK ones which are not in the list of scene already
			var menu = new GenericMenu();
				foreach (var scene in EditorBuildSettings.scenes)
				{
					var name = scene.path;
					if (!name.Contains("ArtanimCommon"))
					{
						var sceneName = name.Substring(name.LastIndexOf("/") + 1).Replace(".unity", "");

						if (!config.StartScenes.Any(s => s.SceneName == sceneName))
						{
							menu.AddItem(new GUIContent(sceneName), false, (object target) =>
							{
							//Dropdown list selection callback. Add selected scene to experience scenes
							var index = list.serializedProperty.arraySize;
								list.serializedProperty.arraySize++;
								list.index = index;
								var element = list.serializedProperty.GetArrayElementAtIndex(index);
								element.FindPropertyRelative("SceneName").stringValue = sceneName;

								serializedObject.ApplyModifiedProperties();

							}, sceneName);
						}
					}
				}
				menu.ShowAsContext();
			};
		}

		private void RenderScenesList()
		{
			ScenesList.DoLayoutList();
		}

		#endregion

		#region Avatars list

		private ReorderableList AvatarsList;

		private void PrepareAvatarsList()
		{
			//Create reorderable list
			AvatarsList = new ReorderableList(serializedObject, serializedObject.FindProperty("Avatars"), true, true, true, true);
			AvatarsList.drawElementCallback = (Rect rect, int index, bool isActivae, bool isFocused) =>
			{
				var element = AvatarsList.serializedProperty.GetArrayElementAtIndex(index);
				var elementWidth = CalcEvenSpaceElementWidth(rect, 3, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN);

				rect.y += 2;
				rect.height = EditorGUIUtility.singleLineHeight;

				//Name
				EditorGUI.LabelField(new Rect(rect.x, rect.y, LIST_LABEL_WIDTH, rect.height), "Name");
				rect.x += LIST_LABEL_WIDTH + LIST_ELEMENT_MARGIN;
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, elementWidth, rect.height), element.FindPropertyRelative("Name"), GUIContent.none);
				rect.x += elementWidth + LIST_ELEMENT_MARGIN;

				//Resource
				var avatarResources = FindAvatarsInResources();
				var avatarResourceProperty = element.FindPropertyRelative("AvatarResource");
				var avatarResourcesIndex = avatarResources.IndexOf(avatarResourceProperty.stringValue);

				EditorGUI.LabelField(new Rect(rect.x, rect.y, LIST_LABEL_WIDTH, rect.height), "Resource");
				rect.x += LIST_LABEL_WIDTH + LIST_ELEMENT_MARGIN;
				avatarResourcesIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, elementWidth, rect.height), avatarResourcesIndex, avatarResources.ToArray());
				avatarResourceProperty.stringValue = avatarResources.ElementAtOrDefault(avatarResourcesIndex);
				rect.x += elementWidth + LIST_ELEMENT_MARGIN;

				//Rig
				var ikRigs = ConfigService.Instance.Rigs.Select(r => r.Name).ToList();
				var rigNameProperty = element.FindPropertyRelative("RigName");
				var rigIndex = ikRigs.IndexOf(rigNameProperty.stringValue);

				EditorGUI.LabelField(new Rect(rect.x, rect.y, LIST_LABEL_WIDTH, rect.height), "Rig");
				rect.x += LIST_LABEL_WIDTH + LIST_ELEMENT_MARGIN;
				rigIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, elementWidth, rect.height), rigIndex, ikRigs.ToArray());
				rigNameProperty.stringValue = ikRigs.ElementAtOrDefault(rigIndex);
				rect.x += elementWidth + LIST_ELEMENT_MARGIN;
			};

			//List header callback
			AvatarsList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Avatars"); };

			//Create empty avatar when adding
			AvatarsList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.index = index;
				var element = list.serializedProperty.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("Name").stringValue = string.Empty;
				element.FindPropertyRelative("AvatarResource").stringValue = string.Empty;
				element.FindPropertyRelative("RigName").stringValue = ConfigService.Instance.Rigs.First().Name;
			};
		}

		private List<string> AvatarsResourcesCache;
		private List<string> FindAvatarsInResources()
		{
			if (AvatarsResourcesCache == null)
			{
				AvatarsResourcesCache = Resources.LoadAll<AvatarController>(string.Empty)
					.Select(a => AssetDatabase.GetAssetPath(a.gameObject))
					.Where(p => p.IndexOf("Resources") > -1)
					.Select(p => p.Substring(p.IndexOf("Resources") + "Resources".Length + 1).Replace(".prefab", ""))
					.ToList();
			}

			return AvatarsResourcesCache;
		}

		private void RenderAvatarsList()
		{
			AvatarsList.DoLayoutList();
		}

		#endregion

		#region Experience properties list

		private ReorderableList ExperiencePropertiesList;

		private void PrepareExperiencePropertiesList()
		{
			//Create reorderable list
			ExperiencePropertiesList = new ReorderableList(serializedObject, serializedObject.FindProperty("ExperienceProperties"), true, true, true, true);
			ExperiencePropertiesList.drawElementCallback = (Rect rect, int index, bool isActivae, bool isFocused) =>
			{
				var element = ExperiencePropertiesList.serializedProperty.GetArrayElementAtIndex(index);
				RenderListProperties(rect, element, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN, "Key", "Value");
			};

			//List header callback
			ExperiencePropertiesList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Properties"); };

			//Create empty avatar when adding
			ExperiencePropertiesList.onAddCallback = (ReorderableList list) =>
			{
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.index = index;
				var element = list.serializedProperty.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("Key").stringValue = "";
				element.FindPropertyRelative("Value").stringValue = "";
			};
		}

		private void RenderExperiencePropertiesList()
		{
			ExperiencePropertiesList.DoLayoutList();
		}

        #endregion

        #region Tracked Props

        private ReorderableList TrackedPropsList;

        private void PrepareTrackedPropsList()
        {
            //Create reorderable list
            TrackedPropsList = new ReorderableList(serializedObject, serializedObject.FindProperty("TrackedProps"), true, true, true, true);

            TrackedPropsList.elementHeightCallback = (int index) => { return (EditorGUIUtility.singleLineHeight + 2) * 4 + 4; };

            TrackedPropsList.drawElementCallback = (Rect rect, int index, bool isActivae, bool isFocused) =>
            {
                var element = TrackedPropsList.serializedProperty.GetArrayElementAtIndex(index);
                RenderListProperties(rect, element, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN, "Name", "Group", "Transient");

                //Start Position
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                var startPosition = element.FindPropertyRelative("StartPosition");
                if (startPosition != null) RenderListProperties(rect, startPosition, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN, "Tolerance", "Vector");

                //Start Start Direction1
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                var startDirection1 = element.FindPropertyRelative("StartDirection1");
                if (startDirection1 != null) RenderListProperties(rect, startDirection1, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN, "Axis", "Tolerance", "Vector");

                //Start Start Direction2
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                var startDirection2 = element.FindPropertyRelative("StartDirection2");
                if (startDirection2 != null) RenderListProperties(rect, startDirection2, LIST_LABEL_WIDTH, LIST_ELEMENT_MARGIN, "Axis", "Tolerance", "Vector");

            };

            //List header callback
            TrackedPropsList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Tracked Props"); };

            //Create empty tracked prop when adding
            TrackedPropsList.onAddCallback = (ReorderableList list) =>
            {
                var index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("Name").stringValue = "";
                element.FindPropertyRelative("Transient").boolValue = true;

                element.FindPropertyRelative("StartPosition.Tolerance").floatValue = 0f;
                element.FindPropertyRelative("StartPosition.Vector").vector3Value = Vector3.zero;

                element.FindPropertyRelative("StartDirection1.Tolerance").floatValue = 0f;
                element.FindPropertyRelative("StartDirection1.Vector").vector3Value = Vector3.zero;
                element.FindPropertyRelative("StartDirection1.Axis").enumValueIndex = 0;

                element.FindPropertyRelative("StartDirection2.Tolerance").floatValue = 0f;
                element.FindPropertyRelative("StartDirection2.Vector").vector3Value = Vector3.zero;
                element.FindPropertyRelative("StartDirection2.Axis").enumValueIndex = 1;
            };
        }

        private void RenderTrackedProps()
		{
            TrackedPropsList.DoLayoutList();
        }

        #endregion

        #region Helpers

        private void RenderListProperties(Rect space, SerializedProperty root, int labelWidth, int elementMargin, params string[] properties)
		{
			var elementWidth = CalcEvenSpaceElementWidth(space, properties.Length, labelWidth, elementMargin);

			space.y += 2;
			space.height = EditorGUIUtility.singleLineHeight;

			foreach (var property in properties)
			{
				//Render label
				EditorGUI.LabelField(new Rect(space.x, space.y, labelWidth, space.height), property);
				space.x += labelWidth + elementMargin;

				//Render value
				EditorGUI.PropertyField(new Rect(space.x, space.y, elementWidth, space.height), root.FindPropertyRelative(property), GUIContent.none);
				space.x += elementWidth + elementMargin;
			}
		}

		private int CalcEvenSpaceElementWidth(Rect space, int numElements, int labelWidth, int elementMargin)
		{
			var availableSpace = space.width - space.x - (numElements * labelWidth) - (numElements * elementMargin);
			return Mathf.FloorToInt(availableSpace / numElements);
		}

		#endregion
	}
}