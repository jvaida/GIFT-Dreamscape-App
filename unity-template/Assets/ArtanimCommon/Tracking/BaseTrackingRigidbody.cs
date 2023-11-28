using Artanim.Location.Monitoring;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Artanim.Tracking
{
    public abstract class BaseTrackingRigidbody : MonoBehaviour
    {
        [FormerlySerializedAs("_RigidbodyName")]
        [Tooltip("Name of the tracked rigidbody in the tracking system.")]
        public string RigidbodyName;

        [Tooltip("Defines if the position of the tracked rigidbody should be applied to the transform.")]
        public bool UpdatePosition = true;
        [Tooltip("Defines if the rotation of the tracked rigidbody should be applied to the transform.")]
        public bool UpdateRotation = true;

        [Header("Visuals (optional)")]
        [Tooltip("Defines if the TrackingRigidbody should hide the given GameObject’s in the Visuals property if the rigidbody is not tracked.")]
        public bool HideIfNotTracked = false;
        [Tooltip("Time in seconds before the rigidbody visuals are hidden when not tracked anymore.")]
        public float HideTimeoutSecs = 5f;
        [Tooltip("List of GameObject which are enabled/disabled if the rigidbody is not tracked.")]
        public GameObject[] Visuals;

        private TrackingController _TrackingController;
        private bool TrackingControllerSearched;
        protected TrackingController TrackingController
        {
            get
            {
                if (_TrackingController == null && !TrackingControllerSearched)
                {
                    _TrackingController = TrackingController.Instance;
                    TrackingControllerSearched = true;
                }
                return _TrackingController;
            }
        }

        public uint LastUpdateFrameNumber { get { return RigidbodySubject != null ? RigidbodySubject.LastUpdateFrameNumber : 0; } }

        public bool IsTracked { get { return RigidbodySubject != null ? RigidbodySubject.IsTracked : false; } }

        public bool IsTransformUpToDate { get { return RigidbodySubject != null ? RigidbodySubject.IsUpToDate : false; } }

        public DateTime LastUpdateTime { get { return RigidbodySubject != null ? RigidbodySubject.LastUpdate : DateTime.MinValue; } }

        public Vector3 RigidbodyPosition { get; private set; }

        public Quaternion RigidbodyRotation { get; private set; }

        public RigidBodySubject RigidbodySubject { get; protected set; }

        #region Public Interface

        public void ResetRigidbodyName(string name)
        {
            StopMonitoringTimeout();
            RigidbodySubject = null;
            RigidbodyName = name;

            //Init new name
            if (!string.IsNullOrEmpty(name))
                RigidbodySubject = TrackingController.TryGetRigidBody(name);

            Update();
        }

        protected bool IsHidden { get; private set; }
        public virtual void ShowVisuals(bool show)
        {
            if(IsHidden != !show)
            {
                IsHidden = !show;
                if (Visuals != null)
                {
                    for (var i = 0; i < Visuals.Length; ++i)
                    {
                        var visual = Visuals[i];
                        if (visual)
                        {
                            visual.SetActive(show);
                        }
                    }
                }
            }
        }

        #endregion

        protected virtual void Update()
        {
            ProfilingMetrics.MarkRbUpdateStart();

            if (TrackingController && !string.IsNullOrEmpty(RigidbodyName))
            {
                // Get subject if not already found
                if (RigidbodySubject == null)
                    RigidbodySubject = TrackingController.TryGetRigidBody(RigidbodyName);

                // And update the transform is there is a change
                if (RigidbodySubject != null)
                {
                    if (RigidbodySubject.IsUpToDate)
                    {
                        RigidbodyPosition = RigidbodySubject.GlobalTranslation;
                        RigidbodyRotation = RigidbodySubject.GlobalRotation;

                        // Update transform
                        if (UpdatePosition)
                            transform.localPosition = RigidbodyPosition;

                        //Update rotation
                        if (UpdateRotation)
                            transform.localRotation = RigidbodyRotation;
                    }

                    //Track timeout
                    if (TimeoutTicketData != null)
                        MonitorTracking();

                    //Hide visuals if needed
                    if (HideIfNotTracked && DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone)
                        ShowVisuals(RigidbodySubject.LastUpdate.AddSeconds(HideTimeoutSecs) > DateTime.UtcNow);
                }
                else
                {
                    //Hide if needed
                    if (HideIfNotTracked && DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone)
                        ShowVisuals(false);
                }
            }

            ProfilingMetrics.MarkRbUpdateEnd();
        }

        private void OnDestroy()
        {
            StopMonitoringTimeout();
        }


        #region Rigidbody update monitoring

        private float TimeoutSeconds = 3f;
        private bool LastIsUpToDate = false;
        private bool HasNotified;
        private float TimeoutTime;
        private Location.Monitoring.OpTicketsTypes.IOpTicketData TimeoutTicketData;
        private OperationalTickets.IOpTicket Ticket;

        public void StartMonitoringTimeout(Location.Monitoring.OpTicketsTypes.IOpTicketData timeoutTicketData, float timeout = 0)
        {
            if (timeoutTicketData == null) throw new System.ArgumentNullException("timeoutTicketData");

            //Init monitoring
            TimeoutTicketData = timeoutTicketData;
            TimeoutSeconds = timeout;
            ResetTrackingTimeout();
            LastIsUpToDate = RigidbodySubject != null ? RigidbodySubject.IsUpToDate : false;
            TimeoutTime = Time.unscaledTime + TimeoutSeconds;
        }

        public void StopMonitoringTimeout()
        {
            TimeoutTicketData = null;
            ResetTrackingTimeout();
        }

        private void MonitorTracking()
        {
            if (RigidbodySubject != null && LastIsUpToDate != RigidbodySubject.IsUpToDate)
            {
                //State change
                if (RigidbodySubject.IsUpToDate)
                {
                    //RB is updated again
                    if (HasNotified)
                    {
                        var message = string.Format("Rigidbody is now back to normal: {0}", RigidbodyName);
                        Debug.Log(message);

                        //Monitoring
                        if (Ticket != null)
                        {
                            Ticket.Close(message);
                            Ticket = null;
                        }
                    }

                    ResetTrackingTimeout();
                }
                else
                {
                    //RB not updated anymore
                    TimeoutTime = Time.unscaledTime + TimeoutSeconds;
                }
                LastIsUpToDate = RigidbodySubject.IsUpToDate;
            }
            else
            {
                if (!LastIsUpToDate && Time.unscaledTime > TimeoutTime && !HasNotified)
                {
                    //RB is timed out
                    var message = string.Format("Rigidbody timed out after {0} seconds. (Rigidbody={1})", TimeoutSeconds, RigidbodyName);
                    Debug.LogWarning(message);

                    //Monitoring
                    if (Ticket != null) Debug.LogError("A ticket is already open for this rigidbody");

                    Ticket = OperationalTickets.Instance.OpenTicket(TimeoutTicketData);

                    HasNotified = true;
                }
            }
        }

        private void ResetTrackingTimeout()
        {
            TimeoutTime = 0f;
            HasNotified = false;

            //Close ticket if open
            if (Ticket != null)
            {
                Ticket.Close();
                Ticket = null;
            }

            LastIsUpToDate = false;
        }

        #endregion


    }
}