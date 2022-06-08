using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
    public class ActionExperienceTrigger : MonoBehaviour, IStandalonePopupAction
    {
        public Text TextItemName;

        private List<string> ExperienceTriggers = new List<string>();

        private int CurrentIndex;

        public string Header
        {
            get
            {
                return "Experience Trigger";
            }
        }

        public void Init()
        {
            gameObject.SetActive(true);

            //Reload triggers based on current scene...
            CurrentIndex = 0;
            ExperienceTriggers.Clear();

            foreach (var trigger in SharedDataUtils.GetMyComponent<LocationComponentWithSession>().Triggers)
                ExperienceTriggers.Add(trigger.TriggerName);

            if (TextItemName)
                TextItemName.text = ExperienceTriggers[CurrentIndex];
        }

        public void ExecuteCurrentItem()
        {
            //Trigger
            NetworkInterface.Instance.SendMessage(new ExperienceTrigger
            {
                TriggerName = ExperienceTriggers[CurrentIndex],
                SessionId = GameController.Instance.CurrentSession != null ? GameController.Instance.CurrentSession.SharedId : Guid.Empty,
            }); ;
        }

        public void NextItem()
        {
            CurrentIndex++;
            if (CurrentIndex == ExperienceTriggers.Count) CurrentIndex = 0;

            if (TextItemName)
                TextItemName.text = ExperienceTriggers[CurrentIndex];
        }

        public void PrevItem()
        {
            CurrentIndex--;
            if (CurrentIndex < 0) CurrentIndex = ExperienceTriggers.Count - 1;

            if (TextItemName)
                TextItemName.text = ExperienceTriggers[CurrentIndex];
        }

        
    }

}