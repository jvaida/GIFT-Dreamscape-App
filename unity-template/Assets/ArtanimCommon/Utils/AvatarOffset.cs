using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/AvatarOffset")]
	public class AvatarOffset : MonoBehaviour
	{
		public string ObjectId;

		private void OnDestroy()
		{
			if(AvatarOffsetController.Instance)
			{
				AvatarOffsetController.Instance.UnregisterAvatarOffset(this);
			}
		}
	}
}