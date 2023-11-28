using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	/// <summary>
	/// Main controller for game client and server.
	/// Responsible to start a session, create avatars.
	/// It will first load a construct scene which is supposed to be the visual content of the experience before the player's skeleton is calibrated and the session is created.
	/// Once the session is created the construct scene is unloaded.
	/// This class also controls the initialization of the tracking and TS3 connection. Additionally, it's transform is the root of the GlobalMocapOffset if used. This allows to offset
	/// all players depending on the experiences need.
	/// </summary>
	[RequireComponent(typeof(GlobalMocapOffset))]
	[RequireComponent(typeof(AvatarOffsetController))]
	public class GameController : SingletonBehaviour<GameController>
	{
		#region Events and actions

		public delegate void OnJoinedSessionHandler(Session session, Guid playerId);
		public event OnJoinedSessionHandler OnJoinedSession;

		public delegate void OnLeftSessionHandler();
		public event OnLeftSessionHandler OnLeftSession;

		public delegate void OnPlayerCalibratedHandler(RuntimePlayer player);
		public event OnPlayerCalibratedHandler OnPlayerCalibrated;

		public delegate void OnSceneLoadedInSessionHandler(string[] sceneNames, bool sceneLoadTimedOut);
		public event OnSceneLoadedInSessionHandler OnSceneLoadedInSession;

		public delegate void OnMainPlayerPropertyChangedHandler(Player player, string propertyName);
		public event OnMainPlayerPropertyChangedHandler OnMainPlayerPropertyChanged;

		public delegate void OnSessionPlayerJoinedHandler(Session session, Guid playerId);
		public event OnSessionPlayerJoinedHandler OnSessionPlayerJoined;

		public delegate void OnSessionPlayerLeftHandler(Session session, Guid playerId);
		public event OnSessionPlayerLeftHandler OnSessionPlayerLeft;

		public delegate void OnSessionStartedHandler();
		public event OnSessionStartedHandler OnSessionStarted;

		public delegate void OnReliableMessageHandler(object data, bool sendBySelf);
		public event OnReliableMessageHandler OnReliableMessage;

		public delegate void OnStreamingMessageHandler(object data, bool sendBySelf);
		public event OnStreamingMessageHandler OnStreamingMessage;

		#endregion

		public enum ESceneTransitionSync { ExperienceDefault, DontWaitForSync, WaitForSync }

		public Transform AvatarsRoot;

		public Text TextSessionId;
		public Text TextStatus;

		public Toggle ToggleAudio;

		public GameObject StandaloneControllerTemplate;

		private readonly List<RuntimePlayer> _RuntimePlayers = new List<RuntimePlayer>();
		public List<RuntimePlayer> RuntimePlayers
		{
			get { return _RuntimePlayers; }
		}

		/// <summary>
		/// The current player id in the location. This value is null if running in server.
		/// </summary>
		public Guid CurrentPlayerId
		{
			get
			{
				var mainPlayer = CurrentPlayer;
				return mainPlayer != null ? mainPlayer.Player.ComponentId : Guid.Empty;
			}
		}

		public RuntimePlayer CurrentPlayer
		{
			get
			{
				for (int i = 0, iMax = RuntimePlayers.Count; i < iMax; ++i)
				{
					var player = RuntimePlayers[i];
					if (player.IsMainPlayer)
					{
						return player;
					}
				}
				return null;
			}
		}

		public Session CurrentSession { get; private set; }

		private const string SKELETON_NAME_FORMAT = "Skeleton-{0}";

		private TS3Controller TS3Controller;
		private MumbleController MumbleController;
		private OVRLipSyncController OVRLipSyncController;
		private StandaloneController StandaloneController;

		private class Avatar
		{
			public string Resource;
			public string RigName;
		}

		private Dictionary<string, Avatar> Avatars = new Dictionary<string, Avatar>();
		private Dictionary<string, Avatar> DesktopAvatars = new Dictionary<string, Avatar>();

		#region Unity events

		void Start()
		{
			TS3Controller = GetComponentInChildren<TS3Controller>();
			MumbleController = GetComponentInChildren<MumbleController>();
			OVRLipSyncController = GetComponentInChildren<OVRLipSyncController>();

            if (TextSessionId)
				TextSessionId.text = "Not in session";

            //Disable TeamSpeak until needed
            TS3Controller.enabled = false;

			if(MumbleController != null)
			{
				MumbleController.ConnectIfEnabled();
			}

			//Disable OVR LipSync until needed
			OVRLipSyncController.enabled = false;

			//Turn on/off native haptics
			GetComponentInChildren<Haptics.Internal.HapticsTriggers>(includeInactive: true).gameObject.SetActive(ConfigService.Instance.ExperienceConfig.EnableNativeHaptics);

			//Handle additional modes
			if (DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
			{
				//Create standalone controller
				StandaloneController = UnityUtils.InstantiatePrefab<StandaloneController>(StandaloneControllerTemplate, transform);
			}
		}

		void OnEnable()
		{
			SceneController.Instance.OnSceneLoadFailed += OnSceneLoadFailed;

			if (SharedDataUtils.IsInitialized)
				Initialize();
			else
				SharedDataUtils.Initialized += Initialize;

			OnSceneLoadedInSession += GameController_OnSceneLoadedInSession;
        }

        private void GameController_OnSceneLoadedInSession(string[] sceneNames, bool sceneLoadTimedOut)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Scene loaded in session: sceneName={0}, timedOut={1}</color>", string.Join(", ", sceneNames), sceneLoadTimedOut);
		}

		void Initialize()
		{
			SharedDataUtils.Initialized -= Initialize;
			SharedDataUtils.SessionRemoved += SharedDataUtils_SessionRemoved;

			//Attach events
			NetworkInterface.Instance.Subscribe<LoadGameScene>(NetworkMessage_LoadGameScene);
			NetworkInterface.Instance.Subscribe<TerminateSession>(NetworkMessage_TerminateSession);
			NetworkInterface.Instance.Subscribe<RequestComponentJoinSession>(NetworkMessage_RequestComponentJoinSession);
			NetworkInterface.Instance.Subscribe<StartSession>(NetworkMessage_StartSession);
			NetworkInterface.Instance.Subscribe<ComponentJoinedSession>(NetworkMessage_ComponentJoinedSession);

			NetworkInterface.Instance.Subscribe<GenericGameMessage>(NetworkMessage_GenericGameMessage);
			NetworkInterface.Instance.Subscribe<GenericGameStreamingMessage>(NetworkMessage_GenericGameStreamingMessage);
			NetworkInterface.Instance.Subscribe<GenericClientStreamingMessage>(NetworkMessage_GenericClientStreamingMessage);

			SharedDataController.Instance.WatchPath<Session, Player, EPlayerStatus>(".Players[].Status", OnSessionPlayerStatusChanged);
			SharedDataController.Instance.WatchListPath<Session, Player>(".Players", OnSessionPlayerListChanged);

			//Specific actions by component type
			//Don't replace by switch statement or else if because of the client and server dev mode which must pass
			//both cases.
			if (NetworkInterface.Instance.IsServer)
			{
				SharedDataUtils.ComponentRemoved += SharedDataUtils_ComponentRemoved;
                SharedDataUtils.SkeletonRemoved += SharedDataUtils_SkeletonRemoved;

				NetworkInterface.Instance.Subscribe<PrepareNewSession>(NetworkMessage_PrepareNewSession);
				NetworkInterface.Instance.Subscribe<RequestPlayerJoinSession>(NetworkMessage_RequestPlayerJoinSession);
				NetworkInterface.Instance.Subscribe<PlayerCalibrationResult>(NetworkMessage_PlayerCalibrationResult);
				
				NetworkInterface.Instance.Subscribe<RequestComponentLeaveSession>(NetworkMessage_RequestComponentLeaveSession);
				NetworkInterface.Instance.Subscribe<EditSessionPlayer>(NetworkMessage_EditSessionPlayer);
				NetworkInterface.Instance.Subscribe<SetNotTrackedBodyPartOnSessionPlayer>(NetworkMessage_SetNotTrackedBodyPartOnSessionPlayer);
				NetworkInterface.Instance.Subscribe<AddNewPlayerOnSession>(NetworkMessage_AddNewPlayerOnSession);
				NetworkInterface.Instance.Subscribe<SetActiveSkeletons>(NetworkMessage_SetActiveSkeletons);

                SharedDataController.Instance.WatchPath<Session, Session, Guid>(".ExperienceServerId", OnSessionServerChanged);
				SharedDataController.Instance.WatchPath<ExperienceClient, ExperienceClient, ushort>(".TSClientId", OnServerTSClientIdChanged);
			}

			if (NetworkInterface.Instance.IsClient)
            {
                NetworkInterface.Instance.Subscribe<RecalibratePlayer>(NetworkMessage_RecalibratePlayer);
			}

			if (NetworkInterface.Instance.IsTrueClient)
            {
                SharedDataController.Instance.WatchPath<Session, Player, ushort>(".Players[].TSClientId", OnClientTSClientIdChanged);
                Location.Monitoring.Displays.Instance.CheckDisplayMissing();
			}

			//Enable / disable audio
			InitAudio();

			//AvatarResources
			var config = ConfigService.Instance.ExperienceConfig;
			foreach(var avatar in config.Avatars)
			{
				var av = new Avatar { Resource = avatar.AvatarResource, RigName = avatar.RigName };
				if (avatar.RigName == ConfigService.Instance.DesktopRig.Name)
                {
					DesktopAvatars.Add(avatar.Name, av);
				}
				else
                {
					Avatars.Add(avatar.Name, av);
				}
			}

            // Get ready to roll!
            PrepareForNewSession();

			//Server always connected to tracking and streaming topics
			if (NetworkInterface.Instance.IsServer)
            {
				TrackingController.Instance.Connect();
				NetworkInterface.Instance.ConnectStreamingTopics();
			}
		}

        void OnDisable()
		{
            SharedDataUtils.Initialized -= Initialize;
			SharedDataUtils.SessionRemoved -= SharedDataUtils_SessionRemoved;
			SharedDataUtils.ComponentRemoved -= SharedDataUtils_ComponentRemoved;
            SharedDataUtils.SkeletonRemoved -= SharedDataUtils_SkeletonRemoved;

			//Detach events
			NetworkInterface.SafeUnsubscribe<PrepareNewSession>(NetworkMessage_PrepareNewSession);
			NetworkInterface.SafeUnsubscribe<RecalibratePlayer>(NetworkMessage_RecalibratePlayer);
			NetworkInterface.SafeUnsubscribe<LoadGameScene>(NetworkMessage_LoadGameScene);
			NetworkInterface.SafeUnsubscribe<RequestComponentJoinSession>(NetworkMessage_RequestComponentJoinSession);
			NetworkInterface.SafeUnsubscribe<ComponentJoinedSession>(NetworkMessage_ComponentJoinedSession);
			NetworkInterface.SafeUnsubscribe<RequestComponentLeaveSession>(NetworkMessage_RequestComponentLeaveSession);
            NetworkInterface.SafeUnsubscribe<EditSessionPlayer>(NetworkMessage_EditSessionPlayer);
            NetworkInterface.SafeUnsubscribe<TerminateSession>(NetworkMessage_TerminateSession);
			NetworkInterface.SafeUnsubscribe<StartSession>(NetworkMessage_StartSession);
			NetworkInterface.SafeUnsubscribe<GenericGameMessage>(NetworkMessage_GenericGameMessage);
			NetworkInterface.SafeUnsubscribe<GenericGameStreamingMessage>(NetworkMessage_GenericGameStreamingMessage);
			NetworkInterface.SafeUnsubscribe<GenericClientStreamingMessage>(NetworkMessage_GenericClientStreamingMessage);
			NetworkInterface.SafeUnsubscribe<RequestPlayerJoinSession>(NetworkMessage_RequestPlayerJoinSession);
			NetworkInterface.SafeUnsubscribe<PlayerCalibrationResult>(NetworkMessage_PlayerCalibrationResult);
			NetworkInterface.SafeUnsubscribe<SetNotTrackedBodyPartOnSessionPlayer>(NetworkMessage_SetNotTrackedBodyPartOnSessionPlayer);
			NetworkInterface.SafeUnsubscribe<AddNewPlayerOnSession>(NetworkMessage_AddNewPlayerOnSession);
			NetworkInterface.SafeUnsubscribe<SetActiveSkeletons>(NetworkMessage_SetActiveSkeletons);

			var sharedDataController = SharedDataController.Instance;
            if (sharedDataController != null)
            {
                sharedDataController.UnwatchPath<Session, Player, EPlayerStatus>(".Players[].Status", OnSessionPlayerStatusChanged);
				sharedDataController.UnwatchPath<Session, Player, ushort>(".TSClientId", OnClientTSClientIdChanged);
				sharedDataController.UnwatchListPath<Session, Player>(".Players", OnSessionPlayerListChanged);
                sharedDataController.UnwatchPath<Session, Session, Guid>(".ExperienceServerId", OnSessionServerChanged);
				sharedDataController.UnwatchPath<ExperienceClient, ExperienceClient, ushort>(".TSClientId", OnServerTSClientIdChanged);
			}

			var sceneController = SceneController.Instance;
            if (sceneController != null)
            {
				SceneController.Instance.OnSceneLoadFailed -= OnSceneLoadFailed;
			}

            // Disconnect from tracking and streaming topics
			var trackingController = TrackingController.Instance;
			if (trackingController != null)
			{
				trackingController.Disconnect();
				NetworkInterface.Instance.DisconnectStreamingTopics();
			}
			//Disconnect TeamSpeak
			if ((TS3Controller != null) && TS3Controller.isActiveAndEnabled)
            {
                TS3Controller.DisconnectTeamspeak(true);
            }

			if (MumbleController != null)
            {
                MumbleController.Disconnect(false);
            }
        }

        void Update()
		{
#if !IK_SERVER
#if EXP_PROFILING
			ExpProfiling.MarkGmCtrlStart();
#endif

			var ikConnector = TrackingController.Instance.TrackingConnector as IkConnector;
			if (ikConnector != null)
            {
				ikConnector.UpdateSkeletons(RuntimePlayers);
			}
			_sessionMetrics.Update();

#if EXP_PROFILING
			ExpProfiling.MarkGmCtrlEnd();
#endif
#endif
        }

		void OnApplicationQuit()
		{
			if(CurrentSession != null)
			{
				if (NetworkInterface.Instance.IsServer)
				{
					//End session
					EndSession();
				}
				else if(NetworkInterface.Instance.IsClient)
				{
					//Leave session
					NetworkInterface.Instance.SendMessage(new RequestComponentLeaveSession
                    {
						ComponentId = NetworkInterface.Instance.NetworkGuid,
					});
				}
			}

		}

		#endregion

		#region Public interface

		public void NotifySceneLoadedInSession(string sceneName, bool sceneLoadTimedOut)
		{
			if (OnSceneLoadedInSession != null)
				OnSceneLoadedInSession(new string[] { sceneName, }, sceneLoadTimedOut);
		}

		/// <summary>
		/// Let the client "join" a session to observe it. This method only applies as Observer component.
		/// </summary>
		/// <param name="session">Session to observe</param>
		public void ObserveSession(Session session, ExperienceClient client = null)
		{
			if(NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver && session != null)
			{
				//Session switch?
				if (CurrentSession != session)
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Observing session: session={0}, client={1}</color>", session.SharedId.Description, client != null ? client.SharedId.Description : "Observer");
				}
				else
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Switching observed player: session={0}, client={1}</color>", session.SharedId.Description, client != null ? client.SharedId.Description : "Observer");
                }

                //Join session
                JoinSession(session.SharedId, client != null ? client.SharedId : Guid.Empty);
			}
		}


		/// <summary>
		/// Terminates the current session.
		///	All assigned components will leave the session and be available for new sessions.
		/// This method only has an effect when running as experience server.
		/// </summary>
		public void EndSession()
		{
			if (NetworkInterface.Instance.IsServer && CurrentSession != null && SharedDataController.Instance.OwnsSharedData(CurrentSession))
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Terminating session...</color>");

				//End session
				NetworkInterface.Instance.SendMessage(new TerminateSession());
			}
		}

		/// <summary>
		/// Request to switch to the next scene in the experience config.
		/// If the current scene is the last on the list, the first scene is loaded.
		/// This method only has an effect when running as experience server and when a ExperienceSettings is available.
		/// </summary>
		public void LoadNextScene()
		{
			if(NetworkInterface.Instance.IsServer && CurrentSession != null)
			{
				var config = ConfigService.Instance.ExperienceConfig;
				var currentScene = config.StartScenes.FirstOrDefault(s => s.SceneName == CurrentSession.CurrentScene);
				if(currentScene != null)
				{
					var newSceneIndex = config.StartScenes.IndexOf(currentScene) + 1;
					if (newSceneIndex >= config.StartScenes.Count)
						newSceneIndex = 0;

					currentScene = config.StartScenes[newSceneIndex];
					LoadGameScene(currentScene.SceneName, Transition.None, loadSequence: ELoadSequence.LoadFirst); //Load first because we don't have a transition
				}
				else
				{
					Debug.LogWarning("Failed to load next scene. Current scene not found in experience config");
				}
			}
			else
			{
				Debug.LogErrorFormat("Unable to load next scene. IsServer={0}, CurrentSession={1}", NetworkInterface.Instance.IsServer, CurrentSession);
			}
		}

		/// <summary>
		/// Request to switch to the previous scene in the experience config.
		/// If the current scene is the first on the list, the last scene is loaded.
		/// This method only has an effect when running as experience server and when a ExperienceSettings is available.
		/// </summary>
		public void LoadPrevScene()
		{
			if (NetworkInterface.Instance.IsServer && CurrentSession != null)
			{
				var config = ConfigService.Instance.ExperienceConfig;
				if (config != null)
				{
					var currentScene = config.StartScenes.FirstOrDefault(s => s.SceneName == CurrentSession.CurrentScene);
					if (currentScene != null)
					{
						var newSceneIndex = config.StartScenes.IndexOf(currentScene) - 1;
						if (newSceneIndex < 0)
							newSceneIndex = config.StartScenes.Count - 1;

						currentScene = config.StartScenes[newSceneIndex];
						LoadGameScene(currentScene.SceneName, Transition.None, loadSequence: ELoadSequence.LoadFirst); //Load first because we don't have a transition
					}
					else
					{
						Debug.LogWarning("Failed to load previous scene. Current scene not found in experience config");
					}
				}
				else
				{
					Debug.LogWarning("Failed to load previous scene. No experience config found");
				}
			}
		}

		/// <summary>
		/// Loads an experience scene with a specified transition on all session components.
		/// The parameter unloadOthers specifies if all other child scenes should be unloaded or if they should stay loaded.
		/// The optional parameter forceReload specifies if the scene must be reloaded even if it’s already loaded.
		/// This method only has an effect when running as experience server.
		/// </summary>
		/// <param name="sceneName">Name of the scene to load</param>
		/// <param name="transition">Transition used to load the scene</param>
		/// <param name="loadSequence">Sequence in which the scenes should be loaded/unloaded</param>
		/// <param name="forceReload">Forces the load of the scene even if it is already loaded</param>
		/// <param name="sceneTransitionSync">Indicates if components have to wait faded out until all have loaded the scene</param>
		/// <param name="customTransitionName">Name of the custom transition passed to the ICameraFader if transition is set to "Custom"</param>
		public void LoadGameScene(string sceneName, Transition transition, ELoadSequence loadSequence = ELoadSequence.UnloadFirst, bool forceReload = false, ESceneTransitionSync sceneTransitionSync = ESceneTransitionSync.ExperienceDefault, string customTransitionName = null)
		{
			if (NetworkInterface.Instance.IsServer)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Loading game scene: {0}</color>", sceneName);

				//Check for valid custom transition
				if(transition == Transition.Custom && string.IsNullOrEmpty(customTransitionName))
				{
					Debug.LogWarning("Requested Transition.Custom without providing a customTransitionName. Falling back to Transition=Black");
					transition = Transition.FadeBlack;
				}

				NetworkInterface.Instance.SendMessage(new LoadGameScene
				{
					SceneName = sceneName,
					Transition = transition,
					LoadSequence = loadSequence,
					ForceReload = forceReload,
					AwaitSceneSync = (LoadGameScene.ESceneTransitionSync)sceneTransitionSync,
					CustomTransitionName = transition == Transition.Custom ? customTransitionName : null,
				});

				//Update session current scene info
				CurrentSession.CurrentScene = sceneName;
			}
		}

		/// <summary>
		///
		/// </summary>
		public void DoUpdateAudio()
		{
			if (ToggleAudio)
				AudioListener.volume = ToggleAudio.isOn ? 1f : 0f;
		}

		/// <summary>
		///
		/// </summary>
		public void LeaveSession()
		{
			if (NetworkInterface.Instance.IsServer)
			{
				//Terminate session
				EndSession();
			}
			else
			{
				//Just leave session
				NetworkMessage_TerminateSession(null);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns></returns>
		public RuntimePlayer GetPlayerByPlayerId(Guid playerId)
		{
			return playerId == Guid.Empty ? null : RuntimePlayers.FirstOrDefault(p => p.Player.ComponentId == playerId);
		}

		/// <summary>
		/// Requests the IK server to calibrate the current player
		/// </summary>
		public void StartAvatarCalibration()
		{
			if (NetworkInterface.Instance.IsTrueClient)
			{
				//Send calibration message
				NetworkInterface.Instance.SendMessage(new RecalibratePlayer
				{
					ExperienceClientId = SharedDataUtils.MySharedId,
				});
			}
		}

		/// <summary>
		/// Retrieves the short name of the skeleton as listed in the config file
		/// </summary>
		/// <param name="playerId">The player id for which to retrieve the skeleton short name. By default the current player is used</param>
		/// <returns>Skeleton short name</returns>
		public string GetPlayerSkeletonName(Guid playerId = default(Guid))
		{
			if (playerId == Guid.Empty)
				playerId = CurrentPlayer.Player.ComponentId;

			var player = GetPlayerByPlayerId(playerId);
			if ((player != null) && (player.Player != null))
			{
				var skeleton = SharedDataUtils.FindChildSharedData<SkeletonConfig>(player.Player.SkeletonId);
				if (skeleton != null)
					return skeleton.Name;
			}
			return null;
		}

		/// <summary>
		/// Retrieves the mapped backpack color as Unity color.
		/// </summary>
		/// <param name="playerId">If Guid.Empty is passed, the current player is used</param>
		/// <param name="defaultColor">Color returned if no mapping was found.</param>
		/// <returns>Unity color</returns>
		public Color GetPlayerColor(Guid playerId, Color defaultColor)
		{
			var skeletonName = GetPlayerSkeletonName(playerId);
			if (!string.IsNullOrEmpty(skeletonName))
			{
				var mapping = ConfigService.Instance.Config.Location.Hostess.HostBackpackMappings.FirstOrDefault(m => m.SkeletonName == skeletonName);
				if ((mapping != null) && (!string.IsNullOrEmpty(mapping.Color)))
				{
					return UnityUtils.ARGBStringToUnityColor(mapping.Color);
				}
			}
			return defaultColor;
		}

		/// <summary>
		/// Sets the component status to ReadyForSession.
		/// This method can only be called if the experience settings ReadyForSessionMode is set to ExperienceControlled and
		/// the current component status is Registration.
		/// The SDK will set the Registration state only at startup and when a session was left.
		/// </summary>
		/// <returns>True if the state was changed. False if the described requirements are not met.</returns>
		public bool SetReadyForSession()
		{
			if (ConfigService.Instance.ExperienceSettings.ReadyForSessionMode == ExperienceSettingsSO.EReadyForSessionMode.ExperienceControlled &&
				SharedDataUtils.GetMyComponent<LocationComponentWithSession>().Status == ELocationComponentStatus.Registration)
			{
				SetComponentStatus(ELocationComponentStatus.ReadyForSession);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Send data in reliable fashion to all session's participants (including self)
		/// </summary>
		/// <param name="data">Some data that can be serialized with MessagePack</param>
		public void SendReliableData(object data)
		{
			if ((!NetworkInterface.HasInstance) || (NetworkInterface.Instance.SessionId == Guid.Empty))
			{
				throw new Exception("Reliable Data can only be send when in a session");
			}

			NetworkInterface.Instance.SendMessage(new GenericGameMessage { Value = data });
		}

		/// <summary>
		/// Send data in a best effort (unreliable) fashion to all session's participants (including self)
		/// </summary>
		/// <param name="data">Some data that can be serialized with MessagePack</param>
		public void SendStreamingData(object data)
		{
			if ((!NetworkInterface.HasInstance) || (NetworkInterface.Instance.SessionId == Guid.Empty))
			{
				throw new Exception("Streaming Data can only be send when in a session");
			}

			if (NetworkInterface.Instance.IsTrueClient)
			{
				NetworkInterface.Instance.SendMessage(new GenericClientStreamingMessage { SessionId = NetworkInterface.Instance.SessionId, Value = data });
			}
			else
			{
				NetworkInterface.Instance.SendMessage(new GenericGameStreamingMessage { Value = data });
			}
		}

		#endregion

		#region Network events

		private void NetworkMessage_PrepareNewSession(PrepareNewSession args)
		{
			var myComponent = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
			if (myComponent.SharedId != args.RecipientId)
				return;

			var status = myComponent.Status;
            switch (status)
            {
                case ELocationComponentStatus.Registration:
					Debug.LogErrorFormat("Component {0} unable to prepare a session because the status is {1}", myComponent.SharedId, status);
					break;
                case ELocationComponentStatus.ReadyForSession:
					var newSession = SharedDataController.Instance.CreateSharedData<Session>();
					Debug.LogFormat("Creating session with guid {0}", newSession.SharedId);

					//Init session values
					newSession.TSChannel = newSession.SharedId.Description;
					newSession.Experience = myComponent.ExperienceName;
					newSession.ExperienceServerId = myComponent.SharedId;
					newSession.ApiSessionId = args.ApiSessionId;

					JoinSession(newSession.SharedId, myComponent.SharedId);

					myComponent.SessionId = newSession.SharedId;

					NetworkInterface.Instance.SendMessage(new ComponentJoinedSession
					{
						Result = ComponentJoinedSession.EJoinSessionResult.Success,
						SessionId = newSession.SharedId
					});
					break;
                default:
					Debug.LogErrorFormat("Component {0} unable to prepare a session because the status is {1}, and already has a session={2}", myComponent.SharedId, status, myComponent.SessionId);
					break;
            }
        }

		private void NetworkMessage_LoadGameScene(LoadGameScene args)
		{
			//Scene sync type
			var awaitSceneSync = false;
			if (args.AwaitSceneSync != Location.Messages.LoadGameScene.ESceneTransitionSync.ExperienceDefault)
				awaitSceneSync = args.AwaitSceneSync == Location.Messages.LoadGameScene.ESceneTransitionSync.WaitForSync;
			else
				awaitSceneSync = ConfigService.Instance.ExperienceSettings.DefaultSceneLoadingType == ExperienceSettingsSO.ESceneLoadingType.WaitForSync;

			InternalLoadGameScene(args.SceneName, args.Transition, loadSequence: args.LoadSequence, forceReload: args.ForceReload, awaitSceneSync: awaitSceneSync, customTransitionName: args.CustomTransitionName);
		}

		private void NetworkMessage_StartSession(StartSession args)
		{
			//Validate to be sure...
			if (CurrentSession == null)
			{
				Debug.LogErrorFormat("Trying to start session {0}. But current session was null", args.SessionId);
				return;
			}

			if (CurrentSession.SharedId != args.SessionId)
			{
				Debug.LogErrorFormat("Trying to start session {0}. But current session is not the same", args.SessionId);
				return;
			}

			if (SharedDataController.Instance.OwnsSharedData(CurrentSession))
			{
				//In case of restart we're getting the start scene override in the request. Override the start scene in the current session
				if (!string.IsNullOrEmpty(args.StartSceneOverride))
				{
					CurrentSession.StartScene = args.StartSceneOverride;
				}

				//Update all TS client ids (just in case)
				foreach (var player in RuntimePlayers)
				{
					var client = SharedDataUtils.FindLocationComponent<ExperienceClient>(player.Player.ComponentId);
					if (client != null)
					{
						player.Player.TSClientId = client.TSClientId;
					}
				}
			}

			StartCurrentSession();
		}

		private void NetworkMessage_RequestPlayerJoinSession(RequestPlayerJoinSession args)
		{
			if(CurrentSession != null && CurrentSession.SharedId == args.SessionId && args.Player != null)
			{
				var player = args.Player;

				//Are we really owner?
				if (SharedDataController.Instance.OwnsSharedData(CurrentSession))
				{
					if (DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone
						|| CurrentSession.Status == ESessionStatus.Initializing
						|| (CurrentSession.Status == ESessionStatus.Started && ConfigService.Instance.ExperienceConfig.AllowAddPlayerWhileRunning))
					{
						//Ok... player and skeleton validation should already have been done in the HostessController
						//Add player to our session
						if (GetPlayerByPlayerId(player.ComponentId) == null)
						{
							var client = SharedDataUtils.FindLocationComponent(player.ComponentId) as ExperienceClient;
							if (client != null)
							{
								//Check user session id
								if (RemoteSessionController.Instance.HasSession)
								{
									var playerInfo = RemoteSessionController.Instance.GetPlayerInfo(player.UserSessionId);
									if (playerInfo.Id != Guid.Empty)
									{
										player.Avatar = playerInfo.AvatarId;
										player.Firstname = playerInfo.FirstName;
										player.Lastname = playerInfo.LastName;

										Debug.LogFormat("Requested to add a player in remote session. SessionId={0}, Player component={1}, UserSessionId={2}, AvatarId={3}", args.SessionId, player.ComponentId, player.UserSessionId, playerInfo.AvatarId);
									}
									else
									{
										Debug.LogErrorFormat("Requested to add a player with invalid user session id. SessionId={0}, Player component={1}, UserSessionId={2}", args.SessionId, player.ComponentId, player.UserSessionId);
										return;
									}
								}

								//Check skeleton
								if (player.IsDesktop && (player.SkeletonId == Guid.Empty))
								{
									if (!CreateAndAssignDesktopAvatarSkeleton(player))
									{
										Debug.LogErrorFormat("Requested to add a desktop player with no matching avatar. SessionId={0}, Player component={1}, Avatar={2}", args.SessionId, player.ComponentId, player.Avatar);
									}
								}

								if (player.SkeletonId != Guid.Empty)
								{
									//Update initials
									if (string.IsNullOrEmpty(player.Initials))
									{
										player.Initials = (string.IsNullOrEmpty(player.Firstname) ? string.Empty : player.Firstname[0].ToString())
											+ (string.IsNullOrEmpty(player.Lastname) ? string.Empty : player.Lastname[0].ToString());
									}

									//Update TS client id
									player.TSClientId = client.TSClientId;

									//Update session
									player.Status = EPlayerStatus.Initializing;

									Debug.LogFormat("Adding player {0} to session with status {1}", args.Player.ComponentId, player.Status);
									CurrentSession.Players.Add(player);

									//Request component to join session
									NetworkInterface.Instance.SendMessage(new RequestComponentJoinSession
									{
										RecipientId = player.ComponentId,
										ComponentId = player.ComponentId,
										SessionId = CurrentSession.SharedId,
									});
								}
								else
								{
									Debug.LogErrorFormat("Requested to add a player with no matching skeleton. SessionId={0}, Player component={1}, Skeleton id={2}", args.SessionId, player.ComponentId, player.SkeletonId);
								}
							}
							else
							{
								Debug.LogErrorFormat("Requested to add a player for unknown client. SessionId={0}, Player component={1}", args.SessionId, player.ComponentId);
							}
						}
						else
						{
							Debug.LogErrorFormat("Requested to add a player that's already into the session. SessionId={0}, Player component={1}", args.SessionId, player.ComponentId);
						}
					}
					else
					{
						Debug.LogErrorFormat("Requested to add a player into a session that's is not in the proper state. SessionId={0}, Player component={1}, Session status={2}", args.SessionId, player.ComponentId, CurrentSession.Status);
					}
				}
				else
				{
					Debug.LogErrorFormat("Requested to add a player to a session I don't own. SessionId={0}, Player component={1}", args.SessionId, player.ComponentId);
				}
			}
		}

		private bool CreateAndAssignDesktopAvatarSkeleton(Player player)
		{
			//For desktop avatars, create a calibrated skeleton
			Avatar avatar;
			if (DesktopAvatars.TryGetValue(player.Avatar, out avatar))
			{
				var skeletonName = string.Format(SKELETON_NAME_FORMAT, player.ComponentId);
				Debug.LogFormat("Creating client skeleton: {0}", skeletonName);

				var bodyPartNames = new SharedDataList<string>();
				for (var i = 0; i < Enum.GetValues(typeof(ESkeletonSubject)).Length; ++i)
				{
					bodyPartNames.Add("N/A");
				}

				var skeleton = SharedDataController.Instance.CreateSharedData<SkeletonConfig>(skeletonName, bodyPartNames);
				skeleton.Status = ESkeletonStatus.Calibrated;

				player.SkeletonId = skeleton.SharedId;
				return true;
			}

			return false;
		}

		private void NetworkMessage_RequestComponentJoinSession(RequestComponentJoinSession args)
		{
			Guid componentId = NetworkInterface.Instance.NetworkGuid;
			if (args.ComponentId == componentId)
			{
				var result = ValidateJoinSession(args.SessionId);
                if (result)
                {
                    //Join session
                    JoinSession(args.SessionId, args.ComponentId);
                }

                //Notify fail
                NetworkInterface.Instance.SendMessage(new ComponentJoinedSession
                {
                    Result = result ? ComponentJoinedSession.EJoinSessionResult.Success : ComponentJoinedSession.EJoinSessionResult.Failed,
                    SessionId = args.SessionId,
                });
            }
		}

		private bool ValidateJoinSession(Guid sessionId)
        {
            //Check our status first
            var myComponent = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
            var status = myComponent.Status;

            if ((status == ELocationComponentStatus.ReadyForSession)
				|| (DevelopmentMode.CurrentMode == EDevelopmentMode.ClientServer && status == ELocationComponentStatus.PreparingSession))
            {
                var mySession = SharedDataUtils.FindMySession();
                if (mySession != null && mySession.SharedId == sessionId)
                {
                    if (NetworkInterface.Instance.IsClient && DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone)
                    {
                        //Check player when running as a client
                        var player = mySession.Players.FirstOrDefault(p => p.ComponentId == NetworkInterface.Instance.NetworkGuid);
                        if (player != null)
                        {
							// Check skeleton
							if ((player.SkeletonId == Guid.Empty) && CreateAndAssignDesktopAvatarSkeleton(player))
                            {
								return true;
							}
							else if (SharedDataController.Instance.FindSharedDataById<SkeletonConfig>(player.SkeletonId) != null)
                            {
                                return true;
                            }
                            else
                            {
                                //Skeleton not found
                                Debug.LogErrorFormat("Component {0} unable to join session={1}, because skeleton {2} was not found.", myComponent.SharedId, sessionId, player.SkeletonId);
                            }
                        }
                        else
                        {
                            //Player not found
                            Debug.LogErrorFormat("Component {0} unable to join session={1}, because player was not found.", myComponent.SharedId, sessionId);
                        }
                    }
                    else
                    {
                        //Server or standalone, no need to check player
                        return true;
                    }
                }
                else
                {
                    //Session not found
                    Debug.LogErrorFormat("Component {0} unable to join session={1}, because the status is {2} and already assigned to session={3}", myComponent.SharedId, sessionId, status, myComponent.SessionId);
                }
            }
            else if (CurrentSession.SharedId != sessionId)
            {

                Debug.LogErrorFormat("Component {0} unable to join session={1}, because the status is {2} and already assigned to session={3}", myComponent.SharedId, sessionId, status, myComponent.SessionId);
            }
            else
            {
                Debug.LogWarningFormat("Component {0} was requested to join the same session again. SessionId={1}", myComponent.SharedId, sessionId);
                return true;
            }

            return false;
        }

		private void NetworkMessage_ComponentJoinedSession(ComponentJoinedSession args)
		{
			//Someone else joined our session?
			if ((args.SenderId != NetworkInterface.Instance.NetworkGuid)
                && (CurrentSession != null)
                && (args.SessionId == CurrentSession.SharedId)
			    && SharedDataController.Instance.OwnsSharedData(CurrentSession))
			{
				var comp = SharedDataUtils.FindLocationComponent<LocationComponentWithSession>(args.SenderId);
				if (comp is ExperienceClient)
				{
					//If its a player, add it to the scene
					var player = CurrentSession.Players.FirstOrDefault(p => p.ComponentId == args.SenderId);
					if (player != null)
					{
                        if(args.Result == ComponentJoinedSession.EJoinSessionResult.Success)
                        {
                            Debug.LogFormat("Player {0} has joined session {1}", player.ComponentId, CurrentSession.SharedId);
                            player.Status = player.IsDesktop ? EPlayerStatus.Calibrated : EPlayerStatus.NotCalibrated;
                        }
                        else
                        {
                            Debug.LogWarningFormat("Player {0} has failed to join session {1}", player.ComponentId, CurrentSession.SharedId);
                            CurrentSession.Players.Remove(player);
                        }
					}
					else
					{
						Debug.LogErrorFormat("Failed to make component join session: there is no player for it. ComponentId={0}, SessionId={1}", args.SenderId, args.SessionId);
					}
				}
				else if (comp == null)
				{
					Debug.LogErrorFormat("Failed to make component join session: unknown client. ComponentId={0}, SessionId={1}", args.SenderId, args.SessionId);
				}
            }
        }

        private void OnSessionPlayerStatusChanged(Session session, Player player, EPlayerStatus status)
		{
			if (session == CurrentSession)
			{
				switch (status)
				{
					case EPlayerStatus.Initializing:
						//Nothing to do, player not yet in session
						break;

					case EPlayerStatus.NotCalibrated:
						//Nothing to do, player added when added to session's players
						break;

					case EPlayerStatus.Calibrated:
						var runtimePlayer = GetPlayerByPlayerId(player.ComponentId);
						if (runtimePlayer != null)
						{
							//Show player avatar on next avatar update
							runtimePlayer.AvatarController.ShowAvatar(true);

							//Notify
							if (OnPlayerCalibrated != null)
								OnPlayerCalibrated(runtimePlayer);
						}
						else
						{
							Debug.LogErrorFormat("Couldn't find runtime player for calibrated player. ComponentId={0}", player.ComponentId);
						}
						break;
				}
			}
		}

		private void OnSessionPlayerListChanged(Session session, IList<Player> newPlayers, IList<Player> oldPlayers)
		{
			if (session != CurrentSession)
				return;

			if (oldPlayers != null)
			{
				foreach (var player in oldPlayers)
				{
					//Player has left session
					var playerToLeave = GetPlayerByPlayerId(player.ComponentId);
					if (playerToLeave != null)
					{
						//Remove runtime player for this player
						RemoveRuntimePlayer(playerToLeave);

						//If we are the removed player, then we're out of session
						if (player.ComponentId == NetworkInterface.Instance.NetworkGuid)
						{
							PrepareForNewSession();
						}

						//Notify leave
						if (OnSessionPlayerLeft != null)
							OnSessionPlayerLeft(CurrentSession, playerToLeave.Player.ComponentId);
					}
					else
						//Player might have never joined the session
						if ((player.Status != EPlayerStatus.Initializing)
                        //Observer can be in session without any other players
                        && (NetworkInterface.Instance.ComponentType != ELocationComponentType.ExperienceObserver))
					{
						Debug.LogErrorFormat("Couldn't find runtime player for removed player. ComponentId={0}", player.ComponentId);
					}
				}
			}

			if (newPlayers != null)
			{
				foreach (var player in newPlayers)
				{
					if (CreatePlayer(player, false))
					{
						//Notify joined
						if (OnSessionPlayerJoined != null)
							OnSessionPlayerJoined(CurrentSession, player.ComponentId);
					}
				}
			}


		}

        private void OnSessionServerChanged(Session session, Session _, Guid newServerId)
        {
            if (session == CurrentSession && newServerId != NetworkInterface.Instance.NetworkGuid)
            {
                Debug.LogFormat("Server from my session was replaced, leaving session. Session={0}, new server={1}", session.SharedId, newServerId);
                PrepareForNewSession();
            }
        }

        private void NetworkMessage_RequestComponentLeaveSession(RequestComponentLeaveSession args)
		{
			if (SharedDataController.Instance.OwnsSharedData(CurrentSession))
			{
				if (args.ComponentId == NetworkInterface.Instance.NetworkGuid)
				{
					var session = CurrentSession;

					PrepareForNewSession();

					//Update session status
					session.Status = ESessionStatus.Ended;

					// And destroy it
					SharedDataController.Instance.RemoveSharedData(session);
				}
				else
				//Remove player from session
				if (!RemovePlayerFromMySession(args.ComponentId))
				{
					Debug.LogErrorFormat("Failed to remove player from session. ComponentId={0}", args.ComponentId);
				}
			}
		}

		private bool RemovePlayerFromMySession(Guid componentId)
		{
			var player = CurrentSession.Players.FirstOrDefault(p => p.ComponentId == componentId);
			if (player == null)
			{
				return false;
			}

			Debug.LogFormat("<color=lightblue>Removing player from session. ComponentId={0}</color>", componentId);
			CurrentSession.Players.Remove(player);

			if (CurrentSession.Players.Count == 0)
			{
				//Ok, we're done... terminate the session
				Debug.LogWarningFormat("Terminating session because all players left");
				EndSession();
			}

			return true;
		}

		private void NetworkMessage_RecalibratePlayer(RecalibratePlayer args)
		{
			var runtimePlayer = GetPlayerByPlayerId(args.ExperienceClientId);
			if (runtimePlayer != null)
			{
				if (runtimePlayer.Player.SkeletonId != Guid.Empty)
				{
					if (runtimePlayer.IsMainPlayer)
					{
						var currentStatus = SharedDataUtils.GetMyComponent<LocationComponentWithSession>().Status;
						if (currentStatus >= ELocationComponentStatus.PreparingSession)
						{
							//Calibrate camera
							//Recalibration should work ok as player is supposed to not move during skeleton calibration
							MainCameraController.Instance.Calibrate();

							//Start avatar calibration if needed (Only client, exclude observer): Clients send RigSetup to IK
							Avatar Avatar;
							Avatars.TryGetValue(runtimePlayer.Player.Avatar, out Avatar);
							runtimePlayer.AvatarController.StartCalibration(true, runtimePlayer.Player, Avatar == null ? null : Avatar.RigName, args.ResetClassification);
						}
						else
						{
							Debug.LogErrorFormat("Cannot calibrate main player when component is not is session. Current status={0}", currentStatus);
						}
					}
					else
					{
						Debug.LogErrorFormat("Failed to calibrate player. The given player is not main player. ComponentId={0}", args.ExperienceClientId);
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to calibrate player. The given player does not have a skeleton assigned. ComponentId={0}", args.ExperienceClientId);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to calibrate player. Couldn't find runtime player with componentId={0}", args.ExperienceClientId);
			}
		}

		private void NetworkMessage_PlayerCalibrationResult(PlayerCalibrationResult args)
		{
			//Is the player from our session?
			var session = SharedDataUtils.FindComponentSession(args.ExperienceClientId);
			if(CurrentSession != null && session != null && CurrentSession.SharedId == session.SharedId)
			{
                //Yes...
                //Check if we own the session. This is the case when a player joins a running session
                //and is calibrated afterwards.
                if (SharedDataController.Instance.OwnsSharedData(session))
				{
					//Check if calibration was successful
					if (args.Result != PlayerCalibrationResult.ECalibrationResult.Failed)
					{
                        var player = SharedDataUtils.FindPlayerByClientId(args.ExperienceClientId);
						if (player != null)
						{
							//Update player status in shared data
							player.Status = EPlayerStatus.Calibrated;
						}
						else
						{
							Debug.LogErrorFormat("Failed to set player calibration state. Player assigned to client not found. Client component ID: {0}", args.ExperienceClientId);
						}
					}
					else
					{
						Debug.LogWarningFormat("Calibration failed for experience client: {0}", args.ExperienceClientId);
					}
				}

                //Was main player calibrated?
                var runtimePlayer = GetPlayerByPlayerId(args.ExperienceClientId);
                if(runtimePlayer != null && runtimePlayer.IsMainPlayer)
                {
                    var skeleton = SharedDataUtils.FindChildSharedData<SkeletonConfig>(runtimePlayer.Player.SkeletonId);
                    if (skeleton != null)
                        SetupRigidbodyMonitoring(skeleton);
                    else
                        Debug.LogErrorFormat("Failed to setup rigidbody monitoring. Skeleton with id={0} was not found!", runtimePlayer.Player.SkeletonId);
                }
			}
		}


		private void NetworkMessage_TerminateSession(TerminateSession args)
		{
            var session = CurrentSession;

            if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Leaving session={0} and going back to construct scene</color>", session != null ? session.SharedId.ToString() : string.Empty);

			//Back to construct
			PrepareForNewSession();

			//Update status if server
			if (session != null && SharedDataController.Instance.OwnsSharedData(session))
			{
				//Update session status
				session.Status = ESessionStatus.Ended;

				// And destroy it
				SharedDataController.Instance.RemoveSharedData(session);
			}
		}

        private void NetworkMessage_EditSessionPlayer(EditSessionPlayer args)
        {
            if (CurrentSession != null && CurrentSession.SharedId == args.SessionId)
            {
                if(SharedDataController.Instance.OwnsSharedData(CurrentSession))
                {
                    var runtimePlayer = GetPlayerByPlayerId(args.ComponentId);
                    if (runtimePlayer != null && runtimePlayer.Player != null)
                    {
						if (args.PlayerAttribute >= EPlayerAttribute.SkeletonId && runtimePlayer.Player.Status >= EPlayerStatus.Calibrated)
                        {
							Debug.LogErrorFormat("Cannot edit {0} on already calibrated player with ComponentId={1}", args.PlayerAttribute, args.ComponentId);
							return;
						}

                        Debug.LogFormat("Editing player with ComponentId={0}. Setting PlayerAttribute={1} to {2}", args.ComponentId, args.PlayerAttribute, args.Value);

                        var player = runtimePlayer.Player;
                        switch (args.PlayerAttribute)
                        {
                            case EPlayerAttribute.Initials:
                                player.Initials = args.Value;
                                break;

                            case EPlayerAttribute.Language:
                                player.Language = args.Value;
                                break;

                            case EPlayerAttribute.ShowSubtitles:
								bool showSubtitles;
								if (bool.TryParse(args.Value, out showSubtitles))
									player.ShowSubtitles = showSubtitles;
								else
									Debug.LogErrorFormat("Could not parse value={0} to PlayerAttribute={1}", args.Value, args.PlayerAttribute);
								break;

							case EPlayerAttribute.Avatar:
								player.Avatar = args.Value;
								break;

                            case EPlayerAttribute.SkeletonId:
								try
								{
									player.SkeletonId = new Guid(args.Value);
								}
								catch (FormatException)
                                {
									Debug.LogErrorFormat("Could not parse value={0} to PlayerAttribute={1}", args.Value, args.PlayerAttribute);
								}
								break;

                            case EPlayerAttribute.CalibrationMode:
								try
								{
									player.CalibrationMode = (ECalibrationMode)Enum.Parse(typeof(ECalibrationMode), args.Value);
								}
								catch (ArgumentException)
                                {
									Debug.LogErrorFormat("Could not parse value={0} to PlayerAttribute={1}", args.Value, args.PlayerAttribute);
								}
								break;

                            case EPlayerAttribute.Firstname:
								player.Firstname = args.Value;
                                break;

                            case EPlayerAttribute.Lastname:
								player.Lastname = args.Value;
                                break;

                            case EPlayerAttribute.UserSessionId:
								try
								{
									player.UserSessionId = new Guid(args.Value);
								}
								catch (FormatException)
                                {
									Debug.LogErrorFormat("Could not parse value={0} to PlayerAttribute={1}", args.Value, args.PlayerAttribute);
								}
								break;

							default:
                                Debug.LogErrorFormat("Couldn't edit unsupported PlayerAttribute={0}", args.PlayerAttribute);
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("Couldn't find player with ComponentId={0} while trying to edit PlayerAttribute={1}", args.ComponentId, args.PlayerAttribute);
                    }
                }
            }
        }

		private void NetworkMessage_SetNotTrackedBodyPartOnSessionPlayer(SetNotTrackedBodyPartOnSessionPlayer args)
		{
			if (CurrentSession == null || CurrentSession.SharedId != args.SessionId || !SharedDataController.Instance.OwnsSharedData(CurrentSession))
				return;

			var runtimePlayer = GetPlayerByPlayerId(args.ComponentId);
			if (runtimePlayer == null || runtimePlayer.Player == null)
			{
				Debug.LogWarningFormat("Couldn't find player with ComponentId={0} while trying to edit Not tracked body parts", args.ComponentId);
				return;
			}

			var player = runtimePlayer.Player;

			player.NotTrackedSkeletonSubjects = new SharedDataList<ESkeletonSubject>(args.NotTrackedBodyParts);
		}

		private void NetworkMessage_AddNewPlayerOnSession(AddNewPlayerOnSession args)
        {
			if (CurrentSession == null || CurrentSession.SharedId != args.SessionId || !SharedDataController.Instance.OwnsSharedData(CurrentSession))
				return;

			if (!ConfigService.Instance.ExperienceConfig.AllowAddPlayerWhileRunning && CurrentSession.Status == ESessionStatus.Started)
            {
				Debug.LogErrorFormat("Cannot add player while session is running. SessionId={0}", CurrentSession.SharedId);
				return;
			}

			if (args.Player == null)
            {
				Debug.LogErrorFormat("Received null player. SessionId={0}", CurrentSession.SharedId);
				return;
			}

			if (args.Player.ComponentId == Guid.Empty)
            {
				Debug.LogError("Player require a ComponentId to be added");
				return;
			}

			if (args.Player.Avatar == null)
            {
				Debug.LogError("Player requires an avatar to be added");
				return;
			}

			if (args.Player.SkeletonId == Guid.Empty)
			{
				Debug.LogError("Player requires a skeletonId to be added");
				return;
			}

			var client = SharedDataUtils.FindLocationComponent(args.Player.ComponentId) as ExperienceClient;
			if (client == null)
            {
				Debug.LogErrorFormat("Cannot find player's client. ComponentId={0}", args.Player.ComponentId);
				return;
			}

			if (client.Status != ELocationComponentStatus.ReadyForSession 
				&& (client.ClientMode != ExperienceClient.EClientMode.ClientAndServer || client.Status != ELocationComponentStatus.PreparingSession))
            {
				Debug.LogErrorFormat("Client {0} in {1} state, not ready for session", client.SharedId, client.Status);
				return;
            }

			if (CurrentSession.Players.Any(p => p.ComponentId == args.Player.ComponentId))
            {
				Debug.LogErrorFormat("Client {0} already in session {1}", args.Player.ComponentId, CurrentSession.SharedId);
				return;
            }

			if (CurrentSession.Players.Any(p => p.SkeletonId == args.Player.SkeletonId))
            {
				Debug.LogErrorFormat("Skeleton {0} already used in session {1}", args.Player.SkeletonId, CurrentSession);
				return;
            }

			args.Player.Status = EPlayerStatus.NotCalibrated;
			args.Player.TSClientId = client.TSClientId;

			Debug.LogFormat("Adding player {0} to session", args.Player.ComponentId);
			CurrentSession.Players.Add(args.Player);

			NetworkInterface.Instance.SendMessage(new RequestComponentJoinSession
			{
				RecipientId = args.Player.ComponentId,
				ComponentId = args.Player.ComponentId,
				SessionId = CurrentSession.SharedId
			});
		}

		private void NetworkMessage_SetActiveSkeletons(SetActiveSkeletons args)
		{
			if (CurrentSession == null || CurrentSession.SharedId != args.SessionId || !SharedDataController.Instance.OwnsSharedData(CurrentSession))
				return;

			CurrentSession.ActiveSkeletons = new SharedDataList<SkeletonGroup>(args.ActiveSkeletons.Select(sk => new SkeletonGroup
			{
				Group = sk.Group,
				Number = sk.Number,
				PodId = sk.PodId
			}));
		}

		private void SharedDataUtils_SessionRemoved(Session session)
		{
			if(CurrentSession != null && CurrentSession.SharedId == session.SharedId)
			{
				//Our session just got removed :-( Prepare for new session
				Debug.LogErrorFormat("My session got removed from SharedData without notification. Preparing for new session. Dead sessionId={0}", session.SharedId);
				PrepareForNewSession();
			}
		}

		private void SharedDataUtils_ComponentRemoved(LocationComponent locationComponent)
		{
			if(locationComponent is ExperienceClient)
			{
				//Was the player part of my session?
				if (SharedDataController.Instance.OwnsSharedData(CurrentSession)
					&& RemovePlayerFromMySession(locationComponent.SharedId)
                    && DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone)
				{
					Debug.LogErrorFormat("Player component of my session got removed from SharedData without notification. Removing player with componentId={0} and computerdId={1}", locationComponent.SharedId, locationComponent.ComputerId);
				}
			}
		}

        private void SharedDataUtils_SkeletonRemoved(SkeletonConfig skeletonConfig)
        {
            if(CurrentSession != null)
            {
                //Was skeleton removed from my session?
                var player = RuntimePlayers.FirstOrDefault(p => p.Player.SkeletonId == skeletonConfig.SharedId);
                if (player != null && SharedDataController.Instance.OwnsSharedData(CurrentSession))
                {
                    Debug.LogFormat("Requesting player to leave session because his skeleton was removed. SkeletonId={0}, PlayerId={1}", skeletonConfig.SharedId, player.Player.ComponentId);
                    NetworkInterface.Instance.SendMessage(new RequestComponentLeaveSession
                    {
                        ComponentId = player.Player.ComponentId,
                    });
                }
            }
        }

		void NetworkMessage_GenericGameMessage(GenericGameMessage args)
		{
			if (OnReliableMessage != null)
			{
				OnReliableMessage.Invoke(args.Value, args.SenderId == NetworkInterface.Instance.NetworkGuid);
			}
		}

		void NetworkMessage_GenericGameStreamingMessage(GenericGameStreamingMessage args)
		{
			if (OnStreamingMessage != null)
			{
				OnStreamingMessage.Invoke(args.Value, args.SenderId == NetworkInterface.Instance.NetworkGuid);
			}
		}

		void NetworkMessage_GenericClientStreamingMessage(GenericClientStreamingMessage args)
		{
			if ((OnStreamingMessage != null) && (args.SessionId == NetworkInterface.Instance.SessionId))
			{
				OnStreamingMessage.Invoke(args.Value, args.SenderId == NetworkInterface.Instance.NetworkGuid);
			}
		}

		#endregion

		#region Internals

		private void StartCurrentSession()
		{
			//Update status
			SetComponentStatus(ELocationComponentStatus.RunningSession);

			//Server side requests to load game scene
			if (NetworkInterface.Instance.IsServer)
			{
				//Don't reload start scene if it's the same as the construct scene (scene we're currently on)
				LoadGameScene(CurrentSession.StartScene, Transition.FadeBlack, loadSequence: ELoadSequence.UnloadFirst, forceReload: CurrentSession.StartScene != SceneController.Instance.MainChildSceneName);

				//Update session status
                CurrentSession.Status = ESessionStatus.Started;
				CurrentSession.StartedTime = DateTime.UtcNow;
			}

            _sessionMetrics.Start();

            //Notify
            if (OnSessionStarted != null)
				OnSessionStarted();
		}

		private void JoinSession(Guid sessionId, Guid componentId)
		{
			var session = SharedDataUtils.FindChildSharedData<Session>(sessionId);
            if (session != null)
            {
                //Yep... let's go!
                if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Joining session={0}</color>", session.SharedId);

                //Init component session
                SetComponentSession(session);

                //Display sessionId
                if (TextSessionId)
                    TextSessionId.text = string.Format("Session ID: {0}", session.SharedId);

                //Create session players / avatars
                //First remove all leftovers
                RemoveAllRuntimePlayers();

				//Create player and avatars
				foreach (var player in CurrentSession.Players)
                {
                    var isMainPlayer = player.ComponentId == NetworkInterface.Instance.NetworkGuid || player.ComponentId == componentId;
                    CreatePlayer(player, isMainPlayer);
                }

				//Connect to tracking and streaming topics once player is created
				//Note: server is already connected
				if (!NetworkInterface.Instance.IsServer)
				{
					TrackingController.Instance.Connect();
					NetworkInterface.Instance.ConnectStreamingTopics();
				}
				//Game client specifics
				var currentPlayer = CurrentPlayer;
                if (currentPlayer != null) //If this fails... Call Olivier!!!
                {
                    var thisPlayer = currentPlayer.Player;
                    if (thisPlayer != null)
                    {
                        //TeamSpeak?
                        if (ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Teamspeak && thisPlayer.TSEnabled && NetworkInterface.Instance.IsTrueClient)
                        {
                            TS3Controller.enabled = true;
                            TS3Controller.ConnectTeamspeak(CurrentSession.TSChannel, SharedDataUtils.MySharedId.Description);
                        }
                    }
                    else
                    {
                        //Very very bad one!
                        Debug.LogErrorFormat("Failed to find the playerId={0} in session={1} but the client component was in the session components list!!!", currentPlayer.Player.ComponentId, session.SharedId);
                    }
                }
                else
                {
                    //Set observer camera
                    MainCameraController.Instance.ResetPlayerCamera();
                }

                //Load game start scene. If we're joining a started session, load the current scene from the game session values otherwise load the construct scene.
                string sceneName = (CurrentSession.Status != ESessionStatus.Started ? GetConstructSceneName() : session.CurrentScene);
                InternalLoadGameScene(sceneName, Transition.FadeBlack, loadSequence: ELoadSequence.UnloadFirst, forceReload: false, awaitSceneSync: false);

                //Update component status
                SetComponentStatus(session.Status <= ESessionStatus.Initializing ? ELocationComponentStatus.PreparingSession : ELocationComponentStatus.RunningSession);

                _sessionMetrics.Join();

                try
                {
                    //Signal session joined
                    if (OnJoinedSession != null)
                    {
                        OnJoinedSession(session, componentId);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.LogErrorFormat("Component {0} could not join session={1} because it didn't find it", componentId, session.SharedId);
                return;
            }
		}

		private void InitAudio()
		{
			if (NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceServer)
			{
				AudioListener.volume = ConfigService.Instance.Config.Location.Server.AudioEnabled ? 1f : 0f;

				//Update toggle
				if (ToggleAudio)
					ToggleAudio.isOn = ConfigService.Instance.Config.Location.Server.AudioEnabled;
			}
		}

		private void OnSceneLoadFailed(string sceneName)
		{
			if (CurrentSession != null && sceneName == CurrentSession.StartScene)
			{
				Debug.LogErrorFormat("Failed to load game start scene {0}... Going back to construct...", sceneName);

				//Check if we need to kill the session
				if(NetworkInterface.Instance.IsServer && CurrentSession != null)
				{
					Debug.LogErrorFormat("Server terminating session because the requested scene could not loaded: {0}", sceneName);
					EndSession();
				}
				else
				{
					//Clients and observer
					PrepareForNewSession();
				}
			}
		}

		private bool CreatePlayer(Player player, bool mainPlayer)
		{
			//Validate
			if (player == null)
			{
				Debug.LogError("Failed to create player. The given player is null");
				return false;
			}

			//Try to create avatar
			GameObject avatarInstance = null;
			if (AvatarsRoot)
			{
				if (player.Avatar != null)
				{
					Avatar Avatar;
					string AvatarResource ="";
					
					if(player.IsDesktop)
                    {
						if (DesktopAvatars.TryGetValue(player.Avatar, out Avatar))
						{
							AvatarResource = Avatar.Resource;
						}
					}
					else if(Avatars.TryGetValue(player.Avatar, out Avatar))
					{
						AvatarResource = Avatar.Resource;
					}

					if (ConfigService.VerboseSdkLog)
						Debug.LogFormat("<color=lightblue>Creating player: IsMainPlayer={0}, PlayerId={1}, PlayerIndex={2}, avatarResource={3}, SkeletonId={4}, SessionId={5}</color>",
							mainPlayer, player.ComponentId, CurrentSession.Players.IndexOf(player), AvatarResource, player.SkeletonId, CurrentSession.SharedId);

					//Remove player first if he's already around
					var removePlayer = GetPlayerByPlayerId(player.ComponentId);
					if (removePlayer != null)
					{
						RemoveRuntimePlayer(removePlayer);
					}

					player.PropertyChanged += OnPlayerPropertyChanged;

					//Create
					GameObject avatarTemplate;
					if (string.IsNullOrEmpty(AvatarResource) && DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
						avatarTemplate = StandaloneController.GetAvatarTemplate();
					else
						avatarTemplate = Resources.Load<GameObject>(AvatarResource);

					if (avatarTemplate)
					{
						avatarInstance = Instantiate(avatarTemplate);
						if (avatarInstance)
						{
							//Setup actor
							var avatarController = avatarInstance.GetComponent<AvatarController>();
							if (avatarController)
							{
								//Create avatar offset root
								var avatarOffset = new GameObject(string.Format("AvatarOffset-{0}", player.ComponentId.ToString()));
								avatarOffset.transform.SetParent(AvatarsRoot);
								avatarOffset.transform.localPosition = Vector3.zero;
								avatarOffset.transform.localRotation = Quaternion.identity;

								//Register player
								var runtimePlayer = new RuntimePlayer(mainPlayer, player, avatarOffset.transform, avatarController, avatarInstance);
								RuntimePlayers.Add(runtimePlayer);

								//Init actor
								avatarController.InitAvatar(
									runtimePlayer,
									mainPlayer,
									player.Status == EPlayerStatus.Calibrated); //Show avatar if player is already calibrated. Can happen for e.g. when the hostess switches the avatar after calibration

								//Parent avatar visuals
								//avatarInstance.transform.SetParent(AvatarsRoot);
								avatarInstance.transform.SetParent(avatarOffset.transform);
								avatarInstance.transform.localPosition = Vector3.zero;
								avatarInstance.transform.localRotation = Quaternion.identity;

								//Main player specific
								if (mainPlayer)
								{
									//Init camera
									MainCameraController.Instance.InitPlayerCamera(player);

									//Teamspeak
									//Find head for main player: if VR is enabled use VR headset transform, otherwise use skeleton head
									runtimePlayer.SetTeamspeakListenerRoot(avatarController.HeadBone);

									//Language
									TextService.Instance.SetLanguage(player.Language);

									//Subtitle
									SubtitleController.Instance.ShowSubtitles = player.ShowSubtitles;
								}

								//Make sure it's active
								avatarInstance.gameObject.SetActive(true);

                                if(NetworkInterface.Instance.IsTrueClient)
                                {
									//Init teamspeak if ready
									if (ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Teamspeak && (runtimePlayer.Player.TSClientId > 0))
                                    {
										InitPlayerTeamSpeak(runtimePlayer);
									}

									if(MumbleController != null)
									{
					                	MumbleController.InitPlayer(runtimePlayer);
									}

									//Init lipsync if needed
									if (ConfigService.Instance.ExperienceConfig.LipsyncMode == Location.Config.ExperienceConfig.ELipsyncMode.Microphone)
                                    {
                                        //Use mic based lipsync on all compoents if avatars use lipsync
                                        InitlayerMicLipsync(runtimePlayer);
                                    }
                                }
                                

                                if (ConfigService.VerboseSdkLog)
									Debug.LogFormat("<color=lightblue>Created player: avatarResource={0}, IsMainPlayer={1}, PlayerId={2}, SkeletonId={3}, SessionId={4}</color>",
										AvatarResource, mainPlayer, player.ComponentId, player.SkeletonId, CurrentSession.SharedId);

								//All good!
								return true;
							}
							else
							{
								Debug.LogErrorFormat("Failed to create player. Could not find avatar controller={0}", AvatarResource);
							}
						}
						else
						{
							Debug.LogErrorFormat("Failed to create player. Could not instantiate avatar resource={0}", AvatarResource);
						}
					}
					else
					{
						Debug.LogErrorFormat("Failed to create player. Could not find avatar resource={0}", AvatarResource);
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to create player. Player avatar is null. Player={0}", player.ComponentId);
				}
			}
			else
			{
				Debug.LogErrorFormat("Failed to create player. No Avatar root defined");
			}

			//Something went wrong... remove avatar instance if it was already created
			if(avatarInstance)
			{
				Destroy(avatarInstance);
			}

			return false;
		}

        private bool IsClientFromMySession(Guid clientId)
        {
            if (CurrentSession != null && clientId != Guid.Empty)
            {
                return CurrentSession.Players.Any(p => p.ComponentId == clientId);
            }

            return false;
        }

        private void RemoveAllRuntimePlayers()
		{
			//Cleanly remove all known registered players
			foreach(var player in RuntimePlayers.ToArray())
			{
				RemoveRuntimePlayer(player);
			}

			//Make sure all avatars are really cleared for the next session, remove all avatars in case they haven't been removed earlier.
			if (AvatarsRoot)
			{
				for (var c = 0; c < AvatarsRoot.childCount; c++)
					Destroy(AvatarsRoot.GetChild(c).gameObject);

				RuntimePlayers.Clear();
			}
		}

		private void RemoveRuntimePlayer(RuntimePlayer player)
		{
			if (player != null && player.PlayerInstance != null)
			{
				Debug.LogFormat("<color=lightblue>Removing player {0}</color>", player.Player.ComponentId);

				//Destroy avatar
				Destroy(player.AvatarOffset.gameObject);

				//Remove from list
				if (!RuntimePlayers.Remove(player))
				{
					Debug.LogErrorFormat("Runtime player already removed {0}", player.Player.ComponentId);
				}

				player.Player.PropertyChanged -= OnPlayerPropertyChanged;

				if (player.IsMainPlayer)
				{
					ClearRigidbodyMonitoring();
				}
			}
		}

        private void OnPlayerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Player properties
            var player = sender as Player;
            if (player != null)
            {
                //General properties (Currently not updated by prod hostess)
                if (e.PropertyName == "Avatar")
                {
                    if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Avatar changed on player = {0} to {1}</color>", player.ComponentId, player.Avatar);
					CreatePlayer(player, CurrentPlayerId == player.ComponentId);
				}

				if(e.PropertyName == "SkeletonId")
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Skeleton changed on player = {0} to {1}</color>", player.ComponentId, player.Avatar);
					CreatePlayer(player, CurrentPlayerId == player.ComponentId);
				}

                //Main player specific updates
                var runtimePlayer = GetPlayerByPlayerId(player.ComponentId);
                if (runtimePlayer != null && runtimePlayer.IsMainPlayer)
                {
                    if (e.PropertyName == "ShowSubtitles")
                    {
                        SubtitleController.Instance.ShowSubtitles = player.ShowSubtitles;
                    }

                    if (e.PropertyName == "Language")
                    {
                        TextService.Instance.SetLanguage(player.Language);
                    }
                }

                //Notify experience listeners
                if (CurrentPlayerId == player.ComponentId && OnMainPlayerPropertyChanged != null)
                {
                    OnMainPlayerPropertyChanged(player, e.PropertyName);
                }
            }
        }

        void OnServerTSClientIdChanged(ExperienceClient client, ExperienceClient _, ushort tsClientId)
        {
			if (SharedDataController.Instance.OwnsSharedData(CurrentSession))
			{
				var runtimePlayer = GetPlayerByPlayerId(client.SharedId);

				// Client may be belong to a different session, in this case player will be null
				if (runtimePlayer != null)
				{
					runtimePlayer.Player.TSClientId = client.TSClientId;
				}
			}
		}

		void OnClientTSClientIdChanged(Session session, Player player, ushort tsClientId)
		{
			if (ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Teamspeak && tsClientId > 0 && CurrentSession == session)
			{
				var runtimePlayer = GetPlayerByPlayerId(player.ComponentId);
				if (runtimePlayer == null)
				{
					Debug.LogErrorFormat("Can't initialize player TeamSpeak. Failed to find runtime player for player component={0}", player.ComponentId);
				}
				else
				{
					InitPlayerTeamSpeak(runtimePlayer);
				}
			}
		}

		private void InitPlayerTeamSpeak(RuntimePlayer player)
		{
            try
            {
                //Muting:
                //Mute main player source only if config TSMuteHostessAudio is set
                //If avatar is lipsynced, don't mute TS audio but mute lipsync audio
                var isLipSyncAvatar = player.AvatarController.IsLipSyncAvatar;
                var muteTS = ConfigService.Instance.ExperienceConfig.TSMuteAudio;
                var muteTSSource = player.IsMainPlayer ? muteTS && ConfigService.Instance.ExperienceConfig.TSMuteHostessAudio : muteTS && !isLipSyncAvatar;

                //Init TS3 audio source
                TS3Controller.InitPlayer(player, muteTSSource);

                //Init lipsync? -> Don't lipsync main player, main player source is hostess.
                if (!player.IsMainPlayer && isLipSyncAvatar)
                {
                    OVRLipSyncController.InitPlayerLipSyncSource(player, muteTS);
                }
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Failed to initialize TS3 player for player component={0} due to: {1}", player.Player.ComponentId, ex.Message);
            }
		}

        private void InitlayerMicLipsync(RuntimePlayer player)
        {
            if(player.AvatarController.IsLipSyncAvatar)
            {
                var lipsyncController = player.AvatarController.GetComponent<AvatarLipSyncController>();

                if (player.IsMainPlayer)
                {
                    //Init mic based audiosource for lipsync on main player
                    OVRLipSyncController.InitPlayerLipSyncSource(player, true);

                    //Setup avatar lipsync controller to stream
                    lipsyncController.SetupLipsyncStreaming();
                }
                else
                {
                    //Setup lipsync to read from streamed lipsync messages
                    lipsyncController.SetupLipsyncListener(player.Player.ComponentId);
                }
            }

        }

		private void PrepareForNewSession()
		{
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=lightblue>Preparing for new session</color>");

            //Keep some info about the session that we are leaving
            Guid previousSessionId = CurrentSession != null ? CurrentSession.SharedId.Guid : Guid.Empty;
            bool previousSessionStarted = CurrentSession != null ? CurrentSession.Status > ESessionStatus.Initializing : false;

            //Update component status
            SetComponentStatus(ELocationComponentStatus.Registration);

            //Leave session
			SetComponentSession(null);

			//Remove avatars
			RemoveAllRuntimePlayers();

			//Setup camera
			MainCameraController.Instance.ResetPlayerCamera();

			//Update component status once loading is done
			if (ConfigService.Instance.ExperienceSettings.ReadyForSessionMode == ExperienceSettingsSO.EReadyForSessionMode.Auto)
			{
				float timeoutSecs = 10f;
				StartCoroutine(WaitForSceneLoad(GetConstructSceneName(), (bool timedOut) =>
				{
					if (timedOut)
						Debug.LogErrorFormat("Timeout loading construct scene while preparing for new session. Construct scene took longer than {0} seconds to load.", timeoutSecs);

					SetComponentStatus(ELocationComponentStatus.ReadyForSession);
				}, timeoutSecs));
			}

			//Load construct scene
			InternalLoadGameScene(GetConstructSceneName(), Transition.FadeBlack, loadSequence: ELoadSequence.UnloadFirst, forceReload: true, awaitSceneSync: false);

            //Disconnect from tracking and streaming topics except for server
            if (!NetworkInterface.Instance.IsServer)
            {
				TrackingController.Instance.Disconnect();
				NetworkInterface.Instance.DisconnectStreamingTopics();
			}

			//Client specific
			if (NetworkInterface.Instance.IsTrueClient) //Here we need to check for real component type... in editor we're client and server
			{
				//Disconnect from Teamspeak
				if (TS3Controller && TS3Controller.isActiveAndEnabled)
				{
					TS3Controller.DisconnectTeamspeak(true);
				}
			}

			//Remove any skeleton that we own
			foreach (var skeleton in SharedDataUtils.GetMyChildSharedData<SkeletonConfig>())
            {
				SharedDataController.Instance.RemoveSharedData(skeleton);
            }

			if (TextSessionId)
				TextSessionId.text = "Not in session";

            _sessionMetrics.Stop(previousSessionId);

            //Notify experience
            if (OnLeftSession != null)
			{
				try
				{
					OnLeftSession();
				}
				catch(Exception ex)
				{
					Debug.LogErrorFormat("Failed to notify experience OnLeftSession. Error={0}\n{1}", ex.Message, ex.StackTrace);
				}
			}

            //Check if we should shutdown on end session
            if (previousSessionStarted && ConfigService.Instance.Config.Location.Client.ShutdownOnEndSession)
            {
                Debug.Log("<color=lightblue>Session terminated, shutting down...</color>");
                Application.Quit();
            }
		}

		private delegate void SceneLoadDone(bool timedOut);
		private IEnumerator WaitForSceneLoad(string sceneName, SceneLoadDone doneAction, float timeoutSecs)
		{
			var isLoaded = false;

			SceneController.OnExperienceSceneLoadedHandler onSceneLoaded = (string name) =>
			{
				if (name == sceneName)
					isLoaded = true;
			};

			var timeoutTime = Time.unscaledTime + timeoutSecs;
			SceneController.Instance.OnExperienceSceneLoaded += onSceneLoaded;
			yield return new WaitUntil(() => isLoaded || Time.unscaledTime > timeoutTime);

			SceneController.Instance.OnExperienceSceneLoaded -= onSceneLoaded;

			doneAction(!isLoaded);
		}

		private void InternalLoadGameScene(string sceneName, Transition transition, ELoadSequence loadSequence = ELoadSequence.LoadFirst, bool forceReload = false, bool awaitSceneSync = false, string customTransitionName = null)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lightblue>Loading new game scene: sceneName={0}, transition={1}, forceReload={2}, awaitSceneSync={3}</color>",
				sceneName, transition, forceReload, awaitSceneSync);

			SceneController.Instance.LoadChildScene(sceneName, transition, loadSequence, forceReload, awaitSceneSync, customTransitionName);
		}

		private void SetComponentStatus(ELocationComponentStatus status)
		{
			//Update component status
			SharedDataUtils.GetMyComponent<LocationComponentWithSession>().Status = status;

			if (TextStatus)
				TextStatus.text = string.Format("{0} {1}",
					DevelopmentMode.CurrentMode != EDevelopmentMode.None ? string.Format("<color=#F62121FF>Dev. mode: {0}</color>  ", DevelopmentMode.CurrentMode) : "",
					status.ToString());
		}

		private void SetComponentSession(Session session)
		{
			var sessionId = session != null ? session.SharedId.Guid : Guid.Empty;

			NetworkInterface.Instance.SessionId = sessionId;
			CurrentSession = session != null ? session : null;

			var thisComponent = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
			if (thisComponent != null)
			{
				thisComponent.SessionId = sessionId;
			}
		}

		private string GetConstructSceneName()
		{
			bool hasCustomName = ConfigService.Instance.ExperienceSettings && !string.IsNullOrEmpty(ConfigService.Instance.ExperienceSettings.ConstructSceneName);
			return hasCustomName ? ConfigService.Instance.ExperienceSettings.ConstructSceneName : "Construct Scene";
		}

		#endregion

		#region Monitoring

		private void SetupRigidbodyMonitoring(SkeletonConfig skeleton)
		{
            //TODO Used by Dashboard, we should get rid of it
			foreach (ESkeletonSubject e in Enum.GetValues(typeof(ESkeletonSubject)))
			{
                var rbName = skeleton.SkeletonSubjectNames[(int)e];
                if(!string.IsNullOrEmpty(rbName))
                {
                    Monitoring.Monitoring.Instance.ProcessContext.AddKeyValue("RB_" + e, rbName);
                }
			}
		}

		private void ClearRigidbodyMonitoring()
		{
			//Keep track of rigidbodies for metrics
			foreach (ESkeletonSubject e in Enum.GetValues(typeof(ESkeletonSubject)))
			{
				Monitoring.Monitoring.Instance.ProcessContext.AddKeyValue("RB_" + e, "");
			}
		}

        //TODO temporary until we upload the metrics
        struct SessionMetrics
        {
            float _startTime, _joinTime;
            long _frameCount;
            long _fpsBelow80Count, _fpsBelow70Count, _fpsBelow60Count, _fpsBelow50Count;
            LogSystem.Logger _logger;
            public void Join()
            {
                _joinTime = Time.realtimeSinceStartup;
                _frameCount = -1;
            }
            public void Start()
            {
                _startTime = Time.realtimeSinceStartup;
                _frameCount = 0;
                _fpsBelow80Count = _fpsBelow70Count = _fpsBelow60Count = _fpsBelow50Count = 0;
                if (_logger == null)
                {
                    _logger = LogSystem.LogManager.Instance.GetLogger("SessionMetrics");
                }
            }
            public void Update()
            {
                if (Time.unscaledDeltaTime > 0)
                {
                    ++_frameCount;
                    float fps = 1f / Time.unscaledDeltaTime;
                    if (fps <= 80) ++_fpsBelow80Count;
                    if (fps <= 70) ++_fpsBelow70Count;
                    if (fps <= 60) ++_fpsBelow60Count;
                    if (fps <= 50) ++_fpsBelow50Count;
                }
            }
            public void Stop(Guid sessionId)
            {
                if ((sessionId != Guid.Empty) && (_logger != null) && _logger.IsInfoEnabled && (_frameCount > 0))
                {
                    float duration = Time.realtimeSinceStartup - _joinTime;
                    float runningDuration = Time.realtimeSinceStartup - _startTime;
                    _logger.InfoFormat("json:{{ \"session_stats\":{{ \"duration\":{0}, \"running_duration\":{1}, \"framerate\":{{ \"average\":{2}, \"below_80_ratio\":{3}, \"below_70_ratio\":{4}, \"below_60_ratio\":{5}, \"below_50_ratio\":{6}, \"session_id\":\"{7}\" }} }} }}"
                        , duration, runningDuration, _frameCount / runningDuration
                        , ((float)_fpsBelow80Count) / _frameCount, ((float)_fpsBelow70Count) / _frameCount, ((float)_fpsBelow60Count) / _frameCount, ((float)_fpsBelow50Count) / _frameCount
                        , sessionId);
                    // Doesn't work with Unity 2017.4, returned memory size is always 0
                    //var process = System.Diagnostics.Process.GetCurrentProcess();
                    //_logger.InfoFormat("memory.working_set={0}, memory.peak_working_set={1}, memory.paged={2}, memory.peak_paged={3}, id={4}"
                    //    , process.WorkingSet64, process.PeakWorkingSet64, process.PagedMemorySize64, process.PeakPagedMemorySize64, sessionId);
                }
            }
        }
        SessionMetrics _sessionMetrics;

        #endregion
    }

}
