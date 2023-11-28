#if REMOTE_SESSIONS
using Artanim.Location.DreamscapeApiConnector.Remote;
using Artanim.Location.RemoteSession;
#endif

using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{
    public class GameProgressController : SingletonBehaviour<GameProgressController>
    {
        GameController _gameController;
        LogSystem.Logger _logger;
        ClientApiConnection _clientApi;

        #region Public methods

        public void AddPlayerEvent(string eventName, object data = null)
        {
            AddPlayerJsonEvent(eventName, data == null ? null : JsonUtility.ToJson(data));
        }

        public void AddPlayerJsonEvent(string eventName, string jsonString = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("eventName");
            }    

            if (CheckSession(string.Format("player event '{0}'", eventName)))
            {
                if (_gameController.CurrentPlayer != null)
                {
                    AddPlayerJsonEvent(_gameController.CurrentPlayer, eventName, jsonString);
                }
                else
                {
                    Debug.LogErrorFormat("Discarding player event '{0}': no active player", eventName);
                }
            }
        }

        public void AddPlayerEvent(RuntimePlayer player, string eventName, object data = null)
        {
            AddPlayerJsonEvent(player, eventName, data == null ? null : JsonUtility.ToJson(data));
        }

        public void AddPlayerJsonEvent(RuntimePlayer player, string eventName, string jsonString = null)
        {
            if (player == null)
            {
                throw new ArgumentNullException("player");
            }
            if (player.Player == null)
            {
                throw new ArgumentException("player");
            }
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("eventName");
            }

            if (CheckSession(string.Format("player event '{0}'", eventName)))
            {
                if (_gameController.RuntimePlayers.Contains(player))
                {
                    LogEvent(player.Player.UserSessionId, eventName, jsonString);
                }
                else
                {
                    Debug.LogErrorFormat("Discarding player event '{0}': player not in current session", eventName);
                }
            }
        }

        public void AddSessionEvent(string eventName, object data = null)
        {
            AddSessionJsonEvent(eventName, data == null ? null : JsonUtility.ToJson(data));
        }

        public void AddSessionJsonEvent(string eventName, string jsonString = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("eventName");
            }

            if (CheckSession(string.Format("session event '{0}'", eventName)))
            {
                LogEvent(Guid.Empty, eventName, jsonString);
            }
        }

        public void PlayerCompletedGame()
        {
            if (CheckSession("completed game status"))
            {
                if (_gameController.CurrentPlayer != null)
                {
                    PlayerCompletedGame(_gameController.CurrentPlayer);
                }
                else
                {
                    Debug.LogError("Discarding completed game status: no active player");
                }
            }
        }

        public void PlayerCompletedGame(RuntimePlayer player)
        {
            if (NetworkInterface.Instance.IsServer)
            {
                // Remote session?
                if (_clientApi == null)
                {
                    RemoteSessionController.Instance.PlayerCompletedGame(player.Player.UserSessionId);
                }
            }
            else
            {
                NetworkInterface.Instance.SendMessage(new GameProgressPlayerStatus
                {
                    RecipientId = _gameController.CurrentSession.ExperienceServerId,
                    UserSessionId = player.Player.UserSessionId,
                    CompletedGame = true,
                });

            }
        }

        #endregion

        #region Internals

        bool CheckSession(string context)
        {
            if (_gameController == null)
            {
                Debug.LogErrorFormat("Discarding {0}: no GameController", context);
                return false;
            }
            else if (_gameController.CurrentSession == null)
            {
                Debug.LogErrorFormat("Discarding {0}: no active session", context);
                return false;
            }

            return true;
        }

        void LogEvent(Guid userSessionId, string eventName, string jsonString)
        {
            // Add more info to JSON
            jsonString = string.Format("{{ \"timestamp\":\"{0}\", \"component_id\":\"{1}\", \"session_id\":\"{2}\", \"data\":{3} }}",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"), Location.SharedData.SharedDataUtils.MySharedId.Guid, _gameController.CurrentSession.SharedId.Guid, jsonString ?? "{}");

            if (ConfigService.VerboseSdkLog) Debug.LogFormat("Sending game progress event {0} for user session id {1} with data: {2}", eventName, userSessionId, jsonString);
            _logger.InfoFormat("json:{{ \"user_session_id\":\"{0}\", \"event_name\":\"{1}\", \"event_data\":{2} }}", userSessionId, eventName, jsonString);

            if (NetworkInterface.Instance.IsServer)
            {
                // Remote session?
                if (_clientApi == null)
                {
                    RemoteSessionController.Instance.LogEvent(userSessionId, eventName, jsonString);
                }
                else
                {
                    _clientApi.LogEvent(userSessionId, eventName, jsonString);
                }
            }
            else
            {
                NetworkInterface.Instance.SendMessage(new GameProgressEvent
                {
                    RecipientId = _gameController.CurrentSession.ExperienceServerId,
                    UserSessionId = userSessionId,
                    Name = eventName,
                    Data = jsonString,
                });
            }
        }

        void OnGameProgressEvent(GameProgressEvent args)
        {
            Debug.LogFormat("Got game progress event {0} for user session id {1} with data: {2}", args.Name, args.UserSessionId, args.Data);
            _logger.InfoFormat("{{\"user_session_id\":\"{0}\",\"event_name\":\"{1}\",\"event_data\":{2}}}", args.UserSessionId, args.Name, args.Data);

            // Remote session?
            if (_clientApi == null)
            {
                RemoteSessionController.Instance.LogEvent(args.UserSessionId, args.Name, args.Data);
            }
            else
            {
                _clientApi.LogEvent(args.UserSessionId, args.Name, args.Data);
            }
        }

        void OnGameProgressPlayerStatus(GameProgressPlayerStatus args)
        {
            Debug.LogFormat("Got game progress player status for user session id {0} with data: {1}", args.UserSessionId, args.CompletedGame);

            if (args.CompletedGame)
            {
                // Remote session?
                if (_clientApi == null)
                {
                    RemoteSessionController.Instance.PlayerCompletedGame(args.UserSessionId);
                }
            }
        }

        #endregion

        #region Unity Events

        void Awake()
        {
            _gameController = GameController.Instance;
            _logger = LogSystem.LogManager.Instance.GetLogger("GameProgress");
        }

        void OnEnable()
        {
            if (NetworkInterface.Instance.IsServer)
            {
                NetworkInterface.Instance.Subscribe<GameProgressEvent>(OnGameProgressEvent);
                NetworkInterface.Instance.Subscribe<GameProgressPlayerStatus>(OnGameProgressPlayerStatus);

                if (!RemoteSessionController.Instance.IsRemoteServer)
                {
                    _clientApi = new ClientApiConnection();
                }
            }
        }

        void OnDisable()
        {
            NetworkInterface.SafeUnsubscribe<GameProgressEvent>(OnGameProgressEvent);
            NetworkInterface.SafeUnsubscribe<GameProgressPlayerStatus>(OnGameProgressPlayerStatus);

            if (_clientApi != null)
            {
                _clientApi.Dispose();
                _clientApi = null;
            }
        }

        #endregion

        #region Temp class for accessing ClientAPI

        class ClientApiConnection
        {
#if REMOTE_SESSIONS
            RemoteServer _remoteServer;

            public ClientApiConnection()
            {
                if (Location.Config.SystemConfig.HasFile)
                {
                    try
                    {
                        var clientApiXml = Location.Config.SystemConfig.Instance.HostessData.Dreamscape;
                        if (clientApiXml == null)
                        {
                            Debug.LogFormat("GameProgress: {0} doesn't have a ClientApi node", Location.Config.SystemConfig.Pathname);
                        }
                        else if (string.IsNullOrEmpty(clientApiXml.BaseUrl))
                        {
                            Debug.LogFormat("GameProgress: {0} doesn't have a ClientApi.BaseUrl attribute or its value is empty", Location.Config.SystemConfig.Pathname);
                        }
                        else
                        {
                            var conn = new RemoteServerConnection(clientApiXml.BaseUrl, clientApiXml.BasePath, clientApiXml.Key, clientApiXml.UseHttps, clientApiXml.Port == 0 ? new ushort?() : clientApiXml.Port);

                            Debug.LogFormat("GameProgress: instantiating server connection to {0}", conn.BaseUrl);
                            _remoteServer = new RemoteServer(new RemoteServerData(conn), null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarningFormat("GameProgress: error instantiating server connection => {0}", ex);
                    }
                }
            }

            public void LogEvent(Guid userSessionId, string key, string jsonString)
            {
                if (_remoteServer != null)
                {
                    _remoteServer.SendUserData(userSessionId, key, jsonString); // It's ok to not await since we don't read back the result
                }
                else
                {
                    Debug.LogFormat("Not connection to send event {0} with data {1}", key, jsonString);
                }
            }

            public void Dispose()
            {
                if (_remoteServer != null)
                {
                    Debug.Log("GameProgress: disposing RemoteServer...");
                    _remoteServer.DisposeAsync().AsTask().Wait();
                    _remoteServer = null;
                }
            }
#else
            public void LogEvent(Guid userSessionId, string key, string jsonString)
            {
            }

            public void Dispose()
            {
            }
#endif
        }

        #endregion
    }
}
