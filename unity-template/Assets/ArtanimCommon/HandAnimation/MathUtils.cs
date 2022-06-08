using UnityEngine;

namespace Artanim.HandAnimation
{
	public static class MathUtils
	{
		private static readonly float Epsilon = 0.0001f;

		public static float Min3(float a, float b, float c)
		{
			return Mathf.Min(a, Mathf.Min(b, c));
		}

		public static float Max3(float a, float b, float c)
		{
			return Mathf.Max(a, Mathf.Max(b, c));
		}

		public static void PointToTriangleDistance(Vector3 xa, Vector3 xb, Vector3 xc, Vector3 xp, out float distance, out Vector3 projected_point)
		{
			Vector3 normal = Vector3.Cross(xb - xa, xc - xa);
			float cos_alpha = Vector3.Dot(xp - xa, normal) / ((xp - xa).magnitude * normal.magnitude);
			distance = (xp - xa).magnitude * cos_alpha;
			projected_point = -distance * normal.normalized;
		}

		/// <summary>
		/// Get this distance between a point, and a line segment defined by two points
		/// </summary>
		/// <returns>Distance of the point to the line segment defined by two points</returns>
		public static float PointToSegmentDistance(Vector3 point, Vector3 segmentA, Vector3 segmentB)
		{
			Vector3 segmentVector = segmentB - segmentA;
			float squaredLength = segmentVector.sqrMagnitude;

			if(squaredLength == 0)
			{
				return (point - segmentA).magnitude;
			}

			float normalizedProjectionDistance = Vector3.Dot(point - segmentA, segmentVector) / squaredLength;
			float t = Mathf.Clamp01(normalizedProjectionDistance);
			Vector3 projected = segmentA + t * segmentVector;

			return (point - projected).magnitude;
		}

		/// <summary>
		/// Decomposes a quaternion into its respective swing and twist components
		/// by determining the twist along a specified axis
		/// See http://allenchou.net/2018/05/game-math-swing-twist-interpolation-sterp/
		/// </summary>
		/// <param name="quaternion">The quaternion to decompose</param>
		/// <param name="twistAxis">The axis along which we wish to determine twist</param>
		/// <param name="swing">The swing component of our input quaternion</param>
		/// <param name="twist">The twist component of our input quaternion along the specified axis</param>
		public static void DecomposeSwingTwist(Quaternion quaternion, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
		{
			Vector3 rotationAxis = new Vector3(quaternion.x, quaternion.y, quaternion.z);

			if (rotationAxis.sqrMagnitude < Epsilon)
			{
				Vector3 rotatedTwistAxis = quaternion * twistAxis;
				Vector3 swingAxis = Vector3.Cross(twistAxis, rotatedTwistAxis);

				if (swingAxis.sqrMagnitude > Epsilon)
				{
					float swingAngle = Vector3.Angle(twistAxis, rotatedTwistAxis);
					swing = Quaternion.AngleAxis(swingAngle, swingAxis);
				}
				else
				{
					// more singularity: 
					// rotation axis parallel to twist axis
					swing = Quaternion.identity; // no swing
				}

				twist = Quaternion.AngleAxis(0.0f, twistAxis);
				return;
			}

			// meat of swing-twist decomposition
			Vector3 p = Vector3.Project(rotationAxis, twistAxis);
			twist = new Quaternion(p.x, p.y, p.z, quaternion.w);
			twist = twist.Normalized();
			swing = quaternion * Quaternion.Inverse(twist);
		}

		public static Quaternion Normalized(this Quaternion quaternion)
		{
			float x = quaternion.x;
			float y = quaternion.y;
			float z = quaternion.z;
			float w = quaternion.w;

			float magnitude = Mathf.Sqrt(x * x + y * y + z * z + w * w);

			if (Mathf.Approximately(magnitude, 0f))
			{
				return Quaternion.identity;
			}

			return new Quaternion(x / magnitude, y / magnitude, z / magnitude, w / magnitude);
		}

		/// <summary>
		/// Computes the twist of a quaternion along a certain axis. 
		/// See "Swing-twist decomposition in Clifford Algebra" for details
		/// </summary>
		/// <param name="quaternion"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static Quaternion ComputeTwist(Quaternion quaternion, Vector3 axis)
		{
			float u = axis.x * -quaternion.x + axis.y * -quaternion.y + axis.z * -quaternion.z;
			float n = axis.x * axis.x + axis.y * axis.y + axis.z * axis.z;
			float m = quaternion.w * n;
			float l = Mathf.Sqrt(m * m + (u * u) * n);

			float qw = m / l;
			float qx = -(axis.x * u) / l;
			float qy = -(axis.y * u) / l;
			float qz = -(axis.z * u) / l;

			return new Quaternion(qx, qy, qz, qw);
		}

		/// <summary>
		/// Get a vector which is perpendicular to the given one
		/// </summary>
		/// <returns></returns>
		public static Vector3 GetPerpendicularVector(Vector3 vector)
		{
			if(vector.x != 0f)
			{
				return new Vector3(-(vector.y + vector.z) / vector.x, 1.0f, 1.0f);
			}
			if (vector.y != 0f)
			{
				return new Vector3(1.0f, -(vector.x + vector.z) / vector.y, 1.0f);
			}
			if (vector.z != 0f)
			{
				return new Vector3(1.0f, 1.0f , -(vector.x + vector.y) / vector.z);
			}

			//Nothing is perpendicular to a zero vector, so just return a zero vector as well
			return Vector3.zero;
		}
	}
}