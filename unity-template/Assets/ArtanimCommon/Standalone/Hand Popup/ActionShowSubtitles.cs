using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class ActionShowSubtitles : MonoBehaviour, IStandalonePopupAction
    {
        public Toggle ToggleSubtitles;

        public string Header
        {
            get
            {
                return "Subtitles";
            }
        }

        public void Init()
        {
            gameObject.SetActive(true);
            UpdateState();
        }

        public void ExecuteCurrentItem()
        {
            GameController.Instance.CurrentPlayer.Player.ShowSubtitles = !GameController.Instance.CurrentPlayer.Player.ShowSubtitles;
            UpdateState();
        }

        public void NextItem()
        {
            
        }

        public void PrevItem()
        {
            
        }

        private void UpdateState()
        {
            if(GameController.Instance && ToggleSubtitles)
            {
                ToggleSubtitles.isOn = GameController.Instance.CurrentPlayer.Player.ShowSubtitles;
            }
        }

        
    }

}