using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	public class HandAnimationColliderManager : SingletonBehaviour<HandAnimationColliderManager>
	{
		[HideInInspector]
		public List<HandAnimationCollider> HandAnimationColliders = new List<HandAnimationCollider>();

		public void RegisterCollider(HandAnimationCollider collider)
		{
			if(!HandAnimationColliders.Contains(collider))
			{
				HandAnimationColliders.Add(collider);
			}
		}

		public void RemoveCollider(HandAnimationCollider collider)
		{
			HandAnimationColliders.Remove(collider);
		}

	}
}