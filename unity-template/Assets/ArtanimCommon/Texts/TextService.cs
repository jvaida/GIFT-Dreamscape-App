using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

	public class TextService
	{
		#region Factory

		private static TextService _instance;

		public static TextService Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new TextService();
					_instance.SetDefaultLanguage();
					_instance.ReloadTexts();
				}
				return _instance;
			}
		}

		#endregion

		private const string DEFAULT_TEXT_PROVIDER_TYPE_NAME = "Artanim.CsvTextProvider";
		private const string DEFAULT_FALLBACK_LANGUAGE = "en";

		private ITextProvider _TextProvider;
		private ITextProvider TextProvider
		{
			get
			{
				if (_TextProvider == null)
				{
					_TextProvider = GetTextProvider();
				}

				return _TextProvider;
			}
		}

		public string CurrentLanguage { get; private set; }
		public string DefaultLanguage { get; private set; }
        public string[] Languages { get { return TextProvider.GetKnownLanguages(); } }

		#region Events

		
		public delegate void OnLanguageChangedHandler(string language);

		/// <summary>
		/// This event is triggered each time the player language has changed.
		/// The new player language is passed in the language parameter.
		/// </summary>
		/// <remarks>The event is static so objects can register to it
		/// without having first to instantiate this class.
		/// In particular, when building we want to avoid doing the instantiation.</remarks>
		public static event OnLanguageChangedHandler OnLanguageChanged;

		#endregion

		public TextService()
		{
			//SetDefaultLanguage();
			//ReloadTexts();
		}

		#region Public interface

		/// <summary>
		/// Sets the current player language.
		/// This method is used internally and should not be called by the experience.
		/// </summary>
		/// <param name="language">Language to be set as current language</param>
		public void SetLanguage(string language)
		{
			if (!string.IsNullOrEmpty(language))
			{
				CurrentLanguage = language.Trim();
			}
			else
			{
				Debug.LogWarning("Tried to set an empty language. Falling back to default language");
				CurrentLanguage = DefaultLanguage;
			}

			//Notify change
			if (OnLanguageChanged != null)
				OnLanguageChanged(CurrentLanguage); 
		}

		/// <summary>
		/// Returns the text corresponding to the player language of the given textId.
		/// If no text is found for the current language, the default language is used.
		/// If the default language text was not found, the given defaultText is returned.
		/// </summary>
		/// <param name="textId">Text ID to retrieve</param>
		/// <param name="defaultText">Default value returned if no text was found</param>
		/// <returns>Language specific text based on the given textId</returns>
		public string GetText(string textId, string defaultText=null)
		{
			if (!string.IsNullOrEmpty(textId))
			{
				var text = TextProvider.GetText(textId.Trim(), !string.IsNullOrEmpty(CurrentLanguage) ? CurrentLanguage : DefaultLanguage, DefaultLanguage);

#if UNITY_EDITOR
				if (text != null && ConfigService.Instance.ExperienceSettings.ShowKeysInEditor)
					text = string.Format("{0}={1}", textId, text);
#endif

				//Fallback to default or not found
				if (text == null)
				{
					text = defaultText != null ? defaultText : string.Format("<Not found: {0}:{1}>", CurrentLanguage, textId);
				}

				return text;
			}
			return textId;
		}

		/// <summary>
		/// This method is used to reload all language specific texts stored in the ITextProvider class.
		/// This method is used internally and should not be called by the experience.
		/// </summary>
		public void ReloadTexts()
		{
			var textAssetPath = ConfigService.Instance.ExperienceSettings.TextAssetPath;
			if (!string.IsNullOrEmpty(textAssetPath))
			{
				if (TextProvider.ReloadTexts(textAssetPath))
				{
					//Set the current language
					SetDefaultLanguage();

					//Verify SDK keys
					if(!VerifySDKKeys())
					{
						Debug.LogWarning("The set text provider does not provide all keys used by the SDK. Check the SDK documentation for a list of keys to be provided.");
					}
				}
				else
				{
					Debug.LogWarningFormat("Failed to read text resources from {0}", textAssetPath);
				}
			}
			else
			{
				Debug.LogWarning("Provided empty text asset path in ExperienceSettings. Please set a correct resource path in ExperienceSettings of your experience.");
			}

			if (OnLanguageChanged != null)
				OnLanguageChanged(CurrentLanguage);
		}

		public void ReloadProvider()
		{
			_TextProvider = null;
			ReloadTexts();
		}

		#endregion

		#region Internals

		private ITextProvider GetTextProvider()
		{
			//Try the configured provider first
			var textProvider = InstantiateTextProvider(ConfigService.Instance.ExperienceSettings.TextProviderTypeName);

			if (textProvider == null)
			{
				//Use SDK default provider
				textProvider = InstantiateTextProvider(DEFAULT_TEXT_PROVIDER_TYPE_NAME);
			}

			if (textProvider == null)
			{
				//Bad... no text provider found!
				Debug.LogErrorFormat("Unable to create configured or default text provider! Configured provider={0}, Default provider={1}",
					ConfigService.Instance.ExperienceSettings.TextProviderTypeName, DEFAULT_TEXT_PROVIDER_TYPE_NAME);
			}

			return textProvider;
		}

		private ITextProvider InstantiateTextProvider(string typeName)
		{
			if (!string.IsNullOrEmpty(typeName))
			{
				try
				{
					var type = Type.GetType(typeName);
					if (type != null)
					{
						var textProvider = Activator.CreateInstance(type) as ITextProvider;
						if (ConfigService.VerboseSdkLog) Debug.LogFormat("Created text provider: Type={0}", textProvider.GetType().FullName);
						return textProvider;
					}
					else
					{
						Debug.LogErrorFormat("Failed to create text provider. Make sure it implements the ITextProvider interface: TypeName={0}", typeName);
					}
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("Failed to create text provider due to {0}:{1}. TypeName={2}", ex.GetType().Name, ex.Message, typeName);
				}
			}

			return null;
		}

		private void SetDefaultLanguage()
		{
			DefaultLanguage = ConfigService.Instance.ExperienceSettings.FallbackLanguage.Trim();
			if (string.IsNullOrEmpty(DefaultLanguage))
			{
				DefaultLanguage = DEFAULT_FALLBACK_LANGUAGE;
				Debug.LogWarningFormat("No fallback language set in ExperienceSettings. Using hardcoded fallback: {0}", DefaultLanguage);
			}

#if UNITY_EDITOR
			//Set settings editor language as current
			CurrentLanguage = ConfigService.Instance.ExperienceSettings.EditorLanguage;
#endif
		}

		private bool VerifySDKKeys()
		{
			foreach(var key in SDKTexts.SDK_DEFAULT_TEXTS.Keys)
			{
				if (!TextProvider.HasKey(key))
					return false;
			}
			return true;
		}

		#endregion



	}

}