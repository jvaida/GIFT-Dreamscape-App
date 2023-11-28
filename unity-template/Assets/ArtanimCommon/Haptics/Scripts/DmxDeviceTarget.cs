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

        [Tooltip("Whether or not the global offset should be taken into account")]
        public bool RelativeToGlobalOffset;

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

        void OnEnable()
        {
            _ctrl = HapticsController.DmxDevicesController;
            if (_ctrl == null) enabled = false;
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
                        _devices = _ctrl.GetDevices(_type, _currentSide);
                    }

                    Vector2 posOrDir;
                    if (PickBy == DevicePicker.PickMethod.Angle)
                    {
                        Vector3 dir;
                        switch (ForwardAxis)
                        {
                            case Axis.Forward:
                                dir = RelativeToGlobalOffset ? GlobalMocapOffset.Instance.UnOffsetDirection(transform.forward) : transform.forward;
                                break;
                            case Axis.Backward:
                                dir = RelativeToGlobalOffset ? GlobalMocapOffset.Instance.UnOffsetDirection(-transform.forward) : -transform.forward;
                                break;
                            case Axis.Left:
                                dir = RelativeToGlobalOffset ? GlobalMocapOffset.Instance.UnOffsetDirection(transform.right) : transform.right;
                                break;
                            case Axis.Right:
                                dir = RelativeToGlobalOffset ? GlobalMocapOffset.Instance.UnOffsetDirection(-transform.right) : -transform.right;
                                break;
                            default:
                                throw new System.InvalidOperationException("Unexpected value for ForwardAxis: " + ForwardAxis);
                        }
                        posOrDir = new Vector2(dir.x, dir.z);
                    }
                    else
                    {
                        var pos = RelativeToGlobalOffset ? GlobalMocapOffset.Instance.UnOffsetPosition(transform.position) : transform.position;
                        posOrDir = new Vector2(pos.x, pos.z);
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
