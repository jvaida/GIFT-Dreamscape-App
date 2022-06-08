using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using Artanim.Location.Messages;
using UnityEngine.UI;

namespace Artanim
{

	public class MainMenu : MonoBehaviour
	{
		public const string KEY_LAST_SELECTED_SCENE = "ArtanimStandaloneScene";
		public const string KEY_SHOW_SUBTITLES = "ArtanimStandaloneSubtitles";
		public const string KEY_STANDALONE_LANGUAGE = "ArtanimStandaloneLanguage";

		public Button ButtonServer;
		public Button ButtonClient;
		public Button ButtonObserver;


        public GameObject PanelStandalone;
        public Dropdown DropdownStandaloneScenes;
        public Toggle ToggleStandaloneSubtitles;
        public InputField InputFieldStandaloneLanguage;

		public GameObject PanelAutoJoinSession;
		public Dropdown DropdownAutoJoinAvatars;
		public GameObject PanelAutoJoinFullname;
		public InputField InputFieldPlayerFirstName;
		public InputField InputFieldPlayerLastName;

		public Toggle ToggleRunExperienceSetup;

		public Text TextDevelopmentMode;

		void Start()
		{
			//Set dev mode text
			if (TextDevelopmentMode)
			{
				TextDevelopmentMode.text = DevelopmentMode.CurrentMode != EDevelopmentMode.None ? string.Format("Development Mode: {0}", DevelopmentMode.CurrentMode.ToString()) : "";
			}

            //Disable buttons when running in standalone mode
            if (PanelStandalone) PanelStandalone.SetActive(DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone);
            if (ButtonServer) ButtonServer.interactable = DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone;
            if (ButtonObserver) ButtonObserver.interactable = DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone;

			//Init "auto join session" state
			if (PanelAutoJoinSession) PanelAutoJoinSession.SetActive(DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone);
			if (PanelAutoJoinFullname) PanelAutoJoinFullname.SetActive(false);
			if (InputFieldPlayerFirstName) InputFieldPlayerFirstName.text = AutoJoinSessionController.PlayerFirstName;
			if (InputFieldPlayerLastName) InputFieldPlayerLastName.text = AutoJoinSessionController.PlayerLastName;

			//Experience setup
			if (ToggleRunExperienceSetup) ToggleRunExperienceSetup.isOn = ExperienceSetupLoader.ShouldRunSetup;

			if (DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
			{
				//Init scenes dropdown
				if(DropdownStandaloneScenes)
				{
					DropdownStandaloneScenes.gameObject.SetActive(true);

					DropdownStandaloneScenes.ClearOptions();
					
					//Load scenes
					DropdownStandaloneScenes.AddOptions(ConfigService.Instance.ExperienceConfig.StartScenes.Select(s => s.SceneName).ToList());

					//Preselect, from player prefs
					var lastSelectedScene = PlayerPrefs.GetString(KEY_LAST_SELECTED_SCENE);
					if (!string.IsNullOrEmpty(lastSelectedScene))
					{
						//Try to preselect last scene
						var lastOption = DropdownStandaloneScenes.options.FirstOrDefault(o => o.text == lastSelectedScene);
						if (lastOption != null)
							DropdownStandaloneScenes.value = DropdownStandaloneScenes.options.IndexOf(lastOption);
					}
				}

				//Init standalone controls
				if (ToggleStandaloneSubtitles) ToggleStandaloneSubtitles.isOn = PlayerPrefs.GetInt(KEY_SHOW_SUBTITLES, 0) == 1;
                if (InputFieldStandaloneLanguage) InputFieldStandaloneLanguage.text = ConfigService.Instance.ExperienceSettings.EditorLanguage;
			}
			else
            {
				if (DropdownAutoJoinAvatars)
				{
					var desktopRigName = ConfigService.Instance.DesktopRig.Name;
					var avatars = ConfigService.Instance.ExperienceConfig.Avatars.Where(a => a.RigName == desktopRigName).Select(a => a.Name).ToList();
					avatars.Insert(0, "<none>");

					DropdownAutoJoinAvatars.ClearOptions();
					DropdownAutoJoinAvatars.AddOptions(avatars);

					int index = avatars.IndexOf(PlayerPrefs.GetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR));
					if (index > 0)
                    {
						DropdownAutoJoinAvatars.value = index;
					}
					else
                    {
						// Invalid avatar
						PlayerPrefs.SetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR, "");
					}
				}
			}				
		}

		void LoadScene(SceneController.EScene scene)
        {
			if (SceneController.Instance)
			{
				if (ExperienceSetupLoader.ShouldRunSetup)
				{
					SceneController.Instance.LoadSetupScene(scene);
				}
				else
				{
					SceneController.Instance.LoadMainScene(scene);
				}
			}
		}

		public void DoLoadExperienceClient()
		{
			LoadScene(SceneController.EScene.ExperienceClient);
		}

		public void DoLoadExperienceServer()
		{
			LoadScene(SceneController.EScene.ExperienceServer);
		}

		public void DoLoadExperienceObserver()
		{
			LoadScene(SceneController.EScene.ExperienceObserver);
		}

		public void DoSelectScene()
		{
			//Save selected scene to editor prefs
			PlayerPrefs.SetString(KEY_LAST_SELECTED_SCENE, DropdownStandaloneScenes.options[DropdownStandaloneScenes.value].text);
		}

		public void DoToggleSubtitles(bool showSubtitles)
		{
			PlayerPrefs.SetInt(KEY_SHOW_SUBTITLES, showSubtitles ? 1 : 0);
		}

		public void DoSelectDesktopAvatar()
		{
			bool isNone = DropdownAutoJoinAvatars.value == 0; // First entry is "none"
			//Save selected avatar to editor prefs
			PlayerPrefs.SetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR, isNone ? "" : DropdownAutoJoinAvatars.options[DropdownAutoJoinAvatars.value].text);
			if (PanelAutoJoinFullname) PanelAutoJoinFullname.SetActive(!isNone);
		}

		public void DoToggleRunExperienceSetup(bool runExperienceSetup)
		{
			PlayerPrefs.SetInt(ExperienceSetupLoader.KEY_RUN_EXPERIENCE_SETUP, runExperienceSetup ? 1 : 0);
		}

		public void DoUpdateStandaloneLanguage(string newLanguage)
        {
            if(!string.IsNullOrEmpty(newLanguage))
            {
                var language = newLanguage.Trim().ToLowerInvariant();
                PlayerPrefs.SetString(KEY_STANDALONE_LANGUAGE, language);
                ConfigService.Instance.ExperienceSettings.EditorLanguage = language;
            }
        }

		public void DoChangeFirstName(string name)
		{
			PlayerPrefs.SetString(AutoJoinSessionController.KEY_PLAYER_FIRSTNAME, name);
		}

		public void DoChangeLastName(string name)
		{
			PlayerPrefs.SetString(AutoJoinSessionController.KEY_PLAYER_LASTNAME, name);
		}
	}
}