using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Location.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Artanim
{

	[AddComponentMenu("Artanim/Experience Trigger Controller")]
	public class ExperienceTriggerController : MonoBehaviour
	{
		public enum ESyncMode { ClientAndServer, Client, Server }

		[Tooltip("Mode defining on which component type the actions are triggered.")]
		public ESyncMode SyncMode = ESyncMode.Server;

		public List<TriggerAction> TriggerActions;

		void OnEnable()
		{
			if(GameController.Instance)
            {
                GameController.Instance.OnJoinedSession += Instance_OnJoinedSession;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession;
            }

			if(NetworkInterface.Instance != null)
				NetworkInterface.Instance.Subscribe<ExperienceTrigger>(NetworkMessage_ExperienceTrigger);

			UpdateTriggerList();
		}

        void OnDisable()
		{
			if (GameController.Instance)
            {
				GameController.Instance.OnJoinedSession -= Instance_OnJoinedSession;
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
			}

			if (NetworkInterface.HasInstance)
				NetworkInterface.SafeUnsubscribe<ExperienceTrigger>(NetworkMessage_ExperienceTrigger);

			RemoveTriggersFromSharedData();
		}

		private void Instance_OnJoinedSession(Session session, System.Guid playerId)
		{
			UpdateTriggerList();
		}

		private void Instance_OnLeftSession()
		{
			UpdateTriggerList();
		}

		private void NetworkMessage_ExperienceTrigger(ExperienceTrigger experienceTrigger)
		{
            if (experienceTrigger != null && NeedTrigger(experienceTrigger))
            {
                RunTrigger(experienceTrigger.TriggerName);
            }
        }

        public void RunTrigger(string triggerName)
        {
            //Debug.LogFormat("Experience trigger received: TriggerName={0}", args.TriggerName);
            if (TriggerActions != null && TriggerActions.Count() > 0 && !string.IsNullOrEmpty(triggerName))
            {
                Debug.LogFormat("Experience trigger event received. Searching for corresponding action. TriggerName={0}", triggerName);
                var action = TriggerActions.FirstOrDefault(t => t.TriggerName == triggerName);
                if (action != null)
                {
                    Debug.LogFormat("Found action for experience trigger={0}", triggerName);
                    action.Action.Invoke();
                }
            }
        }

        protected bool NeedTrigger(ExperienceTrigger experienceTrigger)
		{
			//Check sync mode
			switch (SyncMode)
			{
				case ESyncMode.ClientAndServer:
					break;
				case ESyncMode.Client:
					if (!NetworkInterface.Instance.IsClient)
						return false;
					break;
				case ESyncMode.Server:
					if (!NetworkInterface.Instance.IsServer)
						return false;
					break;
			}

			//Check trigger
			var trigger = TriggerActions.FirstOrDefault(t => t.TriggerName == experienceTrigger.TriggerName);
			if(trigger != null)
            {
				//In session?
				if (GameController.Instance.CurrentSession != null)
				{
					if(!trigger.GlobalTrigger)
                    {
						if (experienceTrigger.SessionId == GameController.Instance.CurrentSession.SharedId)
							return true;
					}
					else
                    {
						if (trigger.TriggerInSession)
							return true;
					}
				}
				else
				{
					if (trigger.GlobalTrigger)
						return true;
				}
			}
			
			return false;
		}

		private List<ExperienceTriggerData> SharedDataTriggers = new List<ExperienceTriggerData>();
		private void UpdateTriggerList()
        {
			var comp = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
			bool syncAsClient = (SyncMode == ESyncMode.Client) || (SyncMode == ESyncMode.ClientAndServer);
			bool syncAsServer = (SyncMode == ESyncMode.Server) || (SyncMode == ESyncMode.ClientAndServer);

			if ((comp != null) &&
				((syncAsClient && NetworkInterface.Instance.IsClient) || (syncAsServer && NetworkInterface.Instance.IsServer)))
            {
				RemoveTriggersFromSharedData();

				//Add all needed
				foreach (var trigger in TriggerActions)
				{
					//In session?
					if (GameController.Instance.CurrentSession != null)
					{
						if (!trigger.GlobalTrigger || trigger.TriggerInSession)
						{
							var sdTrigger = new ExperienceTriggerData { TriggerName = trigger.TriggerName, UseInProduction = trigger.ShowInProduction };
							SharedDataTriggers.Add(sdTrigger);
							comp.Triggers.Add(sdTrigger);
						}
					}
					else
					{
						//Not in session, only add global triggers
						if (trigger.GlobalTrigger)
						{
							var sdTrigger = new ExperienceTriggerData { TriggerName = trigger.TriggerName, };
							SharedDataTriggers.Add(sdTrigger);
							comp.Triggers.Add(sdTrigger);
						}
					}
				}
			}
		}

		private void RemoveTriggersFromSharedData()
		{
			var comp = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
			if (comp != null)
			{
				//Remove all triggers
				foreach (var trigger in SharedDataTriggers)
                {
					comp.Triggers.Remove(trigger);
				}
				SharedDataTriggers.Clear();
			}
		}
	}

	[System.Serializable]
	public class TriggerAction
	{
		public string TriggerName;

		public bool GlobalTrigger;
		public bool TriggerInSession;
		public bool ShowInProduction;

		public UnityEvent Action;
	}

}