using Leap;
using Artanim.Location.Data;
using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;
using Artanim.Location.Network;

namespace Artanim.HandAnimation.Leap
{
	/// <summary>
	/// A HandAnimator and a controll class for a Leap Motion RiggedHand.
	/// It provides for hand offset calibration, and ensures hand tracking can
	/// be used with avatars, instead of the free floating hands the Leap
	/// implementation was initially meant for. 
	/// Hand tracking can be filtered/ignored based on a confidence threshold
	/// cutoff. 
	/// </summary>
	public class ArtanimRiggedHand : TrackingHandAnimator
	{
		[Tooltip("A Leap RiggedHand we want to update, if our tracking data has a confidence value above the specified threshold.")]
		public RiggedHand RiggedHand;

		//Whether or not the hand is currently being tracked by Leap
		//Note that this differs from IsTracked, as provided by Leap in that we
		//additionally factor in the confidence threshold. 
		//So IsTracked can be true, while IsTrackedWithConfidence is not.
		public bool IsTrackedWithConfidence
		{
			get
			{
				return _IsTrackedWithConfidence;
			}
		}
		private bool _IsTrackedWithConfidence = false;

		public float CurrentTrackingConfidence
		{
			get
			{
				return _CurrentTrackingConfidence;
			}
		}
		private float _CurrentTrackingConfidence;

		public Chirality Handedness
		{
			get
			{
				if(RiggedHand != null)
				{
					return RiggedHand.Handedness;
				}
				return Chirality.Left;
			}
		}

		private HandAnimationManager _HandAnimationManager;
		private AvatarController _AvatarController;

		private bool _HasBeenCalibratedOnce = false;
		public bool HasBeenCalibratedOnce
		{
			get
			{
				return _HasBeenCalibratedOnce;
			}
		}

		private bool _HandRegistrationAttempted = false;

		private List<Transform> _HandAnimationDataTransforms;

		public override void Initialize(HandAnimationManager handAnimationManager)
		{
			_HandAnimationManager = handAnimationManager;
			if (RiggedHand.Handedness == Chirality.Right)
			{
				_HandAnimationManager.OnPreHandAnimationUpdate += PreHandAnimationUpdate;
			}
		}

		private void PreHandAnimationUpdate()
		{

		}

		public void Awake()
		{
			if (NetworkInterface.Instance == null || NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				enabled = false;
				return;
			}
		}

		public void Update()
		{
			if (!_HandRegistrationAttempted)
			{
				_AvatarController = GetComponentInParent<AvatarController>();

				if (_AvatarController != null && _AvatarController.IsMainPlayer)
				{
					if (ArtanimHandModelmanager.Instance != null)
					{
						ArtanimHandModelmanager.Instance.RegisterHandModel(this);
					}
					else
					{
						Debug.LogError("[ArtanimRiggedHand] There is no ArtanimHandModelmanager in the scene. Please make sure to add one");
					}
				}
				_HandRegistrationAttempted = true;
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Force Hand Registration (Editor Shortcut)")]
		private void ForceHandRegistrationEditor()
		{
			if (ArtanimHandModelmanager.Instance != null)
			{
				ArtanimHandModelmanager.Instance.RegisterHandModel(this);
			}
		}
#endif 

		public void OnDisable()
		{
			if (_AvatarController != null && _AvatarController.IsMainPlayer)
			{
				_HandRegistrationAttempted = false;
			}
		}

		public void SetLeapHand(Hand hand)
		{
			_CurrentTrackingConfidence = hand.Confidence;
			_IsTrackedWithConfidence = (_CurrentTrackingConfidence >= ArtanimHandModelmanager.Instance.LeapConfig.ConfidenceThreshold);

			if (_IsTrackedWithConfidence)
			{
				RiggedHand.SetLeapHand(hand);
			}
		}

		public void UpdateHand()
		{
			Vector3 palmPosition = RiggedHand.palm.position;

			if (RiggedHand.palm != null)
			{
				RiggedHand.palm.position = RiggedHand.GetWristPosition();
				RiggedHand.palm.rotation = RiggedHand.GetRiggedPalmRotation() * RiggedHand.Reorientation();
			}

			for (int i = 0; i < RiggedHand.fingers.Length; ++i)
			{
				if (RiggedHand.fingers[i] != null)
				{
					RiggedHand.fingers[i].fingerType = (Finger.FingerType)i;
					RiggedHand.fingers[i].UpdateFinger();
				}
			}

			if (RiggedHand.palm != null)
			{
				RiggedHand.palm.position = palmPosition; //cancel out palm positioning. 
				RiggedHand.palm.rotation = (RiggedHand.GetRiggedPalmRotation() * RiggedHand.Reorientation());
			}
		}

		public void SetHandTrackingLost()
		{
			_IsTrackedWithConfidence = false;
		}

		public override bool IsActive()
		{
			return _IsTrackedWithConfidence;
		}

		public override HandAnimationData UpdateHandAnimation()
		{
			UpdateHand();
			HandAnimationData data = new HandAnimationData();

			foreach(var transform in _HandAnimationDataTransforms)
			{
				data.Rotations.Add(transform.localRotation);
			}

			return data;
		}

		public override void SetHandAnimationDataTransforms(List<Transform> handAnimationDataTransforms)
		{
			_HandAnimationDataTransforms = handAnimationDataTransforms;
		}
	}
}