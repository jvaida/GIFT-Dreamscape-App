using Artanim.Location.Data;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using PropTrackingTimeout = Artanim.Location.Monitoring.OpTicketsTypes.Experience.PropTrackingTimeout;
using PropNotReady = Artanim.Location.Monitoring.OpTicketsTypes.Experience.PropNotReady;
using Artanim.Location.Config;

namespace Artanim.Tracking
{

    // Monitor the state of props both when initializing and running a session
    // Tickets are open on detected issues (prop not tracked, prop not properly located, etc.)
	public class PropTrackingMonitor : ServerSideBehaviour
	{

        class PropNotReadyMonitoring
        {
            // Ticket for reporting an issue (if any)
            Location.Monitoring.OperationalTickets.OpTicket<PropNotReady> _ticket;

            readonly TrackedProp _config;

            public string Name { get { return _config.Name; } }

            public bool HasGroup { get; private set; }

            public TrackedProp Config { get { return _config; } }

            public PropNotReadyMonitoring(TrackedProp trackedPropConfig)
            {
                _config = trackedPropConfig;
                HasGroup = (!string.IsNullOrEmpty(_config.Group)) && _config.Group.Any(c => !char.IsWhiteSpace(c));
            }

            // Reports a tracking or initial state issue on this prop
            public void ReportIssue(bool notTracked, bool badPosition, bool badOrientation)
            {
                if ((_ticket != null)
                    && ((_ticket.Data.NotTracked != notTracked) || (_ticket.Data.BadPosition != badPosition) || (_ticket.Data.BadOrientation != badOrientation)))
                {
                    ClearIssue();
                }
                if ((_ticket == null) && (notTracked | badPosition | badOrientation))
                {
                    _ticket = Location.Monitoring.OperationalTickets.Instance.OpenTicket(
                        new PropNotReady()
                        {
                            ComponentId = SharedDataUtils.MySharedId,
                            PropName = Name,
                            NotTracked = notTracked,
                            BadPosition = badPosition,
                            BadOrientation = badOrientation,
                        });
                }
            }

            // Reports everything ok with this prop
            public void ClearIssue()
            {
                if (_ticket != null)
                {
                    _ticket.Close();
                    _ticket = null;
                }
            }
        }

        class PropTimeoutMonitoring
        {
            // Ticket for reporting an issue (if any)
            Location.Monitoring.OperationalTickets.OpTicket<PropTrackingTimeout> _ticket;

            public string Name { get; private set; }

            public PropTimeoutMonitoring(string name)
            {
                Name = name;
            }

            // Reports a tracking issue on this prop
            public void ReportIssue(Guid sessionId)
            {
                if (_ticket == null)
                {
                    _ticket = Location.Monitoring.OperationalTickets.Instance.OpenTicket(
                        new PropTrackingTimeout()
                        {
                            ComponentId = SharedDataUtils.MySharedId,
                            SessionId = sessionId,
                            PropName = Name,
                        });
                }
            }

            // Reports everything ok with this prop
            public void ClearIssue()
            {
                if (_ticket != null)
                {
                    _ticket.Close();
                    _ticket = null;
                }
            }
        }

        PropNotReadyMonitoring[] _propsNotReadyMonitoring;
        PropTimeoutMonitoring[] _permanentPropsMonitoring;
        bool[] _cache_trackedProps;

        float _propNotReadyPeriod, _propNotReadyTrackingTimeout;
        float _propTrackingTimeoutPeriod, _propTrackingTimeout;

        void Start()
        {
            _propNotReadyPeriod = (float)Location.Monitoring.OperationalTickets.Instance.GetParamValue<PropNotReady>("RefreshPeriod", 50);
            _propNotReadyTrackingTimeout = (float)Location.Monitoring.OperationalTickets.Instance.GetParamValue<PropNotReady>("TrackingTimeout", 0.1);
            _propTrackingTimeoutPeriod = (float)Location.Monitoring.OperationalTickets.Instance.GetParamValue<PropTrackingTimeout>("RefreshPeriod", 200);
            _propTrackingTimeout = (float)Location.Monitoring.OperationalTickets.Instance.GetParamValue<PropTrackingTimeout>("Timeout", 5);

            // Disable prop monitoring in standalone mode or if off in config
            if ((DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
                || (!ConfigService.Instance.Config.Location.Server.PropsMonitoring))
            {
                enabled = false;
            }
        }

        // Check props
        void Update()
        {
            Session session = GameController.Instance.CurrentSession;

            if ((session == null) || (session.Status == ESessionStatus.Initializing))
            {
                bool update = (_propsNotReadyMonitoring == null) || ((Time.frameCount % _propNotReadyPeriod) == 0);

                // Create list of props to check
                if (_propsNotReadyMonitoring == null)
                {
                    _propsNotReadyMonitoring = ConfigService.Instance.ExperienceConfig.TrackedProps.Select(p => new PropNotReadyMonitoring(p)).ToArray();
                    _cache_trackedProps = new bool[_propsNotReadyMonitoring.Length];
                    Debug.Log("Monitoring props initial status: " + string.Join(",", _propsNotReadyMonitoring.Select(p => p.Name).ToArray()));
                }

                if (update)
                {
                    UpdatePropNotReady(session);
                }
            }
            else if (_propsNotReadyMonitoring != null)
            {
                // Close all issues
                for (int i = 0, iMax = _propsNotReadyMonitoring.Length; i < iMax; ++i)
                {
                    _propsNotReadyMonitoring[i].ClearIssue();
                }
                _propsNotReadyMonitoring = null;
            }


            if ((session != null) && (session.Status == ESessionStatus.Started))
            {
                bool update = (_permanentPropsMonitoring == null) || ((Time.frameCount % _propTrackingTimeoutPeriod) == 0);

                // Create list of props to check
                if (_permanentPropsMonitoring == null)
                {
                    _permanentPropsMonitoring = ConfigService.Instance.ExperienceConfig.TrackedProps.Where(p => !p.Transient).Select(p => new PropTimeoutMonitoring(p.Name)).ToArray();
                    Debug.Log("Monitoring 'permanent' props: " + string.Join(",", _permanentPropsMonitoring.Select(p => p.Name).ToArray()));
                }

                if (update)
                {
                    UpdatePermanentProps(session);
                }
            }
            else if (_permanentPropsMonitoring != null)
            {
                // Close all issues
                for (int i = 0, iMax = _permanentPropsMonitoring.Length; i < iMax; ++i)
                {
                    _permanentPropsMonitoring[i].ClearIssue();
                }
                _permanentPropsMonitoring = null;
            }
        }

        void UpdatePropNotReady(Session session)
        {
            // Get timeout
            var dateTimeout = DateTime.UtcNow.AddSeconds(-_propNotReadyTrackingTimeout);

            // Iterate props to check which are tracked
            for (int i = 0, iMax = _propsNotReadyMonitoring.Length; i < iMax; ++i)
            {
                var rigidbody = TrackingController.Instance.TryGetRigidBody(_propsNotReadyMonitoring[i].Name);
                _cache_trackedProps[i] = (rigidbody != null) && (rigidbody.LastUpdate >= dateTimeout);
            }

            // Iterate props to check there location
            for (int i = 0, iMax = _propsNotReadyMonitoring.Length; i < iMax; ++i)
            {
                var prop = _propsNotReadyMonitoring[i];
                bool badPosition = false, badOrientation = false;

                // Check position and orientation if prop is tracked
                if (_cache_trackedProps[i])
                {
                    var rigidbody = TrackingController.Instance.TryGetRigidBody(prop.Name);
                    var propConfig = prop.Config;

                    // Check position
                    badPosition = CheckPosition(rigidbody, propConfig);

                    // If bad position, check other locations for props in same group
                    if (badPosition && prop.HasGroup)
                    {
                        for (int j = 0; j < iMax; ++j)
                        {
                            // Check if our prop is placed at this location
                            var prop2 = _propsNotReadyMonitoring[j];
                            if ((i != j) && (propConfig.Group == prop2.Config.Group))
                            {
                                badPosition = CheckPosition(rigidbody, prop2.Config);
                                if (!badPosition)
                                {
                                    // Keep that config
                                    propConfig = prop2.Config;
                                    break;
                                }
                            }
                        }
                    }

                    // Check orientation
                    if (!badPosition)
                    {
                        badOrientation = CheckOrientation(rigidbody, propConfig);
                    }
                }

                // Update ticket
                prop.ReportIssue(!_cache_trackedProps[i], badPosition, badOrientation);
            }
        }

        private bool CheckPosition(RigidBodySubject rigidbody, TrackedProp propConfig)
        {
            bool badPosition = false;

            if ((propConfig.StartPosition != null) && (propConfig.StartPosition.Tolerance > 0))
            {
                var v = new Vector3(propConfig.StartPosition.Vector.X, propConfig.StartPosition.Vector.Y, propConfig.StartPosition.Vector.Z);
                float dist = Vector3.Distance(rigidbody.GlobalTranslation, v);
                badPosition = (dist > propConfig.StartPosition.Tolerance);
            }

            return badPosition;
        }

        private bool CheckOrientation(RigidBodySubject rigidbody, TrackedProp propConfig)
        {
            bool badOrientation = false;

            // Check orientation
            if ((propConfig.StartDirection1 != null) && (propConfig.StartDirection1.Tolerance > 0))
            {
                var v = new Vector3(propConfig.StartDirection1.Vector.X, propConfig.StartDirection1.Vector.Y, propConfig.StartDirection1.Vector.Z);
                float angle = Vector3.Angle(GetAxis(rigidbody.GlobalRotation, propConfig.StartDirection1.Axis), v);
                badOrientation = (angle > propConfig.StartDirection1.Tolerance);
                v = GetAxis(rigidbody.GlobalRotation, propConfig.StartDirection1.Axis);
            }

            // Check second orientation
            if ((!badOrientation) && (propConfig.StartDirection2 != null) && (propConfig.StartDirection2.Tolerance > 0))
            {
                var v = new Vector3(propConfig.StartDirection2.Vector.X, propConfig.StartDirection2.Vector.Y, propConfig.StartDirection2.Vector.Z);
                float angle = Vector3.Angle(GetAxis(rigidbody.GlobalRotation, propConfig.StartDirection2.Axis), v);
                badOrientation = (angle > propConfig.StartDirection2.Tolerance);
            }

            return badOrientation;
        }

        Vector3 GetAxis(Quaternion rotation, StartDirection.AxisName axis)
        {
            switch (axis)
            {
                case StartDirection.AxisName.X:
                    return rotation * Vector3.left;
                case StartDirection.AxisName.Y:
                    return rotation * Vector3.up;
                case StartDirection.AxisName.Z:
                    return rotation * Vector3.forward;
                default:
                    throw new InvalidOperationException("Unknown axis " + axis);
            }
        }

        void UpdatePermanentProps(Session session)
        {
            // Get timeout
            var dataTimeout = DateTime.UtcNow.AddSeconds(-_propTrackingTimeout);

            // Iterate props
            for (int i = 0, iMax = _permanentPropsMonitoring.Length; i < iMax; ++i)
            {
                var prop = _permanentPropsMonitoring[i];
                var rigidbody = TrackingController.Instance.TryGetRigidBody(prop.Name);

                if ((rigidbody == null) || (rigidbody.LastUpdate < dataTimeout))
                {
                    prop.ReportIssue(session.SharedId);
                }
                else
                {
                    prop.ClearIssue();
                }
            }
        }
    }

}
