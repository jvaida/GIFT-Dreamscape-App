using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{

public class ReplaceCameraTest : MonoBehaviour
{
	public GameObject CameraTemplate;

	void OnEnable()
	{
		if (CameraTemplate)
			MainCameraController.Instance.ReplaceCamera(CameraTemplate);
	}

	void OnDisable()
	{
		if (CameraTemplate)
			MainCameraController.Instance.ReplaceCamera(null);
	}

}

}