using Artanim.Algebra;
using Artanim.Location.Data;
using Artanim.Tracking;
using Artanim.Location.Network.Tracking;
using Artanim.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(AvatarController))]
	[RequireComponent(typeof(IKListener))]
	public class SkeletonStreamer : MonoBehaviour
	{
		ITrackingFramePublisher _networkPublisher;
		CapturedFrameInfo _frameInfo = new CapturedFrameInfo();
		SkeletonTransforms[] _skeletonTransforms = new[] { new SkeletonTransforms() };

		private AvatarController _AvatarController;
		private AvatarController AvatarController
		{
			get
			{
				if (!_AvatarController)
					_AvatarController = GetComponent<AvatarController>();
				return _AvatarController;
			}
		}

		private IKListener _IKListener;
		private IKListener IKListener
		{
			get
			{
				if (!_IKListener)
					_IKListener = GetComponent<IKListener>();
				return _IKListener;
			}
		}

		void Start()
		{
			//Only stream our own skeleton
			enabled = AvatarController.IsMainPlayer;

			//Update skeleton
			_skeletonTransforms[0].SkeletonId = AvatarController.SkeletonId;
			_skeletonTransforms[0].HasRollBones = false;
		}

		void OnDisable()
        {
			if (_networkPublisher != null)
            {
				_networkPublisher.Dispose();
				_networkPublisher = null;
			}
        }

		void LateUpdate()
		{
			if (_networkPublisher == null)
			{
				_networkPublisher = new RemotePublisher(Location.Helpers.NetworkSetup.NetBus);
			}

			//Update frame info
			_frameInfo.FrameNumber++;
			_frameInfo.Time = System.DateTime.UtcNow;

			//Update root
			_skeletonTransforms[0].PlayerScale = AvatarController.AvatarAnimator.transform.localScale.ToVect3f();
			_skeletonTransforms[0].RootPosition = AvatarController.AvatarAnimator.transform.localPosition.ToVect3f();
			_skeletonTransforms[0].RootRotation = AvatarController.AvatarAnimator.transform.localRotation.ToQuatf();

			//Update skeleton
			UpdateSkeletonTransforms(AvatarController.AvatarAnimator, ref _skeletonTransforms[0]);

			_networkPublisher.Send(_frameInfo, null, _skeletonTransforms);
		}

		private void UpdateSkeletonTransforms(Animator avatarAnimator, ref SkeletonTransforms skeleton)
		{
			//Update skeleton
			skeleton.SkeletonId = AvatarController.SkeletonId;
			skeleton.HasRollBones = false;

			//Update root
			skeleton.PlayerScale = avatarAnimator.transform.localScale.ToVect3f();
			skeleton.RootPosition = avatarAnimator.transform.localPosition.ToVect3f();
			skeleton.RootRotation = avatarAnimator.transform.localRotation.ToQuatf();

			// Prepare bones list
			int nBones = IKUpdateBones.Rotation.Length;
			skeleton.BoneRotations = ArrayUtils.Resize(skeleton.BoneRotations, nBones);
			skeleton.BonePositions = ArrayUtils.Resize(skeleton.BonePositions, IKUpdateBones.Position.Length);

			// Copy bones transforms
			for (int boneRotIndex = 0; boneRotIndex < nBones; ++boneRotIndex)
			{
				var boneType = IKUpdateBones.Rotation[boneRotIndex];
				var boneTransform = IKListener.GetBoneTransform(boneType);

				//Add local bone rotation
				var rot = null == boneTransform ? new Quatf() : boneTransform.localRotation.ToQuatf();
				skeleton.BoneRotations[boneRotIndex] = rot;

				//Add position too?
				int bonePosIndex = System.Array.IndexOf(IKUpdateBones.Position, boneType);
				if (bonePosIndex >= 0)
				{
					var pos = null == boneTransform ? new Vect3f() : boneTransform.localPosition.ToVect3f();
					skeleton.BonePositions[bonePosIndex] = pos;
				}
			}
		}
	}
}