using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// 
	/// </summary>
	[SerializeField]
	public interface ITextProvider
	{
		/// <summary>
		/// This method is called each time the player language is changed.
		/// This happens when the component is added to a session with the corresponding player language.
		/// </summary>
		/// <param name="language">The current player language</param>
		void SetLanguage(string language);

		/// <summary>
		/// This method is called at startup or in the editor when the “Reload Texts” menu is selected.
		/// </summary>
		/// <param name="textAssetPath">Path relative to StreamingAssets configured in the ExperienceSettings</param>
		/// <returns>True if load was successfull</returns>
		bool ReloadTexts(string textAssetPath);

		/// <summary>
		/// This method is called to retrieve a language specific text by its ID.
		/// </summary>
		/// <param name="key">IK of the text to be returned</param>
		/// <param name="language">Language of the text to be returned</param>
		/// <param name="defaultLanguage">Default language to return if the text was not found in the desired language</param>
		/// <returns>Language specific text, Return null if text was not found</returns>
		string GetText(string key, string language, string defaultLanguage);

		/// <summary>
		/// This method is called to verify that the given key is available.
		/// </summary>
		/// <param name="key">ID of the text to be verified</param>
		/// <returns></returns>
		bool HasKey(string key);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string[] GetKnownLanguages();
	}
}