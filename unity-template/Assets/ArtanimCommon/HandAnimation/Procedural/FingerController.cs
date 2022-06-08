using Artanim.HandAnimation.Config;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
	public struct FingerJoint
	{
		public float CurrentAngle;
		public float RestAngle;
		public float StraightAngle;
	}

	public enum FingerType
	{
		Thumb,
		Index,
		Middle,
		Ring,
		Pinky
	}

	public class FingerController
    {
		public FingerType FingerType;

		public FingerController PrevController;
		public FingerController NextController;
		public FingerSetup FingerSetup;

		private float _Reach = 0.08f;
		private FingerJoint[] _Joints = new FingerJoint[4];
		public Transform[] JointTransforms;

		public bool HasChanged
		{
			get
			{
				return _HasChanged;
			}
			private set
			{
				_HasChanged = value;
			}
		}
		private bool _HasChanged = false;

		private List<IntersectionInfo> _IntersectionPoints = new List<IntersectionInfo>();

		public FingerController(FingerType fingerType, FingerSetup fingerSetup, Transform root)
        {
			FingerType = fingerType;
			FingerSetup = fingerSetup;
			JointTransforms = root.GetComponentsInChildren<Transform>();

			SetRestPose();

			float fingerLength = 0;
			int jointCount = JointTransforms.Length;
			for (int i = 1; i < jointCount; i++)
			{
				fingerLength += (JointTransforms[i].position - JointTransforms[i - 1].position).magnitude;
			}

			if (JointTransforms.Length == 3)
			{
				_Reach = fingerLength * 1.2f;
			}
			else
			{
				_Reach = fingerLength;
			}
        }

        public void ResetPose()
        {
            for (int i = 0; i < JointTransforms.Length; i++)
            {
                _Joints[i].CurrentAngle = _Joints[i].RestAngle;
            }
            _HasChanged = false;
        }

        public void UpdateFingerIntersectionPoints(List<HandAnimationCollider> handAnimationColliders)
        {
            _IntersectionPoints.Clear();
	        Vector3 position = JointTransforms[0].position;
	        Vector3 normal = JointTransforms[0].TransformVector(FingerSetup.RotationAxis.normalized);

			foreach (var handAnimationCollider in handAnimationColliders)
            {
				float reachFactor = (FingerType != FingerType.Thumb) ? handAnimationCollider.ReachFactor : 1.0f;
				handAnimationCollider.FindPlaneIntersectionPoints(_IntersectionPoints, position, normal, reachFactor * _Reach);
            }
        }

        public void SolveJointsRotation(bool blockFirstJoint = false)
        {
			int start = 0;

			Vector3 rotationAxisWorld = JointTransforms[0].TransformDirection(FingerSetup.RotationAxis.normalized);

			for (int ji = start; ji < 3; ji++)
            {
				// finger segment need to be straight/unbended to compute the target angle correctly
				Transform currentJointTransform = JointTransforms[ji];
				FingerJoint currentJoint = _Joints[ji];

				Quaternion localRotation = currentJointTransform.localRotation;
				Quaternion localTwist = MathUtils.ComputeTwist(localRotation, FingerSetup.RotationAxis);
				Quaternion zeroRotation = localRotation * Quaternion.Inverse(localTwist);
				currentJointTransform.localRotation = zeroRotation;

				// vector direction from current to the next joint
				Vector3 boneDirection = currentJointTransform.TransformDirection(FingerSetup.FingerPointing);
                Vector3 currentJointPosition = currentJointTransform.position; // best if taking an initial calibration value
                float smallestAngle = 180;

                for (int i = 0; i < _IntersectionPoints.Count; i++)
                {
                    // joint to intersection point vector
                    Vector3 candidateDirection = (_IntersectionPoints[i].Position - currentJointPosition).normalized;

                    float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(candidateDirection, boneDirection));
                    float dot = Vector3.Dot(candidateDirection, Vector3.Cross(boneDirection, rotationAxisWorld));

					if (dot <= 0 || angle < 90)
                    {
						if (dot > 0)
						{
							angle = -angle;
						}
						if (Mathf.Abs(angle) < Mathf.Abs(smallestAngle))
						{
							smallestAngle = angle;
						}
                    }
                }

                Quaternion tgtRotation = zeroRotation * Quaternion.AngleAxis(currentJoint.RestAngle, FingerSetup.RotationAxis);

				// apply rotation
				if (smallestAngle < 170)
				{
					smallestAngle -= 13; // constant adjustment to prevent interpenetration
					var jointLimit = FingerSetup.GetRange(ji);
					float maxJointLimit = jointLimit.y;
					float minJointLimit = jointLimit.x;

					smallestAngle = smallestAngle > maxJointLimit ? maxJointLimit : smallestAngle < minJointLimit ? minJointLimit : smallestAngle;
					// best if I use a calibration rotation
					tgtRotation = zeroRotation * Quaternion.AngleAxis(smallestAngle, FingerSetup.RotationAxis);
					currentJoint.CurrentAngle = smallestAngle;
					_HasChanged = true;
				}
				else
				{
					currentJoint.CurrentAngle = currentJoint.RestAngle;
				}

				// need to apply so that the child GOs are transformed, and angles are comptued correctly
				currentJointTransform.localRotation = tgtRotation;

				_Joints[ji] = currentJoint;
			}

        }

        public void UpdatePose(int jointIndex)
        {
			FingerJoint joint = _Joints[jointIndex];
            Quaternion targetRotation = Quaternion.AngleAxis(joint.StraightAngle, FingerSetup.RotationAxis) * Quaternion.AngleAxis(joint.CurrentAngle, FingerSetup.RotationAxis);
            JointTransforms[jointIndex].localRotation = targetRotation;
        }


        public void UpdatePose()
        {
			for (int ji = 0; ji < JointTransforms.Length - 1; ji++)
			{
				if (!float.IsNaN(_Joints[ji].CurrentAngle))
				{
					UpdatePose(ji);
				}
			}
        }

        public void FollowNeighbors(float weight)
        {
            if (PrevController == null && NextController == null)
                return;

            for (int jo = 0; jo < 2; jo++)
            {
                float count = 0;
				FingerJoint joint = _Joints[jo];
				joint.CurrentAngle = joint.RestAngle;
				if (NextController != null && NextController.HasChanged)
				{
					joint.CurrentAngle += NextController._Joints[jo].CurrentAngle;
                    count++;
                }
                if (PrevController != null && PrevController.HasChanged)
				{
					joint.CurrentAngle += PrevController._Joints[jo].CurrentAngle;
                    count++;
                }

				if (count == 0)
				{
					continue;
				}

				joint.CurrentAngle *= weight / count;
				_Joints[jo] = joint;
			}
            _Joints[2].CurrentAngle = _Joints[1].CurrentAngle * 0.66f;
            UpdatePose();
		}

		public void SetRestPose()
		{
			for (int i = 0; i < 3; ++i)
			{
				_Joints[i].RestAngle = FingerSetup.RestAngle[i];
			}
		}
    }
}