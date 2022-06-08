using Artanim.HandAnimation.Procedural;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation
{
	public class HandAnimationData
	{
		public List<Quaternion> Rotations; //generally the rotations of bones defined in HandUpdateBones.LeftHandBones or RightHandBones

		public HandAnimationData(int capacity = 16)
		{
			Rotations = new List<Quaternion>(capacity); //Create list with a capacity to stop some of the list copies we saw going on
		}
	}

	public abstract class HandAnimator : MonoBehaviour
	{
		public abstract void Initialize(HandAnimationManager handAnimationManager);

		/// <summary>
		/// Whether or not the HandAnimator is currently active and providing 
		/// HandAnimationData. If for example a tracker is not currently
		/// tracking a hand, this would return false.
		/// </summary>
		/// <returns>Whether or not the HandAnimator is currently providing new data</returns>
		public abstract bool IsActive();

		public abstract void SetHandAnimationDataTransforms(List<Transform> transformsForHandAnimationData);

		public abstract HandAnimationData UpdateHandAnimation();
	}

	public abstract class TrackingHandAnimator : HandAnimator
	{

	}

	public abstract class ProceduralHandAnimator : HandAnimator
	{
		public abstract void SetPreviousPose(HandAnimationData previousPose);

		public abstract bool UpdateInteractionPoints(out InteractionGeometryDescription description);
	}
}