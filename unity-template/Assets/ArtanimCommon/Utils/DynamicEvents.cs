using Artanim.Location.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Artanim
{

	[Serializable]
	public class PlayerEvent : UnityEvent<Player> { }

	[Serializable]
	public class AvatarEvent : UnityEvent<AvatarController> { }

	[Serializable]
	public class IntEvent : UnityEvent<int> { }

	[Serializable]
	public class ClientToServerEvent : UnityEvent<string, AvatarController> { }

}