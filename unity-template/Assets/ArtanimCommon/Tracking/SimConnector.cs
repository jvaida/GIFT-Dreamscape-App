using Artanim.Location.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Tracking
{
    public class SimConnector : ITrackingConnector
    {
        // Connection statistics
        class SimStats : ITrackingConnectorStats
        {
            public uint FrameNumber { get; set; }
            public DateTime FrameCaptureTime { get; set; }
            public long FrameProcessTimestamp { get; set; }
            public float FrameCaptureLatency { get; set; }
            public float FrameProcessLatency { get; set; }
        }

        public enum TrackingQualityMode { Disabled, Good, Poor, Awful };

        public bool IsConnected { get; private set; }

        public string Endpoint { get; private set; }

        public string Version { get; private set; }

        public ITrackingConnectorStats Stats { get { return _stats; } }

        public bool AnimationPaused { get; set; }

        public bool Seated { get; set; }

        public bool CircularLayout { get; set; }

        public float RotationAngle { get; set; }

        public float RotateSpeed { get; set; }

        public bool UseRotationOffset { get; set; }

        public int SkeletonCount
        {
            get { return _skeletonsCount; }
            set
            {
                if (_skeletonNames != null)
                    _skeletonsCount = Mathf.Max(0, Mathf.Min(_skeletonNames.Length, value));
                WheelchairCount = _wheelchairCount; // Update so not greater than skeleton count
            }
        }

        public int WheelchairCount
        {
            get { return _wheelchairCount; }
            set { _wheelchairCount = Mathf.Max(0, Mathf.Min(_skeletonsCount, value)); }
        }

        public int TargetFPS
        {
            get { return _targetFPS; }
            set { _targetFPS = Mathf.Min(180, Mathf.Max(10, value)); }
        }

        readonly SimStats _stats = new SimStats();
        string[] _skeletonNames;
        int _skeletonsCount = 6;
        int _wheelchairCount = 0;
        int _targetFPS = 60;
        float _lastUpdateTime;

        TrackingQualityMetrics _trackingQM;
        TrackingQualityMode _mode;

        readonly string[] _hardwareNames = new string[Enum.GetValues(typeof(ESkeletonSubject)).Length];

        public SimConnector(TrackingQualityMode mode = TrackingQualityMode.Disabled, float rotateSpeed = 30, bool seated = false)
        {
            Debug.LogFormat("SimConnector: Using simulated tracking with rotate speed={0} and tracking quality mode={1}", rotateSpeed, mode);

            _mode = mode;
            if (_mode != TrackingQualityMode.Disabled)
            {
                _trackingQM = new TrackingQualityMetrics();
            }
            RotateSpeed = rotateSpeed;
            UseRotationOffset = true;
            Endpoint = "Sim";
            Version = "2";
            Seated = seated;
            if (Seated)
            {
                CircularLayout = true;
                UseRotationOffset = false;
            }

            // Read hardware config
            var hardwareConfig = ConfigService.Instance.HardwareConfig;
            if ((hardwareConfig != null) && (hardwareConfig.Vicon != null) && (hardwareConfig.Vicon.Types != null))
            {
                foreach (var rbType in hardwareConfig.Vicon.Types)
                {
                    bool hasVersion = (rbType.Versions != null) && (rbType.Versions.Count > 0);
                    if (hasVersion)
                    {
                        string name = rbType.Versions[0].Name;
                        switch (rbType.Name)
                        {
                            case Location.HardwareConfig.ESmartObjectRoleType.HMD:
                                _hardwareNames[(int)ESkeletonSubject.Head] = name;
                                break;
                            case Location.HardwareConfig.ESmartObjectRoleType.Backpack:
                                _hardwareNames[(int)ESkeletonSubject.Pelvis] = name;
                                break;
                            case Location.HardwareConfig.ESmartObjectRoleType.Hand:
                                _hardwareNames[(int)ESkeletonSubject.HandLeft] = name;
                                _hardwareNames[(int)ESkeletonSubject.HandRight] = name;
                                break;
                            case Location.HardwareConfig.ESmartObjectRoleType.Foot:
                                _hardwareNames[(int)ESkeletonSubject.FootLeft] = name;
                                _hardwareNames[(int)ESkeletonSubject.FootRight] = name;
                                break;
                            default:
                                break;
                        }
                        
                    }
                }

                // Make sure there aren't any null in the array
                for (int i = 0; i < _hardwareNames.Length; ++i)
                {
                    if (_hardwareNames[i] == null)
                    {
                        _hardwareNames[i] = string.Empty;
                    }
                }
            }
        }

        public void Connect()
        {
            IsConnected = true;

            _skeletonNames = ConfigService.Instance.Config.Location.Hostess.HostBackpackMappings.Select(m => m.SkeletonName).Distinct().ToArray();
            SkeletonCount = _skeletonsCount; // Refresh value
        }

        public void Disconnect()
        {
            IsConnected = false;

            if (_trackingQM != null)
            {
                _trackingQM.Dispose();
                _trackingQM = null;
            }
        }

        public bool UpdateRigidBodies()
        {
            if (IsConnected)
            {
                // Control FPS
                while (_targetFPS < (1f / (Time.realtimeSinceStartup - _lastUpdateTime + 1e-9f)))
                {
                    System.Threading.Thread.Sleep(0); // Wait just a tiny bit
                }
                _lastUpdateTime = Time.realtimeSinceStartup;

                // Update stats
                _stats.FrameCaptureTime = DateTime.UtcNow;
                ++_stats.FrameNumber;

                // Toggle animation when space bar is hit
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    AnimationPaused = !AnimationPaused;
                }

                // Update rotate angle
                if (!AnimationPaused)
                {
                    RotationAngle += Time.deltaTime * RotateSpeed;
                }
                // Always enforce angle bounds as it can be modified externally
                RotationAngle = Mathf.Repeat(RotationAngle, 360);

                // Update rigidbodies
                int numLines = Mathf.FloorToInt(Mathf.Sqrt(_skeletonsCount));
                int numCols = Mathf.CeilToInt((float)_skeletonsCount / numLines);
                for (int i = 0; i < _skeletonsCount; ++i)
                {
                    float x, z, rotationOffset;
                    if (CircularLayout)
                    {
                        float radius = 2 * (1 + i / 8);
                        float angle = i * Mathf.PI / 4;
                        x = radius * Mathf.Cos(angle);
                        z = radius * Mathf.Sin(angle);
                        rotationOffset = 90 - angle * Mathf.Rad2Deg;
                    }
                    else
                    {
                        x = 1.4f * (i % numCols - 0.5f * (numCols - 1));
                        z = 1.4f * (i / numCols - 0.5f * (numLines - 1));
                        rotationOffset = 27 * i; // Rotation offset is 27 and not 30 so it doesn't loop back to the exact same angle after 12 skeletons
                    }
                    UpdateSkeletonRigidBodies(_skeletonNames[i], new Vector3(x, 0, z), RotationAngle, UseRotationOffset ? rotationOffset : 0, _wheelchairCount > i);
                }
            }

            return IsConnected;
        }

        void UpdateSkeletonRigidBodies(string name, Vector3 startPos, float angle, float rotationOffset, bool wheelChair)
        {
#if IK_SERVER
            var sk = Location.SharedData.SharedDataUtils.FindSkeletonWithRigidBody(string.Format("{0}{1}_{2}",
                SkeletonConstants.GetSkeletonSubjectPrefixes(ESkeletonSubject.Pelvis)[0], _hardwareNames[(int)ESkeletonSubject.Pelvis], name));
            bool animate = (!AnimationPaused) && (sk != null) && (sk.Status == ESkeletonStatus.Calibrated);

            // Rotate the skeletons around their vertical axis
            var globalRot = Quaternion.Euler(0, angle + rotationOffset, 0);
            var pelvisRot = Quaternion.Euler(0, animate ? AnimateAngle(0, 16, 3, rotationOffset) : 0, 0);
            var armsRot = Quaternion.Euler(0, animate ? AnimateAngle(0, 32, 3, rotationOffset) : 0, 0);
            var footOffset = animate ? AnimateValue(0, 0.2f, 2): 0;

            var skeletonSubjets = (ESkeletonSubject[])(Enum.GetValues(typeof(ESkeletonSubject)));
            foreach (var skSubject in skeletonSubjets)
            {
                // Construct rigidbody name
                var prefixes = SkeletonConstants.GetSkeletonSubjectPrefixes(skSubject);
                string prefix = prefixes[prefixes.Length - 1]; // Assume at least one prefix
                string rigidbodyName = string.Format("{0}{1}_{2}", prefix, _hardwareNames[(int)skSubject], name);

                // Quality body part
                bool isHead = skSubject == ESkeletonSubject.Head;
                bool isPelvis = skSubject == ESkeletonSubject.Pelvis;
                bool isHand = (skSubject == ESkeletonSubject.HandLeft) || (skSubject == ESkeletonSubject.HandRight);
                bool isFoot = (skSubject == ESkeletonSubject.FootLeft) || (skSubject == ESkeletonSubject.FootRight);
                bool isLeft = (skSubject == ESkeletonSubject.HandLeft) || (skSubject == ESkeletonSubject.FootLeft);
                bool isRight = (skSubject == ESkeletonSubject.HandRight) || (skSubject == ESkeletonSubject.FootRight);

                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;
                if (Seated || wheelChair)
                {
                    // Cheat for the handle as it's not part of the skeleton
                    bool isHandle = Seated && isFoot && isLeft;
                    if (isHandle) rigidbodyName = string.Format(DreamscapeSEChairConfig.HANDLE_PATTERN, string.Format("SE01_{0}" , name));
                    if (isPelvis) rigidbodyName = string.Format("{0}SE01_{1}", prefix, name);

                    if (isFoot && !isHandle) continue; // No feet for seated players

                    // Set position
                    if (isHand) pos = (Seated ? 1.02f : 0.7f) * Vector3.up + (Seated ? 0.42f : 0.15f) * Vector3.back
                        + (isLeft ? -1 : 1) * 0.2f * Vector3.left;
                    else if (isPelvis) pos = (Seated ? 0.53f : 1f) * Vector3.up - (Seated ? -0.53f : 0.1f) * Vector3.back;
                    else if (isHandle) pos = .95f * Vector3.up + 0.51f * Vector3.back;
                    else pos = (Seated ? 1.45f : 1.3f) * Vector3.up + 0.1f * Vector3.back; // Head                 

                    // Set orientation
                    if (isHead) rot = Quaternion.Euler(0, 180, 0);
                    else if(Seated && (isHandle || isPelvis)) rot = Quaternion.Euler(0, 180, 0);
                    else if(!Seated && isHand) rot = Quaternion.Euler(-20, 0, 30 * (isLeft ? -1 : 1));
                }
                else
                {
                    // Set position
                    float handOff = 0.3f, footOff = 0.12f;
                    if (isHand) pos = 0.9f * Vector3.up + 0.1f * Vector3.back;
                    if (isFoot) pos = 0.08f * Vector3.up;
                    if (isLeft) pos -= (isHand ? handOff : footOff) * Vector3.left;
                    else if (isRight) pos -= (isHand ? handOff : footOff) * Vector3.right;
                    else if (isPelvis) pos = 0.95f * Vector3.up - 0.2f * Vector3.back;
                    else pos = 1.7f * Vector3.up + 0.1f * Vector3.back; // Head

                    // Set orientation
                    if (isHead) rot = Quaternion.Euler(0, 180, 0);
                    else if(isHand) rot = Quaternion.Euler(-80, 0, 50 * (isLeft ? -1 : 1));
                    else if(isFoot) rot = Quaternion.Euler(-20, 0, 0);

                    // Animate pelvis, hands and foot
                    if (isPelvis) rot = pelvisRot;
                    else if (isHand) pos = armsRot * pos;
                    else if (isFoot && isLeft && (footOffset > 0)) pos.y += footOffset;
                    else if (isFoot && isRight && (footOffset < 0)) pos.y -= footOffset;
                }

                // Move skeleton with mouse
                var skeleton = IKServer.IKServer.Instance.GetSkeleton(name);
                if (skeleton != null)
                {
                    var simSkMouseCtrl = skeleton.GetComponentInChildren<SimSkeletonMouseControl>();
                    if (simSkMouseCtrl != null && simSkMouseCtrl.SkeletonPosition.HasValue)
                    {
                        startPos = simSkMouseCtrl.SkeletonPosition.Value;
                    }
                }

                // Get and update our rigidbody
                var subject = TrackingController.Instance.GetOrCreateRigidBody(rigidbodyName);
                subject.UpdateTransform(_stats.FrameNumber, globalRot * pos + startPos, globalRot * rot, true);

                // Tracking quality
                if (_trackingQM != null)
                {
                    float quality = 0.1f;
                    if (_mode == TrackingQualityMode.Poor)
                    {
                        //quality = UnityEngine.Random.value > 0.8 ? 10 : 0.1f;
                        quality = 1 + 1 * Mathf.Sin(Time.realtimeSinceStartup * 2);
                    }
                    else if (_mode == TrackingQualityMode.Awful)
                    {
                        quality = 3 + 3 * Mathf.Sin(Time.realtimeSinceStartup * 2 * Mathf.PI);
                    }
                    _trackingQM.CheckQuality(rigidbodyName, quality);
                }
            }
#endif
        }

        private float AnimateValue(float center, float span, float period, float delay = 0)
        {
            // ping pong animation
            float time = Time.unscaledTime % period + delay;
            return center + span * Mathf.Sin(time / period * 2 * Mathf.PI);
        }

        private float AnimateAngle(float center, float span, float period, float angleOffset = 0)
        {
            return AnimateValue(center, span, period, angleOffset / span * period);
        }
    }
}