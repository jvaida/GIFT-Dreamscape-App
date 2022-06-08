using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

	public class SessionIntroController : MonoBehaviour
	{
		public AudioClip DefaultWelcomeToDreamscapeClip;
		public AudioClip DefaultWakeUpAndDreamClip;
		public GameObject DefaultSessionIntroAudioTemplate;

		private GameObject AudioSourceGO;

		private void Awake()
        {
			if(ConfigService.Instance.ExperienceSettings.EnableSessionIntro)
            {
				var template = GetAudioSourceTemplate();
				AudioSourceGO = UnityUtils.InstantiatePrefab(template, transform);
				AudioSourceGO.transform.localPosition = Vector3.zero;
				AudioSourceGO.transform.localRotation = Quaternion.identity;
            }
			else
            {
				gameObject.SetActive(false);
            }
        }

		public void DoPlayWelcomeToDreamscape()
		{
			var clip = ConfigService.Instance.ExperienceSettings.WelcomeToDreamscapeClip;
			if (!clip)
				clip = DefaultWelcomeToDreamscapeClip;
			PlayAudioClip(clip);
		}

		public void DoPlayWakeUpAndDream()
		{
			var clip = ConfigService.Instance.ExperienceSettings.WakeUpAndDreamClip;
			if (!clip)
				clip = DefaultWakeUpAndDreamClip;
			PlayAudioClip(clip);
		}

		private void PlayAudioClip(AudioClip clip)
        {
			if(AudioSourceGO)
            {
				var translatedAudio = AudioSourceGO.GetComponent<TranslatedAudioSource>();
				if (translatedAudio)
                {
					translatedAudio.Play(clip, "Cat Timeline");
				}
				else
                {
					var audioSource = AudioSourceGO.GetComponent<AudioSource>();
					audioSource.PlayOneShot(clip);
                }
            }
        }

		private GameObject GetAudioSourceTemplate()
        {
			var template = ConfigService.Instance.ExperienceSettings.SessionIntroAudioSourceTemplate;

			if (!template)
            {
				template = DefaultSessionIntroAudioTemplate;
            }
			else if (!template.GetComponent<AudioSource>())
            {
				Debug.LogError("The configured session intro audiosoure template root does not contain an AudioSource component. Falling back to the SDK default audio source.");
				template = DefaultSessionIntroAudioTemplate;
            }

			return template;
        }
	}
}