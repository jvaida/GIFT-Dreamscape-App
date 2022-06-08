using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Artanim.Location.Network;
using System;

namespace Artanim
{
	/// <summary>
	/// Simple network synced behaviour used to send signals to each session component.
	/// </summary>
	[AddComponentMenu("Artanim/Network Trigger")]
	public class NetworkTrigger : NetworkSyncedBehaviour
	{
		public UnityEvent OnTrigger;

		void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<Location.Messages.NetworkTrigger>(OnNetworkTrigger);
		}

		void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<Location.Messages.NetworkTrigger>(OnNetworkTrigger);
		}

		public void Trigger()
		{
			if (NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				NetworkInterface.Instance.SendMessage(new Artanim.Location.Messages.NetworkTrigger { TriggerName = ObjectId });
			}
		}

		private void OnNetworkTrigger(Location.Messages.NetworkTrigger args)
		{
			if(args != null && NeedTrigger(args.TriggerName))
			{
				Debug.LogFormat("NetworkTrigger triggered: {0}", args.TriggerName);
				OnTrigger.Invoke();
			}
		}
	}

}