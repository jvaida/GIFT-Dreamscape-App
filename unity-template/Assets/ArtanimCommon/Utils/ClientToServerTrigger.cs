using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Artanim.Location.Network;
using System;

namespace Artanim
{
	/// <summary>
	/// Sends a trigger event from the client to the server.
	/// </summary>
	[AddComponentMenu("Artanim/Client To Server Trigger")]
	public class ClientToServerTrigger : NetworkSyncedBehaviour
	{
		public ClientToServerEvent OnTrigger;

		void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<Location.Messages.ClientToServerTrigger>(OnClientToServerTrigger);
		}

		void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<Location.Messages.ClientToServerTrigger>(OnClientToServerTrigger);
		}

		public void Trigger()
		{
			if (NetworkInterface.Instance.ComponentType == Location.Data.ELocationComponentType.ExperienceClient && ValidateObjectId()) //Explicit check... avoid oberserver to trigger
			{
				NetworkInterface.Instance.SendMessage(
					new Artanim.Location.Messages.ClientToServerTrigger
					{
						TriggerName = ObjectId,
						SessionId = GameController.Instance.CurrentSession.SharedId,
						RecipientId = SyncMode == ESyncMode.Server ? GameController.Instance.CurrentSession.ExperienceServerId : Guid.Empty,
					}
				);
			}
		}

		private void OnClientToServerTrigger(Location.Messages.ClientToServerTrigger args)
		{
			if(args != null && NeedTrigger(args.TriggerName))
			{
				var player = GameController.Instance.GetPlayerByPlayerId(args.SenderId);
				if(player != null)
				{
					Debug.LogFormat("ClientToServerTrigger triggered: {0} by player {1}", args.TriggerName, player.Player.ComponentId);
					OnTrigger.Invoke(args.TriggerName, player.AvatarController);
				}
				else
				{
					Debug.LogWarningFormat("Failed to trigger ClientToServerTrigger. Player with id {0} was not found in session.", args.SenderId);
				}
			}
		}
	}

}