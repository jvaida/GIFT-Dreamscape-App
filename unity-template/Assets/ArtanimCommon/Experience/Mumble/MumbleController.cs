using UnityEngine;
using UnityEngine.UI;
using Artanim.Location.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using uint64 = System.UInt64;
using anyID = System.UInt16;
using Artanim.Location.SharedData;
using Artanim.Location.Data;
using Artanim.Location.Monitoring;
using Artanim.Location.Monitoring.OpTicketsTypes.Voice;

namespace Artanim
{
	/// <summary>
	/// 
	/// </summary>
	public class MumbleController : MainThreadInvoker
	{
		public GameObject DefaultMumbleAudioSourceTemplate;

#if UNITY_EDITOR
		private const string MENU_MUMBLE_EDITOR = "Artanim/Use Mumble in Editor";
		private const string KEY_MUMBLE_EDITOR = "ArtanimUseMumbleInEditor";
#endif

		private static bool UseMumble
        {
            get
            {
#if UNITY_EDITOR
				return UnityEditor.EditorPrefs.GetBool(KEY_MUMBLE_EDITOR, false);
#else
				return true;
#endif
			}

			set
			{
#if UNITY_EDITOR
				UnityEditor.EditorPrefs.SetBool(KEY_MUMBLE_EDITOR, value);
#endif
			}
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(MENU_MUMBLE_EDITOR, false, 20)]
		public static void DoUseMumbleInEditor()
		{
			UseMumble = !UseMumble;
		}

		[UnityEditor.MenuItem(MENU_MUMBLE_EDITOR, true)]
		public static bool ValidateUseMumbleInEditor()
		{
			UnityEditor.Menu.SetChecked(MENU_MUMBLE_EDITOR, UseMumble);
			return true;
		}
#endif
		public bool IsConnected
		{
			get
			{
				return _mumbleClientStarted && _mumbleController != null && _mumbleController.IsConnected();
			}
		}

		private const string DEFAULT_CHANNEL_PASSWORD = "";

		private Artanim.Location.Mumble.MumbleController _mumbleController;
		private Artanim.Location.Data.MumblePlayer _mumblePlayer;
		private bool _mumbleClientStarted = false;

		public Artanim.Location.Data.MumbleServer MumbleServer { get; private set; }

		public delegate void OnMumbleServerChangedHandler();
		public event OnMumbleServerChangedHandler OnMumbleServerChanged;


#if !UNITY_EDITOR
		private LogSystem.Logger _logger;
#endif

#region Unity signals

		void Awake()
        {
#if !UNITY_EDITOR
			_logger = LogSystem.LogManager.Instance.GetLogger("Mumble");
#endif
			enabled = (ConfigService.Instance.Config.Infrastructure.Mumble != null) && (!ConfigService.Instance.Config.Infrastructure.Mumble.Disabled)
				&& (ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Mumble && UseMumble);
		}

		void OnEnable()
		{
			SharedDataUtils.ChildSharedDataAdded += SharedDataUtils_ChildSharedDataAdded;
			SharedDataUtils.ChildSharedDataRemoved += SharedDataUtils_ChildSharedDataRemoved;

			var silenceTsMix = GetComponent<SilenceTsMix>();
			if(silenceTsMix)
				silenceTsMix.enabled = true;
		}
		
		void OnDisable()
		{
			SharedDataUtils.ChildSharedDataAdded -= SharedDataUtils_ChildSharedDataAdded;
			SharedDataUtils.ChildSharedDataRemoved -= SharedDataUtils_ChildSharedDataRemoved;

			var silenceTsMix = GetComponent<SilenceTsMix>();
			if (silenceTsMix)
				silenceTsMix.enabled = false;
		}
		
		void OnApplicationQuit()
		{
			Disconnect(true);
		}

		void Update()
		{
			if (_mumbleClientStarted)
			{
				uint uiSession = 0;
				if (_mumbleController.Update(ref uiSession))
				{
					if (_mumblePlayer != null)
					{
						_mumblePlayer.UiSession = uiSession;
						MumbleLogFormat("Mumble Player UiSession has changed: Player={0}, uiSession={1}", _mumblePlayer.ParentGuid.ToString(), uiSession);
					}
				}
			}
		}

#endregion

#region Public interface

		/// <summary>
		/// Check if Mumble is enabled and try to connect to location Mumble server
		/// </summary>
		public void ConnectIfEnabled()
		{
			if ((ConfigService.Instance.Config.Infrastructure.Mumble != null) && (!ConfigService.Instance.Config.Infrastructure.Mumble.Disabled)
				&& (ConfigService.Instance.ExperienceConfig.VoiceChat == Location.Config.ExperienceConfig.EVoiceChat.Mumble) && UseMumble)
			{
				enabled = true;
				Connect();
			}
		}

		/// <summary>
		/// Disconnect from Mumble server if connected.
		/// </summary>
		public void Disconnect(bool stopClient)
		{
			MumbleServer = null;

			if (stopClient)
			{
				if(_mumblePlayer!=null)
				{
					SharedDataController.Instance.RemoveSharedData(_mumblePlayer);
					_mumblePlayer = null;
				}

				//Remove all audio sources
				RemoveAllAudioSources();

				_mumbleClientStarted = false;

				if (_mumbleController != null)
				{
					MumbleLog("Disconnecting from Mumble server");

					_mumbleController.StopClient();
					_mumbleController = null;
				}
				else
				{
					MumbleLog("No disconnect... not connected to Mumble server");
				}
			}

			if (OnMumbleServerChanged != null)
			{
				OnMumbleServerChanged();
			}
		}

		/// <summary>
		/// Initializes the audio source for the given player. If the given player is not connected to Mumble (anymore), the audio source will just be destroyed.
		/// If the player already has an audio source assigned, it will be destroyed first.
		/// The create audio source is configured in the experience settings or the SDK default is used.
		/// </summary>
		/// <param name="player">Player to be initialized</param>
		public void InitPlayer(RuntimePlayer player)
		{
			if (player == null && !enabled)
				return;

			MumbleLogFormat("Initializing player Mumble audio for player component={0}", player.Player.ComponentId);

			//Clear existing player audio source
			RemovePlayerAudioSource(player);

			//Create Mumble audio source
			if (player.Player != null)
			{
				var isHostess = player.IsMainPlayer;

				//Get the audio source root. Other players: avatar head, self: camera rigidbody
				Transform audioRoot;
				
				if(!isHostess)
				{
					var avatarHeadPart = player.AvatarController.GetAvatarBodyPart(Location.Messages.EAvatarBodyPart.Head);
					if (avatarHeadPart)
						audioRoot = avatarHeadPart.transform;
					else
						audioRoot = player.AvatarController.HeadBone;
				}
				else
				{
					audioRoot = transform;
				}

				if(audioRoot)
				{
					//Initialize player with configured audio prefab
					MumbleLogFormat("Initializing audio source for player: player={0}", player.Player.ComponentId);

					player.SetMumbleAudioSource(UnityUtils.InstantiatePrefab<MumbleAudioSource>(GetAudioSourceTemplate(isHostess), audioRoot));

					var localPosition = !isHostess ? Vector3.zero : new Vector3(0f, 0f, 1f); //Hostess in front of player
					var localRotation = !isHostess ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f); //Hostess looking at player

					player.MumbleAudioSource.transform.localPosition = localPosition;
					player.MumbleAudioSource.transform.localRotation = localRotation;

					if (isHostess)
					{
						//Attach camera follower
						if (!player.MumbleAudioSource.GetComponent<CameraAttachedGameObject>())
						{
							var camAttached = player.MumbleAudioSource.gameObject.AddComponent<CameraAttachedGameObject>();
							camAttached.Distance = 1f;
							camAttached.LookAt = true;
						}
					}

					player.MumbleAudioSource.Initialize(this, player.Player.ComponentId, isHostess);
				}
				else
				{
					MumbleLogErrorFormat("Failed to find avatar head transform for player component={0} (isHostess={1}). No Mumble audio source will be created.", player.Player.ComponentId, isHostess);
				}
			}
			else
			{
				MumbleLogError("Failed to initialize Mumble for player. Player was null.");
			}
		}

#endregion

#region Error reporting

		private OperationalTickets.IOpTicket MumbleConnectionLostTicket;

		private void OpenConnectionLostReport()
		{
			if(MumbleConnectionLostTicket == null)
			{
				MumbleConnectionLostTicket = OperationalTickets.Instance.OpenTicket(new ConnectionLost { });
			}
		}

		private void CloseConnectionLostReport()
		{
			if(MumbleConnectionLostTicket != null)
			{
				MumbleConnectionLostTicket.Close();
				MumbleConnectionLostTicket = null;
			}
		}

#endregion

#region Location events

		private void SharedDataUtils_ChildSharedDataAdded(ChildSharedData childSharedData)
		{
			if(!IsConnected && (childSharedData is MumbleServer))
			{
				MumbleLogFormat("Mumble server was started now, trying to connect");
				MumbleServer = (MumbleServer)childSharedData;
				ConnectIfEnabled();
			}
		}

		private void SharedDataUtils_ChildSharedDataRemoved(ChildSharedData childSharedData)
		{
			MumbleServer server = childSharedData as MumbleServer;
			if(server == MumbleServer)
			{
				MumbleLog("Mumble server was stopped, disconnecting.");
				Disconnect(false);
			}
		}

#endregion

#region Internals

		private void Connect()
		{
			if (enabled && NetworkInterface.Instance.IsTrueClient)
			{
				if (!IsConnected)
				{
					if (_mumbleController == null)
					{
						_mumbleController = Artanim.Location.Mumble.MumbleController.Instance;
					}

					if (MumbleServer == null)
					{
						// Get Mumble server shared data and extract IP address
						MumbleServer = SharedDataUtils.FindSharedData<MumbleServer>();
					}

					if (MumbleServer == null)
					{
#if !UNITY_EDITOR
						MumbleLogWarning("Mumble server shared data not found");
#endif
					}
					else if (string.IsNullOrEmpty(MumbleServer.IPAddress))
					{
						MumbleLogError("Mumble server shared has an empty IP address");
					}
					else
					{
						if (_mumbleClientStarted == false)
						{
							if (_mumblePlayer == null)
							{
								_mumblePlayer = SharedDataController.Instance.CreateSharedData<Artanim.Location.Data.MumblePlayer>();
							}

							MumbleLogFormat("Connection to Mumble server: Address={0}, Username={1}", MumbleServer.IPAddress, NetworkInterface.Instance.NetworkGuid);
							_mumbleController.StartClient(MumbleServer.IPAddress, NetworkInterface.Instance.NetworkGuid, true, true);
							_mumbleClientStarted = true;
						}

						_mumblePlayer.UiSession = _mumbleController.GetUiSession();
						MumbleLogFormat("Mumble Player UiSession initial value: Player={0}, uiSession={1}", _mumblePlayer.ParentGuid.ToString(), _mumblePlayer.UiSession);

						if (OnMumbleServerChanged != null)
						{
							OnMumbleServerChanged();
						}
					}
				}
				else
				{
					MumbleLog("Mumble already connected");
				}
			}
		}

		private GameObject GetAudioSourceTemplate(bool hostess)
		{
			GameObject audioSourceTemplate = DefaultMumbleAudioSourceTemplate;
			
			return audioSourceTemplate;
		}

		private void RemovePlayerAudioSource(RuntimePlayer player)
		{
			if(player.MumbleAudioSource)
			{
				MumbleLogFormat("Destroying Mumble audiosource for player: {0}", player.Player.ComponentId);
				Destroy(player.MumbleAudioSource.gameObject);
				player.ResetMumbleAudioSource();
			}
		}

		private void RemoveAllAudioSources()
		{
			MumbleLog("RemoveAllAudioSources()");
			
			Invoke(() =>
			{
				foreach (var source in FindObjectsOfType<MumbleAudioSource>())
				{
					Destroy(source.gameObject);
				}
			});
		}

#endregion

#region Logging

		public void MumbleLog(string message)
		{
#if UNITY_EDITOR
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.INFO, message);
#endif
		}

		public void MumbleLogFormat(string format, params object[] args)
		{
			MumbleLog(string.Format(format, args));
		}

		public void MumbleLogWarning(string message)
		{
#if UNITY_EDITOR
			Debug.LogWarning("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.WARNING, message);
#endif
		}

		public void MumbleLogWarningFormat(string format, params object[] args)
		{
			MumbleLogWarning(string.Format(format, args));
		}

		public void MumbleLogError(string message)
		{
#if UNITY_EDITOR
			Debug.LogError("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.ERROR, message);
#endif
		}

		public void MumbleLogErrorFormat(string format, params object[] args)
		{
			MumbleLogError(string.Format(format, args));
		}

#endregion
	}

}