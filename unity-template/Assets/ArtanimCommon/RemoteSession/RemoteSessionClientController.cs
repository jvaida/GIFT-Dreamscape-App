#if REMOTE_SESSIONS
using Artanim.Location.DreamscapeApiConnector.Remote;
using Artanim.Location.HostessAppData;
using Artanim.Location.HostessAppData.Remote;
using Artanim.Location.RemoteSession;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Artanim.Remote
{
    public struct JoinedSessionInfo
    {
        public string Id;
        public string TitleId;
        public DateTimeOffset StartTime;
        public DateTimeOffset EndTime;
        public int SeatsReserved;
        public int MaximumSeats;
        public string ServerInstanceId;
        public string ServerIP;
    }

    public interface IRemoteSessionClientController : IDisposable
    {
        Guid UserSessionId { get; }
        JoinedSessionInfo JoinedSessionInfo { get; }
        void RefreshState();
    }

    public class RemoteSessionClientController : MonoBehaviour, IRemoteSessionClientController
    {
        public event Action<string> GotRequestError;

#if REMOTE_SESSIONS
        public bool Initialized { get { return _remoteClient != null; } }

        public Guid UserSessionId { get { return _remoteClient == null ? Guid.Empty : _remoteClient.UserSessionId; } }

        public bool Connected { get { return _sessionsInfo != null; } }
#else
        public bool Initialized { get { return false; } }

        public Guid UserSessionId { get { return Guid.Empty; } }

        public bool Connected { get { return false; } }
#endif
        public string TitleId { get; private set; }

        public string BaseUrl { get; private set; }

        public ReadOnlyCollection<RemoteSessionInfo> SessionsInfo { get; private set; }

        public JoinedSessionInfo JoinedSessionInfo { get; private set; }

        public IEnumerator JoinOrCreate(string sessionId = null, TaskUtils.Status status = null)
        {
#if REMOTE_SESSIONS
            if (_remoteClient != null)
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    yield return TaskUtils.RunTask(_remoteClient.CreateSession, status);
                }
                else
                {
                    yield return TaskUtils.RunTask(t => _remoteClient.JoinSession(sessionId, t), status);
                }
            }
#else
            if (GotRequestError != null) return null; // Just to avoid compilation warning
            return null;
#endif
        }

#if REMOTE_SESSIONS
        RemoteClient _remoteClient;
        List<RemoteSessionInfo> _sessionsInfo;

        // Sync
        bool _updated;
        object _sync = new object();
        JoinedSessionInfo _updatedJoinedSessionInfo;
        RemoteSessionInfo[] _updatedSessionsInfo;
        string _updatedLastError;

        #region Unity events

        // Start is called before the first frame update
        void Start()
        {
            if (RemoteSessionController.Instance.IsDesktopClient)
            {
                string payload = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(RemoteSessionController.Instance.DesktopClientPayload);
                Initialize(payload, "DtbIKM91hg8R3Gu_I9w05gnrHVyiR2JNQMJsi9WRrqo=");
            }
            else
            {
                Debug.LogError("Missing payload argument");
            }
        }

        void OnDisable()
        {
            if (_remoteClient != null)
            {
                if (RemoteSessionController.Instance)
                {
                    RemoteSessionController.Instance.TakeRemoteClient(this);
                }
                else
                {
                    Debug.LogWarning("Destroying RemoteClient instance because RemoteSessionController was not found");
                    (this as IDisposable).Dispose();
                }
            }
        }

        void Update()
        {
            if (_updated)
            {
                string lastError = null;
                lock (_sync)
                {
                    JoinedSessionInfo = _updatedJoinedSessionInfo;
                    if (_updatedSessionsInfo != null)
                    {
                        if (_sessionsInfo == null)
                        {
                            _sessionsInfo = new List<RemoteSessionInfo>();
                            SessionsInfo = _sessionsInfo.AsReadOnly();
                        }
                        else
                        {
                            _sessionsInfo.Clear();
                        }
                        _sessionsInfo.AddRange(_updatedSessionsInfo);
                    }
                    lastError = _updatedLastError;
                    _updatedLastError = null;
                    _updated = false;
                }

                if ((lastError != null) && (GotRequestError != null))
                {
                    GotRequestError(lastError);
                }
            }
        }

        #endregion

        #region Internals

        private void Initialize(string payload, string key)
        {
            Debug.Log("RemoteSessionClient: instantiating client connection");

            var data = new RemoteClientData(payload, key);
            _remoteClient = new RemoteClient(data);

            BaseUrl = data.BaseUrl;
            TitleId = data.TitleId;

            _remoteClient.ManifestUpdated += RemoteClient_ManifestUpdated;
            _remoteClient.AvailableSessionsChanged += RemoteClient_AvailableSessionsChanged;
            //_remoteClient.JoinedSession += RemoteClient_JoinedSession;
            _remoteClient.AutomaticRequestFailed += RemoteClient_AutomaticRequestFailed;

            _remoteClient.Connect();
        }

        void IDisposable.Dispose()
        {
            if (_remoteClient != null)
            {
                Debug.Log("RemoteSessionClient: disposing...");
                _remoteClient.ManifestUpdated -= RemoteClient_ManifestUpdated;
                _remoteClient.AvailableSessionsChanged -= RemoteClient_AvailableSessionsChanged;
                //_remoteClient.JoinedSession -= RemoteClient_JoinedSession;
                _remoteClient.AutomaticRequestFailed -= RemoteClient_AutomaticRequestFailed;
                _remoteClient.DisposeAsync().AsTask().Wait();
                _remoteClient = null;
            }
        }

        void IRemoteSessionClientController.RefreshState()
        {
            Update();
        }

        private void RemoteClient_ManifestUpdated(object sender, IClientManifest manifest)
        {
            if (manifest != null)
            {
                lock (_sync)
                {
                    if (manifest.DataSession != null)
                    {
                        _updatedJoinedSessionInfo.Id = manifest.DataSession.Id;
                        _updatedJoinedSessionInfo.TitleId = manifest.DataSession.TitleId;
                        _updatedJoinedSessionInfo.StartTime = manifest.DataSession.StartTime;
                        _updatedJoinedSessionInfo.EndTime = manifest.DataSession.EndTime;
                        _updatedJoinedSessionInfo.SeatsReserved = manifest.DataSession.SeatsReserved;
                        _updatedJoinedSessionInfo.MaximumSeats = manifest.DataSession.MaximumSeats;
                    }
                    if (manifest.GameServer != null)
                    {
                        _updatedJoinedSessionInfo.ServerInstanceId = manifest.GameServer.InstanceId;
                        _updatedJoinedSessionInfo.ServerIP = manifest.GameServer.PrivateIP;
                    };

                    _updated = true;
                }
            }
        }

        private void RemoteClient_AvailableSessionsChanged(object sender, IReadOnlyList<IRemoteSession> sessions)
        {
            if (sessions != null)
            {
                lock (_sync)
                {
                    _updatedSessionsInfo = sessions
                        .Where(s => s != null)
                        .Select(s => new RemoteSessionInfo
                        {
                            Id = s.Id,
                            MaximumSeats = s.SeatsAvailable + s.SeatsReserved,
                            SeatsReserved = s.SeatsReserved,
                            StartTime = s.StartTimestamp,
                        })
                        .ToArray();
                    _updated = true;
                }
            }
        }

        //private void RemoteClient_JoinedSession(object sender, IDataSession dataSession)
        //{
        //    if (dataSession != null)
        //    {
        //        lock (_sync)
        //        {
        //            _updatedJoinedSessionInfo = new JoinedSessionInfo
        //            {
        //                Id = dataSession.Id,
        //                TitleId = dataSession.TitleId,
        //                StartTime = dataSession.StartTime,
        //                EndTime = dataSession.EndTime,
        //                MaximumSeats = dataSession.MaximumSeats,
        //                SeatsAvailable = dataSession.SeatsAvailable,
        //            };

        //            _updated = true;
        //        }
        //    }
        //}

        private void RemoteClient_AutomaticRequestFailed(object sender, Location.HostessAppData.ApplicationDataException exception)
        {
            if (exception != null)
            {
                lock (_sync)
                {
                    _updatedLastError = exception.Message;
                    _updated = true;
                }
            }
        }

        #endregion
#else
        void IDisposable.Dispose() {}
        void IRemoteSessionClientController.RefreshState() {}
#endif
    }
}