using Artanim.Location.Data;
using Artanim.Location.Hostess;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	public class AutoJoinSessionController : MonoBehaviour
	{
		#region Static

		public const string DEFAULT_AVATAR_NAME = "Desktop Avatar";
		public const string DEFAULT_AVATAR_NAME_NOVR = "Desktop Avatar NoVR";
		public const string KEY_PLAYER_FIRSTNAME = "ArtanimPlayerFirstName";
		public const string KEY_PLAYER_LASTNAME = "ArtanimPlayerLastName";
		public const string KEY_DESKTOP_AVATAR = "ArtanimDesktopAvatar";

		static bool _StaticInitialized;
		static bool _IsAutoJoinSession;
		static string _AvatarName;

		private static void StaticInitialize()
        {
			_IsAutoJoinSession = RemoteSessionController.Instance.IsDesktopClient;
			if (!_IsAutoJoinSession)
            {
				_AvatarName = CommandLineUtils.GetValue("AutoJoinSession", "default", valueIfMissing: "");
				_IsAutoJoinSession = !string.IsNullOrEmpty(_AvatarName);
				if (_AvatarName == "default")
				{
					_AvatarName = null; // AvatarName getter will return DEFAULT_AVATAR_NAME if name is empty
				}
			}

			_StaticInitialized = true;
		}

		static bool IsAutoJoinSession
		{
            get
            {
				if (!_StaticInitialized) StaticInitialize();
				return (_IsAutoJoinSession || (!string.IsNullOrEmpty(PlayerPrefs.GetString(KEY_DESKTOP_AVATAR))))
					&& (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone);
            }
		}

		public static string AvatarName
		{
			get
			{
				if (!_StaticInitialized) StaticInitialize();

				if (string.IsNullOrEmpty(_AvatarName))
				{
					string name = PlayerPrefs.GetString(KEY_DESKTOP_AVATAR);
					if (!string.IsNullOrEmpty(name))
					{
						return name;
					}
					else
					{
						return XRUtils.Instance.IsDevicePresent ? DEFAULT_AVATAR_NAME : DEFAULT_AVATAR_NAME_NOVR;
					}
				}
				else
				{
					return _AvatarName;
				}
			}
		}

		public static string PlayerFirstName
        {
			get
            {
				string prefs = PlayerPrefs.GetString(KEY_PLAYER_FIRSTNAME);
				string name = CommandLineUtils.GetValue("FirstName", prefs, prefs);
				return string.IsNullOrEmpty(name) ? "Unknown" : name;
			}
        }

		public static string PlayerLastName
        {
			get
            {
				string prefs = PlayerPrefs.GetString(KEY_PLAYER_LASTNAME);
				string name = CommandLineUtils.GetValue("LastName", prefs, prefs);
				return string.IsNullOrEmpty(name) ? "Unknown" : name;
			}
		}

        #endregion

        void OnEnable()
		{
			if (NetworkInterface.Instance.IsClient && (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone) && IsAutoJoinSession)
			{
				Debug.Log("AutoJoinSessionController enabled");

				StartCoroutine(WaitAndJoinSession());
			}
			else
            {
				Debug.Log("AutoJoinSessionController disabled");
				Destroy(this);
			}
		}

		private IEnumerator WaitAndJoinSession()
		{
			var thisClient = SharedDataUtils.GetMyComponent<ExperienceClient>();

			while (true)
			{
				// Wait for ready for session
				Debug.Log("AutoJoin: waiting for 'ReadyForSession' state");
				yield return new WaitUntil(() => thisClient.Status == ELocationComponentStatus.ReadyForSession);

				Session session = null;
				bool isServerReady = false;
				while (!isServerReady)
				{
					// Find available session
					Debug.Log("AutoJoin: searching for session...");
					bool allowJoinWhileStarted = ConfigService.Instance.ExperienceConfig.AllowAddPlayerWhileRunning;
					while (session == null)
					{
						if (DevelopmentMode.CurrentMode == EDevelopmentMode.ClientServer)
						{
							// Only take our own session if client+server
							session = SharedDataUtils.Sessions.FirstOrDefault(s => (s.ParentGuid == thisClient.SharedId)
								&& ((s.Status == ESessionStatus.Initializing) || (allowJoinWhileStarted && (s.Status == ESessionStatus.Started))));
						}
						else
						{
							// Take any session that is ready
							session = SharedDataUtils.Sessions.FirstOrDefault(s => (s.Status == ESessionStatus.Initializing) || (allowJoinWhileStarted && (s.Status == ESessionStatus.Started)));
						}

						// Wait if no session found
						if (session == null)
						{
							yield return new WaitForSecondsRealtime(0.1f);
						}
					}
					Debug.LogFormat("AutoJoin: found session {0}", session.SharedId);

					// Wait for server ready
					if (!NetworkInterface.Instance.IsServer)
					{
						Debug.Log("AutoJoin: waiting for server 'PreparingSession' state");
						while (true)
						{
							var server = SharedDataUtils.FindLocationComponent<LocationComponentWithSession>(session.ParentGuid);
							if (server == null)
							{
								Debug.LogErrorFormat("AutoJoin: couldn't find server with id {0}", session.ParentGuid);

								// Something is wrong, start over after a delay
								yield return new WaitForSecondsRealtime(1);
								break;
							}
							else if (server.Status >= ELocationComponentStatus.PreparingSession)

							{
								Debug.LogFormat("AutoJoin: server in '{0}' state", server.Status);
								isServerReady = true;
								break;
							}

							// Try again in a moment
							yield return new WaitForSecondsRealtime(1);
						}
					}
					else
					{
						// Client+server mode
						isServerReady = true;
					}
				}

				//
				// Join session...
				//

				// Create player
				var player = SessionManager.PrepareSessionPlayer(thisClient, false);

				// Initialize player (those values may be overridden by the server)
				player.Avatar = AvatarName;
				player.Firstname = PlayerFirstName;
				player.Lastname = PlayerLastName;
				player.IsDesktop = true;
				player.UserSessionId = RemoteSessionController.Instance.ClientUserSessionId; // May be empty

				Debug.LogFormat("AutoJoin: requesting to join session with avatar {0}", player.Avatar);

				// Request player to join session
				if (!NetworkInterface.Instance.IsServer)
				{
					NetworkInterface.Instance.SendMessage(new RequestPlayerJoinSession
					{
						Player = player,
						SessionId = session.SharedId,
					});
				}
				else
				{
					// Client+server mode
					player.Status = EPlayerStatus.Calibrated;
					SessionManager.RequestPlayerJoinSession(session, player);
				}

				// Wait until joined session
				Debug.Log("RemoteSession: waiting for 'RunningSession' state");
				yield return new WaitUntil(() => (thisClient.Status >= ELocationComponentStatus.PreparingSession) || (thisClient.Status == ELocationComponentStatus.Registration));

				if (thisClient.Status >= ELocationComponentStatus.PreparingSession)
				{
					Debug.LogFormat("AutoJoin: joined session: {0}", session.SharedId);
				}
				else
				{
					Debug.LogErrorFormat("AutoJoin: something happened that got the client in an unexpected state: {0}", thisClient.Status);
				}
			}
		}
	}
}