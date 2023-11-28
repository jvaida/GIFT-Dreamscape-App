using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artanim
{

	public class ExperienceSettingsSO : ScriptableObject
	{
		public const string EXPERIENCE_SETTINGS_RESOURCE = "Experience Settings";


		#region Factory

		public static ExperienceSettingsSO GetOrCreateSettings()
		{
			var settings = ResourceUtils.LoadResources<ExperienceSettingsSO>(EXPERIENCE_SETTINGS_RESOURCE);
			if (!settings)
			{
#if UNITY_EDITOR
				//Create a new one
				settings = CreateInstance<ExperienceSettingsSO>();

				if (!Application.isPlaying)
				{
					//Store new settings only if application is not playing
					//Create root resource folder if it doesn't exist
					if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
					{
						UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
					}

					//Create new asset in resources folder
					var path = string.Format("Assets/Resources/{0}.asset", EXPERIENCE_SETTINGS_RESOURCE);
					UnityEditor.AssetDatabase.CreateAsset(settings, path);
					settings.UserMessage = string.Format("Created new experience settings in resources path: {0}", path);
				}
				
#else
				Debug.LogWarning("No experience settings found. Create a new ExperienceSettings asset in a resource folder of your project called 'Experience Settings'");
#endif
			}
			else
			{
				var resourcePath = EXPERIENCE_SETTINGS_RESOURCE;
#if UNITY_EDITOR
				resourcePath = UnityEditor.AssetDatabase.GetAssetPath(settings);
#endif
				settings.UserMessage = string.Format("Loaded experience settings from resource path: {0}", resourcePath);
			}

			return settings;
		}

        #endregion

        private void Awake()
        {
            EditorLanguage = PlayerPrefs.GetString(MainMenu.KEY_STANDALONE_LANGUAGE, "en");
        }


        public enum ESceneLoadingType { DontWaitForSync, WaitForSync, }
		public enum EReadyForSessionMode { Auto, ExperienceControlled, }
        public enum EHandTrackerPosition { Hand, Wrist, }
		public enum EXRMode { Plugin, Legacy }


		public string UserMessage { get; set; }


		/// ------------------------------------------------------------------------
		/// Experience Settings
		/// ------------------------------------------------------------------------
		[Header("Experience Settings")]
		[Tooltip("Default client camera template created at startup. Leave empty to use the original default camera. The template must have a VRCameraFader behaviour attached to it.")]
		public GameObject DefaultCameraTemplate;

		[Tooltip("Default camera template used for observer views (used in observer and server). Leave empty to use the original camera. The template must have a VRCameraFader behaviour attached to it.")]
		public GameObject DefaultObserverCameraTemplate;

		[Tooltip("Default camera template used for server view. If not set the server will use the observer or default camera.")]
		public GameObject DefaultServerCameraTemplate;

		[Tooltip("Default prefab template used to display messages to the player. Leave empty to use the original SDK template. The given template must have a behaviour attached implementing the IUserMessageDisplayer interface.")]
		public GameObject DefaultUserMessageDisplayer;

		[Tooltip("Prefab that will be instantiated to setup the experience before loading the first game scene. Leave empty to use the original SDK template.")]
		public GameObject ExperienceSetupTemplate;

		[Tooltip("Name of the scene used in the session initialization phase. Leave empty to use the original construct scene.")]
		public string ConstructSceneName;

		[Tooltip("Defines how the experience handles the 'Ready for Session' state.\n" +
			"Auto: The component will be ready for a session directly when the construct scene is loaded.\n" +
			"ExperienceControlled: The experience construct scene MUST signal the ready state using the GameController.SetReadyForSession() method each time it's loaded.")]
		public EReadyForSessionMode ReadyForSessionMode;

#if UNITY_2019_3_OR_NEWER
		[Tooltip("Informs the SDK whether the experience is using the XR plugin system or the legacy mode")]
		public EXRMode XRMode;
#endif

		[Tooltip("Whether or not the standard SDK UI should be shown on the client.")]
		public bool HideUIInClient;

		[Tooltip("Whether or not to have the SDK monitor the server Window to make sure it always has the focus.")]
		public bool KeepFocusOnServerWindow;

		/// ------------------------------------------------------------------------
		/// Wheelchair and chair settings
		/// ------------------------------------------------------------------------
		[Header("Chair and wheelchair settings")]
		[Tooltip("Default chair to be used in the experience. This value can be overridden per avatar in the AvatarController. If not set, the SDK default wheelchair is used.")]
		public ChairConfig DefaultChairTemplate;

        /// ------------------------------------------------------------------------
        /// Avatar and tracking
        /// ------------------------------------------------------------------------
        [Header("Avatar and Tracking")]
        [Tooltip("Describes where the hand trackers are placed on the player. (Hand: tracker on the players hand, Wrist: tracker on the players forearm/wrist)")]
        public EHandTrackerPosition HandTrackerPosition = EHandTrackerPosition.Hand;
        [Tooltip("The number of times per second avatar hand updates are sent to the other session members.")]
        public int HandUpdatesPerSecond = 10;

        /// ------------------------------------------------------------------------
        /// Standalone
        /// ------------------------------------------------------------------------
        [Header("Standalone")]
        [Tooltip("Whether or not to disable standalone pickupable for TrackingRigidbody")]
        public bool DisableStandalonePickupables = false;
		[Tooltip("Standalone avatar template. If not set the default SDK avatar is used")]
		public GameObject StandaloneAvatarTemplate;

        /// ------------------------------------------------------------------------
        /// Teamspeak
        /// ------------------------------------------------------------------------
        [Header("Teamspeak")]
		[Tooltip("Default audio source used for Teamspeak player voices. If not set, the default SDK audio source is used. The provided prefab must have a TeamSpeakAudioSource behaviour in the root.")]
		public GameObject TeamspeakAudioSource;

		[Tooltip("Default audio source used for Teamspeak hostess voice. If not set, the TeamspeakAudioSource or the default SDK source is used instead. The provided prefab must have a TeamSpeakAudioSource behaviour in the root.")]
		public GameObject TeamspeakHostessAudioSource;

        /// ------------------------------------------------------------------------
        /// Scene loading
        /// ------------------------------------------------------------------------
        [Header("Scene loading")]
		[Tooltip("Default scene loading behaviour. WaitForSync forces all components to wait until all have loaded a scene before fading in")]
		public ESceneLoadingType DefaultSceneLoadingType = ESceneLoadingType.DontWaitForSync;

		[Tooltip("Timeout in seconds the components stop waiting for the scene sync message")]
		public float SceneSyncTimeout = 30f;

		/// ------------------------------------------------------------------------
		/// Text and translation
		/// ------------------------------------------------------------------------
		[Header("Text and translation")]
		[Tooltip("2 character language to be used in the editor. The runtime value is provided by the hostess depending on the players choice.")]
		public string EditorLanguage = "en";

		[Tooltip("Show key and text in the editor.")]
		public bool ShowKeysInEditor = false;

		[Tooltip("2 character language to be used as fallback when the requested language is not found.")]
		public string FallbackLanguage = "en";

        [Tooltip("Default/Fallback resource path for translated resources used if not otherwise specified. Empty means that the translated resources will be searched in the resources folder root.")]
        public string FallbackResourcePath;

		[Tooltip("Fully qualified type of text provider used to retrieve translated texts. If not set, the SDK default implementation is used.")]
		public string TextProviderTypeName = "Artanim.CsvTextProvider";

		[Tooltip("Path relative to StreamingAssets where the text resources are located.")]
		public string TextAssetPath = "Texts/texts.csv";

		[Tooltip("Delimiter used by the text provider if needed.")]
		public string TextDelimiter = ",";

		/// ------------------------------------------------------------------------
		/// Subtitles
		/// ------------------------------------------------------------------------
		[Header("Subtitles")]
		[Tooltip("Prefab used to display subtitles in observers. The given prefab must have a behaviour implementing the ISubtitleDisplayer interface attached to its root. If not set the SDK default is used.")]
		public GameObject ObserverSubtitleDisplayer;

		[Tooltip("Prefab used to display subtitles in VR. The given prefab must have a behaviour implementing the ISubtitleDisplayer interface attached to its root. If not set the SDK default is used.")]
		public GameObject VRSubtitleDisplayer;

		/// ------------------------------------------------------------------------
		/// Session Intro
		/// ------------------------------------------------------------------------
		[Header("Session Intro")]
		public bool EnableSessionIntro = true;
		[EnableIf("EnableSessionIntro")] public AudioClip WakeUpAndDreamClip;
		[EnableIf("EnableSessionIntro")] public AudioClip WelcomeToDreamscapeClip;
		[EnableIf("EnableSessionIntro")] public GameObject SessionIntroAudioSourceTemplate;

		/// ------------------------------------------------------------------------
		/// Logging
		/// ------------------------------------------------------------------------
		[Header("Logging")]
		[Tooltip("Remove most of the SDK info logs inside the editor. This settings only has an effect within the Unity editor.\nThis settings will only have an effect once playmode is restarted in Unity.")]
		public bool VerboseSDKLogs;

#region Editor events

#if UNITY_EDITOR
		private string PrevEditorLanguage;

		private void OnValidate()
		{
			if (string.IsNullOrEmpty(PrevEditorLanguage))
			{
				PrevEditorLanguage = EditorLanguage;
			}
			else if (PrevEditorLanguage != EditorLanguage)
			{
				TextService.Instance.SetLanguage(EditorLanguage);
                PlayerPrefs.SetString(MainMenu.KEY_STANDALONE_LANGUAGE, EditorLanguage);
				PrevEditorLanguage = EditorLanguage;
			}
		}
#endif
#endregion
	}

}