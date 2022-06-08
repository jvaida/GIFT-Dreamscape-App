using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/Follow AvatarOffset")]
	public class FollowAvatarOffset : MonoBehaviour
	{
		[Tooltip("This is just for debugging. Register the offset to follow using the SetPlayer function.")]
		public string PlayerId;

		private Transform AvatarOffset;

		public void SetPlayer(Guid playerId)
		{
			if(playerId != Guid.Empty)
			{
				var player = GameController.Instance.GetPlayerByPlayerId(playerId);
				AvatarOffset = player != null ? player.AvatarOffset : null;
			}
			else
			{
				AvatarOffset = null;
			}

			PlayerId = AvatarOffset ? playerId.ToString() : "";
		}

		void LateUpdate()
		{
			transform.localPosition = AvatarOffset ? AvatarOffset.position : Vector3.zero;
			transform.localRotation = AvatarOffset ? AvatarOffset.rotation : Quaternion.identity;
		}
	}
}