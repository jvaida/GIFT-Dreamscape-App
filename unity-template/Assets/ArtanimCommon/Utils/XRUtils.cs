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
    public class XRUtils
    {
        #region Factory

        private static XRUtils _Instance;
        public static XRUtils Instance
        { 
            get
            {
                if (_Instance == null)
                    _Instance = new XRUtils();
                return _Instance;
            }
        }

        #endregion

#if UNITY_2019_3_OR_NEWER
        private XRUtils()
        {
            //InputDevices.deviceConnected += InputDevices_deviceConnected;
            //InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
        }

        private void InputDevices_deviceConnected(InputDevice device)
        {
            //Debug.LogError($"Device connected: {device.name}");
        }

        private void InputDevices_deviceDisconnected(InputDevice device)
        {
            //Debug.LogError($"Device disconnected: {device.name}");
        }


        private List<InputDevice> _HmdDevices;
        private List<InputDevice> HmdDevices
        { 
            get
            {
                if (_HmdDevices == null)
                {
                    _HmdDevices = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, _HmdDevices);
                }
                return _HmdDevices;
            }
        }
#endif


        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Quaternion GetNodeLocalRotation(XRNode node)
        {
#if UNITY_2019_3_OR_NEWER

            var device = InputDevices.GetDeviceAtXRNode(node);
            if(device != null && device.isValid)
            {
                Quaternion rotation;
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
                return rotation;
            }
            return Quaternion.identity;
            
#else
            return InputTracking.GetLocalRotation(node);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Vector3 GetNodeLocalPosition(XRNode node)
        {
#if UNITY_2019_3_OR_NEWER

            var device = InputDevices.GetDeviceAtXRNode(node);
            if(device != null && device.isValid)
            {
                Vector3 position;
                device.TryGetFeatureValue(CommonUsages.devicePosition, out position);
                return position;
            }
            return Vector3.zero;
#else
            return InputTracking.GetLocalPosition(node);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsDevicePresent
        { 
            get
            {
#if UNITY_2019_3_OR_NEWER
                return HmdDevices.Count > 0;
#else
                return XRDevice.isPresent;
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsUserPresent
        {
            get
            {
#if UNITY_2019_3_OR_NEWER

                InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (headDevice != null && headDevice.isValid)
                {
                    bool userPresent;
                    bool presenceFeatureSupported = headDevice.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);
                    if (presenceFeatureSupported)
                        return userPresent;
                }
                return false;


#else
            return XRDevice.userPresence == UserPresenceState.Present;
#endif
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public string DeviceName
        {
            get
            {

#if UNITY_2019_3_OR_NEWER
                InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (headDevice != null && headDevice.isValid)
                    return headDevice.name;
                else
                    return null;

#else
                return XRDevice.model;
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Recenter()
        {
#if UNITY_2019_3_OR_NEWER
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if(headDevice != null)
            { 
                if (headDevice.isValid)
                { 
                   // Apparently subsystem can be null in some conditions (e.g. in Unity 2019.4.10 with legacy VR enabled)
                   if (headDevice.subsystem != null) 
                        headDevice.subsystem.TryRecenter();
                }
            }
#else
            InputTracking.Recenter();
#endif
        }

    }
}