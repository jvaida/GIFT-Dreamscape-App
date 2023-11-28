#if REMOTE_SESSIONS
using Artanim.Location.DreamscapeApiConnector.Remote;
using Artanim.Location.HostessAppData;
using Artanim.Location.RemoteSession;
using System.Threading.Tasks;
#endif
using Artanim.Location.Data;
using Artanim.Location.Hostess;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Artanim.Remote
{
    //TODO should be readonly
    public struct RemoteSessionInfo
    {
        public string Id;
        public string TitleId;
        public int MaximumSeats;
        public int SeatsReserved;
        public DateTimeOffset StartTime;
        public DateTimeOffset EndTime;
    }

    //TODO should be readonly
    public struct RemotePlayerInfo
    {
        public Guid Id;
        public string AvatarId;
        public string FirstName;
        public string LastName;
        public string AudioLangCode;
        public string CaptionLangCode;
        public bool ClosedCaption;
        public string ExperienceData;
    }

    public class RemoteSessionServerController : SingletonBehaviour<RemoteSessionServerController>
    {
        public static bool IsRemoteServer { get { return GetConfig() != null; } }

        public bool HasSession { get { return enabled && (!string.IsNullOrEmpty(SessionInfo.Id)); } }

        public RemoteSessionInfo SessionInfo { get; private set; }

        public ReadOnlyCollection<RemotePlayerInfo> PlayersInfo { get; private set; }

#if REMOTE_SESSIONS

        public RemotePlayerInfo GetUserSessionData(Guid userSessionId)
        {
            if (_userSessionsInfo != null)
            {
                return _userSessionsInfo.FirstOrDefault(u => u.Id == userSessionId);
            }
            return new RemotePlayerInfo();
        }

        public void LogEvent(Guid userSessionId, string key, string jsonString)
        {
            if (_remoteServer != null)
            {
                var task = _remoteServer.SendUserData(userSessionId, key, jsonString); // It's ok to not await since we don't read back the result
                _pendingTasks.Add(task);
            }
        }

        public void PlayerCompletedGame(Guid userSessionId)
        {
            Debug.LogFormat("RemoteSessionServer: player {0} completed game", userSessionId);
            _playersCompletedGame.Add(userSessionId);
        }

        RemoteServer _remoteServer;
        List<RemotePlayerInfo> _userSessionsInfo = new List<RemotePlayerInfo>();
        List<Guid> _playersCompletedGame = new List<Guid>();
        List<Task> _pendingTasks = new List<Task>();

        // Sync
        bool _updated;
        object _sync = new object();
        RemoteSessionInfo _updatedSessionInfo;
        RemotePlayerInfo[] _updatedUserSessionsInfo;

        #region Unity events

        void Start()
        {
            var config = GetConfig();
            if (config != null)
            {
                try
                {
                    var conn = new RemoteServerConnection(config.BaseUrl, config.BasePath, config.Key, config.UseHttps, config.Port == 0 ? new ushort?() : config.Port);
                    Initialize(conn, config.InstanceId);
                }
                catch
                {
                    enabled = false;
                    throw;
                }
            }

            enabled = _remoteServer != null;
        }

        void OnDisable()
        {
            Shutdown();
        }

        void Update()
        {
            if (_updated)
            {
                lock (_sync)
                {
                    SessionInfo = _updatedSessionInfo;
                    if (_updatedUserSessionsInfo != null)
                    {
                        _userSessionsInfo.Clear();
                        _userSessionsInfo.AddRange(_updatedUserSessionsInfo);
                        _updatedUserSessionsInfo = null;
                    }
                    _updated = false;
                }

                Debug.LogFormat("RemoteSessionServer: updated user sessions info => session id={0}, num players={1}, user session ids={2}",
                    SessionInfo.Id, _userSessionsInfo.Count, string.Join(", ", _userSessionsInfo.Select(us => us.Id).ToArray()));
            }

            for (int i = _pendingTasks.Count - 1; i >= 0; --i)
            {
                if (_pendingTasks[i].IsCompleted)
                {
                    _pendingTasks.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Internals

        private static Location.Config.DreamscapeApiConfig _config;
        private static bool _getConfigDone;

        private static Location.Config.DreamscapeApiConfig GetConfig()
        {
            if (!_getConfigDone)
            {
                _getConfigDone = true;

                if (Location.Config.SystemConfig.HasFile)
                {
                    try
                    {
                        var clientApiXml = Location.Config.SystemConfig.Instance.HostessData.Dreamscape;
                        if (clientApiXml == null)
                        {
                            Debug.LogFormat("RemoteSessionServer: {0} doesn't have a ClientApi node", Location.Config.SystemConfig.Pathname);
                        }
                        else if (string.IsNullOrEmpty(clientApiXml.BaseUrl))
                        {
                            Debug.LogFormat("RemoteSessionServer: {0} doesn't have a ClientApi.BaseUrl attribute or its value is empty", Location.Config.SystemConfig.Pathname);
                        }
                        else if (string.IsNullOrEmpty(clientApiXml.InstanceId))
                        {
                            Debug.LogFormat("RemoteSessionServer: {0} doesn't have a ClientApi.InstanceId attribute or its value is empty", Location.Config.SystemConfig.Pathname);
                        }
                        else
                        {
                            _config = clientApiXml;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else
                {
                    Debug.Log("RemoteSessionServer: couldn't find " + Location.Config.SystemConfig.Pathname);
                }
            }

            return _config;
        }

        private void Initialize(RemoteServerConnection conn, string instanceId)
        {
            Debug.LogFormat("RemoteSessionServer: instantiating server connection to {0} with instanceId {1}", conn.BaseUrl, instanceId);

            PlayersInfo = _userSessionsInfo.AsReadOnly();

            _remoteServer = new RemoteServer(new RemoteServerData(conn), instanceId);

            _remoteServer.JoinedSession += RemoteServer_JoinedSession;
            _remoteServer.SessionUpdated += RemoteServer_SessionUpdated;
            _remoteServer.UserSessionListUpdated += RemoteServer_UserSessionListUpdated;

            _remoteServer.Connect();

            StartCoroutine(TickServer());
            StartCoroutine(UpdateState());
        }

        private void Shutdown()
        {
            if (_remoteServer != null)
            {
                try
                {
                    Debug.Log("RemoteSessionServer: disposing...");
                    _remoteServer.JoinedSession -= RemoteServer_JoinedSession;
                    _remoteServer.SessionUpdated -= RemoteServer_SessionUpdated;
                    _remoteServer.UserSessionListUpdated -= RemoteServer_UserSessionListUpdated;
                    _remoteServer.DisposeAsync().AsTask().Wait();
                    _remoteServer = null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void RemoteServer_JoinedSession(object sender, IDataSession dataSession)
        {
            if (dataSession != null)
            {
                UpdateSessionInfo(dataSession);
            }
        }

        private void RemoteServer_SessionUpdated(object sender, IDataSession dataSession)
        {
            if (dataSession != null)
            {
                UpdateSessionInfo(dataSession);
            }
        }

        private void RemoteServer_UserSessionListUpdated(object sender, IReadOnlyList<IUserSession> userSessions)
        {
            if (userSessions != null)
            {
                lock (_sync)
                {
                    _updatedUserSessionsInfo = userSessions
                        .Where(s => s != null)
                        .Select(s => new RemotePlayerInfo
                        {
                            Id = s.Id,
                            AvatarId = s.AvatarId,
                            FirstName = s.FirstName,
                            LastName = s.LastName,
                            AudioLangCode = s.AudioLangCode,
                            CaptionLangCode = s.CaptionLangCode,
                            ClosedCaption = s.ClosedCaption,
                            ExperienceData = s.ExperienceData,
                        })
                        .ToArray();
                    _updated = true;
                }
            }
        }

        private void UpdateSessionInfo(IDataSession dataSession)
        {
            lock (_sync)
            {
                _updatedSessionInfo = new RemoteSessionInfo
                {
                    Id = dataSession.Id,
                    TitleId = dataSession.TitleId,
                    MaximumSeats = dataSession.MaximumSeats,
                    SeatsReserved = dataSession.SeatsReserved,
                    StartTime = dataSession.StartTime,
                    EndTime = dataSession.EndTime,
                };
                _updated = true;
            }
        }

        private IEnumerator TickServer()
        {
            while (_remoteServer != null)
            {
                _remoteServer.TickAlive();
                yield return new WaitForSecondsRealtime(1);
            }
        }

        private IEnumerator UpdateState()
        {
            var thisServer = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();

            do
            {
                // Clean up old data
                _userSessionsInfo.Clear();
                _playersCompletedGame.Clear();

                // Wait for ready for session
                Debug.Log("RemoteSessionServer: waiting for 'ReadyForSession' state");
                yield return new WaitUntil(() => thisServer.Status == ELocationComponentStatus.ReadyForSession);

                // Wait for session info
                Debug.Log("RemoteSessionServer: waiting for session info");
                yield return new WaitWhile(() => string.IsNullOrEmpty(SessionInfo.Id));

                // Create a new session
                var session = SessionManager.PrepareNewSession();
                session.Experience = ConfigService.Instance.ExperienceConfig.ExperienceName;
                session.ExperienceServerId = SharedDataUtils.MySharedId;
                session.ApiSessionId = SessionInfo.Id;
                session.StartScene = SessionInfo.TitleId;

                Debug.LogFormat("RemoteSessionServer: session created => Id={0}, TitleId={1}", SessionInfo.Id, SessionInfo.TitleId);

                // In client+server mode, we'll join the session in the client code
                if (thisServer is ExperienceServer)
                {
                    // Request join session
                    NetworkInterface.Instance.SendMessage(new RequestComponentJoinSession
                    {
                        ComponentId = thisServer.SharedId,
                        SessionId = session.SharedId,
                    });

                    // Wait for preparing session
                    Debug.Log("RemoteSessionServer: waiting for 'PreparingSession' state");
                    yield return new WaitUntil(() => thisServer.Status != ELocationComponentStatus.ReadyForSession);

                    void LogSessionState()
                    {
                        string userSessions = _userSessionsInfo == null ? "none" : string.Join(", ", _userSessionsInfo.Select(u => u.Id.ToString()).ToArray());
                        Debug.LogFormat("RemoteSessionServer: waiting to start session => Users={0}, SeatsReserved={1}, MaximumSeats={2}, StartTime={3}, EndTime={4}",
                            userSessions, SessionInfo.SeatsReserved, SessionInfo.MaximumSeats, SessionInfo.StartTime.ToLocalTime(), SessionInfo.EndTime.ToLocalTime());
                    }
                    void UpdatePlayerExpData()
                    {
                        //TODO HACK update player experience data
                        foreach (var player in session.Players)
                        {
                            var playerInfo = PlayersInfo.FirstOrDefault(pi => pi.Id == player.UserSessionId);
                            player.ExperienceData = playerInfo.ExperienceData;
                        }
                    }

                    // Wait to start session
                    bool terminateSend = false, cancelled = false;
                    var nextLog = new DateTime();
                    while (true)
                    {
                        // Log every 10 seconds
                        if (DateTime.UtcNow > nextLog)
                        {
                            nextLog = DateTime.UtcNow.AddSeconds(10);
                            LogSessionState();
                        }

                        if (session.Status != ESessionStatus.Initializing)
                        {
                            Debug.LogError("RemoteSessionServer: session in wrong state => " + session.Status);
                            break;
                        }

                        UpdatePlayerExpData();

                        // Check start conditions
                        if ((DateTime.UtcNow >= SessionInfo.StartTime)
                            || ((SessionInfo.MaximumSeats > 0) && (session.Players.Count(p => p.Status == EPlayerStatus.Calibrated) >= SessionInfo.MaximumSeats)))
                        {
                            LogSessionState();

                            Debug.Log("RemoteSessionServer: waiting on all players being calibrated and ready");
                            yield return new WaitUntil(() => session.Players.All(p =>
                            {
                                // Last chance
                                UpdatePlayerExpData();

                                var client = SharedDataUtils.FindLocationComponent<ExperienceClient>(p.ComponentId);
                                return (p.Status == EPlayerStatus.Calibrated) && (client != null) && (client.Status == ELocationComponentStatus.PreparingSession);
                            }));

                            Debug.Log("RemoteSessionServer: starting session");

                            if (session.Players.Count > 0)
                            {
                                var result = SessionManager.StartSession(session);
                                if (result != StartSessionStatus.SessionStarted)
                                {
                                    Debug.LogErrorFormat("RemoteSessionServer: failed to start session => {0}", result);
                                }
                                else
                                {
                                    yield return TaskUtils.RunTask(t => _remoteServer.UpdateSessionStatus(EDataSessionStatus.Boarding, t));
                                    yield return TaskUtils.RunTask(t => _remoteServer.UpdateSessionStatus(EDataSessionStatus.Launching, t));
                                    yield return TaskUtils.RunTask(t => _remoteServer.UpdateSessionStatus(EDataSessionStatus.Started, t));
                                }
                            }
                            else
                            {
                                Debug.LogWarning("RemoteSessionServer: no one in the session, canceling it");

                                NetworkInterface.Instance.SendMessage(new TerminateSession { SessionId = session.SharedId });
                                terminateSend = cancelled = true;
                            }

                            break;
                        }

                        // Wait a bit before next check
                        yield return new WaitForSecondsRealtime(0.1f);
                    }

                    // Wait for session to end
                    Debug.LogFormat("RemoteSessionServer: waiting for session to end");
                    while (true)
                    {
                        yield return new WaitForSecondsRealtime(0.1f);

                        if (session.Status == ESessionStatus.Ended)
                        {
                            Debug.Log("RemoteSessionServer: session status has changed to ended");
                            break;
                        }

                        if ((!terminateSend) && (DateTime.UtcNow >= SessionInfo.EndTime))
                        {
                            Debug.Log("RemoteSessionServer: session has reached end time");
                            NetworkInterface.Instance.SendMessage(new TerminateSession { SessionId = session.SharedId });
                            terminateSend = true;
                        }
                    }
                    
                    // Remove any player that hasn't completed the game
                    if (_userSessionsInfo != null)
                    {
                        foreach (var userSession in _userSessionsInfo.Select(us => us.Id).Except(_playersCompletedGame))
                        {
                            Debug.LogFormat("RemoteSessionServer: removing user session {0} ", userSession);
                            yield return TaskUtils.RunTask(t => _remoteServer.RemoveUserFromSession(userSession, t));
                        }
                    }
                    else
                    {
                        Debug.LogError("RemoteSessionServer: no user session info ");
                    }

                    // Update session status
                    Debug.LogFormat("RemoteSessionServer: session {0} ", cancelled ? " canceled" : "complete");
                    yield return TaskUtils.RunTask(t => _remoteServer.UpdateSessionStatus(cancelled ? EDataSessionStatus.Cancelled : EDataSessionStatus.Complete, t));

                    if (ConfigService.Instance.Config.Location.Server.RemoteSession.UnpairSession)
                    {
                        Debug.Log("RemoteSessionServer: unpair session");
                        bool scaleIn = !ConfigService.Instance.Config.Location.Server.RemoteSession.MultipleSessions;
                        yield return TaskUtils.RunTask(t => _remoteServer.UnpairSession(scaleIn, t));
                        
                    }
                }
            }
            while (ConfigService.Instance.Config.Location.Server.RemoteSession.MultipleSessions);

            // Wait on task completed
            if (_pendingTasks.Count > 0)
            {
                Debug.Log("RemoteSessionServer: waiting on tasks to complete");
                float timeout = Time.realtimeSinceStartup + 30;
                yield return new WaitUntil(() => (_pendingTasks.Count == 0) || (Time.realtimeSinceStartup > timeout));
            }
            if (_pendingTasks.Count > 0)
            {
                Debug.LogError("RemoteSessionServer: some tasks didn't completed");
            }

            // Stop connection with remote server
            Shutdown();

            //
            // Shutdown experience
            //
#if !UNITY_EDITOR
            // Shutdown vrtex:
            if (ConfigService.Instance.Config.Location.Server.RemoteSession.ShutdownVrtex)
            {
                Debug.Log("RemoteSessionServer: preparing to shut down vrtex");
                yield return new WaitForSecondsRealtime(10); // Wait a bit

                // Initialize the logger
                var logger = LogSystem.LogManager.Instance.GetLogger("NotifyProcesses");
                var io = Utils.IO.IOContext.StandardWithLog(logger);

                var liveProcesses = ProcessAnnounce.KeepValidLiveProcesses(ProcessAnnounce.GetAnnouncedLiveProcesses("vrtex-client"));
                if (liveProcesses?.Length > 0)
                {
                    if (liveProcesses.Length > 1)
                    {
                        Debug.LogWarning("RemoteSessionServer: got more than one vrtex to shutdown");
                    }

                    Debug.LogFormat("RemoteSessionServer: sending command to shutdown vrtex with pid => {0}",
                        string.Concat(", ", liveProcesses.Select(p => p.ToString()).ToArray()));

                    ProcessNotify.NotifyProcesses(liveProcesses.Select(p => p.ProcessId).ToArray(), io);
                }
                else
                {
                    Debug.LogError("RemoteSessionServer: couldn't shutdown vrtex");
                }
            }
#endif

            Debug.LogFormat("RemoteSessionServer: done!");
        }

        #endregion

#else
        void Awake()
        {
            enabled = false;
        }


        private static Location.Config.DreamscapeApiConfig GetConfig()
        {
            return null;
        }
#endif
    }
}