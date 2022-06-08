
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Text;
using Artanim.Utils;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dreamscape
{

	[RequireComponent(typeof(AudioListener))]
	public class HapticAudio_main : MonoBehaviour
	{
		const float MUTED_GAIN = -60f;

		private static HapticAudio_main _manager;
        public static HapticAudio_main Instance
        {
            get
            {
                return _manager;
            }
        }
        #region Native function
        [DllImport("dreamscape-native", EntryPoint = "GetDeviceNames")]
		unsafe private static extern void GetDeviceNames(byte* a);     // Does not restarts port audio, so any change in devices will not be detected

		[DllImport("dreamscape-native", EntryPoint = "UpdateDeviceNames")]
		unsafe private static extern void UpdateDeviceNames(byte* a);  // Restarts port audio, so any change in devices will be detected

		[DllImport("dreamscape-native", EntryPoint = "SendAudioPacket")]
		unsafe private static extern void sendAudioPacket_remote(int dataLen, int sampleChannels, [In, Out] float[] data);

		[DllImport("dreamscape-native", EntryPoint = "SendChannelData")]
		unsafe private static extern void sendChannelData_remote(int deviceChannels, [In, Out] float[] data);

		[DllImport("dreamscape-native", EntryPoint = "OpenStreams")]
		unsafe private static extern void OpenStreams_remote(double sampleRate, int numDevices, uint framesPerBuffer, byte* a, byte* b, byte* c);

		[DllImport("dreamscape-native", EntryPoint = "CloseStreams")]
		unsafe private static extern void CloseStreams_remote();
		#endregion

		[HideInInspector]
		public ConfigData AudioChannelsConfiguration = new ConfigData();

		[HideInInspector]
		public int AudioChannelsSize = 0;

		[HideInInspector]
		public bool useDefaultDevConfig = true;

		private int sampleRate = 0;
		int bufferLength = 0;
		int numBuffers = 0;

		[HideInInspector] public bool running = false;
		private float[] firstStageBuffer = new float[2];

		public bool IsMuted { get; private set; }

		void OnValidate()
		{
			if (running) return;
			if (AudioChannelsSize != AudioChannelsConfiguration.AudioChannels.Count)
			{
				if (AudioChannelsSize > AudioChannelsConfiguration.AudioChannels.Count)
				{
					while (AudioChannelsConfiguration.AudioChannels.Count < AudioChannelsSize)
					{
						AudioChannelsConfiguration.AudioChannels.Add(new AudioChannel());
					}
				}
				else
				{
					while (AudioChannelsConfiguration.AudioChannels.Count > AudioChannelsSize)
					{
						AudioChannelsConfiguration.AudioChannels.RemoveAt(AudioChannelsConfiguration.AudioChannels.Count - 1);
					}
				}
			}
		}

		void Start()
		{
            lock (this)
            {
                running = true;
				IsMuted = Artanim.CommandLineUtils.GetValue("MuteHaptics", true);
				if (IsMuted)
				{
					Debug.LogWarning("Haptics audio are forced to be muted");
				}

				RestoreFromXmlConfig();
                OpenStreams();
                _manager = this;
                foreach (AudioChannel a in AudioChannelsConfiguration.AudioChannels)
                {
                    if (a.muteWhenUnoccupied)
                    {
                        a.gain_1 = a.gain_2 = MUTED_GAIN;
                    }
                }
                Debug.Log("Muted all audio channels");
            }
        }

		protected unsafe void OpenStreams()
		{
			sampleRate = AudioSettings.outputSampleRate;
			AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);

			List<string> channelName_1 = new List<string>();
			List<string> channelName_2 = new List<string>();
            List<string> panelName_1 = new List<string>();
            List<string> panelName_2 = new List<string>();
            List<string> deviceName = new List<string>();

			Debug.LogFormat("Opening {0} audio streams", numBuffers);

			int numberOfChannels = AudioChannelsConfiguration.AudioChannels.Count;
			for (int i = 0; i < numberOfChannels; i++)
			{
				channelName_1.Add(AudioChannelsConfiguration.AudioChannels[i].channelName_1);
				channelName_2.Add(AudioChannelsConfiguration.AudioChannels[i].channelName_2);
                panelName_1.Add(AudioChannelsConfiguration.AudioChannels[i].panelName_1);
                panelName_2.Add(AudioChannelsConfiguration.AudioChannels[i].panelName_2);
                deviceName.Add(AudioChannelsConfiguration.AudioChannels[i].audioDeviceName);
			}
			NamePacker myPacker_1 = new NamePacker();
			NamePacker myPacker_2 = new NamePacker();
            //NamePacker myPacker_p1 = new NamePacker();
            //NamePacker myPacker_p2 = new NamePacker();
            NamePacker myPacker_dn = new NamePacker();

			myPacker_1.CreatePackedStringsList_100_by_32(channelName_1);
			myPacker_2.CreatePackedStringsList_100_by_32(channelName_2);
            //myPacker_p1.CreatePackedStringsList_100_by_32(panelName_1);
            //myPacker_p2.CreatePackedStringsList_100_by_32(panelName_2);
            myPacker_dn.CreatePackedStringsList_100_by_32(deviceName);

			unsafe
			{
				fixed (NamePacker.deviceNames* p_1 = &(myPacker_1.devNamGrp))
				fixed (NamePacker.deviceNames* p_2 = &(myPacker_2.devNamGrp))
                //fixed (NamePacker.deviceNames* p_p1 = &(myPacker_p1.devNamGrp))
                //fixed (NamePacker.deviceNames* p_p2 = &(myPacker_p2.devNamGrp))
                fixed (NamePacker.deviceNames* p_dn = &(myPacker_dn.devNamGrp))
				{
					byte* myPacker_pp_1 = (byte*)p_1;
					byte* myPacker_pp_2 = (byte*)p_2;
                    //byte* myPacker_pp_p1 = (byte*)p_p1;
                    //byte* myPacker_pp_p2 = (byte*)p_p2;
                    byte* myPacker_pp_dn = (byte*)p_dn;

					Debug.LogFormat("Opening audio stream: sampleRate={0}, numberOfChannels={1}, bufferLength={2}", sampleRate, numberOfChannels, bufferLength);
					OpenStreams_remote(sampleRate, numberOfChannels, (uint)bufferLength, myPacker_pp_dn, myPacker_pp_1, myPacker_pp_2);
				}
			}
		}

        public void MuteAudioChannel(string panelName)
        {
            Debug.Log("Muting audio channel " + panelName);
            foreach (AudioChannel c in AudioChannelsConfiguration.AudioChannels)
            {
                if (c.panelName_1 == panelName && c.muteWhenUnoccupied)
                    c.gain_1 = MUTED_GAIN;
                else if (c.panelName_2 == panelName && c.muteWhenUnoccupied)
                    c.gain_2 = MUTED_GAIN;
            }
        }

        public void UnMuteAudioChannel(string panelName)
        {
            Debug.Log("calling UnMuteAudioChannel " + panelName);
            foreach (AudioChannel c in AudioChannelsConfiguration.AudioChannels)
            {
                if (c.panelName_1 == panelName && c.muteWhenUnoccupied)
                    c.gain_1 = 1.0f;
                else if (c.panelName_2 == panelName && c.muteWhenUnoccupied)
                    c.gain_2 = 1.0f;
            }
        }

        public void SetAudioChannelValue(string panelName, float input)
        {
            foreach (AudioChannel c in AudioChannelsConfiguration.AudioChannels)
            {
                if (c.panelName_1 == panelName)
                    c.gain_1 = input;
                else if (c.panelName_2 == panelName)
                    c.gain_2 = input;
            }
        }

        private unsafe void CloseStreams()
		{
			Debug.Log("Closing all audio streams");
			CloseStreams_remote();
		}

		void OnAudioFilterRead(float[] data, int channels)
		{
            lock (this)
            {
                float[] firstStage;
                if (firstStageBuffer.Length != data.Length) firstStageBuffer = new float[data.Length];
                int dataLen = data.Length / channels;

                if (AudioChannelsConfiguration.outputGain != 1.0f)
                {
                    for (int i = 0; i < data.Length; i++) firstStageBuffer[i] = data[i] * AudioChannelsConfiguration.outputGain;
                    firstStage = firstStageBuffer;
                }
                else
                {
                    firstStage = data;
                }

                SendChannelData();
                SendAudioPacket(dataLen, channels, firstStage);
                int n = 0;

                while (n < dataLen)
                {
                    int i = 0;
                    while (i < channels)
                    {
                        data[n * channels + i] *= AudioChannelsConfiguration.inUnityGain;
                        i++;
                    }
                    n++;
                }
            }
		}

		public void SendAudioPacket(int dataLen, int channels, float[] data)
		{
			sendAudioPacket_remote(dataLen, channels, data);
		}

		public void SendChannelData()
		{
			int numDeviceChannels = AudioChannelsConfiguration.AudioChannels.Count * 2;
			float[] data = new float[numDeviceChannels];
			if (IsMuted)
			{
				for (int i = 0; i < AudioChannelsConfiguration.AudioChannels.Count; i++)
				{
					data[2 * i] = MUTED_GAIN;
					data[2 * i + 1] = MUTED_GAIN;
				}
			}
			else
			{
				for (int i = 0; i < AudioChannelsConfiguration.AudioChannels.Count; i++)
				{
					data[2 * i] = AudioChannelsConfiguration.AudioChannels[i].gain_1;
					data[2 * i + 1] = AudioChannelsConfiguration.AudioChannels[i].gain_2;
				}
			}
			sendChannelData_remote(numDeviceChannels, data);
		}

		void OnApplicationQuit()
		{
            lock (this)
            {
                Debug.Log("Closing all audio streams");
                CloseStreams_remote();
                running = false;
            }
		}

		public void RestoreFromXmlConfig()
		{
			var path = GetRuntimeDmxConfigPath();
#if UNITY_EDITOR
			if (!useDefaultDevConfig)
			{
				path = EditorUtility.OpenFilePanel(
				 "Load DMX setup to Patch XML configuration file",
				 Application.dataPath + "/StreamingAssets/Haptic_DMX/",
				 "xml");
			}
#endif
			if (path.Length != 0)
			{
				Debug.LogFormat("Reading DMX audio config: {0}", path);
				AudioChannelsConfiguration.ReadFromXML(path);
				AudioChannelsSize = AudioChannelsConfiguration.AudioChannels.Count;
			}
		}

		private string GetRuntimeDmxConfigPath()
		{
			//Check location dir for config file
			if (File.Exists(Path.Combine(Paths.DeploymentDir, "Audio_config.xml")))
			{
				return Path.Combine(Paths.DeploymentDir, "Audio_config.xml");
			}
			else
			{
				//Use default in streaming assets
				return Path.Combine(Application.dataPath, "StreamingAssets/Haptic_DMX/Audio_config.xml");
			}
		}

		public void SaveToXmlConfig()
		{
			var path = Path.Combine(Application.dataPath, "StreamingAssets/Haptic_DMX/Audio_config.xml");
#if UNITY_EDITOR
			if (!useDefaultDevConfig)
			{
				path = EditorUtility.SaveFilePanel(
				 "Save Audio Devices to XML configuration file",
				 Application.dataPath + "/StreamingAssets/Haptic_DMX/",
				 "Audio_config" + ".xml",
				 "xml");
			}
#endif
			if (path.Length != 0)
			{
				Debug.LogFormat("Saving DMX audio config: {0}", path);
				AudioChannelsConfiguration.SaveToXML(path);
			}
		}

		public void UpdateLocalDevices()
		{
			Debug.LogFormat("Updating local devices");
			AudioChannelsConfiguration.audioDeviceWorkingList.GetAudioDeviceList_();
		}

		[Serializable]
		[XmlRoot("AudioDeviceProductionList")]
		public class AudioDeviceProductionList
		{
			[XmlArray("Devices")]
			[XmlArrayItem("DeviceNames")]
			public List<string> audioDeviceNames = new List<string>();

			public void GetAudioDeviceList_()
			{
				audioDeviceNames.Clear();
				NamePacker myPacker = new NamePacker();
				unsafe
				{
					fixed (NamePacker.deviceNames* p = &(myPacker.devNamGrp))
					{
						byte* pp = (byte*)p;
						UpdateDeviceNames(pp);
					}
					Byte[] bytes = new Byte[32];
					fixed (NamePacker.deviceNames* p = &(myPacker.devNamGrp))
					{
						for (int i = 0; i < 100; i++)
						{
							for (int j = 0; j < 32; j++) bytes[j] = p->name[i * 32 + j];
							string s1 = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
							if (s1.Length > 0)
							{
								audioDeviceNames.Add(s1);
							}
						}
					}
				}
			}
			public int GetDeviceCount()
			{
				return audioDeviceNames.Count;
			}
		}

		[Serializable]
		public class AudioChannel
		{
			[XmlAttribute("channelName_1")]
			public string channelName_1 = "Unassigned";

			[XmlAttribute("channelName_2")]
			public string channelName_2 = "Unassigned";

            [XmlAttribute("panelName1")]
            public string panelName_1 = "Unassigned";

            [XmlAttribute("panelName2")]
            public string panelName_2 = "Unassigned";

            [XmlAttribute("audioDeviceName")]
			public string audioDeviceName = "None Given";

			[XmlAttribute("gain_1")]
			public float gain_1 = 0.0f;

			[XmlAttribute("gain_2")]
			public float gain_2 = 0.0f;

			[XmlAttribute("deviceListIndex")]
			public int deviceListIndex = 0;

			[XmlAttribute("isExpanded")]
			public bool isExpanded = false;

            [XmlAttribute("muteWhenUnoccupied")]
            public bool muteWhenUnoccupied = true;

            public AudioChannel() { }
		}

		[Serializable]
		[XmlRoot("configData")]
		public class ConfigData
		{
			[XmlAttribute("inUnityGain")]
			public float inUnityGain = 0.0f;

			[XmlAttribute("outputGain")]
			public float outputGain = 1.0f;

			public AudioDeviceProductionList audioDeviceWorkingList;
			public List<AudioChannel> AudioChannels = new List<AudioChannel>();

			public ConfigData() { }

			public void SaveToXML(string path)
			{
				var serializer = new XmlSerializer(typeof(ConfigData));
				using (var stream = new FileStream(path, FileMode.Create))
				{
					using (var writer = new StreamWriter(stream, Encoding.UTF8))
					{
						serializer.Serialize(writer, this);
					}
				}
			}

			public void ReadFromXML(string path)
			{
				var serializer = new XmlSerializer(typeof(ConfigData));
				try
				{
					using (var reader = XmlReader.Create(path))
					{
						ConfigData tmp = serializer.Deserialize(reader) as ConfigData;
						audioDeviceWorkingList = tmp.audioDeviceWorkingList;
						AudioChannels = tmp.AudioChannels;
						inUnityGain = tmp.inUnityGain;
						outputGain = tmp.outputGain;
					}
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("Failed reading audio config data from={0}: {1}\n{2}", path, ex.Message, ex.StackTrace);
				}
			}
		};
	}

	public class NamePacker
	{
		public unsafe struct deviceNames
		{
			public fixed byte name[100 * 32];
		};

		public deviceNames devNamGrp;

		public void CreatePackedStringsList_100_by_32(List<string> deviceNames)
		{
			unsafe
			{
				fixed (deviceNames* p = &devNamGrp)
				{
					for (int i = 0; i < 100 * 32; i++)
					{
						p->name[i] = 0;
					}
					int index = 0;
					foreach (string deviceName in deviceNames)
					{
						byte[] array = System.Text.Encoding.UTF8.GetBytes(deviceName);
						int count = Math.Min(31, array.Length);
						for (int j = 0; j < count; j++)
						{
							p->name[index * 32 + j] = array[j];
						}
						if (++index >= 100) break;
					}
				}
			}
		}

		public void CreatePackedStrings_3200(string msg)
		{
			unsafe
			{
				fixed (deviceNames* p = &devNamGrp)
				{
					byte[] array = System.Text.Encoding.UTF8.GetBytes(msg);
					int count = Math.Min(100 * 32 - 1, array.Length);
					for (int j = 0; j < count; j++)
					{
						p->name[j] = array[j];
					}
					p->name[count] = 0;
				}
			}
		}
	}

}
