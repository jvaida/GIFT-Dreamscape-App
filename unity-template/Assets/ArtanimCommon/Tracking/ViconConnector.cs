using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using Artanim.Location.Helpers;
using Artanim.Monitoring;
using Artanim.Monitoring.Utils;

using Result = Artanim.TrackerSdk.Result;
using StreamMode = Artanim.TrackerSdk.StreamMode;
using Artanim.Location.Monitoring;
using Artanim.Location.Monitoring.OpTicketsTypes.Tracking;

namespace Artanim.Tracking
{
	public class ViconConnector : ITrackingConnector
	{
		// Connection statistics
		class ViconStats : ITrackingConnectorStats
		{
			public uint FrameNumber { get; set; }
			public DateTime FrameCaptureTime { get; set; }
			public long FrameProcessTimestamp { get; set; }
			public float FrameCaptureLatency { get; set; }
			public float FrameProcessLatency { get; set; }
			public void Reset()
			{
				FrameNumber = 0;
				FrameCaptureTime = new DateTime();
				FrameProcessTimestamp = 0;
				FrameCaptureLatency = FrameProcessLatency = 0;
			}
		}

		private struct TrackerRigidbodyData
		{
			public string Name;
			public double[] Position;
			public double[] Rotation;
			public double Quality;
		}

		private delegate Result ConnectWorkerDelegate(string hostAndPort);

		private volatile bool _isConnected;
		private object _connectionLock = new object();
		private volatile IAsyncResult _connectAsync;

		private readonly ViconStats _stats = new ViconStats();
		private readonly List<RigidBodySubject> RigidbodiesListCache = new List<RigidBodySubject>();
		private readonly List<RigidBodySubject> RigidbodiesListCache2 = new List<RigidBodySubject>();
		private List<TrackerRigidbodyData> _rigidbodyData = new List<TrackerRigidbodyData>(100);

		private MetricsChannel<IKProfiling.Timings> _ikTimingsMetrics;
		private TrackingQualityMetrics _trackingQM;
		private GlobalTrackingQualityMetrics _globalTrackingQM;

		public string Endpoint
		{
			get; private set;
		}

		public string Version
		{
			get; private set;
		}

		public bool IsConnected
		{
			get { return _isConnected; }
		}

		public ITrackingConnectorStats Stats
		{
			get { return _stats; }
		}

		public void Connect()
		{
			try
			{
				// A connection worker might already be running on a different thread
				// We're using a lock to prevent threading issues if the worker terminates at the same time a call to Connect() is made
				// However we are not thread sage generally speaking (which is OK since all Unity logic runs on the main thread)
				lock(_connectionLock)
				{
					if (_isConnected || (_connectAsync != null))
					{
						return;
					}

					// Get endpoint
					Endpoint = HelperMethods.ViconEndpoint;
					if (string.IsNullOrEmpty(Endpoint))
					{
						Debug.LogError("No Vicon endpoint specified in config file");
						return;
					}

					TrackerSdk.Instance.Init();
					Version = TrackerSdk.Instance.Version.ToString();
					_stats.Reset();
					_trackingQM = new TrackingQualityMetrics();
					_globalTrackingQM = new GlobalTrackingQualityMetrics();

					Debug.LogFormat("Initiating connection to Vicon host: {0} using Vicon SDK {1}", Endpoint, TrackerSdk.Instance.Version);

					// Start connection
					var connectWorker = new ConnectWorkerDelegate(TrackerSdk.Instance.Connect);
					var async = AsyncOperationManager.CreateOperation(null);
					var completedCallback = new AsyncCallback(ConnectAsyncCompletedCallback);
					_connectAsync = connectWorker.BeginInvoke(Endpoint, completedCallback, async);
				}
			}
			catch(Exception ex)
			{
				Debug.LogErrorFormat("Exception when trying to connect to Vicon Host: {0}\n{1}\n{2}", Endpoint, ex.Message, ex.StackTrace);
				Disconnect();
			}
		}

		public void Disconnect()
		{
			// First wait the connection async operation to end (if it was still running)
			var async = _connectAsync;
			if(async != null)
			{
				Debug.LogWarning("Waiting on Vicon..."); // This message might show up too late in Unity console when stopping the game

				// Wait on the connection thread to complete to avoid problems like crashes on DLL unloading
				float progress = 0;
				float inc = 0.05f;
				while(!async.IsCompleted)
				{
					progress += inc;
#if UNITY_EDITOR
					UnityEditor.EditorUtility.DisplayProgressBar("Vicon Connect", "Waiting on Vicon connect to complete before quitting", progress);
#endif
					System.Threading.Thread.Sleep(1000);
				}
#if UNITY_EDITOR
				UnityEditor.EditorUtility.ClearProgressBar();
#endif
			}

			if (_ikTimingsMetrics != null)
			{
				_ikTimingsMetrics.Dispose();
				_ikTimingsMetrics = null;
			}

			if (_trackingQM != null)
			{
				_trackingQM.Dispose();
				_trackingQM = null;
			}

			if(_globalTrackingQM != null)
			{
				_globalTrackingQM.Dispose();
				_globalTrackingQM = null;
			}

			if (_isConnected)
			{
				Debug.LogFormat("Disconnecting from Vicon host: {0}", Endpoint);

				TrackerSdk.Instance.Disconnect();

				_isConnected = false;
			}
		}

		public bool UpdateRigidBodies()
		{
			bool updated = false;

			if(_isConnected)
			{
#if IK_PROFILING
				IKProfiling.MarkWaitStart();
#endif
				//Retrieve Vicon frame
				var result = TrackerSdk.Instance.GetFrame();
				if(result == Result.Success)
				{
					_stats.FrameProcessTimestamp = Profiling.CPProfiler.Timestamp;

#if IK_PROFILING
					IKProfiling.MarkWaitEnd();
					if (_ikTimingsMetrics == null)
					{
						_ikTimingsMetrics = MetricsManager.Instance.GetChannelInstance<IKProfiling.Timings>(MetricsAction.Create, "IK Profiling");
					}
					var frame = IKProfiling.Frame;;
					unsafe
					{
						_ikTimingsMetrics.SendRawData(new IntPtr(&frame));
					}
#endif

					//Get latency total as early as possible so we can compute the time at which the frame was captured with as little error as possible
					var latencyResult = TrackerSdk.Instance.GetLatencyTotal();
					if(latencyResult.Result != Result.Success)
						Debug.LogWarningFormat("Failed to get Vicon's latency total: {0}", latencyResult.Result);

					// Compute frame capture time
					var captureTime = DateTime.UtcNow.AddSeconds(-latencyResult.Total);
					_stats.FrameCaptureTime = captureTime;
					_stats.FrameCaptureLatency = (float)latencyResult.Total;

#if IK_PROFILING
					IKProfiling.StartFrame((float)latencyResult.Total);
					IKProfiling.MarkRbStart();
#endif

					// Get current frame number
					uint frameNumber = 0;
					var frameNumberResult = TrackerSdk.Instance.GetFrameNumber();
					if(frameNumberResult.Result == Result.Success)
					{
						frameNumber = frameNumberResult.FrameNumber;

						// Reset rigid bodies frame number when looping
						if (frameNumber < _stats.FrameNumber)
						{
							var rbSubjects = TrackingController.Instance.RigidBodySubjects;
							for (int i = 0, iMax = rbSubjects.Count; i < iMax; ++i)
							{
									rbSubjects[i].ResetFrameNumber();
							}
						}
					}

					if((_stats.FrameNumber + 1) < frameNumber)
					{
						//Debug.LogWarning("Skipped frames between " + _stats.FrameNumber + " and " + frameNumber);
					}
					_stats.FrameNumber = frameNumber;

					// Store rigidbodies to send them to other components (only for the IK server in DDS_IK mode)
					var rigidBodies = RigidbodiesListCache;
					if(rigidBodies != null)
					{
						rigidBodies.Clear();
					}

					// Keep track of all rigid bodies updated this frame
					var foundRigidBodies = RigidbodiesListCache2;
					foundRigidBodies.Clear();

					// Reset list that stores the data read from Tracker
					_rigidbodyData.Clear();

					// Get all rigidbodies from Tracker
					for (uint index = 0, iMax = TrackerSdk.Instance.GetSubjectCount().SubjectCount; index < iMax; index++)
					{
						var rigidbodyName = TrackerSdk.Instance.GetSubjectName(index).SubjectName;
						var rootName = TrackerSdk.Instance.GetSubjectRootSegmentName(rigidbodyName).SegmentName;
						var posResponse = TrackerSdk.Instance.GetSegmentGlobalTranslation(rigidbodyName, rootName);
						var rotResponse = TrackerSdk.Instance.GetSegmentGlobalRotationQuaternion(rigidbodyName, rootName);
						var qualityResponse = TrackerSdk.Instance.GetObjectQuality(rigidbodyName);

						var position = posResponse.Translation;
						var rotation = rotResponse.Rotation;

						if ((posResponse.Result == Result.Success) && (position != null) && (position.Length >= 3)
							&& (rotResponse.Result == Result.Success) && (rotation != null) && (rotation.Length >= 3))
						{
							_rigidbodyData.Add(new TrackerRigidbodyData()
							{
								Name = rigidbodyName,
								Position = position,
								Rotation = rotation,
								Quality = qualityResponse.Quality,
							});
						}
					}
#if IK_PROFILING
					IKProfiling.MarkRbRead();
#endif
					// Process retrieved rigidbodies data
					var qualitySum = 0f;
					var qualityCount = 0;
					for (int i = 0, iMax = _rigidbodyData.Count; i < iMax; ++i)
					{
						var data = _rigidbodyData[i];
						string rigidbodyName = data.Name;

						double[] position = data.Position, rotation = data.Rotation;
						Vector3 pos;
						Quaternion rot;
						float quality = (float)data.Quality;

						// Check quality
						if (_trackingQM.CheckQuality(rigidbodyName, quality))
						{
							// Convert to Vector3
							pos = new Vector3((float)position[0], (float)position[1], (float)position[2]);
							rot = new Quaternion((float)rotation[0], (float)rotation[1], (float)rotation[2], (float)rotation[3]);
						}
						else
						{
							//Debug.LogWarningFormat("Skipping object transform update {0} because of low quality: {1}", rigidBodyName, quality);
							pos = Vector3.zero;
							rot = Quaternion.identity;
						}

						// Get our rigidbody
						var subject = TrackingController.Instance.GetOrCreateRigidBody(rigidbodyName, applyRigidbodyConfig: true);

						//Update rigidbody with Unity specific conversion
						subject.UpdateTransform(
							frameNumber,
							Referential.TrackingPositionToUnity(pos),
							Referential.TrackingRotationToUnity(rot.x, rot.y, rot.z, rot.w),
							true,
							sourcePosition: pos,
							sourceRotation: rot,
                            trackingQuality: quality);

						foundRigidBodies.Add(subject);
						updated = true;

						//Store rigid body to send
						if ((rigidBodies != null) && (!subject.IsSkeletonSubject || subject.Subject == Location.Data.ESkeletonSubject.Head))
						{
							rigidBodies.Add(subject);
						}

						//Global tracking quality stats
						if(subject.IsSkeletonSubject)
						{
							qualitySum += quality;
							++qualityCount;
						}

					}

					//Send global quality metric
					if (_rigidbodyData.Count > 0)
						_globalTrackingQM.AddFrameQualityAverage(qualitySum / qualityCount);

					// Update objects that didn't received any position
					{
						var rbSubjects = TrackingController.Instance.RigidBodySubjects; // We want this variable to stay in this scope
						if (foundRigidBodies.Count < rbSubjects.Count)
						{
							for (int i = 0, iMax = rbSubjects.Count; i < iMax; ++i)
							{
								var subject = rbSubjects[i];
								if (!foundRigidBodies.Contains(subject))
								{
									subject.UpdateTransform(frameNumber, Vector3.zero, Quaternion.identity, false, Vector3.zero, Quaternion.identity);
									updated = true;
								}
							}
						}

					}

					_stats.FrameProcessLatency = Profiling.CPProfiler.DurationSince(_stats.FrameProcessTimestamp);

#if IK_PROFILING
					IKProfiling.MarkRbWrite();
#endif
				}
				else
				{
					if(result == Result.NotConnected)
					{
						// We lost the connection to Tracker
						Debug.LogWarning("Got disconnected from Vicon: " + result);
						Disconnect();
					}
					else if(result != Result.NoFrame)
					{
						Debug.LogWarningFormat("Failed to retrieve Vicon frame: Result={0}", result.ToString());
					}
				}
			}

			return updated;
		}

		#region Internals

		~ViconConnector()
		{
			// In case Disconnect hasn't been called
			if (IsConnected)
			{
				Debug.LogWarning("ViconConnector wasn't disconnected before being destroyed");
				Disconnect();
			}

			//Close open ticket if any
			CloseDisconnectedTicket();
		}

		private void ConnectAsyncCompletedCallback(IAsyncResult ar)
		{
			System.ComponentModel.AsyncOperation async;
			Result result = Result.ClientConnectionFailed;

			try
			{
				// get the original worker delegate and the AsyncOperation instance
				var worker = (ConnectWorkerDelegate)((AsyncResult)ar).AsyncDelegate;
				async = (System.ComponentModel.AsyncOperation)ar.AsyncState;

				// finish the asynchronous operation
				result = worker.EndInvoke(ar);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			bool success = result == Result.Success;
			if (success)
			{
				// We want to wait for the next available frame
				TrackerSdk.Instance.SetStreamMode(StreamMode.ServerPush);

				// We want the kinematic segment data
				TrackerSdk.Instance.EnableSegmentData();

				//Set axis mapping
				//-> Commented: Should be based on Unity tracker plugin. But we're using a 90 degree flipped scene (legacy reasons)
				//TrackerSdk.Instance.SetAxisMapping(TrackerSdk.Direction.Forward, TrackerSdk.Direction.Up, TrackerSdk.Direction.Right);
				TrackerSdk.Instance.SetAxisMapping(TrackerSdk.Direction.Left, TrackerSdk.Direction.Up, TrackerSdk.Direction.Forward);

				//Close ticket if it was opened before
				CloseDisconnectedTicket();

				Debug.LogFormat("Successfully connected to Vicon host: Host={0}", Endpoint);
			}
			else
			{
				//Open ticket if not already opened
				OpenDisconnectedTicket();

				Debug.LogFormat("Failed to connect to Vicon host: Host={0}", Endpoint);
			}

			// clear the running task flag
			lock (_connectionLock)
			{
				_connectAsync = null;
				_isConnected = success;
			}
		}

		#endregion

		#region Operational Ticket

		private OperationalTickets.IOpTicket DisconnectedTicket;


		private void OpenDisconnectedTicket()
		{
			if (DisconnectedTicket == null)
			{
				var data = new IkNoDataStream { endPoint = Endpoint, };
				DisconnectedTicket = OperationalTickets.Instance.OpenTicket(data);
			}
		}

		private void CloseDisconnectedTicket()
		{
			if(DisconnectedTicket != null)
			{
				DisconnectedTicket.Close();
				DisconnectedTicket = null;
			}
		}

		#endregion
	}
}
