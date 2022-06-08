using Artanim.Location.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{

	public class ExperienceSetupLoader : SingletonBehaviour<ExperienceSetupLoader>
	{
        #region Static

        public const string KEY_RUN_EXPERIENCE_SETUP = "ArtanimRunExperienceSetup";
		
		static bool _shouldRunSetup;

		public static void Initialize()
        {
			PlayerPrefs.SetInt(RuntimeLocationComponent.KEY_DOMAIN_ID, CommandLineUtils.GetValue("DomainId", -1, -1));
			PlayerPrefs.SetString(RuntimeLocationComponent.KEY_INITIAL_PEERS, CommandLineUtils.GetValue("InitialPeers", "", ""));

			_shouldRunSetup = RemoteSessionController.Instance.IsDesktopClient || CommandLineUtils.GetValue("RunSetup", true);
		}

		public static bool ShouldRunSetup
		{
			get
            {
				return _shouldRunSetup || (PlayerPrefs.GetInt(KEY_RUN_EXPERIENCE_SETUP, 0) == 1);
			}
		}

        #endregion

        [SerializeField]
		GameObject DefaultExperienceSetupTemplate;

		bool _done;

		public IEnumerator LoadMainSceneWhenDone(SceneController.EScene scene, Transition transition, bool forceLoad)
        {
			Debug.Log("Experience setup scene loaded, waiting on setup to complete before transitioning to loading scene " + scene);

			yield return new WaitUntil(() => _done);

			Debug.Log("Done running experience setup, now loading scene " + scene);
			SceneController.Instance.LoadMainScene(scene, transition, forceLoad);
        }

		IEnumerator Start()
		{
			// Get template
			var experienceSetupTemplate = DefaultExperienceSetupTemplate;
			if (ConfigService.Instance.ExperienceSettings.ExperienceSetupTemplate)
			{
				if (ConfigService.Instance.ExperienceSettings.ExperienceSetupTemplate.GetComponent<IExperienceSetup>() != null)
				{
					experienceSetupTemplate = ConfigService.Instance.ExperienceSettings.ExperienceSetupTemplate;
				}
				else
				{
					Debug.LogWarningFormat("The user experience setup template configured in the ExperienceSettings ({0}) does not have a behaviour implementing the IExperienceSetup interface attached. Using SDK default experience setup</color>", ConfigService.Instance.ExperienceSettings.ExperienceSetupTemplate.name);
				}
			}

			// Instantiate and run
			var settings = new ExperienceSetupSettings();
			yield return UnityUtils.InstantiatePrefab<IExperienceSetup>(experienceSetupTemplate, transform).Run(settings);

			if (settings.DomainId != uint.MaxValue)
            {
				PlayerPrefs.SetInt(RuntimeLocationComponent.KEY_DOMAIN_ID, (int)settings.DomainId);
			}

			if ((settings.ComponentsIps != null) && (settings.ComponentsIps.Count > 0))
			{
				PlayerPrefs.SetString(RuntimeLocationComponent.KEY_INITIAL_PEERS, string.Join(";", settings.ComponentsIps.Select(ip => ip.ToString()).ToArray()));
			}

			_done = true;
		}
	}
}