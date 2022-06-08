using Artanim.HandAnimation.Config;
using Artanim.Location.Data;
using Artanim.Location.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation
{
	/// <summary>
	/// Central manager for (possibly) a variety of hand animation sources.
	/// Various hand animation sources, such as tracking by a Leap Motion or
	/// procedural animation (with a HandController) for example, when combined
	/// suffer from from order of execution issues, as well as all wanting to 
	/// modify the same "scenegraph", sometimes assuming an unmodified graph at
	/// their start. To address this, the HandAnimationManager does the following:
	/// - It manages HandAnimators, and triggers their update as a part of its 
	///   LateUpdate Unity callback. 
	/// - It makes sure to reset the scenegraph to the appropriate initial start 
	///   pose before the call of each HandAnimator. 
	/// - It assumes responsibility for setting the final (possibly combined) hand 
	///   animation state on the scenegraph. 
	/// </summary>
	public class HandAnimationManager : MonoBehaviour
	{
		public AvatarHandDefinition.ESide Handedness;

		public TrackingHandAnimator TrackedAnimator;
		public ProceduralHandAnimator ProceduralAnimator;

		private AvatarHandDefinition _AvatarHandDefinition;
		public AvatarHandDefinition AvatarHandDefinition
		{
			get{ return _AvatarHandDefinition; }
			set{ _AvatarHandDefinition = value; }
		}
			
		[Header("Debug options")]
		[Tooltip("When disabled, the avatar is not checked to see if it's the main player. Particularly useful when testing in-editor. Normally should be set to true")]
		public bool ApplyToMainPlayerOnly = true;

		public Action OnPreHandAnimationUpdate; //Hook for anything that needs to happen before hand animation has been applied
		public Action OnPostHandAnimationUpdate; //Hook for anything that needs to happen after hand animation has been applied

		private AvatarController _AvatarController;
		private Animator _AvatarAnimator;
		private HandAnimationData _RestPoseData;
		private HandAnimationData _PreviousPoseData;
		private List<Transform> _HandAnimationTransforms;
		private Quaternion _RestTwist;

		private bool _WristIsTracked = true;

		private void Start()
		{
#if !UNITY_EDITOR
            if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				enabled = false;
				return;
			}
#endif

            _AvatarController = GetComponentInParent<AvatarController>();
			if (_AvatarController == null)
			{
				Debug.LogError("[HandAnimationManager] No AvatarController found in the parent hierarchy of this hand. Make sure the hand is part of an avatar with an AvatarController.");
				enabled = false;
				return;
			}

			_AvatarAnimator = _AvatarController.AvatarAnimator;
			if (_AvatarAnimator == null)
			{
				Debug.LogError("[HandAnimationManager] No Animator found in the parent hierarchy of this hand. Make sure the hand is part of an avatar with an Animator.");
				enabled = false;
				return;
			}

			_HandAnimationTransforms = new List<Transform>();
			var bones = (Handedness == AvatarHandDefinition.ESide.Left) ? HandUpdateBones.LeftHandBones : HandUpdateBones.RightHandBones;

			foreach (var index in bones)
			{
				_HandAnimationTransforms.Add(_AvatarAnimator.GetBoneTransform((HumanBodyBones)index));
			}

			if (TrackedAnimator != null)
			{
				TrackedAnimator.Initialize(this);
				TrackedAnimator.SetHandAnimationDataTransforms(_HandAnimationTransforms);
			}
			if (ProceduralAnimator != null)
			{
				ProceduralAnimator.Initialize(this);
				ProceduralAnimator.SetHandAnimationDataTransforms(_HandAnimationTransforms);
			}

			_RestPoseData = new HandAnimationData();
			_PreviousPoseData = new HandAnimationData();
			foreach (var handTransforms in _HandAnimationTransforms)
			{
				_RestPoseData.Rotations.Add(handTransforms.localRotation);
				_PreviousPoseData.Rotations.Add(handTransforms.localRotation);
			}

			Quaternion swing;
			MathUtils.DecomposeSwingTwist(transform.localRotation, AvatarHandDefinition.TwistAxis, out swing, out _RestTwist);

			_WristIsTracked = (ConfigService.Instance.ExperienceSettings.HandTrackerPosition == ExperienceSettingsSO.EHandTrackerPosition.Hand);
		}

        private void LateUpdate()
		{
            //isActiveAndEnabled test because for unknown reasons Unity calls LateUpdate once after start, event if its disabled!
            if (!isActiveAndEnabled || (ApplyToMainPlayerOnly && !_AvatarController.IsMainPlayer)) 
			{
				return;
			}

			if (OnPreHandAnimationUpdate != null)
			{
				OnPreHandAnimationUpdate();
			}

			HandAnimationData data = _RestPoseData;

			Quaternion wristRotation = transform.localRotation;

			if (_WristIsTracked)
			{
				data.Rotations[0] = wristRotation;
			}
			else
			{
				if (AvatarHandDefinition.ExaggerateTwist)
				{
					ExaggerateWristTwist();
				}
			}

			ResetHand(wristRotation);

			bool updatedHandAnimation = false;
			bool isInteracting = false;
			if (ProceduralAnimator != null)
			{
				Procedural.InteractionGeometryDescription description = Procedural.InteractionGeometryDescription.None;
				isInteracting = ProceduralAnimator.UpdateInteractionPoints(out description);

				//If we're interacting with any geometry, prefer the procedural data over the tracked data
				if (isInteracting)
				{
					ProceduralAnimator.SetPreviousPose(_PreviousPoseData);
					data = ProceduralAnimator.UpdateHandAnimation();
					updatedHandAnimation = true;
				}
				else
				{
					if (TrackedAnimator != null && TrackedAnimator.IsActive())
					{
						data = TrackedAnimator.UpdateHandAnimation();
						updatedHandAnimation = true;
					}
				}
			}
			else if(TrackedAnimator != null && TrackedAnimator.IsActive())
			{
				data = TrackedAnimator.UpdateHandAnimation();
				updatedHandAnimation = true;
			}

			if(!updatedHandAnimation)
			{
				//Neither the tracked animator nor the procedural animator gave us data
				//Make sure to set the wrist rotation in the data
#if !UNITY_EDITOR
				data.Rotations[0] = wristRotation;
#endif
			}

			ApplyHandAnimationData(data);

			if (OnPostHandAnimationUpdate != null)
			{
				OnPostHandAnimationUpdate();
			}
		}

		private void ApplyHandAnimationData(HandAnimationData data)
		{
			if (AvatarHandDefinition.ApplySmoothing)
			{
				for (int i = 0; i < _HandAnimationTransforms.Count; ++i)
				{
					var rotation = Quaternion.Slerp(_PreviousPoseData.Rotations[i], data.Rotations[i], AvatarHandDefinition.SmoothingFactor);
					_HandAnimationTransforms[i].localRotation = rotation;
					_PreviousPoseData.Rotations[i] = rotation;
				}
			}
			else
			{
				for (int i = 0; i < _HandAnimationTransforms.Count; ++i)
				{
					_HandAnimationTransforms[i].localRotation = data.Rotations[i];
					_PreviousPoseData = data;
				}
			}
		}

		private void ResetHand()
		{
			for (int i = 0; i < _HandAnimationTransforms.Count; ++i)
			{
				_HandAnimationTransforms[i].localRotation = _RestPoseData.Rotations[i];
			}
		}

		/// <summary>
		/// Reset hand transform orientations to their rest pose, except for the wrist.
		/// We generally want to apply hand animation data with respect to Vicon tracking
		/// data, instead of fully overriding it.
		/// </summary>
		/// <param name="wristRotation"></param>
		private void ResetHand(Quaternion wristRotation)
		{
#if UNITY_EDITOR
			//We don't have vicon data in the editor to reset the wrist, so reset it from our own data instead
			_HandAnimationTransforms[0].localRotation = _RestPoseData.Rotations[0];
#else
			_HandAnimationTransforms[0].localRotation = wristRotation;
#endif

			for (int i = 1; i < _HandAnimationTransforms.Count; ++i)
			{
				_HandAnimationTransforms[i].localRotation = _RestPoseData.Rotations[i];
			}
		}

		private void ExaggerateWristTwist()
		{
			Quaternion swing;
			Quaternion twist;

			MathUtils.DecomposeSwingTwist(transform.localRotation, AvatarHandDefinition.TwistAxis, out swing, out twist);

			Quaternion exaggeratedTwist = Quaternion.SlerpUnclamped(_RestTwist, twist, AvatarHandDefinition.ExaggerationFactor);
			Quaternion exaggeratedOrientation = swing * exaggeratedTwist;

			transform.localRotation = exaggeratedOrientation;
		}
	}
}