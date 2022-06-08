using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;

namespace Artanim
{

	/// <summary>
	/// Simple network synced integer behaviour.
	/// </summary>
	[AddComponentMenu("Artanim/Network Synced Integer")]
	public class NetworkSyncedInteger : GameSessionNetworkSynced
	{
		[Tooltip("Action called when value changed")]
		public IntEvent OnValueChanged;

		/// <summary>
		/// Returns the current value or -1 if not initialized
		/// </summary>
		public int Value
		{
			get
			{
				return GameSessionController.Instance.GetValue(ObjectId, -1);
			}
			set
			{
				GameSessionController.Instance.SetValue(ObjectId, value);
			}
		}

		protected override void OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false)
		{
			if (value is int)
			{
				Debug.LogFormat("OnValueUpdated {0}", (int)value);
				OnValueChanged.Invoke((int)value);
			}
		}
	}
}