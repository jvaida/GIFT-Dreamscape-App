using System;
using UnityEngine;

namespace Artanim.HandAnimation.Config
{
    [Serializable]
    public class FingerSetup
    {
        [Tooltip("First joint angle range.")]
        [MinMaxRange(-20f, 120f)]
        public Vector2 Joint0AngleRange = new Vector2(-8f, 90f);

        [Tooltip("Second joint angle range.")]
        [MinMaxRange(-20f, 120f)]
        public Vector2 Joint1AngleRange = new Vector2(0f, 100f);

        [Tooltip("Third joint angle range.")]
        [MinMaxRange(-20f, 120f)]
        public Vector2 Joint2AngleRange = new Vector2(0f, 75f);

        [Tooltip("Resting position angle per joint.")]
        public Vector3 RestAngle = new Vector3(3f, 15f, 10f);

		[Tooltip("Rotation axis for the finger joints.")]
		public Vector3 RotationAxis = new Vector3(0f, 0f, 1f);

		[Tooltip("Local axis from base of the finger pointing towards the tip.")]
		public Vector3 FingerPointing = new Vector3(0f, 1f, 0f);

		public Vector2 GetRange(int jointIndex)
		{
			switch(jointIndex)
			{
				case 0:
					return Joint0AngleRange;
				case 1:
					return Joint1AngleRange;
				case 2:
					return Joint2AngleRange;
				default:
					return Vector2.zero;
			}
		}
    }
}