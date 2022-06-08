using Artanim.Location.Network;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Artanim.LogSystem;
using System.Text;
using Artanim.Location.SharedData;
using Artanim.Location.Data;

namespace Artanim
{
    public class SessionTiming : SingletonBehaviour<SessionTiming>
    {
        public enum EEventType { SessionStart, SessionEnd, SceneLoad, SectionEnd, }

        private LogSystem.Logger SessionEventsLogger;

        private SessionEvent[] LastEventsByType = new SessionEvent[Enum.GetValues(typeof(EEventType)).Length];
        private Guid CurrentSessionId = Guid.Empty;

        #region Unity Events

        private void OnEnable()
        {
            if(SessionEventsLogger == null)
                SessionEventsLogger = LogManager.Instance.GetLogger("SessionTiming");

            if (NetworkInterface.Instance.IsServer && GameController.Instance)
            {
                GameController.Instance.OnSessionStarted += Instance_OnSessionStarted;
                GameController.Instance.OnSceneLoadedInSession += Instance_OnSceneLoadedInSession;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession;
            }
        }

        private void OnDisable()
        {
            if(NetworkInterface.Instance.IsServer && GameController.HasInstance)
            {
                GameController.Instance.OnSessionStarted -= Instance_OnSessionStarted;
                GameController.Instance.OnSceneLoadedInSession -= Instance_OnSceneLoadedInSession;
                GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
            }
        }

        #endregion

        #region Public Interface

        public void RegisterSectionEnd(string sectionName)
        {
            AddEvent(EEventType.SectionEnd, sectionName);
        }

        #endregion

        #region Location Events

        private void Instance_OnSessionStarted()
        {
            Reset();

            //Keep session id
            if (GameController.Instance && GameController.Instance.CurrentSession != null)
                CurrentSessionId = GameController.Instance.CurrentSession.SharedId.Guid;

            //Log session start infos containing all session components
            LogCurrentSessionStartInfos();

            AddEvent(EEventType.SessionStart, "Session Start");
        }

        private void Instance_OnSceneLoadedInSession(string[] sceneNames, bool sceneLoadTimedOut)
        {
            AddEvent(EEventType.SceneLoad, sceneNames != null ? string.Join(", ", sceneNames) : string.Empty);
        }

        private void Instance_OnLeftSession()
        {
            AddEvent(EEventType.SessionEnd, "Session End");
            CurrentSessionId = Guid.Empty;
        }

        #endregion

        #region Internals

        private void Reset()
        {
            for (var i = 0; i < Enum.GetValues(typeof(EEventType)).Length; ++i)
                LastEventsByType[i] = null;
        }

        private void AddEvent(EEventType eventType, string eventName)
        {
            if(NetworkInterface.Instance.IsServer)
            {
                var sessionEvent = new SessionEvent(eventType, eventName);

                //Log event
                LogEvent(sessionEvent);

                //Store last event by type
                LastEventsByType[(int)eventType] = sessionEvent;
            }
        }

        private void LogEvent(SessionEvent sessionEvent)
        {
            switch (sessionEvent.EventType)
            {
                case EEventType.SessionStart:
                    break;

                case EEventType.SessionEnd:
                    LogEventComparedToLast(sessionEvent, EEventType.SectionEnd, EEventType.SectionEnd); //No fallback if not section was logged
                    LogEventComparedToLast(sessionEvent, EEventType.SceneLoad);
                    LogEventComparedToLast(sessionEvent, EEventType.SessionStart);
                    break;

                case EEventType.SceneLoad:
                    LogEventComparedToLast(sessionEvent, EEventType.SceneLoad);
                    break;

                case EEventType.SectionEnd:
                    LogEventComparedToLast(sessionEvent, EEventType.SectionEnd);
                    break;
            }
        }

        private void LogCurrentSessionStartInfos()
        {
            if(GameController.Instance && GameController.Instance.CurrentSession != null)
            {
                var session = GameController.Instance.CurrentSession;
                var sessionStartLog = new StringBuilder();
                sessionStartLog.AppendFormat("Session started with components: SessionId={0}, SharedData={1}, ParentId={2}", session.SharedId.Guid, session.SharedId.Description, session.ParentGuid);

                //Server
                var server = SharedDataUtils.FindLocationComponent(session.ExperienceServerId);
                if (server != null)
                    sessionStartLog.AppendFormat("\n\tServer: Hostname={0}, ComputerId={1}, Id={2}, SharedData={3}", server.ComputerHostname, server.ComputerId, server.SharedId.Guid, server.SharedId.Description);

                //Clients
                foreach(var player in session.Players)
                {
                    var client = SharedDataUtils.FindLocationComponent(player.ComponentId);
                    if (client != null)
                    {
                        var skeleton = SharedDataUtils.FindChildSharedData<Location.Data.SkeletonConfig>(player.SkeletonId);
                        string skeletonInfo = skeleton != null ?
                            string.Format("{0}, Group={1}, Number={2}, Head={3}, Id={4}, SharedData={5}, NotTrackedSubjects={6}",
                                skeleton.Name, skeleton.Group, skeleton.Number, skeleton.SkeletonSubjectNames[(int)ESkeletonSubject.Head], skeleton.SharedId.Guid, skeleton.SharedId.Description,
                                skeleton.NotTrackedSubjects != null ? string.Join(", ", skeleton.NotTrackedSubjects.Select(s => s.ToString()).ToArray()) : string.Empty)
                            : "<not found>";
                        sessionStartLog.AppendFormat("\n\tClient: Hostname={0}, Avatar={1}, Skeleton=[{2}], ComputerId={3}, Id={4}, SharedData={5}, CalibrationMode={6}, IsDesktop={7}",
                            client.ComputerHostname, player.Avatar, skeletonInfo, client.ComputerId, client.SharedId.Guid, client.SharedId.Description, player.CalibrationMode, player.IsDesktop);
                    }
                }

                SessionEventsLogger.Log(LogManager.Levels.INFO, sessionStartLog.ToString());
            }
        }

        private void LogEventComparedToLast(SessionEvent sessionEvent, EEventType lastEventType, EEventType fallbackEventType = EEventType.SessionStart)
        {
            var lastEvent = LastEventsByType[(int)lastEventType];

            //Fallback?
            if(lastEvent == null)
                lastEvent = LastEventsByType[(int)fallbackEventType];

            if(lastEvent != null)
            {
                var elapsed = sessionEvent.Time - lastEvent.Time;
                var message = string.Format("{0}-{1}: {2} >> {3} : {4}",
                            CurrentSessionId,
                            sessionEvent.EventType,
                            lastEvent.Display,
                            sessionEvent.Display,
                            FormatTimeSpan(elapsed));

                SessionEventsLogger.Log(LogManager.Levels.INFO, message);
                Debug.Log(message);
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return string.Format("{0:00}:{1:00}.{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }

        #endregion

        #region Classes

        private class SessionEvent
        {
            public EEventType EventType { get; private set; }

            public string Name { get; private set; }

            public DateTime Time { get; private set; }

            public string Display
            {
                get
                {
                    switch (EventType)
                    {
                        case EEventType.SessionStart:
                        case EEventType.SessionEnd:
                            return Name;
                        case EEventType.SceneLoad:
                        case EEventType.SectionEnd:
                            return string.Format("{0}:{1}", EventType, Name);
                        default:
                            return Name;
                    }
                }
            }

            public SessionEvent(EEventType eventType, string eventName)
            {
                EventType = eventType;
                Name = eventName;
                Time = DateTime.UtcNow;
            }
        }

        #endregion
    }
}