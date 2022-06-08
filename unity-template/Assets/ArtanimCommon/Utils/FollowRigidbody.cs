using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artanim.Tracking;

namespace Artanim
{
	[AddComponentMenu("Artanim/Follow Rigidbody")]
	public class FollowRigidbody : MonoBehaviour
	{
		[SerializeField][Tooltip("The rigidbody to follow")]
		TrackingRigidbody HeadRigidBody = null;

		[SerializeField][Tooltip("Whether or not to update the position of this game object to match the one of the rigidbody")]
		bool UpdatePosition = false;

		[SerializeField][Tooltip("Whether or not to update the rotation of this game object to match the one of the rigidbody")]
		bool UpdateRotation = false;

		// Update is called once per frame
		void Update()
		{
			// Update position if required
			if (HeadRigidBody && UpdatePosition)
				transform.localPosition = HeadRigidBody.RigidbodyPosition;
			// Update rotation if required
			if (HeadRigidBody && UpdateRotation)
				transform.localRotation = HeadRigidBody.RigidbodyRotation;
		}
	}
}