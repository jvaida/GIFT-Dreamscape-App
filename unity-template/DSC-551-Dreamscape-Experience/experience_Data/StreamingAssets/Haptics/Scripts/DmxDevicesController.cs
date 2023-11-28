using Artanim.Haptics.Internal;
using Artanim.Haptics.PodLayout;
using Artanim.Location.DMX;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics
{
	public class DmxDevicesController : MonoBehaviour
	{
		DMXController _ctrl;
        Dictionary<string, BaseDevice> _dmxDevices;

        /// <summary>
        /// Whether or not to send any value to DMX devices
        /// </summary>
        public bool IsMuted
        {
            get { return (_ctrl != null) && _ctrl.IsMuted; }
            set { if (_ctrl != null) _ctrl.IsMuted = value; }
        }

        /// <summary>
        /// All available DMX device names
        /// </summary>
        public BaseDevice[] AllDevices { get; private set; }

        /// <summary>
        /// Returns the DMX devices matching the given criteria
        /// </summary>
        /// <param name="type">Type of device (all of them if null or empty)</param>
        /// <param name="side">Limit devices to those located on the given pod's side(s)</param>
        /// <returns>Array of devices</returns>
        public DmxComponentConfig[] GetDevices(string type = null, PodSide side = PodSide.All)
        {
            //TODO this methods needs to return actual DMX devices once the native module loads the position of the devices
            if (_dmxDevices == null)
            {
                return null;
            }

            if (side == PodSide.None)
            {
                return new DmxComponentConfig[0];
            }
            else
            {
                var devEnum = PodLayoutConfig.Instance.Components.OfType<DmxComponentConfig>();
                if (!string.IsNullOrEmpty(type))
                {
                    devEnum = devEnum.Where(c => c.Type == type);
                }
                var devices = devEnum.ToArray();
                if (devices.Length > 0)
                {
                    devices = DevicePicker.Select(devices, d => new Vector2(d.Position.X, d.Position.Z), side).ToArray();
                }
                return devices;
            }
        }

        /// <summary>
        /// Set the DMX value of a device as identified by its name
        /// </summary>
        /// <param name="name">Device name</param>
        /// <param name="value">DMX value</param>
        public void SetDeviceValue(string name, float value)
        {
            if (_dmxDevices == null)
            {
                return;
            }

            BaseDevice device;
            if (_dmxDevices.TryGetValue(name, out device))
            {
                device.SetChannelCurrentValue(0, value);
            }
            else
            {
                Debug.LogError("No DMX device named " + name);
            }
        }

        /// <summary>
        /// Returns the DMX value of a device as identified by its name
        /// </summary>
        /// <param name="name">Device name</param>
        /// <returns>DMX value</returns>
        public float GetDeviceValue(string name)
        {
            float value = 0;
            if (_dmxDevices != null)
            {
                BaseDevice device;
                if (_dmxDevices.TryGetValue(name, out device))
                {
                    value = device.GetChannelCurrentValue(0);
                }
            }
            return value;
        }

        void Setup()
        {
            string error = null;
            _ctrl = DMXController.Instance;

            if (_ctrl == null)
            {
                error = "DMXController: Failed to retrieve singleton";
            }
            else
            {
                string pathname = Path.Combine(Application.streamingAssetsPath, "Haptics\\dmx_config.xml");
                if (_ctrl.LoadConfig(pathname))
                {
                    if (CommandLineUtils.GetValue("MuteHaptics", true))
                    {
                        _ctrl.IsMuted = true;
                    }
                    _dmxDevices = Enumerable.Range(0, _ctrl.DeviceCount).Select(i => _ctrl.GetDevice(i)).ToDictionary(d => d.Name, d => d);
                    AllDevices = _dmxDevices.Values.ToArray();
                }
                else
                {
                    error = "DMXController: Error loading config, disabling self";
                }
            }

            if (error != null)
            {
                Debug.LogError(error);
                enabled = false;
            }
        }

        void CleanUp()
        {
            if ((_ctrl != null) && (_dmxDevices != null))
            {
                foreach (var device in _dmxDevices.Values)
                {
                    device.SetChannelCurrentValue(0, 0);
                }
                _ctrl.Update();
                foreach (var device in _dmxDevices.Values)
                {
                    device.Dispose();
                }
                _dmxDevices.Clear();
            }
        }

        #region Unity messages

        void Awake()
        {
            // Disable self if required
            enabled = ConfigService.Instance.ExperienceConfig.EnableNativeHaptics;
        }

        void OnEnable()
		{
            Setup();
        }

        void OnDisable()
        {
            CleanUp();
            _dmxDevices = null;
            _ctrl = null;
        }

        void LateUpdate()
		{
            if (_ctrl != null)
            {
                _ctrl.Update();
            }
        }

        #endregion
    }
}
