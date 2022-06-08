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
	[RequireComponent(typeof(SilenceTsMix))]
	public class TS3Controller : MainThreadInvoker
	{
		#region Events

		public delegate void OnConnectedHandler();
		public event OnConnectedHandler OnConnected;

		public delegate void OnDisconnectedHandler();
		public event OnDisconnectedHandler OnDisconnected;

		public delegate void OnChannelsUpdatedHandler();
		public event OnChannelsUpdatedHandler OnChannelsUpdated;

		#endregion

		[Tooltip("Current TS connection status")]
		public ConnectStatus ConnectStatus;

		[Tooltip("Current TS channel. (Editor debug, don't change)")]
		public string ChannelName;

		[Tooltip("Current TS nickname. (Editor debug, don't change)")]
		public string NickName;

		public GameObject DefaultTSAudioSourceTemplate;

		public bool IsConnected
		{
			get
			{
				//TSLogFormat("client={0}, status={1}", TeamSpeakClient != null ? "not null" : "null", TeamSpeakClient != null ? TeamSpeakClient.GetConnectionStatus().ToString() : "N/A");
				return TeamSpeakClient != null && (ConnectStatus)TeamSpeakClient.GetConnectionStatus() != ConnectStatus.STATUS_DISCONNECTED;
			}
		}

		private const string DEFAULT_CHANNEL_PASSWORD = "";

		private TeamSpeakClient TeamSpeakClient;

#if !UNITY_EDITOR
		private LogSystem.Logger _logger;
#endif

		#region Unity signals

		void Awake()
        {
#if !UNITY_EDITOR
			_logger = LogSystem.LogManager.Instance.GetLogger("TeamSpeak");
#endif
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
			DisconnectTeamspeak(true);
		}

#endregion

		#region Public interface

		/// <summary>
		/// Connect to location TeamSpeak server to the given channel and with the given nickname.
		/// </summary>
		/// <param name="channelName">Name of the channel to join</param>
		/// <param name="nickName">Nickname to use</param>
		public void ConnectTeamspeak(string channelName, string nickName)
		{
			ChannelName = channelName;
			NickName = nickName;

			if (!IsConnected)
			{
				TeamSpeakClient = TeamSpeakClient.GetInstance();

				//Attach TS events
				TeamSpeakCallbacks.onTalkStatusChangeEvent += OnTalkStatusChangeEvent;
				TeamSpeakCallbacks.onServerErrorEvent += TeamSpeakCallbacks_onServerErrorEvent;
				TeamSpeakCallbacks.onConnectStatusChangeEvent += TeamSpeakCallbacks_onConnectStatusChangeEvent;
				TeamSpeakCallbacks.onNewChannelCreatedEvent += TeamSpeakCallbacks_onNewChannelCreatedEvent;
				TeamSpeakCallbacks.onDelChannelEvent += TeamSpeakCallbacks_onDelChannelEvent;
				TeamSpeakCallbacks.onIgnoredWhisperEvent += TeamSpeakCallbacks_onIgnoredWhisperEvent;

				//enabling logging of some pre-defined errors.
				TeamSpeakClient.logErrors = false;

				//Connect client
				string host = ConfigService.Instance.Config.Infrastructure.TeamSpeak.HostOverride;
				if (!string.IsNullOrEmpty(host))
				{
					TSLog("Using TS server host override: " + host);
				}
				else
				{
					// Get TS server shared data and extract IP address
					var tsInfo = SharedDataUtils.FindSharedData<TeamSpeakServer>();
					if (tsInfo == null)
					{
#if !UNITY_EDITOR
						TSLogWarning("TS server shared data not found");
#endif
					}
					else if (string.IsNullOrEmpty(tsInfo.IPAddress))
					{
						TSLogError("TS server shared has an empty IP address");
					}
					else
					{
						host = tsInfo.IPAddress;
					}
				}
				if (!string.IsNullOrEmpty(host))
				{
					var tsConfig = ConfigService.Instance.Config.Infrastructure.TeamSpeak;
					var tsChannel = new string[] { ChannelName, "" }; // :-( This doesn't work anymore... TS will not create the channel
					var port = TeamSpeakServer.Port;

					TSLogFormat("Connection to TS server: Address={0}, Port={1}, Channel={2}, Nickname={3}", host, port, channelName, NetworkInterface.Instance.NetworkGuid);
					TeamSpeakClient.StartClient(host, port, tsConfig.ServerPassword, NickName, ref tsChannel, DEFAULT_CHANNEL_PASSWORD);

					if (!TeamSpeakClient.started)
					{
						TSLogWarningFormat("Not connected to TS server: {0}:{1}", host, port);
					}
				}
			}
			else
			{
				TSLog("TS already connected");
			}
		}

		/// <summary>
		/// Disconnect from TeamSpeak server if connected.
		/// </summary>
		/// <param name="forgetClientInfos">Indicated if the given nickname and channel have to be cleared. Only clear this info when we leave the session and not when we just get disconnected.</param>
		public void DisconnectTeamspeak(bool forgetClientInfos)
		{
			//Reset client id to shared data
			var thisClient = SharedDataUtils.GetMyComponent<ExperienceClient>();
			if (thisClient != null)
			{
				thisClient.TSClientId = 0;
			}

			//Clear client infos if needed
			if (forgetClientInfos)
			{
				ChannelName = null;
				NickName = null;
			}

			//Remove all audio sources
			RemoveAllAudioSources();

			//Disconnect
			if (IsConnected)
			{
				TSLog("Disconnecting from TS server");

				//Detach TS events
				TeamSpeakCallbacks.onTalkStatusChangeEvent -= OnTalkStatusChangeEvent;
				TeamSpeakCallbacks.onServerErrorEvent -= TeamSpeakCallbacks_onServerErrorEvent;
				TeamSpeakCallbacks.onNewChannelCreatedEvent -= TeamSpeakCallbacks_onNewChannelCreatedEvent;
				TeamSpeakCallbacks.onDelChannelEvent -= TeamSpeakCallbacks_onDelChannelEvent;
				TeamSpeakCallbacks.onIgnoredWhisperEvent -= TeamSpeakCallbacks_onIgnoredWhisperEvent;
				TeamSpeakCallbacks.onConnectStatusChangeEvent -= TeamSpeakCallbacks_onConnectStatusChangeEvent;

				TeamSpeakClient.StopClient();
				TeamSpeakClient = null;

				ConnectStatus = ConnectStatus.STATUS_DISCONNECTED;

				if (OnDisconnected != null)
					OnDisconnected();
			}
			else
			{
				TSLog("No disconnect... not connected to TS server");
			}

			//Handle, cleanup error report
			if (!string.IsNullOrEmpty(ChannelName))
			{
				//Still having a target channel name means we lost the connection or were not able to connect at all but should have
				OpenConnectionLostReport();
			}
			else
			{
				//Close report if open. We explicitly want to disconnect from TS (e.g. session end)
				CloseConnectionLostReport();
			}
		}

		/// <summary>
		/// Creates a new channel with the given name and joins it.
		/// Just join the channel if it channel already exists
		/// </summary>
		/// <param name="channelName">Channel name</param>
		public void CreateAndJoinChannel(string channelName)
		{
			var channelId = TeamSpeakClient.GetChannelIDFromChannelNames(new string[] { channelName, "" });
			if (channelId > 0)
			{
				JoinChannel(channelId);
			}
			else
			{
				//Create channel
				ChannelName = channelName;

				uint64 _channelID = 0;
				/* Set data of new channel. Use channelID of 0 for creating channels. */

				if (channelName != "")
					TeamSpeakClient.SetChannelVariableAsString(TeamSpeakClient.GetServerConnectionHandlerID(), _channelID, ChannelProperties.CHANNEL_NAME, channelName);

				/* Flush changes to server */
				TeamSpeakClient.FlushChannelCreation(TeamSpeakClient.GetServerConnectionHandlerID(), 0);

				TSLogFormat("Joining TS channel: {0}", channelName);
			}
		}

		/// <summary>
		/// Joins the channel with the given channel ID.
		/// Do nothing if the channel does not exist.
		/// </summary>
		/// <param name="channelId">TS channel id</param>
		public void JoinChannel(uint64 channelId)
		{
			if (channelId > 0)
			{
				TSLogFormat("Trying to join to channel: channelId={0}, we're in={1}", channelId, TeamSpeakClient.GetChannelOfClient(TeamSpeakClient.GetClientID()));
				if (channelId != TeamSpeakClient.GetChannelOfClient(TeamSpeakClient.GetClientID()))
				{
					TSLogFormat("Connecting to channelId: {0}", channelId);
					//Join channel
					TeamSpeakClient.RequestClientMove(TeamSpeakClient.GetClientID(), channelId, "");
				}
				else
				{
					TSLogFormat("Already in channel {0}, doing nothing.", channelId);
				}
			}
		}

		/// <summary>
		/// Initializes the audio source for the given player. If the given player is not connected to TS (anymore), the audio source will just be destroyed.
		/// If the player already has an audio source assigned, it will be destroyed first.
		/// The create audio source is configured in the experience settings or the SDK default is used.
		/// </summary>
		/// <param name="player">Player to be initialized</param>
		/// <param name="muteOutput">Whether or not to mute output (useful when only doing lipsync)</param>
		public void InitPlayer(RuntimePlayer player, bool muteOutput)
		{
			if (player == null)
				return;

			TSLogFormat("Initializing player TS3 audio for player component={0} with muteOutput={1}", player.Player.ComponentId, muteOutput);

			//Clear existing player audio source
			RemovePlayerAudioSource(player);

			//Create TS audio source
			if (player.Player != null)
			{
				//Check client TS id
				if(player.Player.TSClientId > 0)
				{
					var isHostess = player.IsMainPlayer;

					//Get the audio source root. Other players: avatar head, self: camera rigidbody
					Transform audioRoot = null;
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
						TSLogFormat("Initializing audio source for player: player={0}, tsClientId={1}", player.Player.ComponentId, player.Player.TSClientId);
						player.SetTSAudioSource(UnityUtils.InstantiatePrefab<TeamSpeakAudioSource>(GetAudioSourceTemplate(isHostess),audioRoot));

                        //Mute?
                        player.TSAudioSource.MuteOutput = muteOutput;

						var localPosition = !isHostess ? Vector3.zero : new Vector3(0f, 0f, 1f); //Hostess in front of player
						var localRotation = !isHostess ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f); //Hostess looking at player

						player.TSAudioSource.transform.localPosition = localPosition;
						player.TSAudioSource.transform.localRotation = localRotation;

						if (isHostess)
						{
							//Attach camera follower
							if (!player.TSAudioSource.GetComponent<CameraAttachedGameObject>())
							{
								var camAttached = player.TSAudioSource.gameObject.AddComponent<CameraAttachedGameObject>();
								camAttached.Distance = 1f;
								camAttached.LookAt = true;
							}
						}

						player.TSAudioSource.Initialize(player.Player.TSClientId, isHostess); //Main player -> hostess source
					}
					else
					{
						TSLogErrorFormat("Failed to find avatar head transform for player component={0} (isHostess={1}). No TS3 audio source will be created.", player.Player.ComponentId, isHostess);
					}
				}
				else
				{
					//Don't need to do anything. ClientId 0 means disconnected from TS and we already cleaned up the audio source for it earlier.
				}
			}
			else
			{
				TSLogError("Failed to initialize TS for player. Player was null.");
			}
		}

		#endregion

		#region TeamSpeak events

		private void TeamSpeakCallbacks_onIgnoredWhisperEvent(ulong serverConnectionHandlerID, ushort clientID)
		{
			TeamSpeakClient.AllowWhispersFrom(clientID);
		}

		private void OnTalkStatusChangeEvent(uint64 serverConnectionHandlerID, int status, int isReceivedWhisper, anyID clientID)
		{
			TSLogFormat("TS status change event: {0} {1}", serverConnectionHandlerID, status);
		}

		private void TeamSpeakCallbacks_onServerErrorEvent(ulong serverConnectionHandlerID, string errorMessage, uint error, string returnCode, string extraMessage)
		{
			if(error != public_errors.ERROR_channel_already_in)
			{
				TSLogErrorFormat("TS server error: message={0}, error={1}, returnCode={2}, extraMessage={3}", errorMessage, error, returnCode, extraMessage);
			}
		}

		private void TeamSpeakCallbacks_onConnectStatusChangeEvent(ulong serverConnectionHandlerID, int newStatus, uint errorNumber)
		{
			ConnectStatus = (ConnectStatus)newStatus;
			TSLogFormat("Connect status changed to: {0}", ConnectStatus);

			if (errorNumber > 0)
			{

				if(public_errors.ERROR_failed_connection_initialisation == errorNumber)
				{
					//Failed to connect... disconnect to clean up
					TSLogError("TS status change event: failed to connect TS server, disconnecting");
					DisconnectTeamspeak(false);
				}
				else if(public_errors.ERROR_connection_lost == errorNumber)
				{
					//Connection lost... disconnect to clean up
					TSLogError("TS status change event: connection lost to TS server, disconnecting");
					DisconnectTeamspeak(false);
				}
				else
				{
					TSLogErrorFormat("TS status change event: Error={0}, Message={1}", errorNumber, TeamSpeakClient.GetErrorMessage(errorNumber));
				}
			}
			else
			{
				if (ConnectStatus == ConnectStatus.STATUS_CONNECTION_ESTABLISHED)
				{
					TSLogFormat("TS connected");

					//Set client id to shared data
					var clientId = TeamSpeakClient.GetClientID();
					Invoke(() =>
					{
						var thisClient = SharedDataUtils.GetMyComponent<ExperienceClient>();
						if (thisClient != null)
						{
							thisClient.TSClientId = clientId;
						}
					});

					//Set configures void activate level
					var voiceActivationLevle = ConfigService.Instance.Config.Infrastructure.TeamSpeak.VoiceActivationLevel;
					TSLogFormat("Setting voice activation level to: {0}", voiceActivationLevle);
					TeamSpeakClient.SetPreProcessorConfigValue(TeamSpeakClient.PreProcessorConfig.voiceactivation_level, voiceActivationLevle);

					//Switch channel
					CreateAndJoinChannel(ChannelName);

					//Mute microphone if needed
					if(ConfigService.Instance.ExperienceConfig.TSMuteMic)
					{
						TSLog("Muting TS client input device");
						TeamSpeakClient.SetClientSelfVariableAsInt(ClientProperties.CLIENT_INPUT_DEACTIVATED, (int)InputDeactivationStatus.INPUT_DEACTIVATED);
					}

					//Close error report if it was open before
					CloseConnectionLostReport();

					if (OnConnected != null)
						OnConnected();
				}
				else if (ConnectStatus == ConnectStatus.STATUS_DISCONNECTED)
				{
					TSLogFormat("TS disconnected");
					DisconnectTeamspeak(false);
				}
			}
		}

		private void TeamSpeakCallbacks_onDelChannelEvent(ulong serverConnectionHandlerID, ulong channelID, ushort invokerID, string invokerName, string invokerUniqueIdentifier)
		{
			if (OnChannelsUpdated != null)
				OnChannelsUpdated();
		}

		private void TeamSpeakCallbacks_onNewChannelCreatedEvent(ulong serverConnectionHandlerID, ulong channelID, ulong channelParentID, ushort invokerID, string invokerName, string invokerUniqueIdentifier)
		{
			//Is it our channel and do we have to switch?
			if (TeamSpeakClient.GetChannelVariableAsString(channelID, ChannelProperties.CHANNEL_NAME) == ChannelName && TeamSpeakClient.GetChannelOfClient(TeamSpeakClient.GetClientID()) != channelID)
			{
				JoinChannel(channelID);
			}

			if (OnChannelsUpdated != null)
				OnChannelsUpdated();
		}

		#endregion

		#region Error reporting

		private OperationalTickets.IOpTicket TSConnectionLostTicket;

		private void OpenConnectionLostReport()
		{
			if(TSConnectionLostTicket == null)
			{
				TSConnectionLostTicket = OperationalTickets.Instance.OpenTicket(new ConnectionLost { });
			}
		}

		private void CloseConnectionLostReport()
		{
			if(TSConnectionLostTicket != null)
			{
				TSConnectionLostTicket.Close();
				TSConnectionLostTicket = null;
			}
		}

		#endregion

		#region Location events

		private void SharedDataUtils_ChildSharedDataAdded(ChildSharedData childSharedData)
		{
			if(!IsConnected && !string.IsNullOrEmpty(ChannelName) && !string.IsNullOrEmpty(NickName) && (childSharedData is TeamSpeakServer))
			{
				TSLogFormat("TS server was started now, trying to connect to channel={0}, nickname={1}", ChannelName, NickName);
				ConnectTeamspeak(ChannelName, NickName);
			}
		}

		private void SharedDataUtils_ChildSharedDataRemoved(ChildSharedData childSharedData)
		{
			if(childSharedData is TeamSpeakServer)
			{
				TSLog("TS server was stopped, disconnecting.");
				DisconnectTeamspeak(false);
			}
		}

		#endregion

		#region Internals

		private GameObject GetAudioSourceTemplate(bool hostess)
		{
			GameObject audioSourceTemplate = null;

			if(ConfigService.Instance.ExperienceSettings != null)
			{
				//Default experience source
				audioSourceTemplate = ConfigService.Instance.ExperienceSettings.TeamspeakAudioSource;

				//Hostess?
				if(hostess && ConfigService.Instance.ExperienceSettings.TeamspeakHostessAudioSource != null)
					audioSourceTemplate = ConfigService.Instance.ExperienceSettings.TeamspeakHostessAudioSource;
			}

			//Validate
			if(audioSourceTemplate != null)
			{
				if(!audioSourceTemplate.GetComponent<TeamSpeakAudioSource>())
				{
					TSLogWarningFormat("The setup TS audiosource does not have a TeamSpeakAudioSource behaviour attached: {0}. Using SDK default template.", audioSourceTemplate.name);
					audioSourceTemplate = DefaultTSAudioSourceTemplate;
				}
			}

			if (audioSourceTemplate == null)
				audioSourceTemplate = DefaultTSAudioSourceTemplate;
			
			return audioSourceTemplate;
		}

		private void RemovePlayerAudioSource(RuntimePlayer player)
		{
			if(player.TSAudioSource)
			{
				TSLogFormat("Destroying TS audiosource for player: {0}", player.Player.ComponentId);
				Destroy(player.TSAudioSource.gameObject);
				player.ResetTSAudioSource();
			}
		}

		private void RemoveAllAudioSources()
		{
			Invoke(() =>
			{
				foreach (var source in FindObjectsOfType<TeamSpeakAudioSource>())
				{
					Destroy(source.gameObject);
				}
			});
		}

		#endregion

		#region Logging

		private string GetErrorMsessage(uint tsError)
		{
			string msg = null;
			IntPtr strPtr = IntPtr.Zero;
			if (TeamSpeakInterface.ts3client_getErrorMessage(tsError, out strPtr) == public_errors.ERROR_ok)
			{
				string tmp_str = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(strPtr);
				msg = string.Copy(tmp_str);
				TeamSpeakInterface.ts3client_freeMemory(strPtr);
			}
			return msg ?? tsError.ToString();
		}

		private void TSLog(string message)
		{
#if UNITY_EDITOR
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.INFO, message);
#endif
		}

		private void TSLogFormat(string format, params object[] args)
		{
			TSLog(string.Format(format, args));
		}

		private void TSLogWarning(string message)
		{
#if UNITY_EDITOR
			Debug.LogWarning("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.WARNING, message);
#endif
		}

		private void TSLogWarningFormat(string format, params object[] args)
		{
			TSLogWarning(string.Format(format, args));
		}

		private void TSLogError(string message)
		{
#if UNITY_EDITOR
			Debug.LogError("<color=orange>" + message + "</color>");
#else
			_logger.Log(LogSystem.LogManager.Levels.ERROR, message);
#endif
		}

		private void TSLogErrorFormat(string format, params object[] args)
		{
			TSLogError(string.Format(format, args));
		}

		#endregion
	}

}