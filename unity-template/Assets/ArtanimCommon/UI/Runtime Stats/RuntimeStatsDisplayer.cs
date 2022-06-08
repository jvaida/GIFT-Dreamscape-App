using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
	public class RuntimeStatsDisplayer : MonoBehaviour
	{
		public Text TextFPS;
		public Color ColorGood;
		public Color ColorBad;

		public void ToggleVisibility()
        {
			gameObject.SetActive(!gameObject.activeSelf);
        }

		private void Awake()
		{
			gameObject.SetActive(false);
		}

		bool isConnected;
		private void Update()
		{
			if(XRUtils.Instance.IsDevicePresent != isConnected)
			{
				isConnected = XRUtils.Instance.IsDevicePresent;
				Debug.LogFormat("Changed HMD to connected: {0}", isConnected);
			}

			if(TextFPS)
			{
				var targetRate = XRUtils.Instance.IsDevicePresent ? XRDevice.refreshRate : Screen.currentResolution.refreshRate;
				var minBound = targetRate * 0.5f;
				TextFPS.color = Color.Lerp(ColorBad, ColorGood, (FPSMetrics.FpsAvg - minBound) / (targetRate - minBound));
				TextFPS.text = string.Format("{0:0.00}FPS ({1:0.00}min, {2:0.00}max) {3}", 
					FPSMetrics.FpsAvg, 
					FPSMetrics.MinFps, 
					FPSMetrics.MaxFps, 
					targetRate);
			}
		}
	}
}