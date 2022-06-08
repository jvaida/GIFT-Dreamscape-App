using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class ActionLanguage : MonoBehaviour, IStandalonePopupAction
    {
        public Text TextItemName;

        private List<string> Languages = new List<string>();

        private int CurrentIndex;
        private int CurrentLanguageIndex;

        public string Header
        {
            get
            {
                return "Language";
            }
        }

        public void Init()
        {
            gameObject.SetActive(true);

            //Get scenes
            Languages.Clear();
            Languages = TextService.Instance.Languages.ToList();

            //Reload triggers based on current scene...
            CurrentLanguageIndex = Languages.IndexOf(TextService.Instance.CurrentLanguage);
            CurrentIndex = CurrentLanguageIndex;

            UpdateSelectedItem();
        }

        public void ExecuteCurrentItem()
        {
            TextService.Instance.SetLanguage(Languages[CurrentIndex]);
            CurrentLanguageIndex = CurrentIndex;
            UpdateSelectedItem();
        }

        public void NextItem()
        {
            CurrentIndex++;
            if (CurrentIndex == Languages.Count) CurrentIndex = 0;

            UpdateSelectedItem();
        }

        public void PrevItem()
        {
            CurrentIndex--;
            if (CurrentIndex < 0) CurrentIndex = Languages.Count - 1;

            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (TextItemName)
            {
                TextItemName.text = Languages[CurrentIndex];
                TextItemName.color = CurrentLanguageIndex == CurrentIndex ? Color.yellow : Color.white;
            }
        }

        
    }

}