using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// The GameObject assigned to this behaviour will be only enabled when running on the server. 
	/// Standalone mode is considered as being both server and client, so the GameObject won't be disabled
	/// except if DisableInStandalone is true.
	/// </summary>
	[AddComponentMenu("Artanim/Server Side GameObject")]
	public class ServerSideGameObject : MonoBehaviour
	{
		[SerializeField]
		bool DisableInStandalone = false;

		virtual protected void Awake()
		{
			gameObject.SetActive(NetworkInterface.Instance.IsServer
				&& (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone || !DisableInStandalone));
		}
	}
}