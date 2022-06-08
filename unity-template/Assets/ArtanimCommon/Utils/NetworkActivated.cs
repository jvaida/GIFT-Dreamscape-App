using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;

namespace Artanim
{

	/// <summary>
	/// Simple network synced activate / deactivate behaviour.
	/// </summary>
	[AddComponentMenu("Artanim/Network Activated")]
	public class NetworkActivated : GameSessionNetworkSynced
	{
		public enum EMode { ActAsTrigger, ActAsState }

		[Tooltip("Sync behaviour of NetworActivated: " +
			"ActAsTrigger: Actions are triggered all activate/deactivate even if state doesn't change. " +
			"ActAsState: Actions are only triggered when the state changes from active to deactive.")]
		public EMode Mode = EMode.ActAsTrigger;

		[Tooltip("Action called when activated")]
		public UnityEvent OnActivated;

		[Tooltip("Action called when deactivated")]
		public UnityEvent OnDeactivated;

		public bool IsActive
		{
			get
			{
				var value = GameSessionController.Instance.GetValue(ObjectId, 0);
				return value > 0;
			}
		}

		protected override void OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false)
		{
			if (!isInitializing || Mode == EMode.ActAsState)
			{
				var intValue = (int)value;
				if (intValue > 0)
					OnActivated.Invoke();
				else
					OnDeactivated.Invoke();
			}
		}

        private void Start()
        {

        }

        public void Activate()
		{
			if (NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				var value = GameSessionController.Instance.GetValue<int>(ObjectId, 0);
				switch (Mode)
				{
					case EMode.ActAsTrigger:
						if (value < 0)
							value = 0;

						GameSessionController.Instance.SetValue(ObjectId, ++value);
						break;

					case EMode.ActAsState:
						GameSessionController.Instance.SetValue(ObjectId, 1);
						break;
				}
			}
		}

		public void Deactivate()
		{
			if (NetworkInterface.Instance.IsServer && ValidateObjectId())
			{
				var value = GameSessionController.Instance.GetValue<int>(ObjectId, 0);
				switch (Mode)
				{
					case EMode.ActAsTrigger:
						if (value > 0)
							value = 0;

						GameSessionController.Instance.SetValue(ObjectId, --value);
						break;

					case EMode.ActAsState:
						GameSessionController.Instance.SetValue(ObjectId, 0);
						break;
				}
			}
		}

		public void Toggle()
		{
			if (IsActive)
			{
				Deactivate();
			}
			else
			{
				Activate();
			}
		}
	}
}