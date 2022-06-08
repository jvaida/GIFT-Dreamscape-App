using Artanim;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	/// <summary>
	/// A simple behaviour which will offset itself to always be in front of the camera at a set distance.
	/// </summary>
	[AddComponentMenu("Artanim/Camera Attached GameObject")]
	public class CameraAttachedGameObject : MonoBehaviour
	{
		[Tooltip("Distance to the camera")]
		public float Distance;

		[Tooltip("Rotate GameObject to look at camera")]
		public bool LookAt;

		void LateUpdate()
		{
			Transform cameraTransform = null;

			if (MainCameraController.HasInstance && MainCameraController.Instance.ActiveCamera)
				cameraTransform = MainCameraController.Instance.ActiveCamera.transform;
			else if(Camera.main)
				cameraTransform = Camera.main.transform;

			if (cameraTransform)
			{
				transform.position = cameraTransform.position + cameraTransform.forward * Distance;
				transform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);

				if (LookAt)
					transform.LookAt(cameraTransform);
			}
		}

	}
}