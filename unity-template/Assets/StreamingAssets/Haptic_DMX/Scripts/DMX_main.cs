using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using UnityEngine.UI;
using Artanim.Utils;
using Artanim.Location.Helpers;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dreamscape
{

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	unsafe struct Descriptor_Header
	{
		public fixed byte H1[8];
		public ushort OpCode;
		public ushort PortVer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	unsafe struct ArtDMX_packet
	{
		public byte Sequence; // = 0x00
		public byte Physical; // = 0x00
		public ushort Universe; // = 0x00
		public ushort Length; // = 512 reversed ind
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	unsafe struct DmxChannels
	{
		public fixed byte bytearray[512];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	unsafe struct DmxMessage
	{
		public Descriptor_Header descriptor_Header;
		public ArtDMX_packet artDMX_Packet;
		public DmxChannels dmxChannels;
	}

	[ExecuteInEditMode]
	unsafe public class DMX_main : MonoBehaviour
	{
        private const int NUM_DMX_CHANNELS = 512;

        [HideInInspector] public string defaultDmxConfigFile;

		public string dmxIp = "*.*.*.49";
		public int dmxPort = 6454;

		public DMX_device[] dMX_Devices;

        public bool inspectorSendDmx = false;

		private Socket _dmxSocket;
		private Socket dmxSocket
		{
			get
			{
				if (_dmxSocket == null)
                {
                    Debug.LogFormat("Connecting to DMX endpoint: {0}", dmxEndPoint);
                    _dmxSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                }
                return _dmxSocket;
			}
		}

		private IPEndPoint _dmxEndPoint;
		private IPEndPoint dmxEndPoint
		{
			get
			{
                if (_dmxEndPoint == null)
                {
					int port = dmxPort;
					string host = null;
					if (File.Exists(Artanim.Location.Config.SystemConfig.Pathname))
					{
						// Load system config
						var config = Artanim.Location.Config.SystemConfig.Instance;
						if ((config != null) && (config.Pod != null))
						{
							host = config.Pod.DmxServer;
						}
                    }

					if ((host != null) && host.Contains(":"))
					{
						int systemPort;
						var parts = host.Split(':');
						if ((parts.Length != 2) || string.IsNullOrEmpty(parts[0])
							|| (!int.TryParse(parts[1], out systemPort)) || (systemPort <= 0))
						{
							Debug.LogErrorFormat("Invalid QSys host in {0} -> {1}", Artanim.Location.Config.SystemConfig.Pathname, host);
							host = null;
						}
						else
						{
							// Use host and port from system.xml
							host = parts[0];
							port = systemPort;
						}
					}

					// If invalid, fall back to config.xml
					bool useConfig = string.IsNullOrEmpty(host);
					Debug.LogFormat("Using DMX host value from {0}", useConfig ? Path.GetFileName(defaultDmxConfigFile) : Artanim.Location.Config.SystemConfig.Pathname);
					if (useConfig)
					{
						host = HelperMethods.ResolveIPAddress(dmxIp);
					}

					_dmxEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
				}
				return _dmxEndPoint;
			}
		}

		private DmxMessage dmxMessage;

		public bool IsMuted { get; private set; }

		private string GetRuntimeDmxConfigPath()
		{
			//Check location dir for config file
			if (File.Exists(Path.Combine(Paths.DeploymentDir, "DMX_config.xml")))
			{
				return Path.Combine(Paths.DeploymentDir, "DMX_config.xml");
			}
			else
			{
				//Use default in streaming assets
				return Path.Combine(Application.dataPath, "StreamingAssets/Haptic_DMX/DMX_config.xml");
			}
		}


		private void Start()
		{
			IsMuted = Artanim.CommandLineUtils.GetValue("MuteHaptics", true);
			if (IsMuted)
			{
				Debug.LogWarning("DMX channels values will always be muted (set to zero)");
			}

			defaultDmxConfigFile = GetRuntimeDmxConfigPath();

			Debug.LogFormat("Loading default config file: {0}", defaultDmxConfigFile);
			RestoreToDmxPatchXML_(defaultDmxConfigFile);
			fixed (DmxMessage* p = &dmxMessage)
			{
				for (int i = 0; i < NUM_DMX_CHANNELS; i++)
					p->dmxChannels.bytearray[i] = 0;

				byte[] bytes = Encoding.ASCII.GetBytes("Art-Net\0");

				for (int i = 0; i < 8; i++)
					p->descriptor_Header.H1[i] = bytes[i];

				p->descriptor_Header.OpCode = 0x5000;
				p->descriptor_Header.PortVer = 0x0e00;
				p->artDMX_Packet.Sequence = 0;
				p->artDMX_Packet.Physical = 0;
				p->artDMX_Packet.Universe = 0;
				p->artDMX_Packet.Length = 0x0002;
			}

			SendPacket();
		}

		private void Update()
		{
			var a = dmxSocket;

			fixed (DmxMessage* p = &dmxMessage)
			{
				if (dMX_Devices != null)
				{
					for (var d = 0; d < dMX_Devices.Length; ++d)
					{
						var device = dMX_Devices[d];

						// string deviceName = device.dmxDevice.deviceName;
						if (device != null && device.dmxDevice != null && device.dmxDevice.dmxChannel != null)
						{
							for (var c = 0; c < device.dmxDevice.dmxChannel.Count; ++c)
							{
								var channelData = device.dmxDevice.dmxChannel[c];
								int dmxChannel = channelData.channel - 1;

								float dmxValue = Mathf.Clamp(!device.dmxDevice.isDisabled ? channelData.value : 0f, 0f, 255f);

								if (dmxChannel > -1 && dmxChannel < NUM_DMX_CHANNELS)
								{
									p->dmxChannels.bytearray[dmxChannel] = (byte)(IsMuted ? 0f : dmxValue);
								}
							}
						}

					}
				}
			}

			SendPacket();
		}

		private void OnDestroy()
		{
			fixed (DmxMessage* p = &dmxMessage)
			{
				for (int i = 0; i < NUM_DMX_CHANNELS; i++)
					p->dmxChannels.bytearray[i] = 0;

				SendPacket();
			}
		}

		private void SendPacket()
		{
#if UNITY_EDITOR
			if (!inspectorSendDmx)
				return;
#endif

			// byte[] send_buffer = Encoding.ASCII.GetBytes(text_to_send);
			byte[] sendBuffer = GetBytes(dmxMessage);

			try
			{
				//Debug.LogFormat("Sent {0}bytes to {1}", sendBuffer.Length, dmxEndPoint.ToString());
				dmxSocket.SendTo(sendBuffer, dmxEndPoint);
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Error sending DMX packet: EndPoint={0}, Error={0}\n{1}", dmxEndPoint.ToString(), ex.Message, ex.StackTrace);
			}
		}

		private byte[] GetBytes(DmxMessage str)
		{
			int size = Marshal.SizeOf(str);
			byte[] arr = new byte[size];

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(str, ptr, true);
			Marshal.Copy(ptr, arr, 0, size);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

#if UNITY_EDITOR
		public void RestoreFromDmxPatchXML()
		{
			var path = EditorUtility.OpenFilePanel(
				"Load DMX setup to Patch XML configuration file",
				Application.dataPath + "/StreamingAssets/Haptic_DMX/",
				"xml");

			if (!string.IsNullOrEmpty(path))
			{
				Debug.LogFormat("Loading DMX config file: {0}", path);
				string cwd = Directory.GetCurrentDirectory();
				cwd = Path.GetFullPath(cwd);
				path = Path.GetFullPath(path);
				string relativePath = EvaluateRelativePath(cwd, path);
				defaultDmxConfigFile = relativePath;

				RestoreToDmxPatchXML_(path);
			}
		}
#endif

		private void RestoreToDmxPatchXML_(string path)
		{
			DmxDevicesContainer fromFile = DmxDevicesContainer.Restore(path);

			dmxIp = fromFile.dmxIp;
			dmxPort = fromFile.dmxPort;
			foreach (DMX_device.DmxDevice fileDev in fromFile.dmxDevice)
			{
				fileDev.fileDeviceFound = false;
			}

			if (dMX_Devices != null)
			{
				foreach (DMX_device dev in dMX_Devices)
				{
					if (dev != null)
					{

#if UNITY_EDITOR
						DMX_device.DmxDevice.ErrorLevel matchFound = dev.dmxDevice.SetMatch(fromFile.dmxDevice);
						if (matchFound == DMX_device.DmxDevice.ErrorLevel.PartialMatch)
						{
							EditorUtility.DisplayDialog("WARNING:",
								"Channels names in Device " + dev.dmxDevice.deviceName + " do not match the channel names in the configuration file. \n\n" +
								"Restore was halted in progress.",
								"Ok, got it");
							break;
						}
						if (matchFound == DMX_device.DmxDevice.ErrorLevel.NoMatch)
						{
							EditorUtility.DisplayDialog("WARNING:",
								"Could not find a match in the configuration file for device: " + dev.dmxDevice.deviceName + "\n\n" +
								"Restore was halted in progress.",
								"Ok, got it");
							break;
						}
#else
                    dev.dmxDevice.SetMatch(fromFile.dmxDevice);
#endif
                    }
                }
#if UNITY_EDITOR
				foreach (DMX_device.DmxDevice fileDev in fromFile.dmxDevice)
				{
					if (!fileDev.fileDeviceFound)
					{
						EditorUtility.DisplayDialog("WARNING:",
							"Device: " + fileDev.deviceName + " is listed in the configuration file, but does not exist in \"DMX_Devices\"\n\n" +
							"No further consistency checks were made.  There may be additional errors.",
							"Ok, got it");
						break;
					}
				}
#endif
			}
		}
#if UNITY_EDITOR
		public void SaveToDmxPatchXML()
		{
			var path = EditorUtility.SaveFilePanel(
				"Save DMX setup to Patch XML configuration file",
				Application.dataPath + "/StreamingAssets/Haptic_DMX/",
				"DMX_config" + ".xml",
				"xml");

			if (!string.IsNullOrEmpty(path))
			{
				Debug.LogFormat("Saving DMX config file: {0}", path);
				string cwd = Directory.GetCurrentDirectory();
				cwd = Path.GetFullPath(cwd);
				path = Path.GetFullPath(path);
				string relativePath = EvaluateRelativePath(cwd, path);
				defaultDmxConfigFile = relativePath;

				SaveToDmxPatchXML_(path);
			}
		}
#endif

		public void SaveToDmxPatchXML_(string path)
		{
			DmxDevicesContainer outputSet = new DmxDevicesContainer();

			if (dMX_Devices != null)
			{
				foreach (DMX_device dev in dMX_Devices)
				{
					if (dev != null)
					{
						outputSet.dmxDevice.Add(dev.dmxDevice);
					}
				}
			}
			outputSet.dmxIp = dmxIp;
			outputSet.dmxPort = dmxPort;

			outputSet.Save(path);
		}


		[XmlRoot("DmxDeviceCollection")]
		public class DmxDevicesContainer
		{
			[XmlAttribute("dmxIp")]
			public string dmxIp = "11.11.11.11";

			[XmlAttribute("dmxPort")]
			public int dmxPort = 1111;

			[XmlArray("Devices")]
			[XmlArrayItem("Device")]
			public List<DMX_device.DmxDevice> dmxDevice = new List<DMX_device.DmxDevice>();

			public void Save(string path)
			{
				var serializer = new XmlSerializer(typeof(DmxDevicesContainer));
				using (var stream = new FileStream(path, FileMode.Create))
				{
					using (var writer = new StreamWriter(stream, Encoding.UTF8))
					{
						serializer.Serialize(writer, this);
					}
				}
			}

			public static DmxDevicesContainer Restore(string path)
			{
				var type = new DmxDevicesContainer();
				var serializer = new XmlSerializer(typeof(DmxDevicesContainer));
				try
				{
					using (var reader = XmlReader.Create(path))
					{
						type = serializer.Deserialize(reader) as DmxDevicesContainer;
					}
				}
				catch (IOException ex)
				{
					Debug.LogErrorFormat("Failed reading DMX config data from={0}: {1}\n{2}", path, ex.Message, ex.StackTrace);
				}
				return type;
			}
		}

		public string EvaluateRelativePath(string mainDirPath, string absoluteFilePath)
		{
			string[] firstPathParts = mainDirPath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
			string[] secondPathParts = absoluteFilePath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

			int sameCounter = 0;
			for (int i = 0; i < Math.Min(firstPathParts.Length,
			secondPathParts.Length); i++)
			{
				if (
				!firstPathParts[i].ToLower().Equals(secondPathParts[i].ToLower()))
				{
					break;
				}
				sameCounter++;
			}

			if (sameCounter == 0)
			{
				return absoluteFilePath;
			}

			string newPath = String.Empty;
			for (int i = sameCounter; i < firstPathParts.Length; i++)
			{
				if (i > sameCounter)
				{
					newPath += Path.DirectorySeparatorChar;
				}
				newPath += "..";
			}
			if (newPath.Length == 0)
			{
				newPath = ".";
			}
			for (int i = sameCounter; i < secondPathParts.Length; i++)
			{
				newPath += Path.DirectorySeparatorChar;
				newPath += secondPathParts[i];
			}
			return newPath;
		}
	}

}