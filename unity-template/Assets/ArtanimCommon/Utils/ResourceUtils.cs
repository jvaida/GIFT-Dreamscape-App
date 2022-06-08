using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;

namespace Artanim
{
	public static class ResourceUtils
	{
		public const char SEPARATOR_RESOURCE_LANGUAGE = '_';
		public const string FORMAT_LANGUAGE_RESOURCE = "{0}_{1}";

		public static T LoadResources<T>(string name) where T : UnityEngine.Object
		{
			var resource = Resources.Load<T>(name);

			if (resource != null && resource is T)
			{
				return (T)resource;
			}
			else
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("Failed to load from resources: Type={0}, Name={1}", typeof(T).Name, name);
				return null;
			}
		}

		public static T GetPlayerLanguageResource<T>(string originalName, string searchResourcePath, T fallback=null) where T : UnityEngine.Object
		{
			if (!string.IsNullOrEmpty(originalName))
			{
				var resourceName = originalName;

				//Find resource name
				if (resourceName.LastIndexOf(SEPARATOR_RESOURCE_LANGUAGE) == resourceName.Length - 3)
				{
					//This is a language specific name -> search a resurce by replacing the language name
					resourceName = resourceName.Substring(0, resourceName.Length - 3);
				}

                //Experience default resource path used?
                if (string.IsNullOrEmpty(searchResourcePath))
                    searchResourcePath = ConfigService.Instance.ExperienceSettings.FallbackResourcePath;

				//Try load language specific resource
				var resourcePath = string.Format("{0}/{1}", searchResourcePath, string.Format(FORMAT_LANGUAGE_RESOURCE, resourceName, TextService.Instance.CurrentLanguage));
				var languageResource = LoadResources<T>(resourcePath);
				if (!languageResource)
				{
					//No language specific resource found, search for default language one as fallback
					resourcePath = string.Format("{0}/{1}", searchResourcePath, string.Format(FORMAT_LANGUAGE_RESOURCE, resourceName, TextService.Instance.DefaultLanguage));
					languageResource = LoadResources<T>(resourcePath);
				}

				if (!languageResource)
				{
					//Bad one... did not find player language resource nor default language resource!!!
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("Failed to find language specific resource for player language nor default language. ResourceName: {0}, ResourceType={1}, Player language={2}, Default language={3}, ResourcePath={4}",
						resourceName, typeof(T).Name, TextService.Instance.CurrentLanguage, TextService.Instance.DefaultLanguage.ToString(), resourcePath);
					languageResource = fallback;
				}
				return languageResource;
			}

			return fallback;
		}


	}
}