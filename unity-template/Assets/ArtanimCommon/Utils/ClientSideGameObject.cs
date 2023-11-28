using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// The GameObject assigned to this behaviour will be only enabled when running on the client. 
	/// </summary>
	[AddComponentMenu("Artanim/Client Side GameObject")]
	public class ClientSideGameObject : MonoBehaviour
	{
		virtual protected void Awake()
		{
			gameObject.SetActive(NetworkInterface.Instance.IsClient);
		}
	}
}