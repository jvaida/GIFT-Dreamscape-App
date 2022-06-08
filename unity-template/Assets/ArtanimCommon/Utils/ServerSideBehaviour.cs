using UnityEngine;
using System.Collections;
using Artanim.Location.Network;

namespace Artanim
{

	/// <summary>
	/// Abstract MonoBehaviour using Awake to enable/disable itself based on the component type of the application.
	/// Standalone mode is considered as being both server and client, so the MonoBehaviour won't be disabled
	/// except if DisableInStandalone is true.
	/// </summary>
	public abstract class ServerSideBehaviour : MonoBehaviour
	{
		[SerializeField]
		bool DisableInStandalone = false;

		virtual protected void Awake()
		{
			enabled = NetworkInterface.Instance.IsServer
				&& (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone || !DisableInStandalone);
		}
	}

}