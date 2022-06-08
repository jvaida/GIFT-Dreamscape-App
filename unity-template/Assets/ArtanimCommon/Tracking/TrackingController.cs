//#define DISABLE_SKELETON_SEND

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artanim.Location.Network;
using Artanim.Location.Data;
using Artanim.IKServer;

namespace Artanim.Tracking
{
	public class TrackingController : SingletonBehaviour<TrackingController>
	{
        private const string ARG_SIMULATION = "-simTracking";
        private const string ARG_IKPLAYER = "-ikPlayer";

        #region Events

        public delegate void OnRigidbodySubjectAddedHandler(RigidBodySubject rigidbody);
        public event OnRigidbodySubjectAddedHandler OnRigidbodySubjectAdded;

        #endregion

        [SerializeField]
		Text TextConnectionInfo = null;

		[SerializeField]
		GameObject DefaultRigidBody = null;

        [SerializeField]
        GameObject SkeletonRigidbody = null;

		[SerializeField]
		Transform RigidBodyRoot = null;

		[SerializeField]
		Toggle ToggleRigidbodiesList = null;

		[SerializeField]
		GameObject PanelRigidBodiesList = null;

		[SerializeField]
		Text TextRigidbodiesList = null;

		[SerializeField]
		bool CreateRigidBodyVisuals = true;

        [SerializeField]
        bool CreateSkeletonRigidBodyVisuals = false;

		ITrackingConnector _trackingConnector;
		string _connectedStr, _connectingStr;
        bool _connectOnStart;

		Dictionary<string, RigidBodySubject> _rigidbodies = new Dictionary<string, RigidBodySubject>();
		RigidBodySubject[] _cache_rigidbodies = new RigidBodySubject[0]; // Be sure to update this array every time a RigidBodySubject is added or removed from the above field

        int _updateCounter; // Count updates for display purposes
#if EXP_PROFILING
		long _updateTimestamp; // For profiling
#endif

        //TODO need to remove this!
		public ITrackingConnector TrackingConnector { get { return _trackingConnector; } }

        void Start()
		{
			Instance = this;

            if(NetworkInterface.Instance.ComponentType != ELocationComponentType.IKServer && DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone)
            {
                enabled = false;
                return;
            }

			//Create tracking connector
			if (NetworkInterface.Instance.ComponentType == ELocationComponentType.IKServer)
			{
                //Check command line arguments
                var simTrackingArg = System.Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.ToLowerInvariant().StartsWith(ARG_SIMULATION.ToLowerInvariant()));
                if (simTrackingArg != null)
                {
                    IkMode.CurrentMode = EIkMode.Simulation;
                }

                var ikPlayerArg = System.Environment.GetCommandLineArgs().FirstOrDefault(arg => arg.ToLowerInvariant().StartsWith(ARG_IKPLAYER.ToLowerInvariant()));
                if (ikPlayerArg != null)
                {
                    IkMode.CurrentMode = EIkMode.Player;
                }

                //Create connector
                switch (IkMode.CurrentMode)
                {
                    default:
                        //Start normal (ViconConnector)
                        _trackingConnector = new ViconConnector();
                    break;

                    case EIkMode.Simulation:
                        //Start simulation...
                        var mode = SimConnector.TrackingQualityMode.Disabled;
                        float rotateSpeed = 30;
                        bool seated = false;
                        if (simTrackingArg != null)
                        {
                            //Yes, then read optional arguments
                            foreach (var optArg in simTrackingArg.Split(':'))
                            {
                                //Is it an integer? => that's our rotate speed
                                int i;
                                if (int.TryParse(optArg, out i))
                                {
                                    rotateSpeed = i;
                                }
                                else
                                {
                                    string optArgLower = optArg.ToLowerInvariant();
                                    if (optArgLower == "seated")
                                    {
                                        seated = true;
                                    }
                                    else
                                    {
                                        //Or is it a tracking mode? => Get the corresponding enum name (case might differ)
                                        string enumName = System.Enum.GetNames(mode.GetType()).FirstOrDefault(e => e.ToLowerInvariant() == optArgLower);
                                        if (enumName != null)
                                        {
                                            //And convert to enum
                                            mode = (SimConnector.TrackingQualityMode)System.Enum.Parse(mode.GetType(), enumName);
                                        }
                                    }
                                }
                            }
                        }

                        //IK will run with a tracking simulation
                        _trackingConnector = new SimConnector(mode, rotateSpeed, seated);
                        break;

                    case EIkMode.Player:
                        //Start IK player connection...
                        if (ikPlayerArg != null)
                        {
                            var colIndex = ikPlayerArg.IndexOf(':');
                            if (colIndex > 0)
                                PlayerPrefs.SetString(IKPlaybackConnector.KEY_IK_PLAYER_IP, ikPlayerArg.Substring(colIndex + 1).Trim());
                        }

                        _trackingConnector = new IKPlaybackConnector();
                        break;
                }
            }
            else
			{
				//Everyone else listen to IK server DDS messages
				_trackingConnector = new IkConnector();
            }

            if (_connectOnStart)
            {
                _trackingConnector.Connect();
                _connectOnStart = false;
            }

            _connectedStr = string.Format("Connected to {0} (v:{1})",_trackingConnector.Endpoint, _trackingConnector.Version);
			_connectingStr = string.Format("Connecting to {0} (SDK: {1})", _trackingConnector.Endpoint, _trackingConnector.Version);

			if (ConfigService.VerboseSdkLog) Debug.LogFormat("Selected tracking connector of type: {0}", _trackingConnector.GetType().Name);
        }

        void OnDisable()
		{
			Disconnect();
        }

        void Update()
		{
#if EXP_PROFILING
			ExpProfiling.MarkVcStart();
			_updateTimestamp = Profiling.CPProfiler.Timestamp;
#endif

            // Create rigid body visual if needed
            if (CreateRigidBodyVisuals)
			{
				DoCreateRigidBodyVisuals();
			}

			//Reset rigidbodies update state
			for (int i = 0, iMax = RigidBodySubjects.Count; i < iMax; ++i)
			{
				RigidBodySubjects[i].ResetIsUpToDate();
			}

			//And update them with the latest data
			bool updated = _trackingConnector.UpdateRigidBodies();

            //Reconnect if necessary
            if ((!updated) && (!_trackingConnector.IsConnected))
			{
#if IK_SERVER
                _trackingConnector.Connect();
#endif
            }

#if EXP_PROFILING
			float updateRbDuration = Profiling.CPProfiler.DurationSince(_updateTimestamp);
			float messageLatency = (float)(System.DateTime.UtcNow - TrackingConnector.Stats.FrameCaptureTime).TotalSeconds - updateRbDuration;
			ExpProfiling.SetIkUpdateLatency(TrackingConnector.Stats.FrameCaptureLatency, TrackingConnector.Stats.FrameProcessLatency, messageLatency);
#endif

            //Update rigidbody list and display
            if (_updateCounter % 100 == 0)
			{
				if (TextConnectionInfo)
					TextConnectionInfo.text = _trackingConnector.IsConnected ? _connectedStr : _connectingStr;
				UpdateRigidbodiesList();
			}
			++_updateCounter;

			ProfilingMetrics.MarkVcEnd();
		}

        #region Public interface

        public bool IsConnected
		{
			get { return (_trackingConnector != null) && _trackingConnector.IsConnected; }
		}

		public ITrackingConnectorStats Stats
		{
			get { return (_trackingConnector == null) ? null : _trackingConnector.Stats; }
		}

		public IList<RigidBodySubject> RigidBodySubjects
		{
			get { return _cache_rigidbodies; }
		}

        public void Connect()
        {
            if (_trackingConnector == null)
            {
                _connectOnStart = true;
            }
            else
            {
                _trackingConnector.Connect();
            }
        }

        public void Disconnect()
        {
            if (_trackingConnector != null)
            {
                _trackingConnector.Disconnect();
            }
        }

        //-------------------------------------------------------------------------------------------------
        //--- EndeffectorClassification filters. These should only be used during classification
        //--- for a short time never after.
        //-------------------------------------------------------------------------------------------------
        public bool UsePDC;
        public IEnumerable<RigidBodySubject> UnClassifiedRigidbodies
        {
            get
            {
                if(!UsePDC)
                    return RigidBodySubjects.Where(r => r.IsSkeletonSubject && !r.IsSkeletonSubjectClassified && r.IsUpToDate);
                else
                    return RigidBodySubjects.Where(r => r.IsSkeletonSubject && !r.PDC_IsSkeletonSubjectClassified && r.IsUpToDate);
            }
        }

        public IEnumerable<RigidBodySubject> SkeletonRigidbodies
        {
            get { return RigidBodySubjects.Where(r => r.IsSkeletonSubject); }
        }

        //-------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------


        public RigidBodySubject GetOrCreateRigidBody(string rigidBodyName, bool applyRigidbodyConfig = false)
		{
			var subject = TryGetRigidBody(rigidBodyName);
			if(subject == null)
			{
                // Create rigidbody subject
				subject = new RigidBodySubject(rigidBodyName, applyRigidbodyConfig);
                subject.CreateVisual = (CreateRigidBodyVisuals && !subject.IsSkeletonSubject) || (CreateSkeletonRigidBodyVisuals && subject.IsSkeletonSubject);

				_rigidbodies.Add(rigidBodyName, subject);
				_cache_rigidbodies = _rigidbodies.Values.ToArray();

				// Trigger event
				if (OnRigidbodySubjectAdded != null) OnRigidbodySubjectAdded(subject);
			}
			return subject;
		}

		public RigidBodySubject TryGetRigidBody(string rigidBodyName)
		{
			if (!string.IsNullOrEmpty(rigidBodyName))
			{
				RigidBodySubject subject;
				_rigidbodies.TryGetValue(rigidBodyName, out subject);

				return subject;
			}
			else
			{
				Debug.LogWarning("Trying to get a rigid body with empty name!");
			}
			return null;
		}

        #endregion

        #region Internals

        private void DoCreateRigidBodyVisuals()
        {
            // Create the visual for all rigidbodies that don't have one yet
            for (int i = 0, iMax = _cache_rigidbodies.Length; i < iMax; ++i)
            {
                var subject = _cache_rigidbodies[i];
                if (subject.CreateVisual)
                {
                    CreateRigidBodyVisual(subject);
                    subject.CreateVisual = false;
                }
            }
        }

        private void CreateRigidBodyVisual(RigidBodySubject subject)
		{
            var rigidbodyTemplate = !subject.IsSkeletonSubject ? DefaultRigidBody : SkeletonRigidbody;

			//Create default visual
			if (RigidBodyRoot && rigidbodyTemplate)
			{
                var instance = UnityUtils.InstantiatePrefab<BaseTrackingRigidbody>(rigidbodyTemplate, RigidBodyRoot);
                if(instance)
                {
                    instance.transform.position = Vector3.zero;
                    instance.transform.rotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    instance.name = subject.Name;
                    instance.ResetRigidbodyName(subject.Name);

                    //_rigidbodyVisuals.Add(instance);
                }
			}
		}

		private void UpdateRigidbodiesList()
		{
			if(PanelRigidBodiesList && TextRigidbodiesList && ToggleRigidbodiesList && ToggleRigidbodiesList.isOn)
			{
                if (!PanelRigidBodiesList.activeSelf)
                {
                    LogRigidBodiesList();
                }

                PanelRigidBodiesList.SetActive(true);

				var rigidBodiesList = new StringBuilder();
				rigidBodiesList.AppendLine("Rigidbodies:");

				foreach(var rigidbody in _rigidbodies.Values)
				{
					rigidBodiesList.AppendLine(string.Format("{0}:{1}", rigidbody.Name, rigidbody.RigidbodyConfigPrefix));
				}

				TextRigidbodiesList.text = rigidBodiesList.ToString();
			}
			else if(PanelRigidBodiesList)
			{
				PanelRigidBodiesList.SetActive(false);
			}
		}

        private void LogRigidBodiesList()
        {
            var rigidBodiesList = new StringBuilder();
            rigidBodiesList.Append("Rigidbodies:");

            foreach (var rigidbody in _rigidbodies.Values)
            {
                rigidBodiesList.Append(string.Format("\n{0}:{1} - IsUpToDate={2} - GlobalTranslation: X=\"{3}\" Y=\"{4}\" Z=\"{5}\"", rigidbody.Name, rigidbody.RigidbodyConfigPrefix, rigidbody.IsUpToDate,
                    rigidbody.GlobalTranslation.x, rigidbody.GlobalTranslation.y, rigidbody.GlobalTranslation.z));
                rigidBodiesList.Append(string.Format(" - GlobalRotation Euler: X=\"{0}\" Y=\"{1}\" Z=\"{2}\"",
                    rigidbody.GlobalRotation.eulerAngles.x, rigidbody.GlobalRotation.eulerAngles.y, rigidbody.GlobalRotation.eulerAngles.z));
                Quaternion rot = rigidbody.GlobalRotation;
                Vector3 rotAxis = rot * Vector3.left;
                rigidBodiesList.Append(string.Format(" - GlobalRotation X Axis: X=\"{0}\" Y=\"{1}\" Z=\"{2}\"", rotAxis.x, rotAxis.y, rotAxis.z));
                rotAxis = rot * Vector3.up;
                rigidBodiesList.Append(string.Format(" - GlobalRotation Y Axis: X=\"{0}\" Y=\"{1}\" Z=\"{2}\"", rotAxis.x, rotAxis.y, rotAxis.z));
                rotAxis = rot * Vector3.forward;
                rigidBodiesList.Append(string.Format(" - GlobalRotation Z Axis: X=\"{0}\" Y=\"{1}\" Z=\"{2}\"", rotAxis.x, rotAxis.y, rotAxis.z));
            }

            Debug.Log(rigidBodiesList);
        }

        #endregion
    }
}
