using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Monitoring;
using Artanim.Location.Network;
using Artanim.Location.SharedData;

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

	public class RuntimeLocationComponent : MonoBehaviour
	{
		[SerializeField]
        GameObject CanvasUI = null;
		[SerializeField]
		Text TextExperienceName = null;
		[SerializeField]
		Text TextOptions = null;
		[SerializeField]
		Text TextComponentType = null;
		[SerializeField]
		Text TextNetworkInfo = null;
		[SerializeField]
		Text TextSharedDataName = null;
		[SerializeField]
		Text TextNetworkGUID = null;

		Utils.RingBuffer<Action> _msgQueue = new Utils.RingBuffer<Action>(14); // Can queue up to 16384 messages
		bool _keepfocus;

		public const string KEY_DOMAIN_ID = "ArtanimDomainId";
		public const string KEY_INITIAL_PEERS = "ArtanimInitialPeers";

		void Awake()
		{
			SharedDataUtils.Initialize();

			if (SharedDataUtils.IsInitialized)
				ChangeComputerId();
			else
				SharedDataUtils.Initialized += ChangeComputerId;
		}

		void ChangeComputerId()
		{
			SharedDataUtils.Initialized -= ChangeComputerId;

			// Override the computer id when appropriate switch found on the command line
			bool foundComputerId = false;
			string cid = "-computerId".ToLowerInvariant();
			foreach (var argument in Environment.GetCommandLineArgs())
			{
				if (!foundComputerId)
				{
					foundComputerId = (argument.ToLowerInvariant() == cid);
				}
				else
				{
					try
					{
						byte byteVal = 0;
						if (byte.TryParse(argument, out byteVal))
						{
							SharedDataUtils.GetMyComponent<LocationComponent>()._OverrideComputerId(new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, byteVal));
						}
						else
						{
							SharedDataUtils.GetMyComponent<LocationComponent>()._OverrideComputerId(new Guid(argument));
						}
						Debug.LogWarning("Changed computer id to: " + SharedDataUtils.GetMyComponent<LocationComponent>().ComputerId);
					}
					catch (Exception)
					{
						Debug.LogWarning("Failed to change computer id");
					}
					break;
				}
			}
		}

		void OnEnable()
		{
			var componentType = SceneController.Instance.SceneComponentType;
			if (componentType == ELocationComponentType.Undefined)
			{
				Debug.LogError("Undefined component type");
			}
			else
			{
				int domainId = PlayerPrefs.GetInt(KEY_DOMAIN_ID, -1);
				if (domainId > 0)
                {
					Debug.LogFormat("<color=white>Using Domain Id from experience settings: {0}</color>", domainId);
                }

				//Setup network connection
				Location.Helpers.NetworkSetup.CreateNetBus(componentType.ToString(), domainId >= 0 ? (uint)domainId : Location.Helpers.HelperMethods.DefaultNetworkDomainId);
				bool devMode = (componentType == ELocationComponentType.ExperienceClient) && (DevelopmentMode.CurrentMode != EDevelopmentMode.None);
				NetworkInterface.Instance.InitializeFromConfig(componentType, Location.Helpers.NetworkSetup.NetBus, action =>
				{
					if (!_msgQueue.TryPush(action)) Debug.LogError("Message queue is full");
				}, devMode);

				string peers = PlayerPrefs.GetString(KEY_INITIAL_PEERS);
				if (!string.IsNullOrEmpty(peers))
                {
					Debug.LogFormat("<color=white>Adding peers from experience settings: {0}</color>", peers);

					foreach (var ip in peers.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
					{
						Location.Helpers.NetworkSetup.NetBus.AddPeer(ip);
					}
				}

				//Setup events before connecting otherwise we risk missing events
				if (NetworkInterface.Instance.ComponentType != ELocationComponentType.IKServer)
					NetworkInterface.Instance.Subscribe<GoEmergencyState>(GoEmergencyState);

				NetworkInterface.Instance.Subscribe<ShutdownComponent>(NetworkMessage_ShutdownComponent);

				SharedDataUtils.ComponentAdded += SharedDataUtils_ComponentAddedRemoved;
				SharedDataUtils.ComponentRemoved += SharedDataUtils_ComponentAddedRemoved;

				//Connect
				NetworkInterface.Instance.Connect();
				MonitoringConnector.Initialize();

				//Initialize
				Initialize();

				// Create ExperienceConfig SharedData
				CreateExperienceConfigData();

				//Log header with application infos
				LogApplicationInfos(componentType);

#if !UNITY_EDITOR
				if (NetworkInterface.Instance.IsServer && ConfigService.Instance.ExperienceSettings.KeepFocusOnServerWindow || CommandLineUtils.GetValue("KeepFocus", true))
				{
					var hWnd = WindowUtils.GetUnityWindowHandle();
					if (hWnd != IntPtr.Zero)
					{
						Debug.Log("Enabling keep focus on window " + hWnd);
						Utils.OperatingSystem.MainWindowFocusInit(hWnd);
						_keepfocus = true;
					}
					else
					{
						Debug.LogError("Couldn't find Unity window to keep focus on");
					}
				}
#else
				_keepfocus = false;
#endif
			}
		}

		void OnApplicationQuit()
		{
			if (ConfigService.VerboseSdkLog) Debug.Log("ONAPPLICATIONQUIT CALLED");
		}

		void Initialize()
		{
			//Set the component experience name to shared data
			var expComponent = SharedDataUtils.GetMyComponent<LocationComponentWithExperience>();
			if (expComponent != null)
			{
				//Set the experience name
				//Note: ConfigService.Instance.ExperienceConfig must be not null
				expComponent.ExperienceName = ConfigService.Instance.ExperienceConfig.ExperienceName;
			}

			//Component type specific setup
			switch (NetworkInterface.Instance.ComponentType)
			{
				case ELocationComponentType.IKServer:
					break;

				case ELocationComponentType.ExperienceServer:

					//Disable VR
					DisableVR();

					//Setup quality settings
					SetupQualitySettings(NetworkInterface.Instance.ComponentType);

					break;

				case ELocationComponentType.ExperienceClient:

					//Setup quality settings
					SetupQualitySettings(NetworkInterface.Instance.ComponentType);

					//Render scale
#if UNITY_2017_3_OR_NEWER
					XRSettings.eyeTextureResolutionScale = ConfigService.Instance.Config.Location.Client.RenderScale;
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("Set VR render scale to: {0}", XRSettings.eyeTextureResolutionScale);
#else
					XRSettings.renderScale = ConfigService.Instance.Config.Location.Client.RenderScale;
					if (ConfigService.VerboseLog) Debug.LogFormat("Set VR render scale to: {0}", XRSettings.renderScale);
#endif

					//Handle development modes
					if (DevelopmentMode.CurrentMode != EDevelopmentMode.None)
					{
						//Set mode in shared data
						(expComponent as ExperienceClient).ClientMode = ExperienceClient.EClientMode.ClientAndServer;
						Debug.LogWarning("Network development model activated. The component will act as client and server.");
					}

					break;

				case ELocationComponentType.ExperienceObserver:

					//Disable VR
					DisableVR();

					//Setup quality settings
					SetupQualitySettings(NetworkInterface.Instance.ComponentType);

					Debug.LogError("Game Observer mode is deprecated");

					break;

				default:
					break;
			}

			//Setup UI
			if (CommandLineUtils.GetValue("HideUI", true) ||
				(NetworkInterface.Instance.IsClient && DevelopmentMode.CurrentMode != EDevelopmentMode.ClientServer && ConfigService.Instance.ExperienceSettings.HideUIInClient))
            {
				if (CanvasUI) CanvasUI.SetActive(false);
			}
			else
			{
				//Update UI and texts
				if (TextExperienceName) TextExperienceName.text = ConfigService.Instance.ExperienceConfig.ExperienceName;
				if (TextOptions) TextOptions.text = GetExperienceInfoString();
				if (TextComponentType) TextComponentType.text = NetworkInterface.Instance.ComponentType.ToString();
				if (TextSharedDataName) TextSharedDataName.text = SharedDataUtils.MySharedId.ToString();
				if (TextNetworkGUID) TextNetworkGUID.text = NetworkInterface.Instance.NetworkGuid.ToString();
				UpdateNetworkTextInfo();
			}
		}

		string GetExperienceInfoString()
		{
			var parts = new List<string>();
			parts.Add("PodId=" + SharedDataUtils.GetMyComponent<LocationComponent>().PodId);

			if (NetworkInterface.Instance.ComponentType != ELocationComponentType.IKServer)
            {
				if ((ConfigService.Instance.Config.Infrastructure.TeamSpeak != null) && (!ConfigService.Instance.Config.Infrastructure.TeamSpeak.Disabled)
					&& ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Teamspeak)
				{
					parts.Add("TeamSpeak");
					if (ConfigService.Instance.ExperienceConfig.TSMuteAudio) parts.Add("TSMuteAudio");
					if (ConfigService.Instance.ExperienceConfig.TSMuteMic) parts.Add("TSMuteMic");
				}
				if ((ConfigService.Instance.Config.Infrastructure.Mumble != null) && (!ConfigService.Instance.Config.Infrastructure.Mumble.Disabled)
					&& ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Mumble)
				{
					if (ConfigService.Instance.Config.Infrastructure.Mumble.DebugMode)
					{
						parts.Add("Mumble (DebugMode)");
					}
                    else
                    {
						parts.Add("Mumble");
                    }
				}
				if (ConfigService.Instance.Config.Location.Client.ShutdownOnEndSession)
				{
					parts.Add("QuitOnEndSession");
				}
				if (ConfigService.Instance.Config.Location.Client.RenderScale != 1)
				{
					parts.Add("Scaling=" + ConfigService.Instance.Config.Location.Client.RenderScale);
				}
				if (CommandLineUtils.GetValue("MuteHaptics", true))
				{
					parts.Add("MuteHaptics");
				}
			}

			// Build string from parts (3 elements per line)
			string s = "";
			for (int i = 0; i < parts.Count; i+=2)
            {
				if (s.Length > 0) s += "\n";
				s += string.Join(", ", parts.Skip(i).Take(2).ToArray());
			}
			return s;
		}

		void CreateExperienceConfigData()
		{
			if (NetworkInterface.Instance.IsServer)
			{
				var ExperienceConfigData = SharedDataController.Instance.CreateSharedData<ExperienceConfigSharedData>();

				ExperienceConfigData.TSEnabled = ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Teamspeak;
				ExperienceConfigData.TSMuteMic = ConfigService.Instance.ExperienceConfig.TSMuteMic;

				ExperienceConfigData.AllowAddPlayerWhileRunning = ConfigService.Instance.ExperienceConfig.AllowAddPlayerWhileRunning;
				ExperienceConfigData.ExperienceName = ConfigService.Instance.ExperienceConfig.ExperienceName;
				ExperienceConfigData.SeatedExperience = ConfigService.Instance.ExperienceConfig.SeatedExperience;

				foreach(var Scene in ConfigService.Instance.ExperienceConfig.StartScenes)
				{
					ExperienceConfigData.StartScenes.Add(Scene.SceneName);
				}

				foreach(var Avatar in ConfigService.Instance.ExperienceConfig.Avatars.Where(a => a.RigName != ConfigService.Instance.DesktopRig.Name))
				{
					ExperienceConfigData.Avatars.Add(Avatar.Name);
				}

				foreach(var TrackedProp in ConfigService.Instance.ExperienceConfig.TrackedProps)
				{
					ExperienceConfigData.TrackedProps.Add(TrackedProp.Name);
				}
			}
		}

		void OnDisable()
		{
			if (ConfigService.VerboseSdkLog) Debug.Log("RuntimeLocationComponent shutdown initiated");

			if (_keepfocus)
			{
				Utils.OperatingSystem.MainWindowFocusShutdown();
			}

			//Detach events. (GoEmergencyState is not detached on purpose)
			NetworkInterface.SafeUnsubscribe<ShutdownComponent>(NetworkMessage_ShutdownComponent);
			NetworkInterface.SafeUnsubscribe<GoEmergencyState>(GoEmergencyState);
			SharedDataUtils.ComponentAdded -= SharedDataUtils_ComponentAddedRemoved;
			SharedDataUtils.ComponentRemoved -= SharedDataUtils_ComponentAddedRemoved;

			//Shutdown
			MonitoringConnector.Shutdown();
			NetworkInterface.Instance.Disconnect();
		}

		void OnDestroy()
		{
            NativeController.DisposeAllSingletons();
        }

		void Update()
		{
#if EXP_PROFILING
			Monitoring.Utils.ExpProfiling.StartFrame();
#endif

			// Forward all events
			Action ev = null;
			while (_msgQueue.TryPull(ref ev))
			{
				if (ev != null)
				{
					try
					{
						ev.Invoke();
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
				else
				{
					Debug.LogError("Null element in message queue");
				}
			}
		}

		private void LogApplicationInfos(ELocationComponentType componentType)
		{
			if (ConfigService.VerboseSdkLog)
			{
                try
                {
                    // Log easily readable info
                    var info = new StringBuilder();
                    info.AppendLine("\n=== Component Info ========================================================================");

                    // Log same info for grok parser
                    var infoParse = new StringBuilder();
                    infoParse.Append("logparse");

                    AppendLogInfo("Component type", componentType, info, infoParse);

                    //Experience related infos
                    if (componentType == ELocationComponentType.ExperienceClient || componentType == ELocationComponentType.ExperienceServer || componentType == ELocationComponentType.ExperienceObserver)
                    {
                        AppendLogInfo("Experience name", ConfigService.Instance.ExperienceConfig.ExperienceName, info, infoParse);
                        AppendLogInfo("Mode", DevelopmentMode.CurrentMode, info);
                    }

                    //HMD infos
                    AppendLogInfo("HMD enabled", XRSettings.enabled, info, infoParse);
                    if (XRSettings.enabled)
                    {
                        AppendLogInfo("HMD device", XRSettings.loadedDeviceName, info, infoParse);
                        AppendLogInfo("HMD model", XRUtils.Instance.DeviceName, info, infoParse);
                        AppendLogInfo("HMD refresh rate", XRDevice.refreshRate, info, infoParse);
                        AppendLogInfo("HMD render scale", XRSettings.eyeTextureResolutionScale, info, infoParse);
                    }

                    //Display
                    AppendLogInfo("Display refresh rate", Screen.currentResolution.refreshRate, info);
                    AppendLogInfo("Display resolution", string.Format("{0}x{1}", Screen.currentResolution.width, Screen.currentResolution.height), info);

                    //Device
                    AppendLogInfo("Device model", SystemInfo.deviceModel, info, infoParse);
                    AppendLogInfo("Device name", SystemInfo.deviceName, info);
                    AppendLogInfo("Device type", SystemInfo.deviceType, info);
                    AppendLogInfo("Device memory", SystemInfo.systemMemorySize, info, infoParse);

                    //GPU
                    AppendLogInfo("GPU name", SystemInfo.graphicsDeviceName, info, infoParse);
                    AppendLogInfo("GPU type", SystemInfo.graphicsDeviceType, info);
                    AppendLogInfo("GPU vendor", SystemInfo.graphicsDeviceVendor, info);
                    AppendLogInfo("GPU version", SystemInfo.graphicsDeviceVersion, info, infoParse);
                    AppendLogInfo("GPU memory", SystemInfo.graphicsMemorySize, info, infoParse);
                    AppendLogInfo("GPU shader level", SystemInfo.graphicsShaderLevel, info, infoParse);

                    //CPU
                    AppendLogInfo("CPU type", SystemInfo.processorType, info, infoParse);
                    AppendLogInfo("CPU count", SystemInfo.processorCount, info, infoParse);
                    AppendLogInfo("CPU frequency", SystemInfo.processorFrequency, info, infoParse);

                    AppendLogInfo("Battery status", SystemInfo.batteryStatus, info);
                    AppendLogInfo("Battery level", SystemInfo.batteryLevel, info);


                    info.Append("============================================================================================");

                    Debug.Log(info.ToString());
                    Debug.Log(infoParse.ToString());
				}
				catch(Exception ex)
				{
					Debug.LogErrorFormat("Failed to retrieve application infos due to: {0}", ex.Message);
				}
			}
		}

        private void AppendLogInfo(string name, object value, StringBuilder info, StringBuilder infoParse = null)
        {
            string valueStr = value.ToString();
            info.Append(name);
            info.Append(": ");
            info.Append(valueStr);
            info.Append("\n");
            if (infoParse != null)
            {
                AppendLogParse(infoParse, name, valueStr);
            }
        }

        const string _logParseDelim = ":";
        const string _logParseSep = "->";
        const string _logParseSubsitute = "_";

        private void AppendLogParse(StringBuilder info, string name, string value)
        {
            info.Append(_logParseDelim);
            info.Append(name.Replace(_logParseDelim, _logParseSubsitute).Replace(_logParseSep, _logParseSubsitute));
            info.Append(_logParseSep);
            info.Append(value.Replace(_logParseDelim, _logParseSubsitute).Replace(_logParseSep, _logParseSubsitute));
        }

        private void GoEmergencyState(GoEmergencyState args)
		{
			SceneController.Instance.DoEmergency();
		}

		private void NetworkMessage_ShutdownComponent(ShutdownComponent args)
		{
            Debug.Log("Shutdown requested by message. Shutting down...");
            Application.Quit();
		}

		private void SharedDataUtils_ComponentAddedRemoved(LocationComponent locationComponent)
		{
			UpdateNetworkTextInfo();
		}

		private void UpdateNetworkTextInfo()
		{
			if (TextNetworkInfo) TextNetworkInfo.text = string.Format("Domain={0}, Components={1}", Location.Helpers.NetworkSetup.NetBus.DomainID, SharedDataUtils.Components.Length);
		}

		private void DisableVR()
		{
			if (XRUtils.Instance.IsDevicePresent)
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("Disabling VR headset");
				XRSettings.enabled = false;
			}
		}

		private void SetupQualitySettings(ELocationComponentType componentType)
		{
			var settingIndex = FindQualitySettingIndex(componentType);

			if (settingIndex == -1 && componentType == ELocationComponentType.ExperienceObserver)
			{
				//Try settings for client
				settingIndex = FindQualitySettingIndex(ELocationComponentType.ExperienceClient);
			}

			//Apply quality settings
			if(settingIndex > -1)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=white>Setting quality level set for {0} to {1}</color>", componentType.ToString(), QualitySettings.names[settingIndex]);
				QualitySettings.SetQualityLevel(settingIndex, true);
			}
			else
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=white>No quality level found for {0}, leaving setup default level.</color>", componentType.ToString());
			}

			//Override VSync and target FPS if needed
			switch (componentType)
			{
				case ELocationComponentType.ExperienceServer:
					//VSync
					if (QualitySettings.vSyncCount != 0)
					{
						if (ConfigService.VerboseSdkLog) Debug.Log("<color=white>Overriding V-Sync for ExperienceServer. Setting it to 'Don't Sync'</color>");
						QualitySettings.vSyncCount = 0;
					}

					//Target FPS from config
					Application.targetFrameRate = ConfigService.Instance.ExperienceConfig.ServerFPS;
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=white>Set configured ExperienceServer target FPS to {0}</color>", Application.targetFrameRate);
					
					break;

				case ELocationComponentType.ExperienceClient:
					//VSync
					if (QualitySettings.vSyncCount != 1)
					{
						if (ConfigService.VerboseSdkLog) Debug.Log("<color=white>Overriding V-Sync for ExperienceClient. Setting it to 'Every V Blank'</color>");
						QualitySettings.vSyncCount = 1;
					}
					break;

				case ELocationComponentType.ExperienceObserver:
					//Leave as is
					break;
				default:
					break;
			}
		}

		private int FindQualitySettingIndex(ELocationComponentType componentType)
		{
			return Array.IndexOf(QualitySettings.names, componentType.ToString());
		}
	}
}