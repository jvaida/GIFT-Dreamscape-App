using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Artanim
{
	public abstract class GameSessionNetworkSynced : NetworkSyncedBehaviour
	{
		/// <summary>
		/// Triggered when value is updates and the corresponding mode is applicable
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="playerValue"></param>
		protected abstract void OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false);

		protected virtual void OnEnable()
		{
			GameSessionController.Instance.OnValueUpdated += GameSessionController_OnValueUpdated;
		}

		protected virtual void OnDisable()
		{
			if(GameSessionController.HasInstance)
			{
				GameSessionController.Instance.OnValueUpdated -= GameSessionController_OnValueUpdated;
			}
		}

		private void GameSessionController_OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false)
		{
			if(NeedTrigger(key))
			{
				OnValueUpdated(key, value, playerValue, isInitializing);
			}
		}

	}

}
