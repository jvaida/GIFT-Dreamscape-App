using Artanim.Location.Messages;
using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Events;

namespace Artanim
{
	[AddComponentMenu("Artanim/Avatar Area")]
	public class AvatarArea : GameSessionNetworkSynced
	{
		[Header("Area options")]

		[Header("Body parts")]
		[Tooltip("Specifies if the area should be activated on any body part.")]
		public bool ActivateOnAllBodyParts;

		[Tooltip("Specifies a specific body part the area is monitoring.")]
		public EAvatarBodyPart ActivateBodyPart = EAvatarBodyPart.Head;


		[Header("Avatars")]
		[Tooltip("Enable to activate when all session avatars enter the area.")]
		public bool ActivateOnAllAvatars;

		[Tooltip("The minimal number of avatars needed to activate the area.")]
		public int MinAvatarCount;


		[Header("Actions")]
		[Tooltip("Triggers when the area enters the active state.")]
		public UnityEvent OnAreaActivated;

		[Tooltip("Triggers when the area exits the active state.")]
		public UnityEvent OnAreaDeactivated;


		private List<Guid> InAvatars = new List<Guid>();

		#region Unity events

		void Start()
		{
			//Check for collider
			if (!GetComponent<Collider>())
			{
				Debug.LogWarning("No collider found. Please add at least one collider to the AvatarArea object.");
			}
			else
			{
				//Only activate the collider on server side
				if (!NetworkInterface.Instance.IsServer)
					GetComponent<Collider>().enabled = false;
			}
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (NetworkInterface.Instance.IsServer && GameController.Instance)
                GameController.Instance.OnSessionPlayerLeft += Instance_OnSessionPlayerLeft;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (NetworkInterface.Instance.IsServer && GameController.HasInstance)
                GameController.Instance.OnSessionPlayerLeft -= Instance_OnSessionPlayerLeft;
        }


        private void Instance_OnSessionPlayerLeft(Location.Data.Session session, Guid playerId)
        {
            //Remove player from "in" list
            if (InAvatars.Contains(playerId))
                InAvatars.Remove(playerId);

            CheckActivationState();
        }

        void OnTriggerEnter(Collider other)
		{
			var avatar = other.GetComponentInParent<AvatarController>();
			if (avatar)
			{
				var bodyPart = other.GetComponent<AvatarBodyPart>();
				if (bodyPart)
				{
					UpdateState(avatar, bodyPart, true);
				}
			}
		}

		void OnTriggerExit(Collider other)
		{
			var avatar = other.GetComponentInParent<AvatarController>();

			if (avatar)
			{
				var bodyPart = other.GetComponent<AvatarBodyPart>();
				if (bodyPart)
				{
					UpdateState(avatar, bodyPart, false);
				}
			}
		}

		#endregion

		#region Events

		protected override void OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false)
		{
			if ((bool)value)
            {
				OnAreaActivated.Invoke();
            }
			else
            {
				OnAreaDeactivated.Invoke();
            }
		}

		#endregion

		#region Internals

		private void UpdateState(AvatarController avatar, AvatarBodyPart bodyPart, bool isIn)
		{
			if(avatar && bodyPart)
			{
				//Check body part
				if (ActivateOnAllBodyParts || ActivateBodyPart == bodyPart.BodyPart)
				{
					//OK, we need to do something....
					if(isIn)
					{
						//Player in area
						if(!InAvatars.Contains(avatar.PlayerId))
						{
							//Add player as in
							InAvatars.Add(avatar.PlayerId);
							CheckActivationState();
						}
					}
					else
					{
						//Player out area
						if (InAvatars.Contains(avatar.PlayerId))
						{
							//Remove him
							InAvatars.Remove(avatar.PlayerId);
							CheckActivationState();
						}
					}
				}
			}
		}


        private void CheckActivationState()
        {
            var countNeeded = MinAvatarCount;
            if (ActivateOnAllAvatars)
                countNeeded = GameController.Instance.RuntimePlayers.Count;

            if(InAvatars.Count >= countNeeded)
            {
                GameSessionController.Instance.SetValue(ObjectId, true);
            }
            else
            {
                GameSessionController.Instance.SetValue(ObjectId, false);
            }
        }

		#endregion

	}
}