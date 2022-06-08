using UnityEngine;
using System.Collections;

namespace Artanim.Tracking
{
	public static class Referential
	{
		public static readonly float TrackingToUnityScale = 0.001f;

		// Map position from Tracker's space to Unity's space
		public static Vector3 TrackingPositionToUnity(Vector3 v)
		{
			var unityVec = new Vector3(-v.x, v.y, v.z);
			return unityVec * TrackingToUnityScale;
		}

		public static Quaternion TrackingRotationToUnity(float x, float y, float z, float w)
		{
			var rotation = new Quaternion(-x, y, z, -w);
			rotation *= Quaternion.LookRotation(Vector3.right); //90 degree fix for rotated axis legacy reasons

			return rotation;
		}
	}
}