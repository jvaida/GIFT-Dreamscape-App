using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Artanim.Location.Config;
using Artanim;

namespace Dreamscape
{

	[ExecuteInEditMode]
	public class DMX_device : MonoBehaviour
	{
		public float speed = 0.0f;
		protected float _dmxValue = 0.0f;
		public DmxDevice dmxDevice = new DmxDevice();
		public List<DMX_device> slaveDevices;
		public DMX_subDevice[] subDevices;
		public bool UseGameObjectNameAsDeviceName = true;
		protected string oldName = null;
		//public string buttonText = "Enter your own Device name.";
		//public string buttonText_useingGameObjecName = "You can enter your own Device name.";
		//public string buttonText_notUseingGameObjecName = "Using game object name as device name.";

		public void Reset()
		{

		}

		public void Start()
		{
			updateSubDeviceList();
#if UNITY_EDITOR
			CheckForEditorNameChange();
#endif
		}

		public void Update()
		{
            //_dmxValue = speed * 255.0f;
            _dmxValue = Mathf.Clamp(ConvertDeviceSpeed(speed, Mathf.Clamp01(_dmxValue / 255f)) * 255f, 0f, 255f);

			DefaultUniformChanneleUpdate();
			UpdateSubDevicesAndSlaveDevices();
#if UNITY_EDITOR
			CheckForEditorNameChange();
#endif
		}

		// override this method in yor subclass if the channel is supposed to inherit the device name
		public virtual void CheckForEditorNameChange()
		{
			if (name != oldName)
			{
				dmxDevice.updateName(name);
				oldName = name;
			}
		}

		public void updateSubDeviceList()
		{
			bool includeInactive = false;
			subDevices = GetComponentsInChildren<DMX_subDevice>(includeInactive);
		}

		//public void OnValidate()
		//{
		//    Debug.Log("OnValidate was just called");
		//}


		// Methods  
		public void DefaultUniformChanneleUpdate()
		{
			int count = dmxDevice.getChannelCount();
			for (int i = 0; i < count; i++)
			{
				dmxDevice.setDMXChannelValue(i, (int)_dmxValue);
			}
		}

		public void AdjustDeviceChannelName(string newName)
		{
			Debug.Log("DMX_device.AdjustDeviceChannelName");
		}

		public void UpdateSubDevicesAndSlaveDevices()
		{
			//bool includeInactive = true;
			//subDevices = GetComponentsInChildren<DMX_subDevice>(includeInactive);

			if (slaveDevices != null && slaveDevices.Count > 0)
			{
				foreach (DMX_device child in slaveDevices)
				{
					if (child != null)
					{
						child.speed = speed;
					}
				}
			}
			if (subDevices != null && subDevices.Length > 0)
			{
				foreach (DMX_subDevice child in subDevices)
				{
					child.speed = speed;
				}
			}
		}

        #region Value Mapping

        private bool IsStopped = true;
        private bool IsStarting = false;
        private float StartupTime = 0f;

        private float ConvertDeviceSpeed(float targetSpeed, float currentSpeed)
        {
            if (targetSpeed == currentSpeed)
                return targetSpeed;

            if (dmxDevice.ValueMapping != null)
            {
                if (targetSpeed > 0f)
                {
                    targetSpeed = GetMappedTargetValue(targetSpeed);

                    if (dmxDevice.ValueMapping.NeedStartup)
                    {
                        if (IsStopped)
                        {
                            IsStopped = false;
                            IsStarting = true;
                            StartupTime = 0f;
                        }

                        if (IsStarting)
                        {
                            if (targetSpeed < dmxDevice.ValueMapping.StartupSpeed)
                            {
                                StartupTime += Time.deltaTime;
                                if (StartupTime > dmxDevice.ValueMapping.StartupTimeSecs)
                                {
                                    //End startup
                                    IsStarting = false;
                                }
                                return dmxDevice.ValueMapping.StartupSpeed;
                            }
                        }
                    }

                    return dmxDevice.ValueMapping.TransitionSpeed == 0f ? targetSpeed : Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * dmxDevice.ValueMapping.TransitionSpeed);
                }

                IsStopped = true;
                return 0f;
            }
            else
            {
                return targetSpeed;
            }
        }

        private float GetMappedTargetValue(float targetSpeed)
        {

            if (dmxDevice.ValueMapping.ValueRanges.Count > 0)
            {
                //Segmented value by configuration
                for (var i = 0; i < dmxDevice.ValueMapping.ValueRanges.Count; ++i)
                {
                    if (targetSpeed <= dmxDevice.ValueMapping.ValueRanges[i].SourceSpeed)
                    {
                        var toRange = dmxDevice.ValueMapping.ValueRanges[i];

                        if (i > 0)
                        {
                        	var fromRange = dmxDevice.ValueMapping.ValueRanges[i - 1];
                            var perc = (targetSpeed - fromRange.SourceSpeed) / (toRange.SourceSpeed - fromRange.SourceSpeed);
                            var targetValue = fromRange.TargetSpeed + (toRange.TargetSpeed - fromRange.TargetSpeed) * perc;
                            return targetValue;
                        }
                        else
                        {
                            //Min value
                            return toRange.TargetSpeed;
                        }
                    }
                }

                //Max speed
                return dmxDevice.ValueMapping.ValueRanges[dmxDevice.ValueMapping.ValueRanges.Count - 1].TargetSpeed;
            }

            return targetSpeed;
        }

        #endregion

        [System.Serializable]
		public class DmxDevice : System.Object
		{
			// Data
			[XmlAttribute("deviceName")]
			public string deviceName = "default device name";

			[XmlAttribute("deviceType")]
			public string deviceType = "None";

			[XmlAttribute("isDisabled")]
			public bool isDisabled = false;
            

            [XmlAttribute("valueMapping")]
            public string valueMappingName = null;

            [XmlIgnore]
            public DmxDeviceValueMapping ValueMapping;

            [System.NonSerialized]
			public bool fileDeviceFound = false;

			[XmlArray("DmxChannels")]
			[XmlArrayItem("DmxChannel")]
			public List<DmxChannel> dmxChannel = new List<DmxChannel>();

			public enum ErrorLevel { NoError, NoMatch, PartialMatch };

			public void initialize(int universes_, int channel_, float value_, string deviceName_, string deviceType_)
			{
				dmxChannel.Clear();
				addChannel(universes_, channel_, value_, deviceName_);
				deviceName = deviceName_;
			}

			// Methods
			public void addChannel(int universes_, int channel_, float value_, string name_)
			{
				dmxChannel.Add(new DmxChannel(universes_, channel_, value_, name_));
			}
			public int getChannelCount()
			{
				return dmxChannel.Count;
			}

			public void setChannelName(int channelNumber, string name)
			{
				if (channelNumber >= 0 && channelNumber < dmxChannel.Count)
				{
					dmxChannel[channelNumber].name = name;
				}
				else
				{
					Debug.LogError("Set channel name out of range for device " + deviceName + ". Got an index of " + channelNumber.ToString() + " out of " + dmxChannel.Count.ToString());
				}
			}

			public void setDMXChannelValue(int channelNumber, float value)
			{
				if (channelNumber >= 0 && channelNumber < dmxChannel.Count)
				{
					dmxChannel[channelNumber].value = value;
				}
				else
				{
					Debug.LogError("Set channel value out of range for device " + deviceName + ". Got an index of " + channelNumber.ToString() + " out of " + dmxChannel.Count.ToString());
				}
			}

			public void setDMXNormalizedValue(int channelNumber, float value)
			{
				setDMXChannelValue(channelNumber, value * 255.0f);
			}

			public void setDMXBase100Value(int channelNumber, float value)
			{
				setDMXChannelValue(channelNumber, value * 255.0f / 100.0f);
			}

			public ErrorLevel SetMatch(List<DmxDevice> updatedDeviceInfo)
			{
				ErrorLevel foundMatch = ErrorLevel.NoMatch;
				foreach (DmxDevice candidate in updatedDeviceInfo)
				{
					bool isMatch = deviceType == candidate.deviceType &&
								   deviceName == candidate.deviceName &&
								   dmxChannel.Count == candidate.dmxChannel.Count;
					bool isCopied = ItemCopy(candidate);

                    //Search value mapping
                    if(isMatch && !string.IsNullOrEmpty(valueMappingName))
                    {
                        var mapping = ConfigService.Instance.HardwareConfig.DmxConfig.DeviceValueMappings.FirstOrDefault(m => m.Name == valueMappingName);
                        if(mapping != null)
                        {
                            ValueMapping = mapping;
                            //Sort mappings on source speed
                            ValueMapping.ValueRanges = ValueMapping.ValueRanges.OrderBy(m => m.SourceSpeed).ToList();
                        }
                    }

					if (isMatch && !isCopied)
					{
						foundMatch = ErrorLevel.PartialMatch;
						break;
					}
					if (isMatch && isCopied)
					{
						foundMatch = ErrorLevel.NoError;
						candidate.fileDeviceFound = true;
						break;
					}
				}
				return foundMatch;
			}

			private bool ItemCopy(DmxDevice source)
			{
				//Set device properties
				isDisabled = source.isDisabled;

                valueMappingName = source.valueMappingName;
                if(string.IsNullOrEmpty(valueMappingName))
                {
                    //Set default name to device mapping
                    var deviceType = source.deviceType.ToLower().Substring("DMX_".Length);
                    valueMappingName = string.Format("default_{0}", deviceType);
                }


				//Set channels
				bool foundAllChannels = true;
				foreach (DmxChannel existingChannel in dmxChannel)
				{
					bool rv = existingChannel.SetMatch(source.dmxChannel);
					if (!rv) foundAllChannels = false;
				}
				return foundAllChannels;
			}

			public static T DeepClone<T>(T obj)
			{
				using (var ms = new MemoryStream())
				{
					var formatter = new BinaryFormatter();
					formatter.Serialize(ms, obj);
					ms.Position = 0;

					return (T)formatter.Deserialize(ms);
				}
			}

			public void updateName(string name, bool updateChannel0 = false)
			{
				deviceName = name;
				if (updateChannel0) updateChannel0Name(name);
			}

			public void updateChannel0Name(string name)
			{
				if (dmxChannel.Count > 0) dmxChannel[0].name = name;
				// sic
				// override this in your subclass of the channel is supposed to inherit the device name
			}
		}

		[System.Serializable]
		public class DmxChannel : System.Object
		{
			[XmlAttribute("universes")]
			public int universes = 1;
			[XmlAttribute("channel")]
			public int channel = 1234;
			[XmlAttribute("value")]
			public float value = 0.0f;
			[XmlAttribute("name")]
			public string name = "default name";

			public DmxChannel() { }

			public DmxChannel(int universes_, int channel_, float value_, string name_)
			{
				universes = universes_;
				channel = channel_;
				value = value_;
				name = name_;
			}
			public bool SetMatch(List<DmxChannel> updatedChannelList)
			{
				bool foundMatch = false;
				foreach (DmxChannel candidate in updatedChannelList)
				{
					if (name == candidate.name)
					{
						universes = candidate.universes;
						channel = candidate.channel;
						value = candidate.value;
						foundMatch = true;
						break;
					}
				}
				return foundMatch;
			}
		}

       
    }

}