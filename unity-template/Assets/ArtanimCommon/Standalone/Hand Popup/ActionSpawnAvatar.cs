using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class ActionSpawnAvatar : MonoBehaviour, IStandalonePopupAction
    {
        public Text TextItemName;

        private List<Location.Config.Avatar> Avatars { get { return ConfigService.Instance.ExperienceConfig.Avatars; } }
        private int CurrentIndex;

        public string Header
        {
            get
            {
                return "Spawn Avatar";
            }
        }

        public void Init()
        {
            gameObject.SetActive(true);

            if (TextItemName)
                TextItemName.text = Avatars[CurrentIndex].Name;
        }

        public void ExecuteCurrentItem()
        {
            //Spawn avatar
            var standaloneController = GetComponentInParent<StandaloneController>();
            if (standaloneController)
            {
                standaloneController.SpawnPlayer(Avatars[CurrentIndex]);
            }
            else
            {
                Debug.LogError("Cannot spawn avatar. No StandaloneController found in parents.");
            }
        }

        public void NextItem()
        {
            CurrentIndex++;
            if (CurrentIndex == Avatars.Count) CurrentIndex = 0;

            if (TextItemName)
                TextItemName.text = Avatars[CurrentIndex].Name;
        }

        public void PrevItem()
        {
            CurrentIndex--;
            if (CurrentIndex < 0) CurrentIndex = Avatars.Count - 1;

            if (TextItemName)
                TextItemName.text = Avatars[CurrentIndex].Name;
        }
    }

}