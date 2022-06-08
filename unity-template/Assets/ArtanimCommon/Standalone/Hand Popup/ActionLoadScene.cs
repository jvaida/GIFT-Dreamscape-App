using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class ActionLoadScene : MonoBehaviour, IStandalonePopupAction
    {
        public Text TextItemName;

        private List<string> SceneNames = new List<string>();

        private int CurrentIndex;
        private int CurrentSceneIndex;

        public string Header
        {
            get
            {
                return "Load Scene";
            }
        }

        public void Init()
        {
            gameObject.SetActive(true);

            //Get scenes
            SceneNames.Clear();
            SceneNames = ConfigService.Instance.ExperienceConfig.StartScenes.Select(s => s.SceneName).ToList();

            //Reload triggers based on current scene...
            CurrentSceneIndex = SceneNames.IndexOf(SceneController.Instance.MainChildSceneName);
            CurrentIndex = CurrentSceneIndex;

            UpdateSelectedItem();
        }

        public void ExecuteCurrentItem()
        {
            GameController.Instance.LoadGameScene(SceneNames[CurrentIndex], Transition.FadeBlack);
        }

        public void NextItem()
        {
            CurrentIndex++;
            if (CurrentIndex == SceneNames.Count) CurrentIndex = 0;

            UpdateSelectedItem();
        }

        public void PrevItem()
        {
            CurrentIndex--;
            if (CurrentIndex < 0) CurrentIndex = SceneNames.Count - 1;

            UpdateSelectedItem();
        }

        private void UpdateSelectedItem()
        {
            if (TextItemName)
            {
                TextItemName.text = SceneNames[CurrentIndex];
                TextItemName.color = CurrentSceneIndex == CurrentIndex ? Color.yellow : Color.white;
            }
        }

        
    }

}