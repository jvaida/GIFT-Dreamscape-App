using Artanim.Location.Data;
using Artanim.Location.Monitoring;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

using HmdMissing = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.Missing;
using HmdUnplugged = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.Unplugged;
using HmdOffHead = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.OffHead;
using HmdTrackingLost = Artanim.Location.Monitoring.OpTicketsTypes.Hmd.TrackingLost;

namespace Artanim
{

	/// <summary>
	/// Main controller for all camera interactions.
	/// </summary>
	[RequireComponent(typeof(HeadsetCalibration))]
	[RequireComponent(typeof(Animator))]
	public class MainCameraController : SingletonBehaviour<MainCameraController>
	{
		[Tooltip("Tracked player head position")]
		public TrackingRigidbody HeadRigidBody;

		[Tooltip("Root position where the cameras are created")]
		public Transform CameraRoot;
		
		[Tooltip("Prefab to the camera created at runtime")]
		public GameObject DefaultCameraTemplate;

		[Tooltip("Prefab to the camera created at runtime")]
		public GameObject DefaultCameraTemplate2019;

		[Tooltip("Observer camera used on clients")]
        public GameObject DefaultClientObserverCameraTemplate;

		[Tooltip("Prefab used as observer camera")]
		public GameObject DefaultObserverCameraTemplate;

		[Tooltip("Prefab used as server camera")]
		public GameObject DefaultServerCameraTemplate;

		[Tooltip("Default prefab used as user message displayer. Update the ExperienceSettings to change the displayer")]
		public GameObject DefaultUserMessageDisplayer;

		[Tooltip("Default init position of the VR camera when the player is not specified yet")]
		public Vector3 DefaultCameraPosition;

		[Tooltip("Offset applied by the calibration")]
		public Transform CalibrationOffset;

		[Tooltip("Global mocap offset")]
		public Transform GlobalOffset;

		[Tooltip("Avatar offset")]
		public Transform AvatarOffset;

		[Tooltip("Correction for HMD sensor data")]
		public Transform SensorCorrection;

		[Tooltip("List of game objects used for displaying debug calibration info")]
		public GameObject[] CalibrationDebugDisplay;

		[Tooltip("Network Activated component for toggling debug headset crosshair")]
		public NetworkActivated CrosshairNetworkActivated;


		public ICameraFader CameraFader
		{
			get
			{
				if (PlayerCamera && PlayerCamera.activeInHierarchy)
					return PlayerCameraFader;
				else
					return ObserverCameraFader;
			}
		}

		public GameObject ActiveCamera
		{
			get
			{
				if (PlayerCamera && PlayerCamera.activeInHierarchy)
					return PlayerCamera;
				else
					return ObserverCamera;
			}
		}

		public bool CalibrationCross
		{
			get
			{
				return (CalibrationDebugDisplay != null) && (CalibrationDebugDisplay.Count(go => go != null && go.activeInHierarchy) > 0);
			}
			set
			{
				if (CalibrationDebugDisplay != null)
				{
					foreach (var go in CalibrationDebugDisplay)
					{
						go.SetActive(value);
					}
				}
			}
		}
		
		public GameObject PlayerCamera { get; private set; }
		public GameObject ObserverCamera { get; private set; }

		public IUserMessageDisplayer UserMessageDisplayer { get; private set; }

		private Player Player;
		private HeadsetCalibration HeadsetCalibration;
		private GameObject CurrentCameraTemplate;

		private ICameraFader PlayerCameraFader;
		private ICameraFader ObserverCameraFader;
		
		#region Public interface

		/// <summary>
		/// Init camera for a player. The camera rig will be offset to the given players head (rigidbody).
		/// </summary>
		/// <param name="player">Player configuration</param>
		public void InitPlayerCamera(Player player)
		{
			if (player != null)
			{
				var skeleton = Location.SharedData.SharedDataUtils.FindChildSharedData<SkeletonConfig>(player.SkeletonId);
				if (skeleton != null)
				{
					Player = player;

					var headRigidBody = skeleton.SkeletonSubjectNames[(int)ESkeletonSubject.Head];

					if (!string.IsNullOrEmpty(headRigidBody))
					{
						//Init
						InitPlayer(headRigidBody);
						if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=yellow>Initialized camera to head rigid body: {0}</color>", headRigidBody);
					}
					else
					{
						Debug.LogErrorFormat("<color=yellow>Failed to initialize player camera. The given skeleton does not have a valid head rigidbody. SkeletonId={0}</color>", player.SkeletonId);
					}
				}
				else
				{
					if (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone) //Skeleton is not available in standalone mode
					{
						Debug.LogErrorFormat("<color=yellow>Failed to initialize player camera. The given player skeleton was null. SkeletonId={0}</color>", player.SkeletonId);
					}
				}
			}
			else
			{
				Debug.LogError("<color=yellow>Failed to initialize player camera. The given player is invalid (null)</color>");
			}
		}

		/// <summary>
		/// Removes the player initialization. The camera is positioned to the given DefaultCameraOffset.
		/// If no VR headset is available the player camera is switched off and the observer camera is
		/// enabled instead.
		/// </summary>
		public void ResetPlayerCamera()
		{
			ResetPlayer();
		}

		/// <summary>
		/// Calibrate / recalibrate player headset.
		/// The headset calibration only works if a player has already be initialized (InitPlayerCamera)
		/// and a VR headset is available.
		/// The normal calibration will delay until the headset is steady.
		/// </summary>
		public void Calibrate()
		{
			HeadsetCalibration.Calibrate();
		}

		/// <summary>
		/// Replace the current VR camera with the given one
		/// </summary>
		/// <param name="cameraTemplate">The new camera</param>
		public void ReplaceCamera(GameObject cameraTemplate)
		{
			InitCamera(cameraTemplate);
		}

		/// <summary>
		/// Show/hide the headset calibration crosshair
		/// </summary>
		public void DoToggleCalibrationCross()
		{
			if (CrosshairNetworkActivated)
			{
				CrosshairNetworkActivated.Toggle();
			}
		}

		public void ShowCalibrationCross()
		{
			if (NetworkInterface.Instance.IsTrueClient)
				CalibrationCross = true;
		}

		public void HideCalibrationCross()
		{
			if (NetworkInterface.Instance.IsTrueClient)
				CalibrationCross = false;
		}

		#endregion

		#region Unity events

		void Awake()
		{
			HeadsetCalibration = GetComponent<HeadsetCalibration>();

			//Hide calibration cross by default
			CalibrationCross = false;

			Init();
			ResetPlayer();
		}

		void LateUpdate()
		{
			//Apply global offset
			if (GlobalOffset && GlobalMocapOffset.Instance)
			{
				GlobalOffset.localPosition = GlobalMocapOffset.Instance.GlobalPositionOffset;
				GlobalOffset.localRotation = GlobalMocapOffset.Instance.GlobalRotationOffset;
			}

			//Apply avatar specific offset on client only
			if (NetworkInterface.Instance.IsClient && AvatarOffset && AvatarOffsetController.Instance)
			{
				var avatarOffset = AvatarOffsetController.Instance.MainPlayerOffset;
				AvatarOffset.localPosition = avatarOffset != null ? avatarOffset.localPosition : Vector3.zero;
				AvatarOffset.localRotation = avatarOffset != null ? avatarOffset.localRotation : Quaternion.identity;
			}

			////Check HMD state
			//MonitorHmd();
		}

		#endregion

		#region Events

		private void OnOpTicketOpened(OperationalTickets.IOpTicket report)
		{
			if (report.Data.GetType() == typeof(HmdTrackingLost))
			{
				UserMessageDisplayer.DisplayUserMessage(
					EUserMessageType.Warning,
					SDKTexts.ID_TRACKING_LOST,
					isTextId: true,
					defaultText: SDKTexts.SDK_DEFAULT_TEXTS[SDKTexts.ID_TRACKING_LOST]);
			}
		}

		private void OnOpTicketClosing(OperationalTickets.IOpTicket report)
		{
			if (report.Data.GetType() == typeof(HmdTrackingLost))
			{
				UserMessageDisplayer.HideUserMessage();
			}
		}

		#endregion

		#region Internals

		private void Init()
		{
			//Init observer camera fader
			ObserverCameraFader = CreateObserverCamera();
			ObserverCamera = (ObserverCameraFader as MonoBehaviour).gameObject;

			//Init user message displayer
			UserMessageDisplayer = CreateUserMessageDisplayer();

			//Create default camera
			InitCamera(GetDefaultCameraTemplate());

			//Init rigidbody rotation when no HMD is present and enabled
			if (HeadRigidBody)
			{
				HeadRigidBody.UpdateRotation = !XRSettings.enabled;
			}
		}

		private void InitCamera(GameObject cameraTemplate)
		{
			if (!cameraTemplate)
				cameraTemplate = GetDefaultCameraTemplate();

			if (cameraTemplate)
			{
				if (cameraTemplate == CurrentCameraTemplate)
					return;

				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=yellow>Creating camera from template: {0}</color>", cameraTemplate.name);

				//Create camera
				var newCameraInstance = Instantiate(cameraTemplate);
				if(newCameraInstance)
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=yellow>VR device present: {0}</color>", XRUtils.Instance.IsDevicePresent);

					var newCameraFader = newCameraInstance.GetComponent<ICameraFader>();
					if (newCameraFader != null)
					{
						//Check for rotation on the template. If there is one it will fail since unity stores the initial rotation on
						//VR cameras.
						if (newCameraInstance.transform.rotation != Quaternion.identity)
							Debug.LogErrorFormat("<color=yellow>The given camera template has a rotation. Set it to 0 in Unity! template={0}, rotation: {1}</color>", newCameraInstance.name, newCameraInstance.transform.rotation.eulerAngles);

						//Set active according to prev camera if available
						newCameraInstance.SetActive(PlayerCamera && PlayerCamera.activeInHierarchy);

						//Set previous faded value
						CopyFaderState(CameraFader, newCameraFader);

						//Replace current camera with new one
						UnityUtils.RemoveAllChildren(CameraRoot);

						//Parent new camera to camera root and disable it
						newCameraInstance.transform.SetParent(CameraRoot);
						newCameraInstance.transform.localPosition = Vector3.zero;
						newCameraInstance.transform.localRotation = Quaternion.identity;

						//Assign new camera
						PlayerCamera = newCameraInstance;
						PlayerCameraFader = newCameraFader;
						CurrentCameraTemplate = cameraTemplate;

						//Turn positional tracking on or off
						SetupPositionalTracking();
					}
					else
					{
						Debug.LogError("<color=yellow>The given camera template does not have a CameraFader script attached to it</color>");
						DestroyImmediate(newCameraInstance);
						return;
					}
				}
				else
				{
					Debug.LogError("<color=yellow>The given camera template could not be created</color>");
					DestroyImmediate(newCameraInstance);
					return;
				}
			}
			else
			{
				Debug.LogError("<color=yellow>No camera template set</color>");
			}
		}

		private void SetupPositionalTracking()
        {
			if (!NetworkInterface.Instance.IsTrueClient) return;

			bool disablePositionalTracking = DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone;
			if (disablePositionalTracking && GameController.Instance.CurrentPlayer != null)
            {
				disablePositionalTracking = !GameController.Instance.CurrentPlayer.IsDesktopAvatar;
			}

			bool isLegacyXR = true;
#if UNITY_2019_3_OR_NEWER
			isLegacyXR = ConfigService.Instance.ExperienceSettings.XRMode == ExperienceSettingsSO.EXRMode.Legacy;
#endif

			if (!isLegacyXR)
			{
				// New plugin system for XR
				if (PlayerCamera)
				{
					//Add tracked pose driver if needed

					//Note to developers: import the "XR Legacy Input Helpers" package if you're getting a compilation error here
					var poseDriver = PlayerCamera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
					if (!poseDriver)
					{
						poseDriver = PlayerCamera.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
						poseDriver.SetPoseSource(UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRDevice, UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.Center);
					}
					poseDriver.trackingType = disablePositionalTracking ?
						UnityEngine.SpatialTracking.TrackedPoseDriver.TrackingType.RotationOnly :
						UnityEngine.SpatialTracking.TrackedPoseDriver.TrackingType.RotationAndPosition;
				}
			}
			else
			{
				if (!ConfigService.Instance.Config.Location.Client.HMD.EnablePositionalTracking)
				{
					//Disable positional tracking using Unity API
					InputTracking.disablePositionalTracking = disablePositionalTracking;
				}
				else if (PlayerCamera)
				{
					var camTrackingController = PlayerCamera.GetComponent<CameraTrackingController>();
					if (disablePositionalTracking)
					{
						//Disable positional tracking using a sensor correction transform
						if (!camTrackingController)
						{
							camTrackingController = PlayerCamera.AddComponent<CameraTrackingController>();
							camTrackingController.Setup(SensorCorrection);
						}
					}
					else
					{
						Destroy(camTrackingController);
					}
				}
			}
		}

		private IUserMessageDisplayer CreateUserMessageDisplayer()
		{
			var messageDisplayerTemplate = DefaultUserMessageDisplayer;
			if(ConfigService.Instance.ExperienceSettings.DefaultUserMessageDisplayer)
			{
				if(ConfigService.Instance.ExperienceSettings.DefaultUserMessageDisplayer.GetComponent<IUserMessageDisplayer>() != null)
				{
					messageDisplayerTemplate = ConfigService.Instance.ExperienceSettings.DefaultUserMessageDisplayer;
				}
				else
				{
					Debug.LogWarningFormat("<color=yellow>The user message displayer configured in the ExperienceSettings ({0}) does not have a behaviour implementing the IUserMessageDisplayer interface attached. Using SDK default displayer</color>", ConfigService.Instance.ExperienceSettings.DefaultUserMessageDisplayer.name);
				}
			}

			var messageDisplayer = UnityUtils.InstantiatePrefab<IUserMessageDisplayer>(messageDisplayerTemplate, transform);
			return messageDisplayer;
		}

		private ICameraFader CreateObserverCamera()
		{
			GameObject cameraTemplate;

			//Override server camera. Only when used as server and no dev mode
			if(NetworkInterface.Instance.IsServer && DevelopmentMode.CurrentMode == EDevelopmentMode.None)
			{
				cameraTemplate = ConfigService.Instance.ExperienceSettings.DefaultServerCameraTemplate;
			}
            else if(NetworkInterface.Instance.IsTrueClient)
            {
                //Use default client observer camera as observer and not the configured observer camera
                cameraTemplate = DefaultClientObserverCameraTemplate;
            }
			else
			{
                //Use observer camera in settings
                cameraTemplate = ConfigService.Instance.ExperienceSettings.DefaultObserverCameraTemplate;
			}

			//Verify template from settings
			if(cameraTemplate && cameraTemplate.GetComponent<ICameraFader>() == null)
			{
                //Fall back to default
                Debug.LogWarningFormat("<color=yellow>The {0} camera configured in the ExperienceSettings ({1}) does not have a ICameraFader attached. Using SDK default camera</color>",
                    NetworkInterface.Instance.IsServer ? "server" : "observer", cameraTemplate.name);
				cameraTemplate = null;
			}

			//If no template found.... use default SDK camera
			if (!cameraTemplate)
			{
				if(NetworkInterface.Instance.IsServer && DevelopmentMode.CurrentMode == EDevelopmentMode.None)
					cameraTemplate = DefaultServerCameraTemplate;
				else
					cameraTemplate = DefaultObserverCameraTemplate;
			}
			
			var observerCamera = UnityUtils.InstantiatePrefab<ICameraFader>(cameraTemplate, GlobalOffset);

			//Position: Apply template offset as local offset
			(observerCamera as MonoBehaviour).transform.localPosition = cameraTemplate.transform.position;
			(observerCamera as MonoBehaviour).transform.rotation = cameraTemplate.transform.rotation;

			return observerCamera;
		}

		private void InitPlayer(string headRigidBodyName)
		{
			//Turn positional tracking on or off
			//SetupPositionalTracking();

			//Setup rigidbody
			HeadRigidBody.ResetRigidbodyName(headRigidBodyName);

			//Only monitor rigidbody in clients
			if (NetworkInterface.Instance.IsTrueClient)
			{
                //TODO Used by Hostess, we should get rid of it
                float timeout = (float)OperationalTickets.Instance.GetParamValue<HmdTrackingLost>("RigidbodyTimeout", 3);
				var ticketData = new HmdTrackingLost()
				{
					ComponentId = SharedDataUtils.MySharedId.Guid,
					RigidbodyName = headRigidBodyName,
					Timeout = timeout,
				};
				HeadRigidBody.StartMonitoringTimeout(ticketData, timeout);
			}

			//This is to make sure we don't subscribe twice
			OperationalTickets.Instance.TicketOpened -= OnOpTicketOpened;
			OperationalTickets.Instance.TicketClosing -= OnOpTicketClosing;

			//Attach timeout
			OperationalTickets.Instance.TicketOpened += OnOpTicketOpened;
			OperationalTickets.Instance.TicketClosing += OnOpTicketClosing;

			//Enable camera
			EnablePlayerCamera();

			//Setup the calibration
			HeadsetCalibration.Setup(Player, HeadRigidBody, CalibrationOffset);

			//Calibrate
			if (XRUtils.Instance.IsDevicePresent)
			{
				HeadsetCalibration.Calibrate();
			}
		}
		
		private void ResetPlayer()
		{
			//Turn positional tracking on or off
			//SetupPositionalTracking();

			//Release rigidbody
			HeadRigidBody.ResetRigidbodyName(null);
			HeadRigidBody.transform.localPosition = DefaultCameraPosition;
			HeadRigidBody.transform.localRotation = Quaternion.identity;
			HeadRigidBody.StopMonitoringTimeout();
			OperationalTickets.Instance.TicketOpened -= OnOpTicketOpened;
			OperationalTickets.Instance.TicketClosing -= OnOpTicketClosing;

			//Remove the user message if displayed
			if (UserMessageDisplayer != null)
				UserMessageDisplayer.HideUserMessage();

			//Reset player
			Player = null;

			//Enable observer camera if needed
			if(NetworkInterface.Instance.IsTrueClient && XRUtils.Instance.IsDevicePresent)
			{
				EnablePlayerCamera();
			}
			else
			{
				//Server, Observer....
				EnableObserverCamera();
			}
		}

		private void EnablePlayerCamera()
		{
			//Setup camera fader
			CopyFaderState(CameraFader, PlayerCameraFader);

			//Disable observer
			if (ObserverCamera) ObserverCamera.SetActive(false);

			//Enable player camera
			if (PlayerCamera) PlayerCamera.SetActive(true);
		}

		private void EnableObserverCamera()
		{
			//Setup camera fader
			CopyFaderState(CameraFader, ObserverCameraFader);

			//Enable observer
			if (ObserverCamera) ObserverCamera.SetActive(true);

			//Disable player camera
			if (PlayerCamera) PlayerCamera.SetActive(false);
		}

		private void CopyFaderState(ICameraFader from, ICameraFader to)
		{
			if(from != null && to != null && from != to)
			{
				if (ConfigService.VerboseSdkLog)
					Debug.LogFormat("<color=yellow>Copying fader state from {0} to {1}, state={2}</color>", (from as MonoBehaviour).gameObject.name, (to as MonoBehaviour).gameObject.name, from.GetTragetTransition());

				to.SetFaded(from.GetTragetTransition());
			}
		}

		private GameObject GetDefaultCameraTemplate()
		{
			if (ConfigService.Instance.ExperienceSettings != null && ConfigService.Instance.ExperienceSettings.DefaultCameraTemplate != null)
            {
				return ConfigService.Instance.ExperienceSettings.DefaultCameraTemplate;
            }
			else
            {
#if UNITY_2019_3_OR_NEWER
				return DefaultCameraTemplate2019;
#else
				return DefaultCameraTemplate;
#endif
            }
		}

#endregion
	}

}