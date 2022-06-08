using Artanim.Algebra;
using Artanim.Location.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Tracking
{

	public class IKListener : MonoBehaviour
	{
		public Guid SkeletonId { get; private set; }
		public bool IsMainPlayer { get; private set; }

		public Animator AvatarAnimator;
		public AdditionalBone[] AdditionalBones;

		public void Init(Guid skeletonId, bool isMainPlayer)
		{
			SkeletonId = skeletonId;
			IsMainPlayer = isMainPlayer;
		}

		public void UpdateSkeleton(SkeletonTransforms skeleton)
		{
			// Avatar might disable the IKListener if it's controlling its skeleton
			if (!enabled)
            {
				return;
            }

			bool globalSpace = ConfigService.Instance.Config.Location.IKServer.Streaming.GlobalSpace;

			//Update scale
			AvatarAnimator.transform.localScale = skeleton.PlayerScale.ToUnity();

			//Update root
			AvatarAnimator.transform.localPosition = skeleton.RootPosition.ToUnity();
			AvatarAnimator.transform.localRotation = skeleton.RootRotation.ToUnity();

			//Update bones
			for (int boneRotIndex = 0, numBones = IKUpdateBones.Rotation.Length; boneRotIndex < numBones; ++boneRotIndex)
			{
				var boneRot = skeleton.BoneRotations[boneRotIndex];

				// We might have "dummy bones" from the server (with <0 index).
				var boneType = IKUpdateBones.Rotation[boneRotIndex];

				var boneTransform = GetBoneTransform(boneType);
				if (boneTransform != null)
				{
					//Update position?
					int bonePosIndex;
					int nBones = IKUpdateBones.Position.Length;
					for (bonePosIndex = 0; bonePosIndex < nBones; bonePosIndex++) // Array.Index() make some allocation so we do a manual search
					{
						if (IKUpdateBones.Position[bonePosIndex] == boneType) break;
					}
					if (bonePosIndex < nBones)
					{
						Vector3 pos = skeleton.BonePositions[bonePosIndex].ToUnity();
						if (globalSpace) boneTransform.position = pos; else boneTransform.localPosition = pos;
					}

					//Update rotation
					Quaternion rot = boneRot.ToUnity();
					if (globalSpace) boneTransform.rotation = rot; else boneTransform.localRotation = rot;
				}
				else if (skeleton.HasRollBones || (!IKUpdateBones.IsRollBone(boneType)))
				{
					Debug.LogWarningFormat("No transform received for bone: {0}", boneType.ToString());
				}
			}
		}

		public Transform GetBoneTransform(ERigBones bone)
		{
			//Is it a Mecanim bone?
			if((int)bone <= 60)
			{
				//Mecanim bone, read from animator
				return AvatarAnimator.GetBoneTransform((HumanBodyBones)bone);
			}
			else if(AdditionalBones != null)
			{
				//Additional bone, read from properties
				for (int i = 0, iMax = AdditionalBones.Length; i < iMax; ++i)
				{
					var addBone = AdditionalBones[i];
					if ((addBone != null) && (addBone.Bone == bone))
					{
						return addBone.BoneTransform;
					}
				}
			}

			return null;
		}
	}

}