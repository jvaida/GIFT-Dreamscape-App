using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	[AddComponentMenu("Artanim/Translated UI Text")]
	[ExecuteInEditMode]
	[RequireComponent(typeof(Text))]
	public class TranslatedUIText : MonoBehaviour
	{
		/// <summary>
		/// Text ID to be displayed
		/// </summary>
		public string TextId;

		private Text _Text;
		private Text Text
		{
			get
			{
				if (_Text == null)
					_Text = GetComponent<Text>();
				return _Text;
			}
		}

		#region Unity events

		private void Start()
		{
			UpdateText();
		}

		private void OnEnable()
		{
			TextService.OnLanguageChanged += Instance_OnLanguageChanged;
		}
		
		private void OnDisable()
		{
			TextService.OnLanguageChanged -= Instance_OnLanguageChanged;
		}

		#endregion

		#region Editor events
#if UNITY_EDITOR

		private string PrevTextId;

		private void OnValidate()
		{
			if (string.IsNullOrEmpty(PrevTextId))
			{
				PrevTextId = TextId;
			}
			else if (PrevTextId != TextId)
			{
				UpdateText();
				PrevTextId = TextId;
			}
		}

#endif
		#endregion

		private void Instance_OnLanguageChanged(string language)
		{
			UpdateText();
		}

		private void UpdateText()
		{
			Text.text = TextService.Instance.GetText(TextId);

#if UNITY_EDITOR
			//Force editor to redraw this object
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

	}
}