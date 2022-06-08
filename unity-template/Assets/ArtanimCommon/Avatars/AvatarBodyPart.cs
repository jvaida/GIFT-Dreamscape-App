using Artanim.Location.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(Rigidbody))]
	public class AvatarBodyPart : MonoBehaviour
	{
		public EAvatarBodyPart BodyPart;

		void Start()
		{
			//Validate collider
			var collider = GetComponent<Collider>();
			if(!collider)
			{
				Debug.LogWarningFormat("No collider found for body part {0} on object {1}. Add a collider to this GameObject.", BodyPart.ToString(), name);
			}
			else
			{
				//Validate if its trigger
				if(!collider.isTrigger)
				{
					Debug.LogWarningFormat("The collider found for body part {0} on object {1} is not a trigger.", BodyPart.ToString(), name);
				}
			}


			//Validate Rigidbody
			var rigidBody = GetComponent<Rigidbody>();
			if(!rigidBody.isKinematic)
			{
				Debug.LogWarningFormat("The Rigidbody on body part {0} on object {1} is not Kinematic. It should be kinematic unless you know what you're doing.", BodyPart.ToString(), name);
			}
		}
	}

}