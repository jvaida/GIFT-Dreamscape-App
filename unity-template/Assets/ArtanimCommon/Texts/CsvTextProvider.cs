using CsvHelper;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using System;

namespace Artanim
{
	public class CsvTextProvider :  ITextProvider
	{
		private Dictionary<string, Text> Texts = new Dictionary<string, Text>();
        private List<string> KnownLanguages = new List<string>();

		#region Public interface

		public string GetText(string key, string language, string defaultLanguage)
		{
			string translation = null;
			if (HasKey(key))
			{
				var text = Texts[key];
				if (!text.LanguageTexts.TryGetValue(language.ToLowerInvariant(), out translation))
				{
					//Try default language
					text.LanguageTexts.TryGetValue(defaultLanguage.ToLowerInvariant(), out translation);
				}
			}
			return translation;
		}

		public bool ReloadTexts(string textAssetPath)
		{
			try
			{
				//Clear old content
				Texts.Clear();
                KnownLanguages.Clear();

				//Build streaming assets path
				var path = Path.Combine(Application.streamingAssetsPath, textAssetPath);
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("Reading texts from {0}...", path);

                //Read CSV file
				using (var csv = new CsvReader(new StreamReader(path)))
				{
					csv.Configuration.Delimiter = ConfigService.Instance.ExperienceSettings.TextDelimiter;
					Debug.LogFormat("Reading {0} with delimiter {1}", textAssetPath, csv.Configuration.Delimiter);
					
					//Read line
					while (csv.Read())
					{
						//Read languages
						var text = new Text() { Id = csv.GetField("id"), };
						foreach (var header in csv.FieldHeaders)
						{
							if (header != "id")
                            {
                                var language = header.ToLowerInvariant().Trim();
                                text.LanguageTexts.Add(language, csv.GetField(header));

                                if (!KnownLanguages.Contains(language))
                                    KnownLanguages.Add(language);
                            }
						}
						Texts.Add(text.Id.Trim(), text);
					}
				}

				if (ConfigService.VerboseSdkLog)
                {
                    var str = new System.Text.StringBuilder();
                    foreach (var kv in Texts)
                    {
                        str.AppendFormat("\n- Found text: ID={0}, Languages={1}", kv.Key, string.Join(", ", kv.Value.LanguageTexts.Keys.ToArray()));
                    }
                    Debug.LogFormat("Loaded {0} texts from {1} (see list below){2}", Texts.Count, path, str);
                }
				return true;
			}
			catch(Exception ex)
			{
				Debug.LogWarningFormat("Failed to read text resources from {0}. {1}:{2}", textAssetPath, ex.GetType().Name, ex.Message);
				return false;
			}
		}

		public void SetLanguage(string language)
		{
			//Ignore, since we already cache all texts
		}

		public bool HasKey(string key)
		{
			return Texts.ContainsKey(key);
		}

        public string[] GetKnownLanguages()
        {
            return KnownLanguages.ToArray();
        }

        #endregion

        private class Text
		{
			public string Id { get; set; }
			public Dictionary<string, string> LanguageTexts = new Dictionary<string, string>();
		}
	}
}