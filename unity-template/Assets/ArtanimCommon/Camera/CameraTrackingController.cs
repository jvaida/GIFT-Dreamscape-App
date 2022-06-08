using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    /// <summary>
    /// Controls the VR camera rotation 
    /// </summary>
    public class CameraTrackingController : MonoBehaviour
    {
		private Transform SensorCorrection;

		public void Setup(Transform sensorCorrection)
		{
            if(DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone)
            {
			    SensorCorrection = sensorCorrection;
            }
		}

        private void OnPreCull()
        {
            if(DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone) //Allow tracked movement when running in standalone mode
            {
				//Inverse sensor position
				if (SensorCorrection)
				{
                    SensorCorrection.localPosition = -XRUtils.Instance.GetNodeLocalPosition(XRNode.CenterEye);
				}
            }
        }
    }
}