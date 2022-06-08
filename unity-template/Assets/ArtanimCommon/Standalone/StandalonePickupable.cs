using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public class StandalonePickupable : MonoBehaviour
	{
		private const string COLLIDER_NAME = "StandalonePickupableCollider";

		public Transform RegisteredParent { get; private set; }

		private Vector3 StartParentPosition;
		private Vector3 StartPickupPosition;
		private Quaternion StartParentRotation;
		private Quaternion StartPickupRotation;
		
		private void Start()
		{
			//Create hand trigger collider
			CreatePickupableCollider(transform);
		}

		private void LateUpdate()
		{
			//Update position
			if (RegisteredParent)
			{
				var parentMatrix = Matrix4x4.TRS(
						RegisteredParent.position,
						RegisteredParent.rotation * Quaternion.Inverse(StartParentRotation),
						RegisteredParent.lossyScale);

				transform.position = parentMatrix.MultiplyPoint3x4(StartPickupPosition - StartParentPosition);
				transform.rotation = (RegisteredParent.rotation * Quaternion.Inverse(StartParentRotation)) * StartPickupRotation;
			}
		}

		public void RegisterParent(Transform newParent)
		{
			RegisteredParent = newParent;
			StartParentPosition = RegisteredParent.position;
			StartPickupPosition = transform.position;
			StartParentRotation = RegisteredParent.rotation;
			StartPickupRotation = transform.rotation;
		}

		public void UnregisterParent()
		{
			RegisteredParent = null;
		}

		private bool IsHand(Collider other)
		{
			if(other)
			{
				var bodyPart = other.GetComponent<AvatarBodyPart>();
				return bodyPart && (bodyPart.BodyPart == Location.Messages.EAvatarBodyPart.LeftHand || bodyPart.BodyPart == Location.Messages.EAvatarBodyPart.RightHand);
			}
			return false;
		}

		private BoxCollider CreatePickupableCollider(Transform root)
		{
			var colliderChild = root.Find(COLLIDER_NAME);
			if(!colliderChild)
			{
				//Create child transform
				var newChild = new GameObject(COLLIDER_NAME);
				newChild.transform.parent = root;

				//Adjust transform to match inversed parent
				newChild.transform.localPosition = Vector3.zero;
                newChild.transform.localRotation = Quaternion.Inverse(root.rotation);

				//Reverse parent scale in case there is a scale on it
				var scale = Vector3.one;
				scale.x /= root.transform.localScale.x;
				scale.y /= root.transform.localScale.y;
				scale.z /= root.transform.localScale.z;
				newChild.transform.localScale = scale;

				//Create collider
				var collider = newChild.AddComponent<BoxCollider>();
				collider.isTrigger = true;

				var bounds = UnityUtils.HierarchyBounds(root);
				collider.center = (bounds.center - root.position);
				collider.size = bounds.size;

				return collider;
			}
			else
			{
				return colliderChild.GetComponent<BoxCollider>();
			}
		}
	}
}