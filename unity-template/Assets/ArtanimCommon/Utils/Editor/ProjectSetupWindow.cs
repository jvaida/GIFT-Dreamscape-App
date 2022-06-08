using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tools
{
	public class ProjectSetupWindow : EditorWindow
	{
		private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header project setup.jpg";

		private Texture2D ImageHeader;

		[MenuItem("Artanim/Tools/Setup Project...", priority = 3)]
		static void Init()
		{
			var setupWindow = (ProjectSetupWindow)GetWindow(typeof(ProjectSetupWindow), true, "Project Setup", true);
			setupWindow.minSize = new Vector2(800f, 600f);
			setupWindow.maxSize = new Vector2(800f, 1200f);
			setupWindow.Show();
		}

		private void Awake()
		{
			var mainFolder = EditorUtils.GetSDKAssetFolder();
			if (!string.IsNullOrEmpty(mainFolder))
			{
				ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));
			}
		}

		private Vector2 _scrollPos = Vector2.zero;
		private void OnGUI()
		{
			if (ImageHeader)
				GUILayout.Label(ImageHeader);

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
			{
				RenderWindow();
			}
			EditorGUILayout.EndScrollView();
		}

		private void RenderWindow()
		{
			RenderTargetPlatformCheck();
            RenderCopyPDBFilesCheck();
            RenderRunInBackgroundCheck();
			RenderAllowUnsafeCodeCheck();
			RenderVRSupportedCheck();
			RenderOculusCheck();
			RenderSDKScenesCheck();
			RenderSDKConfigCheck();
		}

		private delegate bool CheckSetup(ref string message);
		private delegate void Fix();
		private delegate void RenderAdditional();

		#region Target Platform

		private void RenderTargetPlatformCheck()
		{
			DrawSetupSection("Target Platform",
				check: (ref string message) =>
				{
					if (EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone && EditorUserBuildSettings.selectedStandaloneTarget == BuildTarget.StandaloneWindows64)
					{
						message = "The build target is set to 'Standalone' and 'x86_64'.";
						return true;
					}
					else
					{
						message = "The SDK requires the project target to be 'standalone' and 'x86_64' architecture.";
						return false;
					}
				},

				fix: () =>
				{
					EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
				},

				additional: () =>
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open Build Settings", GUILayout.Width(200)))
						{
							GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			);
		}

        #endregion

        #region Copy PDB Files

        private void RenderCopyPDBFilesCheck()
        {
            DrawSetupSection("Copy PDB Files",
                check: (ref string message) =>
                {
                    var copyPDBValue = EditorUserBuildSettings.GetPlatformSettings(BuildTargetGroup.Standalone.ToString(), "CopyPDBFiles");

                    var copyPDB = false;
                    if (!bool.TryParse(copyPDBValue, out copyPDB))
                        copyPDB = false;

                    if (copyPDB)
                    {
                        message = "Project Setting 'Copy PDB files' is enabled.";
                        return true;
                    }
                    else
                    {
                        message = "For the SDK crash handler to get call stack informations, the 'Copy PDB files' must be enabled in the project build settings.";
                        return false;
                    }
                },

                fix: () =>
                {
                    EditorUserBuildSettings.SetPlatformSettings(BuildTargetGroup.Standalone.ToString(), "CopyPDBFiles", true.ToString());
                },

                additional: () =>
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Open Build Settings", GUILayout.Width(200)))
                        {
                            GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            );
        }

        #endregion

        #region Allow Unsafe Code
        private void RenderAllowUnsafeCodeCheck()
		{
#if UNITY_2018_1_OR_NEWER
			DrawSetupSection("Allow Unsafe Code",
				check: (ref string message) =>
				{
					if (PlayerSettings.allowUnsafeCode)
					{
						message = "Project Setting 'Allow unsafe Code' is enabled.";
						return true;
					}
					else
					{
						message = "The SDK requires the Project Setting 'Allow unsafe Code' to be enabled.";
						return false;
					}
				},

				fix: () =>
				{
					PlayerSettings.allowUnsafeCode = true;
				},

				additional: () =>
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open Player Settings", GUILayout.Width(200)))
						{
							EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			);
#endif
		}
		#endregion

		#region Run In Background

		private void RenderRunInBackgroundCheck()
		{
			DrawSetupSection("Run In Background",
				check: (ref string message) =>
				{
					if (PlayerSettings.runInBackground)
					{
						message = "Project Setting 'Run In Background' is enabled.";
						return true;
					}
					else
					{
						message = "The SDK requires the Project Setting 'Run In Background' to be enabled.";
						return false;
					}
				},

				fix: () =>
				{
					PlayerSettings.runInBackground = true;
				},

				additional: () =>
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open Player Settings", GUILayout.Width(200)))
						{
							EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			);
		}

        #endregion

        #region VR Supported

        private void RenderVRSupportedCheck()
		{
			DrawSetupSection("Virtual Reality Supported",
				check: (ref string message) =>
				{
					if (PlayerSettings.virtualRealitySupported)
					{
						message = "Project Setting 'Virtual Reality Supported' is enabled.";
						return true;
					}
					else
					{
						message = "The SDK requires the Project Setting 'Virtual Reality Supported' to be enabled.";
						return false;
					}
				},

				fix: () =>
				{
					PlayerSettings.virtualRealitySupported = true;
				},

				additional: () =>
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open Player Settings", GUILayout.Width(200)))
						{
							EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			);
		}

		#endregion

		#region Oculus (Only for Unity 2017)

		private const string OCULUS_SDK_NAME = "Oculus";

		private void RenderOculusCheck()
		{
#if UNITY_2017
			if (PlayerSettings.virtualRealitySupported)
			{
				DrawSetupSection("Oculus Support",
					check: (ref string message) =>
					{
						var vrSdks = PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Standalone);
						if (vrSdks[0] == OCULUS_SDK_NAME)
						{
							message = "Oculus Virtual Reality SDK is enabled and first in the list.";
							return true;
						}
						else
						{
							message = "The SDK requires the Oculus Virtual Reality SDK to be enabled and first in the list.";
							return false;
						}
					},

					fix: () =>
					{
						var vrSdks = new List<string>(PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Standalone));
						if (!vrSdks.Contains(OCULUS_SDK_NAME))
						{
							vrSdks.Insert(0, OCULUS_SDK_NAME);
							PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, vrSdks.ToArray());
						}
						else
						{
							vrSdks.Remove(OCULUS_SDK_NAME);
							vrSdks.Insert(0, OCULUS_SDK_NAME);
							PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, vrSdks.ToArray());
						}
					},

					additional: () =>
					{
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Open Player Settings", GUILayout.Width(200)))
							{
								EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
							}
						}
						EditorGUILayout.EndHorizontal();
					}
				);
			}
#endif
		}

		#endregion

		#region SDK scenes
		private static readonly string SCENE_TYPE = ".unity";
		private static readonly string SDK_MAIN_SCENE = "Scenes/Main Scene";
		private static readonly string[] REQUIRED_SDK_SCENES = new string[]
		{
			SDK_MAIN_SCENE,
			"Scenes/Emergency Scene/Emergency Scene",
			"Scenes/Main Menu/Main Menu Scene",
			"Scenes/Experience Controller/Experience Controller Scene",
			"Scenes/Observer/Experience Observer Scene",
			"Scenes/Construct/Construct Scene",
		};

		private void RenderSDKScenesCheck()
		{
			//Check main folder
			var mainFolder = EditorUtils.GetSDKAssetFolder();
			if (!string.IsNullOrEmpty(mainFolder))
			{
				//Check scenes
				var missingScenes = new List<string>();
				var disabledScenes = new List<string>();
				var mainSceneFirst = false;
				var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
				foreach (var sdkScene in REQUIRED_SDK_SCENES)
				{
					var buildScene = buildScenes.FirstOrDefault(s => s.path == string.Concat(mainFolder, sdkScene, SCENE_TYPE));
					if (buildScene == null)
					{
						missingScenes.Add(sdkScene);
					}
					else if (!buildScene.enabled)
					{
						disabledScenes.Add(sdkScene);
					}
				}

				//Check first scene
				if (buildScenes.Count > 0)
					mainSceneFirst = buildScenes[0].path == string.Concat(mainFolder, SDK_MAIN_SCENE, SCENE_TYPE);

				//Display and fix
				DrawSetupSection("SDK Scenes",
					check: (ref string message) =>
					{
						if (missingScenes.Count == 0 && disabledScenes.Count == 0 && mainSceneFirst)
						{
							message = "All required SDK scenes are in the build and enabled and the main scene is first in the list.";
							return true;
						}
						else
						{
							if (missingScenes.Count > 0)
								message += string.Concat("Missing SDK scenes:\n", string.Join("\n", missingScenes.ToArray()));

							if (disabledScenes.Count > 0)
								message += string.Concat(message.Length > 0 ? "\n\n" : "", "Disabled SDK scenes:\n", string.Join("\n", disabledScenes.ToArray()));

							if (!mainSceneFirst)
								message += string.Concat(message.Length > 0 ? "\n\n" : "", "The SDK main scene is not first in the list.");

							return false;
						}
					},

					fix: () =>
					{
						//Add missing
						foreach (var missingScene in missingScenes)
						{
							buildScenes.Add(new EditorBuildSettingsScene(string.Concat(mainFolder, missingScene, SCENE_TYPE), true));
						}

						//Enabled disabled
						foreach (var disabledScene in disabledScenes)
						{
							var buildScene = buildScenes.FirstOrDefault(s => s.path == string.Concat(mainFolder, disabledScene, SCENE_TYPE));
							if (buildScene != null)
								buildScene.enabled = true;
						}

						//Set main scene first
						if (!mainSceneFirst)
						{
							var mainScene = buildScenes.FirstOrDefault(s => s.path == string.Concat(mainFolder, SDK_MAIN_SCENE, SCENE_TYPE));
							if (mainScene != null)
							{
								buildScenes.Remove(mainScene);
								buildScenes.Insert(0, mainScene);
							}
						}

						//Apply to editor
						EditorBuildSettings.scenes = buildScenes.ToArray();
					},

					additional: () =>
					{
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Open Build Settings", GUILayout.Width(200)))
							{
								GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
							}
						}
						EditorGUILayout.EndHorizontal();
					}
				);
			}
		}


		#endregion

		#region SDK Config

		private void RenderSDKConfigCheck()
		{
			var configExists = File.Exists(Path.Combine(Application.streamingAssetsPath, ConfigService.EXPERIENCE_CONFIG_NAME));
			var settingsExists = ResourceUtils.LoadResources<ExperienceSettingsSO>(ExperienceSettingsSO.EXPERIENCE_SETTINGS_RESOURCE) != null;

			//Draw infos
			DrawSetupSection("Experience Config",
				check: (ref string message) =>
				{
					if (configExists && settingsExists)
					{
						message = "SDK experience config and settings found.";
						return true;
					}
					else
					{
						if (!configExists)
							message = "Missing experience config file in Streaming Assets.";

						if (!settingsExists)
							message += string.Concat(message.Length > 0 ? "\n" : "", "Missing experience settings in Resources.");

						return false;
					}
				},

				fix: () =>
				{
					if (!configExists)
						ExperienceConfigSO.GetOrCreateConfig();

					if (!settingsExists)
						ExperienceSettingsSO.GetOrCreateSettings();
				},

				additional: () =>
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open Experience Config", GUILayout.Width(200)))
						{
							ExperienceConfigWindow.Open();
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			);
		}

		#endregion

		#region Internals

		private void DrawSetupSection(string title, CheckSetup check, Fix fix, RenderAdditional additional = null)
		{
			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

				//Check setup
				var message = "";
				var result = check(ref message);

				//Info
				EditorGUILayout.HelpBox(message, result ? MessageType.Info : MessageType.Warning);

				//Fix button?
				if (!result)
				{
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Fix It", GUILayout.Width(200)))
						{
							fix();
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				else if (additional != null)
				{
					additional();
				}

				EditorGUILayout.Space();
				GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndVertical();
		}
	}

	#endregion

}