//#define HEADSET_METRICS

using Artanim.Location.Data;
using Artanim.Monitoring;
using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AFAU = Artanim.Utils.ArrayUtils;

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

	public class HeadsetCalibration : MonoBehaviour
	{
		public Quaternion InitialRotationOffset { get; private set; }

		Player Player;
		TrackingRigidbody HeadRigidBody;
		Transform CalibrationOffset;

		bool _calibrated;

		MetricsChannel<HeadsetRotationMetricsData> _headsetRotationMetrics;
		MetricsChannel<float> _oculusDriftMetrics;

		// The data structure used to send the rotation metrics
		[StructLayout(LayoutKind.Sequential)]
		struct HeadsetRotationMetricsData
		{
			public float RigidBody;
			public float HMD;
			public float RigidBody_x;
			public float HMD_x;
			public float RigidBody_z;
			public float HMD_z;
		}

		#region Public interface

		public void Setup(Player player, TrackingRigidbody headRigidBody, Transform calibrationOffset)
		{
			Player = player;
			HeadRigidBody = headRigidBody;
			CalibrationOffset = calibrationOffset;
		}

		public void Calibrate()
		{
            if (HeadRigidBody)
            {
                // Compute initial rotation offset between rigidbody and headset
                InitialRotationOffset = HeadRigidBody.RigidbodyRotation * Quaternion.Inverse(XRUtils.Instance.GetNodeLocalRotation(XRNode.CenterEye));

                _calibrated = true;
            }
            else
            {
                Debug.LogError("Failed to calibrate headset. No head rigidbody was setup.");
            }
        }

		#endregion

		#region Unity events

		void Awake()
		{
			var conf = ConfigService.Instance.Config.Location.Client.HMD.ContinuousCalibration;
			if ((conf == null) || (conf.Disabled))
			{
				enabled = false;
			}
			else
			{
				//Load calibration settings
				_smoothingAlpha = Mathf.Abs(conf.CorrectionFactor);
				_correctYOnly = conf.CorrectionFactor >= 0;
				_correctAngleThreshold = conf.CorrectionAngleThreshold;
				_correctThresholdNumFrames = conf.CorrectionThresholdNumFrames;
			}
		}

#if HEADSET_METRICS
		IEnumerator Start()
		{
			var waitEndOfFrame = new WaitForEndOfFrame();
			while (true)
			{
				yield return waitEndOfFrame;
				if (enabled)
				{
					SendMetrics();
				}
			}
		}

		void SendMetrics()
		{
			var camCtrl = MainCameraController.Instance;
			if (HeadRigidBody && (camCtrl != null) && (camCtrl.PlayerCamera != null))
			{
				var rot = HeadRigidBody.RigidbodyRotation;
				var rotVR = camCtrl.PlayerCamera.transform.rotation;

				var rotationData = new HeadsetRotationMetricsData()
				{
					RigidBody = rot.y,
					HMD = rotVR.y,
					//TODO TEMP!
					RigidBody_x = rot.x,
					HMD_x = rotVR.x,
					RigidBody_z = rot.z,
					HMD_z = rotVR.z,
				};
				unsafe
				{
					_headsetRotationMetrics.SendRawData(new System.IntPtr(&rotationData));
				}

				float angle = Quaternion.Angle(rot, rotVR);
				unsafe
				{
					_oculusDriftMetrics.SendRawData(new System.IntPtr(&angle));
				}
			}
		}
#endif

		void OnEnable()
		{
#if HEADSET_METRICS
			// Create metrics channels
			_headsetRotationMetrics = MetricsManager.Instance.GetChannelInstance<HeadsetRotationMetricsData>(MetricsAction.Create, "Headset Rotation");
			_oculusDriftMetrics = MetricsManager.Instance.GetChannelInstance<float>(MetricsAction.Create, "Oculus Drift");
#endif
		}

		void OnDisable()
		{
			// Destroy metrics channels
			if (_headsetRotationMetrics != null)
			{
				_headsetRotationMetrics.Dispose();
				_headsetRotationMetrics = null;
			}
			if (_oculusDriftMetrics != null)
			{
				_oculusDriftMetrics.Dispose();
				_oculusDriftMetrics = null;
			}
		}

		void Update()
		{
			if (Player != null && XRSettings.enabled && _calibrated)
			{
				UpdateContinuousCalibration();
			}
		}

        #endregion

        #region Continuous Calibration

		// smoothing alpha, the largest the error angle the quicker the drift error gets corrected, at a minimum rate of 0.005f
		float _smoothingAlpha = 0.005f;
		// Whether or not we only correct the camera rotation around the vertical axis
		bool _correctYOnly = false;
		// Threshold for skipping smooth correction and do an instant calibrations (snap)
		float _correctAngleThreshold = 30f;
		// Number of continuous frames with enough error before snapping
		int _correctThresholdNumFrames = 45, _correctThresholdCounter;
		// Keep last value to detect a change
		uint _lastRigidbodyFrameNumber;

		void UpdateContinuousCalibration()
		{
			// Also don't apply a correction if the rigidbody hasn't moved
			if (_lastRigidbodyFrameNumber != HeadRigidBody.LastUpdateFrameNumber)
			{
				_lastRigidbodyFrameNumber = HeadRigidBody.LastUpdateFrameNumber;

				//
				// Below is code given by Henrique :)
				//

				// current error (biased by the optical tracking latency)
				Quaternion R_h_v = HeadRigidBody.RigidbodyRotation * Quaternion.Inverse(XRUtils.Instance.GetNodeLocalRotation(XRNode.CenterEye));
				Quaternion R_h_v_s = CalibrationOffset.localRotation;
				if (_correctYOnly)
				{
					// normally, we should normalize the quat after such operation, but Unity seems to take care of that
					R_h_v.x = R_h_v.z = 0;
					R_h_v_s.x = R_h_v_s.z = 0;
				}

				// check error
				if (Quaternion.Angle(R_h_v_s, R_h_v) > _correctAngleThreshold)
				{
					++_correctThresholdCounter;
				}
				else
				{
					_correctThresholdCounter = 0;
				}

				// update rotation
				if (_correctThresholdCounter > _correctThresholdNumFrames)
				{
					// immediate re-orientation
					CalibrationOffset.localRotation = R_h_v;
					_correctThresholdCounter = 0;
				}
				else
				{
					// new offset
					CalibrationOffset.localRotation = Quaternion.Slerp(R_h_v_s, R_h_v, _smoothingAlpha);
				}
			}
			else if (_correctThresholdCounter <= _correctThresholdNumFrames)
			{
				// Count missed frames as errors, so we have an immediate re-orientation when getting tracking back
				++_correctThresholdCounter;
			}
		}

        #endregion
	}
}