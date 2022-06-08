using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRNode = UnityEngine.VR.VRNode;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace Artanim
{
	public class InitialCalibrationCross
		: MonoBehaviour
	{
		[SerializeField]
		HeadsetCalibration HeadsetCalibration = null;

		// LateUpdate is called once per frame
		void LateUpdate()
		{
			transform.localRotation = HeadsetCalibration.InitialRotationOffset * XRUtils.Instance.GetNodeLocalRotation(XRNode.CenterEye);
		}
	}
}