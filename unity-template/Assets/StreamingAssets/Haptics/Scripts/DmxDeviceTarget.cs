using Artanim.Haptics.PodLayout;
using Artanim.Haptics.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Artanim.Haptics
{
    public class DmxDeviceTarget : MonoBehaviour
    {
        public enum Axis { Forward, Backward, Left, Right };

        [SerializeField]
        [Tooltip("Type of device")]
        string _type = "Fan";

        [Range(0f, 50f)]
        [Tooltip("Number of devices to drive")]
        public int Count = 1;

        [Range(0f, 1f)]
        [Tooltip("Device(s) DMX value, for example a fan's speed (can be animated)")]
        [UnityEngine.Serialization.FormerlySerializedAs("Speed")]
        public float Value = 0.5f;

        [Tooltip("Whether or not to mute the effect")]
        public bool Muted = false;

        [Tooltip("Restrict the side of the pod on which to select the devices")]
        public PodSide Side = PodSide.All;

        [Tooltip("Whether to select devices based on distance or angle with the forward axis")]
        public DevicePicker.PickMethod PickBy = DevicePicker.PickMethod.Distance;

        [Tooltip("Direction of the forward axis when PickBy is set to Angle")]
        public Axis ForwardAxis = Axis.Forward;

        // Used to monitor changes in the above properties
        int _currentCount;
        PodSide _currentSide = PodSide.None;
        DevicePicker.PickMethod _currentPickMehod;
        Axis _currentForwardAxis;
        Vector3 _lastPosition;
        Quaternion _lastRotation;

        DmxComponentConfig[] _devices;
        string[] _previousDevices;

        DmxDevicesController _ctrl;

        public void Mute(bool mute)
        {
            Muted = mute;
        }

        void OnEanbled()
        {
            _ctrl = HapticsController.DmxDevicesController;
            if (_ctrl) enabled = false;
        }

        void OnDisable()
        {
            Reset();
            _devices = null;
            _currentSide = PodSide.None;
        }

        void Reset()
        {
            if ((_previousDevices != null) && (_ctrl != null))
            {
                foreach (var dev in _previousDevices)
                {
                    _ctrl.SetDeviceValue(dev, 0);
                }
                _previousDevices = null;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if ((Count > 0) && (!Muted))
            {
                string[] nearbyDevices = _previousDevices;

                if ((_previousDevices == null)
                    || (_currentCount != Count)
                    || (_currentSide != Side)
                    || (_currentPickMehod != PickBy)
                    || (_currentForwardAxis != ForwardAxis)
                    || ((PickBy == DevicePicker.PickMethod.Distance) && (_lastPosition != transform.position))
                    || ((PickBy == DevicePicker.PickMethod.Angle) && (_lastRotation != transform.rotation)))
                {
                    _currentCount = Count;
                    _currentPickMehod = PickBy;
                    _currentForwardAxis = ForwardAxis;
                    _lastPosition = transform.position;
                    _lastRotation = transform.rotation;

                    if (_currentSide != Side)
                    {
                        _currentSide = Side;
                        if (_ctrl)
                        {
                            _devices = _ctrl.GetDevices(_type, _currentSide);
                        }
                    }

                    Vector2 posOrDir;
                    if (PickBy == DevicePicker.PickMethod.Angle)
                    {
                        switch (ForwardAxis)
                        {
                            case Axis.Forward:
                                posOrDir = new Vector2(transform.forward.x, transform.forward.z);
                                break;
                            case Axis.Backward:
                                posOrDir = -new Vector2(transform.forward.x, transform.forward.z);
                                break;
                            case Axis.Left:
                                posOrDir = new Vector2(transform.right.x, transform.right.z);
                                break;
                            case Axis.Right:
                                posOrDir = -new Vector2(transform.right.x, transform.right.z);
                                break;
                            default:
                                throw new System.InvalidOperationException("Unexpected value for ForwardAxis: " + ForwardAxis);
                        }
                    }
                    else
                    {
                        posOrDir = new Vector2(transform.position.x, transform.position.z);
                    }

                    if (_devices != null)
                    {
                        var orderedDevices = DevicePicker.SelectAndOrder(_devices, f => new Vector2(f.Position.X, f.Position.Z), posOrDir, PickBy, _currentSide);
                        nearbyDevices = orderedDevices.Take(Count).Select(f => f.Name).ToArray();
                    }
                }

                if ((nearbyDevices != null) && (nearbyDevices.Length > 0))
                {
                    foreach (var device in nearbyDevices)
                    {
                        _ctrl.SetDeviceValue(device, Value);
                    }
                    if ((_previousDevices != null) && (_previousDevices != nearbyDevices))
                    {
                        foreach (var device in _previousDevices.Except(nearbyDevices))
                        {
                            _ctrl.SetDeviceValue(device, 0);
                        }
                    }
                    _previousDevices = nearbyDevices;
                }
                else
                {
                    Reset();
                }
            }
            else
            {
                Reset();
            }
        }
    }
}
