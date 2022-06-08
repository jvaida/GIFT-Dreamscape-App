#if REMOTE_SESSIONS
using Artanim.Location.Network;
using Artanim.Location.RemoteSession;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public struct RemoteSessionInfo
    {
        public static RemoteSessionInfo Empty
        {
            get
            {
                return new RemoteSessionInfo("", 0, 0, new DateTimeOffset(), new DateTimeOffset(), "");
            }
        }

        public RemoteSessionInfo(string sessionId, int seatsReserved, int maximumSeats, DateTimeOffset startTime, DateTimeOffset endTime, string startScene)
        {
            SessionId = sessionId;
            SeatsReserved = seatsReserved;
            MaximumSeats = maximumSeats;
            StartTime = startTime;
            EndTime = endTime;
            StartScene = startScene;
        }

        public string SessionId { get; private set; }

        public int SeatsReserved { get; private set; }

        public int MaximumSeats { get; private set; }

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset EndTime { get; private set; }

        public string StartScene { get; private set; }
    }

    public class RemoteSessionController : SingletonBehaviour<RemoteSessionController>
    {
        #region Session info

        /// <summary>
        /// Whether or not there is a remote session.
        /// For a remote client launched from the portal, it will always return true.
        /// For a remote server running on AWS, it will return true once assigned to a remote session.
        /// </summary>
        /// <remarks>When running in client & server mode, it will return true when there is a SDK session going on</remarks>
        public bool HasSession
        {
            get
            {
#if REMOTE_SESSIONS
                if (DevelopmentMode.CurrentMode == EDevelopmentMode.ClientServer)
                {
                    // Emulate remote session
                    return GameController.Instance && (GameController.Instance.CurrentSession != null);
                }
                else if (IsDesktopClient)
                {
                    return ClientUserSessionId != Guid.Empty;
                }
                else if (IsRemoteServer && Remote.RemoteSessionServerController.Instance)
                {
                    return Remote.RemoteSessionServerController.Instance.HasSession;
                }
#endif
                return false;
            }
        }

        RemoteSessionInfo _remoteSessionInfo;

        /// <summary>
        /// Remote session information as received from the Remote Session endpoint
        /// </summary>
        /// <remarks>When running in client & server mode, it will return the SDK session data</remarks>
        public RemoteSessionInfo SessionInfo
        {
            get
            {
#if REMOTE_SESSIONS
                if ((DevelopmentMode.CurrentMode == EDevelopmentMode.ClientServer) && HasSession)
                {
                    // Emulate remote session
                    var creationTime = GameController.Instance.CurrentSession.CreationTime;
                    var startScenes = ConfigService.Instance.ExperienceConfig.StartScenes;
                    return new RemoteSessionInfo("FakeRemoteSession",
                        GameController.Instance.RuntimePlayers.Count, 4,
                        DateTime.SpecifyKind(creationTime.AddMinutes(5), DateTimeKind.Utc),
                        DateTime.SpecifyKind(creationTime.AddMinutes(60), DateTimeKind.Utc),
                        (startScenes == null || startScenes.Count == 0) ? null : startScenes[0].SceneName);
                }
                else if (IsDesktopClient)
                {
                    var si = _remoteClient.JoinedSessionInfo;
                    return new RemoteSessionInfo(si.Id, si.SeatsReserved, si.MaximumSeats, si.StartTime, si.EndTime, si.TitleId);
                }
                else if (IsRemoteServer && Remote.RemoteSessionServerController.Instance)
                {
                    var si = Remote.RemoteSessionServerController.Instance.SessionInfo;
                    return new RemoteSessionInfo(si.Id, si.SeatsReserved, si.MaximumSeats, si.StartTime, si.EndTime, si.TitleId);
                }
#endif
                return RemoteSessionInfo.Empty;
            }
        }

        /// <summary>
        /// Returns the player's experience data as a JSON string for the given player, or null if not found
        /// </summary>
        /// <param name="player">The player for which to get the experience data (leave to null to get data for current player)</param>
        /// <returns>The JSON string for the experience data</returns>
        /// <remarks>Poll this data until you get a non-null string</remarks>
        public string GetPlayerExperienceData(RuntimePlayer player = null)
        {
            string data = null;
#if REMOTE_SESSIONS
            if (player == null && GameController.Instance)
            {
                player = GameController.Instance.CurrentPlayer;
            }
            data = player?.Player?.ExperienceData;
#endif
            return data;
        }

        #endregion

        #region Client side

        /// <summary>
        /// The payload we get when launched from the portal
        /// </summary>
        public string DesktopClientPayload
        {
            get; private set;
        }

        /// <summary>
        /// Whether or not we're running as a remote desktop client (i.e. launched from the portal)
        /// </summary>
        public bool IsDesktopClient
        {
            get
            {
                if (DesktopClientPayload == null)
                {
                    DesktopClientPayload = GetRemoteSessionClientPayload();
                    if (IsDesktopClient)
                    {
                        Debug.Log("Running as Desktop Client");
                    }
                }
                return !string.IsNullOrEmpty(DesktopClientPayload);
            }
        }

        private string GetRemoteSessionClientPayload()
        {
#if UNITY_EDITOR
            // https://dsl-portal-poc.dev.asu.techscapelearn.com/
            //return "gAAAAABgYv5PRcr-insmHAMYljuebx-g_tC2wJfqPLH3WBqBXIvdRA-kFbwm_os5eOh5jpQuezVynleUNgCXvNHHoY4a8B3JKVNor27Y13lTbIiV7vfMdm6J8zjUIxCtvJtpKrKRHwtVxGWDBeQNBP_L02xTewvPaT7jXaEwa14Qllp6SrDpZQsi9QGWPzbGA4STYLW14VMOCLX0ZI701sBl3WuljF_PoFT8hagIxqsnUQCsOueOZ8K2UXZ8sz09TOKo3lY-CEdtoJfiQJBk63o7bRYCECv9U9V7UFpnpbCm9jUxxldrEbigp1EMAPmFQQiGXTlDFpYlVxfiHMjx_hs_3_cNNiuJf_P6_Ttlmfv3DEcZrUm5CdJM4PR3p3NX1OnbUSVDJEOaJ6x_cnkvlBehRADZDL67lvTYumcZ6IPevC_ajOXeRHSqN_N_ZfVpQFwDC8uDOGINDBQNoYfQwwwgDNwE908s7gc8vYn4z4Sk6m-x70_ugNYRXZeH7hMDKgnvQ3mJvPMdSbBSY2peRZO_W9B1590mhgKoQxB5nW1GWX4lCaRIaj7Q0qBgt_L68O5nsVAGkLECJa9pmfZ73h94vaF6LjSFkXdlEyDgqvEaZu81PQjYjTJXTv5jd5TQjtXiAawOX62dSTjXtNMJ9lC7x1X2mU1_U-BOmxiPos-sIfZUncPvJmIPnhPY4G2I4oe5R58FSco7QH8y0kAS4z868BF-BG6UeCe5YjjDaLH0fWJaSZyEZw2d75-82PAEXnR6jFbslh7ozmHdIxJXf7xVc2n5ZtavsbIYNZJjquraV46X7iIgqf9invWrJf8kG06p5EaKtsGQtdmspVtCYZt5N5ix5oqwTrHO-x9aaeIQvkPgZSflwr39wp-qbbxYPrc0CjBefTlK3X89FUUjczovLPgaCQngrxnKmEkWC6e6Bu8SKDSsqyOx8ntNcwcSH6hzow5sJFaiAhlgyVfxH9rlGILtUCwqgAU0LPs9cBv4p0t2m5zOp4Qg5mCVbBJv2HeB-bwr-UpPBPmMyrCMG2KGaE8JRkgJJ80ZfWuO8474zHdvELpLvqhmzze8w_0sjRERjqmc14TmbXS0iQ4Hfq-NAaGWmeo85klL0aEDBqf_7R4Dhxc--qTsXCNlczJGAEeWn2JY4xK-xksNVoE6KlOe2z7CQ3d6ZEHD-ZWe4USNGSLMbHKBUa8y-1WDX9QFKp7Ay2-9IJUaMJcWaPDfQHo0hwGlGuFvvS0UIrJqu07PqZixJEdqB-FHEHy-rM8s9x0JOTV4xTe_dKBWlJKY5AG0QWetpWN2oP9r3P0ge_fJoGydLm8LY8w%3D";
            return "";
#else
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("dreamscapevr:launchclient?"))
                {
                    if (arg.StartsWith("dreamscapevr:launchclient?payload="))
                    {
                        return arg.Substring(arg.IndexOf('=') + 1);
                    }
                    else
                    {
                        Debug.LogError("Invalid desktop client argument: " + arg);
                    }
                }
            }
            return "";
#endif
        }

        /// <summary>
        /// The user session id for the player on this client, when in a remote session
        /// </summary>
        public Guid ClientUserSessionId
        {
            get; private set;
        }

#if REMOTE_SESSIONS
        Remote.IRemoteSessionClientController _remoteClient;

        //TODO HACK we should do this differently
        public void TakeRemoteClient(Remote.IRemoteSessionClientController remoteClient)
        {
            if (remoteClient == null)
            {
                throw new ArgumentNullException("remoteClient");
            }
            if (_remoteClient != null)
            {
                throw new InvalidOperationException("RemoteClient already assigned");
            }
            if (enabled)
            {
                Debug.Log("Taking RemoteClient instance");
                ClientUserSessionId = remoteClient.UserSessionId;
                _remoteClient = remoteClient;
            }
            else
            {
                Debug.LogWarning("Destroying RemoteClient instance because RemoteSessionController is disabled");
                (remoteClient as IDisposable).Dispose();
            }
        }

        void OnDisable()
        {
            if (_remoteClient != null)
            {
                _remoteClient.Dispose();
                _remoteClient = null;
            }
        }

        void Update()
        {
            if (_remoteClient != null)
            {
                _remoteClient.RefreshState();
            }
        }
#endif

            #endregion

            #region Server side

            /// <summary>
            /// Returns true if system.xml has a valid ClientApi node with an InstanceId
            /// in which case the SDK will connect to the RemoteSession endpoints
            /// </summary>F
        public bool IsRemoteServer
        {
            get
            {
                return Remote.RemoteSessionServerController.IsRemoteServer;
            }
        }

        /// <summary>
        /// Returns information for a given player in the remote session owned by this server
        /// </summary>
        /// <param name="userSessionId">The user session id of the player</param>
        /// <returns>Information about the remote player</returns>
        public Remote.RemotePlayerInfo GetPlayerInfo(Guid userSessionId)
        {
#if REMOTE_SESSIONS
            if (IsRemoteServer && HasSession)
            {
                return Remote.RemoteSessionServerController.Instance.GetUserSessionData(userSessionId);
            }
            else if (!IsRemoteServer)
            {
                Debug.LogWarning("Can't get player data because not running as server");
            }
            else
            {
                Debug.LogWarning("Can't get player data because not in session");
            }
#endif
            return new Remote.RemotePlayerInfo();
        }

        /// <summary>
        /// Log an JSON event for a specific player to the remote session endpoint
        /// </summary>
        /// <param name="userSessionId">The player's user session id</param>
        /// <param name="key">Name of the event</param>
        /// <param name="jsonString">Event data (as a JSON string)</param>
        public void LogEvent(Guid userSessionId, string key, string jsonString)
        {
#if REMOTE_SESSIONS
            if (IsRemoteServer && HasSession)
            {
                Remote.RemoteSessionServerController.Instance.LogEvent(userSessionId, key, jsonString);
            }
            else if (!IsRemoteServer)
            {
                if (DevelopmentMode.CurrentMode != EDevelopmentMode.ClientServer)
                {
                    Debug.LogWarning("Can't send session event because not running as remote server");
                }
            }
            else
            {
                Debug.LogWarning("Can't send session event because not in session");
            }
#endif
        }

        /// <summary>
        /// Notify the SDK that the given player has completed the game. This is used to tell the
        /// Remote Session endpoint which players have a complete data set
        /// </summary>
        /// <param name="userSessionId">The player's user session id</param>
        public void PlayerCompletedGame(Guid userSessionId)
        {
#if REMOTE_SESSIONS
            if (IsRemoteServer && HasSession)
            {
                Remote.RemoteSessionServerController.Instance.PlayerCompletedGame(userSessionId);
            }
            else if (!IsRemoteServer)
            {
                if (DevelopmentMode.CurrentMode != EDevelopmentMode.ClientServer)
                {
                    Debug.LogWarning("Cant send player status event because not running as remote server");
                }
            }
            else
            {
                Debug.LogWarning("Cant send player status event because not in session");
            }
#endif
        }

        #endregion
    }
}
