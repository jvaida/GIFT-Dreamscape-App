using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim.Remote
{
    [RequireComponent(typeof(RemoteSessionClientController))]
    public class DefaultRemoteExperienceClientSetup : MonoBehaviour, IExperienceSetup
    {
        [SerializeField]
        Text _headerText = null;

        [SerializeField]
        Text _consoleText = null;

        [SerializeField]
        Dropdown _sessionsDropdown = null;

        [SerializeField]
        Button _joinButton = null;

        [SerializeField]
        Button _startButton = null;

        bool _ready;

        uint _domainId = uint.MaxValue;
        List<System.Net.IPAddress> _componentsIps = new List<System.Net.IPAddress>();

        RemoteSessionClientController _remoteSession;
        List<RemoteSessionInfo> _sessions = new List<RemoteSessionInfo>();

        IEnumerator Start()
        {
            _headerText.text = "";
            _consoleText.text = "";

            _sessionsDropdown.ClearOptions();
            _sessionsDropdown.interactable = false;
            _joinButton.interactable = false;
            _startButton.interactable = false;

            // Wait for RemoteSessionClientController to Start
            yield return null;

            // Get RemoteSessionClientController
            _remoteSession = GetComponent<RemoteSessionClientController>();
            _remoteSession.GotRequestError += msg =>
            {
                if ((this != null) && enabled)
                {
                    ConsoleLogError(msg);
                }
            };

            if (_remoteSession.Initialized)
            {
                ConsoleLog("Payload received, title id = " + _remoteSession.TitleId);
                _headerText.text = _remoteSession.TitleId;

                // Wait for RemoteSessionController to be ready
                ConsoleLog("Connecting to: " + _remoteSession.BaseUrl);
                yield return new WaitUntil(() => _remoteSession.Connected);

                ConsoleLog("Connected!");

                _joinButton.interactable = true;
                _sessionsDropdown.interactable = true;

                StartCoroutine(AsyncRefreshSessions());
            }
            else
            {
                ConsoleLogError("Error reading payload");
            }
        }

        IEnumerator AsyncRefreshSessions()
        {
            while (true)
            {
                if ((_sessionsDropdown.options.Count == 0) || (!Enumerable.SequenceEqual(_sessions, _remoteSession.SessionsInfo)))
                {
                    _sessions.Clear();
                    _sessions.AddRange(_remoteSession.SessionsInfo);

                    var sessionsOptions = _sessions.Select(s => string.Format("{0} - {1} seats reserved out of {2}", s.StartTime.ToLocalTime().ToString("h:mm tt"), s.SeatsReserved, s.MaximumSeats)).ToList();
                    sessionsOptions.Insert(0, "New remote session");

                    _sessionsDropdown.ClearOptions();
                    _sessionsDropdown.AddOptions(sessionsOptions);

                    ConsoleLog("Session list updated");
                }

                yield return new WaitForSecondsRealtime(1);
            }
        }

        IEnumerator AsyncJoin()
        {
            bool canStart = false;
            _joinButton.interactable = false;
            _sessionsDropdown.interactable = false;

            var status = new TaskUtils.Status();

            if (_sessionsDropdown.value == 0)
            {
                ConsoleLog("Creating new remote session");

                // Create new session
                yield return _remoteSession.JoinOrCreate(status: status);
            }
            else
            {
                var session = _sessions[_sessionsDropdown.value - 1];
                ConsoleLog("Joining remote session " + session.Id);

                // Join existing session
                yield return _remoteSession.JoinOrCreate(session.Id, status);
            }

            if (!status.Success)
            {
                ConsoleLog("Error joining remote session!");

                _joinButton.interactable = true;
                _sessionsDropdown.interactable = true;

                yield break;
            }

            yield return new WaitUntil(() => _remoteSession.JoinedSessionInfo.Id != null);

            ConsoleLog("UserSessionId: " + _remoteSession.UserSessionId);

            ConsoleLog(string.Format("Server instance: {0}  IP: {1}, StartTime={2}, SessionId={3}", _remoteSession.JoinedSessionInfo.ServerInstanceId, _remoteSession.JoinedSessionInfo.ServerIP, _remoteSession.JoinedSessionInfo.StartTime.ToLocalTime(), _remoteSession.JoinedSessionInfo.Id));

            if (string.IsNullOrEmpty(_remoteSession.JoinedSessionInfo.ServerIP))
            {
                ConsoleLog(string.Format("Waiting for server IP..."));

                yield return new WaitWhile(() => string.IsNullOrEmpty(_remoteSession.JoinedSessionInfo.ServerIP));
            }

            try
            {
                _componentsIps.Add(System.Net.IPAddress.Parse(_remoteSession.JoinedSessionInfo.ServerIP));
            }
            catch (FormatException e)
            {
                ConsoleLog("Invalid server IP!");
                Debug.LogException(e);
            }

            canStart = _componentsIps.Count > 0;
            _startButton.interactable = canStart;
        }

        void ConsoleLog(string txt)
        {
            Debug.Log(">> " + txt);
            RawConsoleLog(txt);
        }

        void ConsoleLogError(string txt)
        {
            Debug.LogError(">> " + txt);
            RawConsoleLog(txt);
        }

        void RawConsoleLog(string txt)
        {
            _consoleText.text = txt + "\n" + _consoleText.text;
        }

        public IEnumerator Run(ExperienceSetupSettings outSettings)
        {
            yield return new WaitUntil(() => _ready);

            outSettings.DomainId = _domainId;
            outSettings.ComponentsIps = _componentsIps;
        }

        public void JoinSession()
        {
            StartCoroutine(AsyncJoin());
        }

        public void RefreshSessions()
        {
            if (_sessionsDropdown.interactable)
            {
                StartCoroutine(AsyncRefreshSessions());
            }
        }

        public void StartExperience()
        {
            _ready = true;
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}