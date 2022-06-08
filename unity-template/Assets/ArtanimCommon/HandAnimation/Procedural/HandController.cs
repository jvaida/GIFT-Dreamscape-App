using Artanim.HandAnimation.Config;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artanim.HandAnimation.Procedural
{
	public enum EHandInteractionType
	{
		None,
		Grasp,
		Touch
	}

	public class HandController : ProceduralHandAnimator
	{

		#region Accessor properties
		private AvatarController _AvatarController;
		private AvatarController AvatarController
		{
			get
			{
				if (_AvatarController == null)
				{
					_AvatarController = GetComponentInParent<AvatarController>();
				}
				return _AvatarController;
			}
		}

		private AvatarHandController _AvatarHandController;
		private AvatarHandController AvatarHandController
		{
			get
			{
				if(_AvatarHandController == null)
				{
					_AvatarHandController = GetComponentInParent<AvatarHandController>();
				}
				return _AvatarHandController;
			}
		}

		private AvatarHandDefinition _HandDefinition;
		private AvatarHandDefinition HandDefinition
		{
			get
			{
				if(_HandDefinition == null)
				{
					_HandDefinition = AvatarHandController.HandDefinition;
				}
				return _HandDefinition;
			}
		}

		private HandSetup _HandSetup;
		private HandSetup HandSetup
		{
			get
			{
				if(_HandSetup == null)
				{
					_HandSetup = HandDefinition.GetHandSetup(Side);
				}

				return _HandSetup;
			}
		}
		#endregion

		public AvatarHandDefinition.ESide Side;

		private FingerController[] _FingerControllers;

	    private Quaternion _FrameStartOrientation;

		private List<IntersectionInfo> _IntersectionPoints = new List<IntersectionInfo>();
		private HandAnimationCollider _NearestIntersectionMesh;

		private List<Transform> _HandAnimationDataTransforms;

		private int _LastActiveFrame;
		private float _InterpolationStartTime;
		private HandAnimationData _StartPoseAnimationData;
		private EHandInteractionType _CurrentInteractionType;

		private bool _SolveWrist = true;

		private Transform _ThumbRoot;

		private bool _IsInitialized = false;

		public override void Initialize(HandAnimationManager handAnimationManager)
		{
			//Intentionally empty for now
		}

		public void Start()
        {
			//Only procedurally animate the wrist if the cluster is not on the hand
			_SolveWrist = (ConfigService.Instance.ExperienceSettings.HandTrackerPosition == ExperienceSettingsSO.EHandTrackerPosition.Wrist);

			//Get finger roots so we can get all their joints in the FingerControllers 
			var animator = AvatarController.AvatarAnimator;
			var isLeft = (Side == AvatarHandDefinition.ESide.Left);
			Transform pinkyRoot = isLeft ? animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal) : animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
			Transform ringRoot = isLeft ? animator.GetBoneTransform(HumanBodyBones.LeftRingProximal) : animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
			Transform middleRoot = isLeft ? animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) : animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
			Transform indexRoot = isLeft ? animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal) : animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
			_ThumbRoot = isLeft ? animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal) : animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);

			_FingerControllers = new FingerController[]
			{
				new FingerController(FingerType.Pinky, HandSetup.Pinky, pinkyRoot),
				new FingerController(FingerType.Ring, HandSetup.Ring, ringRoot),
				new FingerController(FingerType.Middle, HandSetup.Middle, middleRoot),
				new FingerController(FingerType.Index, HandSetup.Index, indexRoot),
				new FingerController(FingerType.Thumb, HandSetup.Thumb, _ThumbRoot)
			};

			for (int i = 0; i < _FingerControllers.Length - 2; i++)
			{
                _FingerControllers[i + 1].PrevController = _FingerControllers[i];
                _FingerControllers[i].NextController = _FingerControllers[i + 1];
            }

	        foreach (var fingerController in _FingerControllers)
	        {
				fingerController.SetRestPose();
			}

			_IsInitialized = true;
        }

		public override bool UpdateInteractionPoints(out InteractionGeometryDescription description)
		{
			description = InteractionGeometryDescription.None;

			_FrameStartOrientation = transform.localRotation;
			if (_IsInitialized && HandAnimationColliderManager.HasInstance)
			{
				//Handle Wrist
				var descriptionResult = UpdateWristIntersectionPoints(HandAnimationColliderManager.Instance.HandAnimationColliders);
				if(_IntersectionPoints.Count > 0)
				{
					description = descriptionResult;
					return true;
				}
			}

			return false;
		}

		private void UpdateHand()
		{
			_FrameStartOrientation = transform.localRotation;
			if (_IntersectionPoints.Count != 0)
			{
				ApplyInteractionType(_CurrentInteractionType);

				if (_SolveWrist)
				{
					SolveWristExtensionFlexion();
					SolveWristDeviation();
				}

				//Handle Fingers
				foreach (var fingerController in _FingerControllers)
				{
					fingerController.UpdateFingerIntersectionPoints(HandAnimationColliderManager.Instance.HandAnimationColliders);
				}
				UpdateFingerPoses();
			}
		}

		public void ResetPose()
        {
			foreach(var fingerController in _FingerControllers)
            {
                fingerController.ResetPose();
                fingerController.UpdatePose();
            }
        }

        private void UpdateFingerPoses()
        {
			foreach (var fingerController in _FingerControllers)
			{
				fingerController.ResetPose();
				fingerController.SolveJointsRotation(fingerController.FingerType == FingerType.Thumb);
			}

			foreach (var fingerController in _FingerControllers)
	        {
		        if (!fingerController.HasChanged)
		        {
			        fingerController.FollowNeighbors(0.6f);
		        }
			}

	        foreach (var fingerController in _FingerControllers)
	        {
		        if (!fingerController.HasChanged)
		        {
			        fingerController.FollowNeighbors(0.6f);
		        }
	        }
		}

		public override bool IsActive()
		{
			return true;
		}

		public override HandAnimationData UpdateHandAnimation()
		{
			UpdateHand();
			HandAnimationData data = new HandAnimationData();

			foreach (var transform in _HandAnimationDataTransforms)
			{
				data.Rotations.Add(transform.localRotation);
			}

			float elapsedTime = Time.realtimeSinceStartup - _InterpolationStartTime;
			if (elapsedTime > HandDefinition.StartInterpolationDuration)
			{
				return data;
			}
			else
			{
				HandAnimationData interpolatedData = new HandAnimationData();
				float timeLeft = HandDefinition.StartInterpolationDuration - elapsedTime;
				float normalizedTime = 1.0f - (timeLeft / HandDefinition.StartInterpolationDuration);

				for (int i = 0; i < data.Rotations.Count; ++i)
				{
					interpolatedData.Rotations.Add(Quaternion.Slerp(_StartPoseAnimationData.Rotations[i], data.Rotations[i], normalizedTime));
				}

				return interpolatedData;
			}

		}

		public override void SetHandAnimationDataTransforms(List<Transform> handAnimationDataTransforms)
		{
			_HandAnimationDataTransforms = handAnimationDataTransforms;
		}

		public override void SetPreviousPose(HandAnimationData previousPose)
		{
			if(_LastActiveFrame < Time.frameCount - 1)
			{
				_StartPoseAnimationData = previousPose;
				_InterpolationStartTime = Time.realtimeSinceStartup;
			}
			_LastActiveFrame = Time.frameCount;
		}

		#region Intersection testing

		public InteractionGeometryDescription UpdateWristIntersectionPoints(List<HandAnimationCollider> geometryControllers)
		{
			_IntersectionPoints.Clear();
			_NearestIntersectionMesh = null;
			_CurrentInteractionType = EHandInteractionType.None;

			Vector3 position = transform.position;
			Vector3 normal = transform.TransformVector(HandSetup.RotationAxis.normalized);
			Vector3 palmPosition = position + 0.33f * HandDefinition.Reach * transform.TransformDirection(HandSetup.HandForward);
			Vector3 down = transform.TransformDirection(HandSetup.PalmFacing);

			int currentStartIndex = 0;
			float nearestDistance = float.MaxValue;
			InteractionGeometryDescription interactionGeometryDescription = InteractionGeometryDescription.None;

			foreach (var mesh in geometryControllers)
			{
				bool foundCloserMesh = false;
				mesh.FindPlaneIntersectionPoints(_IntersectionPoints, position, normal, HandDefinition.Reach);

				//Filter out found intersections depending on their position with respect to the palm
				//We don't want the hand to reach too far down (giving a sticky effect), so we calculate
				//an effective range. The range is full for points at the finger tips, but lower for
				//points below the palm
				for(int intersectionIndex = currentStartIndex; intersectionIndex < _IntersectionPoints.Count;)
				{
					var intersection = _IntersectionPoints[intersectionIndex];
					float dot = Vector3.Dot(down, (intersection.Position - palmPosition).normalized);
					dot = Mathf.Clamp01(dot);
					float effectiveRange = HandDefinition.ReachStickiness * HandDefinition.Reach + (1.0f - dot) * (1.0f - HandDefinition.ReachStickiness) * HandDefinition.Reach;

					if (Vector3.Distance(palmPosition, intersection.Position) > effectiveRange)
					{
						_IntersectionPoints.RemoveAt(intersectionIndex);
					}
					else
					{
						++intersectionIndex;
					}
				}

				if(currentStartIndex == _IntersectionPoints.Count)
				{
					continue;
				}

				Vector3 averageNormal = Vector3.zero;
				for(int i = currentStartIndex; i < _IntersectionPoints.Count; ++i)
				{
					var intersectionPoint = _IntersectionPoints[i];
					float distance = Vector3.Distance(position, intersectionPoint.Position);

					//Add a weighting to the normals. Prioritize nearby normals in object interaction type determination
					float weight = 1.0f - (distance / HandDefinition.Reach);

					averageNormal += weight * intersectionPoint.Normal;

					if(distance < nearestDistance)
					{
						nearestDistance = distance;
						foundCloserMesh = true;
						if(intersectionPoint.Geometry != null)
						{
							_NearestIntersectionMesh = intersectionPoint.Geometry;
							interactionGeometryDescription = (InteractionGeometryDescription) intersectionPoint.Geometry.GeometryType;
						}
					}
				}

				if (foundCloserMesh)
				{
					averageNormal /= (_IntersectionPoints.Count - currentStartIndex);
					float magnitude = averageNormal.magnitude;
					if (magnitude < 0.3f)
					{
						_CurrentInteractionType = EHandInteractionType.Grasp;
					}
					else if (magnitude < 0.5)
					{
						_CurrentInteractionType = EHandInteractionType.None;
					}
					else
					{
						_CurrentInteractionType = EHandInteractionType.Touch;
					}

				}

				currentStartIndex = _IntersectionPoints.Count;
			}
			return interactionGeometryDescription;
		}

		private void ApplyInteractionType(EHandInteractionType interactionType)
		{
			if (interactionType == EHandInteractionType.Grasp)
			{
				_ThumbRoot.localRotation = HandSetup.Thumb.AbductedOrientation;
			}
			else if(interactionType == EHandInteractionType.Touch)
			{
				_ThumbRoot.localRotation = HandSetup.Thumb.AdductedOrientation;
			}
			else
			{
				_ThumbRoot.localRotation = HandSetup.Thumb.NeutralOrientation;
			}
		}

		private void SolveWristExtensionFlexion()
		{
			//Reset hand to extreme "open" position, and figure out from there
			//what the smallest angle is to resolve a potential touch within range
			transform.localRotation = _FrameStartOrientation * Quaternion.AngleAxis(HandDefinition.WristRotationRange.x, HandSetup.RotationAxis);

			Vector3 palmUpVector = transform.TransformDirection(-HandSetup.PalmFacing);
			Vector3 boneDirection = transform.TransformVector(HandSetup.HandForward);

			float smallestAngle = 180;

			float nearestIntersectionDistance = float.MaxValue;
			foreach (var intersection in _IntersectionPoints)
			{
				// joint to intersection point vector
				Vector3 candidateDirection = (intersection.Position - transform.position).normalized;
				Vector3 candidatePalmDirection = Vector3.Cross(transform.TransformDirection(HandSetup.RotationAxis), candidateDirection);

				//If our target direction is "above" our hand in the extreme position, ignore it
				//If the surface normal faces in the palm direction (i.e. we'd be touching with the top of our hand) ignore it as well
				if (Vector3.Dot(palmUpVector, candidateDirection) > 0.0f || Vector3.Dot(intersection.Normal, candidatePalmDirection) > 0.0f)
				{
					continue;
				}

				float offsetAngle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(boneDirection, candidateDirection));
				float angle = HandDefinition.WristRotationRange.x + offsetAngle;

				if (angle < smallestAngle)
				{
					smallestAngle = angle;
				}

				float distance = Vector3.Distance(transform.position, intersection.Position);
				if (distance < nearestIntersectionDistance)
				{
					nearestIntersectionDistance = distance;
				}
			}

			Quaternion targetRotation = _FrameStartOrientation * Quaternion.AngleAxis(HandDefinition.RestRotation, HandSetup.RotationAxis);

			if (smallestAngle < HandDefinition.WristRotationRange.y)
			{
				smallestAngle -= 5.0f; //Subtract minor angle to avoid interpenetration
				smallestAngle = Mathf.Clamp(smallestAngle, HandDefinition.WristRotationRange.x, HandDefinition.WristRotationRange.y);

				float halfReach = HandDefinition.Reach * 0.5f;
				float factor = 1.0f - ((nearestIntersectionDistance - halfReach) / halfReach);
				factor = Mathf.Clamp01(factor);

				targetRotation = Quaternion.Slerp(_FrameStartOrientation, _FrameStartOrientation * Quaternion.AngleAxis(smallestAngle, HandSetup.RotationAxis), factor);
			}

			transform.localRotation = targetRotation;

		}

		private void SolveWristDeviation()
		{
			if(_NearestIntersectionMesh == null)
			{
				return;
			}

			HandAlignmentEdge nearestEdge = null;
			float nearestEdgeDistance = float.MaxValue;
			foreach (var edge in _NearestIntersectionMesh.HandAlignmentEdges)
			{
				Transform trans = _NearestIntersectionMesh.transform;
				Matrix4x4 mat = Matrix4x4.TRS(trans.position, trans.rotation, Vector3.one);
				Vector3 vertexA = mat.MultiplyPoint(edge.VertexA);
				Vector3 vertexB = mat.MultiplyPoint(edge.VertexB);
				float distance = MathUtils.PointToSegmentDistance(transform.position, vertexA, vertexB);

				if (distance < HandDefinition.Reach && distance < nearestEdgeDistance)
				{
					nearestEdge = new HandAlignmentEdge()
					{
						VertexA = vertexA,
						VertexB = vertexB
					};
					nearestEdgeDistance = distance;
				}
			}

			if(nearestEdge != null)
			{
				Vector3 startLocal = transform.InverseTransformPoint(nearestEdge.VertexA);
				Vector3 endLocal = transform.InverseTransformPoint(nearestEdge.VertexB);
				Vector3 alignmentAxis = HandSetup.HandAlignmentAxis.normalized;

				Vector3 alignmentTargetVector = (endLocal - startLocal).normalized;

				alignmentTargetVector = Vector3.ProjectOnPlane(alignmentTargetVector, HandSetup.PalmFacing);

				alignmentTargetVector.Normalize();

				if (Vector3.Dot(alignmentTargetVector, alignmentAxis) < 0)
				{
					alignmentTargetVector = -alignmentTargetVector;
				}

				float angle = Vector3.SignedAngle(alignmentAxis, alignmentTargetVector, HandSetup.PalmFacing);

				angle = Mathf.Clamp(angle, HandSetup.DeviationAngleRange.x, HandSetup.DeviationAngleRange.y);
				transform.localRotation *= Quaternion.AngleAxis(angle, HandSetup.PalmFacing);
			}
		}

		#endregion
	}
}