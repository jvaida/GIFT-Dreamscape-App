using Artanim.Algebra;
using Artanim.Location.Data;
using Artanim.Location.Network.Tracking;
using Artanim.Location.SharedData;
using Artanim.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Tracking
{
	public class IkConnector : ITrackingConnector
	{
		// Connection statistics
		class IkStats : ITrackingConnectorStats
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

		// All times are in seconds
		struct SystemLatency
		{
			public byte FrameNumberDelta;
			public float CaptureDeltaTime;
			public float CaptureLatency;
			public float IkLatencyCumul;
			public float MessageLatencyCumul;
			public float TotalLatency;
		}

		private volatile bool _isConnected = false;
		private IkStats _stats = new IkStats();

		private MetricsChannel<SystemLatency> _systemLatencyMetrics;
		private DateTime _lastCaptureTime;

		private ITrackingFrameSubscription _networkSubscription, _remoteSubscription;
		private OutOfPodSubscription _outOfPodSubscription;

		public bool IsConnected
		{
			get { return _isConnected; }
		}

		public string Endpoint
		{
			get { return string.Empty; }
		}

		public ITrackingConnectorStats Stats
		{
			get { return _stats; }
		}

		public string Version
		{
			get { return ""; }
		}

		public void Connect()
		{
			if (_isConnected) return;

			_stats.Reset();

			// Create metrics channels
			if (_systemLatencyMetrics == null)
			{
				_systemLatencyMetrics = MetricsManager.Instance.GetChannelInstance<SystemLatency>(MetricsAction.Create, "System Latency");
			}
			_lastCaptureTime = new DateTime(0, DateTimeKind.Utc);

			if (_networkSubscription == null)
			{
				var networkInterface = Location.Network.NetworkInterface.Instance;
				_networkSubscription = networkInterface.IsServer || !networkInterface.IsTrueClient
					? new DefaultSubscription(Location.Helpers.NetworkSetup.NetBus) as ITrackingFrameSubscription // Everything but a normal client => Server, Client as server, observer
					: new TargetedSubscription(Location.Helpers.NetworkSetup.NetBus, GameController.Instance.CurrentPlayer.Player.SkeletonId, GameController.Instance.CurrentSession.SharedId);

				_outOfPodSubscription = new OutOfPodSubscription(Location.Helpers.NetworkSetup.NetBus);
				_remoteSubscription = new RemoteSubscription(Location.Helpers.NetworkSetup.NetBus, networkInterface.IsServer);

				if (GameController.Instance && (GameController.Instance.CurrentSession != null))
                {
					RefreshOutOfPodSubscription(GameController.Instance.CurrentSession);
				}
			}

			if (GameController.Instance)
			{
                GameController.Instance.OnJoinedSession += Instance_OnJoinedSession; ;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession; ;
				GameController.Instance.OnSessionPlayerJoined += Instance_OnSessionPlayerJoined;
				GameController.Instance.OnSessionPlayerLeft += Instance_OnSessionPlayerLeft;
			}

			_isConnected = true;
		}

        public void Disconnect()
		{
			if (!_isConnected) return;

			// Destroy metrics channels
			if (_systemLatencyMetrics != null)
			{
				_systemLatencyMetrics.Dispose();
				_systemLatencyMetrics = null;
			}

			if (_networkSubscription != null)
			{
				_networkSubscription.Dispose();
				_networkSubscription = null;
			}

			if (_outOfPodSubscription != null)
			{
				_outOfPodSubscription.Dispose();
				_outOfPodSubscription = null;
			}

			if (_remoteSubscription != null)
			{
				_remoteSubscription.Dispose();
				_remoteSubscription = null;
			}

			if (GameController.HasInstance)
			{
				GameController.Instance.OnJoinedSession -= Instance_OnJoinedSession; ;
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession; ;
				GameController.Instance.OnSessionPlayerJoined -= Instance_OnSessionPlayerJoined;
				GameController.Instance.OnSessionPlayerLeft -= Instance_OnSessionPlayerLeft;
			}

			_isConnected = false;
		}

		private void Instance_OnJoinedSession(Session session, Guid playerId)
		{
			RefreshOutOfPodSubscription(session);
		}

		private void Instance_OnLeftSession()
		{
			if (_outOfPodSubscription != null)
            {
				_outOfPodSubscription.DisconnectAll();
			}
		}

		private void Instance_OnSessionPlayerJoined(Session session, Guid playerId)
		{
			RefreshOutOfPodSubscription(session);
		}

		private void Instance_OnSessionPlayerLeft(Session session, Guid playerId)
		{
			RefreshOutOfPodSubscription(session, removedPlayerId: playerId);
		}

		private void RefreshOutOfPodSubscription(Session session, Guid removedPlayerId = default(Guid))
        {
			if (_outOfPodSubscription != null)
            {
				// Match pod ids in lower case (to 2 pod ids with different case are considered the same)
				string myPodId = Location.Helpers.HelperMethods.PodId.ToLowerInvariant();

				// Connect to our own pod id if running as a "desktop avatar" as we won't get updates from the IKServer
				bool connectToSelf = (GameController.Instance.CurrentPlayer != null) && GameController.Instance.CurrentPlayer.IsDesktopAvatar;

				// Get pods
				var podIds = session.Players
					.Select(p => SharedDataUtils.FindLocationComponent(p.ComponentId) as ExperienceClient)
					.Where(client => (client != null) && (client.SharedId != removedPlayerId))
					.Select(client => client.PodId != null ? client.PodId.ToLowerInvariant() : string.Empty)
					.Where(podId => connectToSelf || (podId != myPodId))
					.ToArray();

				// And connect to their tracking data
				_outOfPodSubscription.ConnectToPods(podIds);

				Debug.Log("Connected to IK topics from pod(s): "
					+ string.Join(", ", _outOfPodSubscription.PodIds.Select(podId => string.IsNullOrEmpty(podId) ? "<empty pod id>" : podId).ToArray()));
			}
		}

		public bool UpdateRigidBodies()
		{
			bool dataUpdated = false;

			if (_networkSubscription != null)
			{
				// Update rigidbodies with the data coming from other pods
				// We do it first so to not override the data coming from our own pod
				if (UpdateRigidBodies(_outOfPodSubscription)) dataUpdated = true;

				// There shouldn't be any rigidbody from the remote clients but that may change
				if (UpdateRigidBodies(_remoteSubscription)) dataUpdated = true;

				// Then update rigidbodies with the data coming from our own pod
				if (UpdateRigidBodies(_networkSubscription)) dataUpdated = true;

				// Latency metrics
				var captureTime = _networkSubscription.LastFrameInfo.CaptureInfo.Time;
				if (_lastCaptureTime.Ticks > 0)
				{
					var data = new SystemLatency()
					{
						FrameNumberDelta = (byte)Mathf.Max(0, (int)_networkSubscription.LastFrameInfo.CaptureInfo.FrameNumber - (int)_stats.FrameNumber),
						CaptureDeltaTime = (float)(captureTime - _lastCaptureTime).TotalSeconds,
						CaptureLatency = _networkSubscription.LastFrameInfo.CaptureInfo.Latency,
						IkLatencyCumul = (float)(_networkSubscription.LastFrameInfo.SendTime - captureTime).TotalSeconds,
						MessageLatencyCumul = (float)(_networkSubscription.LastFrameInfo.ReceptionTime - captureTime).TotalSeconds,
						TotalLatency = (float)(DateTime.UtcNow - captureTime).TotalSeconds,
					};
					unsafe
					{
						_systemLatencyMetrics.SendRawData(new IntPtr(&data));
					}
				}
			}

			return dataUpdated;
		}

        private bool UpdateRigidBodies(ITrackingFrameSubscription subscription)
        {
			bool dataUpdated = false;

			subscription.Update();

			for (int i = 0, iMax = subscription.RigidBodiesCount; i < iMax; ++i)
			{
				string podId; uint frameNumber;
				var rbData = subscription.GetRigidBodyTransform(i, out frameNumber, out podId);

				var position = rbData.Position.ToUnity();
				var rotation = rbData.Orientation.ToUnity();

				// Update rigidbody
				var rigidBody = TrackingController.Instance.GetOrCreateRigidBody(rbData.Name);
				rigidBody.UpdateTransform(frameNumber, position, rotation, isTracked: true);

				dataUpdated = true;
			}

			return dataUpdated;
		}

		public void UpdateSkeletons(List<RuntimePlayer> runtimePlayers)
		{
			if ((_networkSubscription != null) && (runtimePlayers != null) && (runtimePlayers.Count > 0))
            {
				for (int i = 0, iMax = runtimePlayers.Count; i < iMax ; ++i)
				{
					var avatar = runtimePlayers[i].AvatarController;

					// Try to get our avatar skeleton from the regular pod's topic
					var skeletonUpdate = _networkSubscription.GetSkeletonTransforms(avatar.SkeletonId);
					if (skeletonUpdate.SkeletonId == avatar.SkeletonId)
					{
						avatar.UpdateSkeleton(skeletonUpdate);
					}
					else
					{
						// And if it fails try the out of pod topic
						skeletonUpdate = _outOfPodSubscription.GetSkeletonTransforms(avatar.SkeletonId);
						if (skeletonUpdate.SkeletonId == avatar.SkeletonId)
						{
							avatar.UpdateSkeleton(skeletonUpdate);
						}
					}

					// If we still don't have a skeleton, try with the remote topic
					if (skeletonUpdate.SkeletonId != avatar.SkeletonId)
					{
						skeletonUpdate = _remoteSubscription.GetSkeletonTransforms(avatar.SkeletonId);
						if (skeletonUpdate.SkeletonId == avatar.SkeletonId)
						{
							avatar.UpdateSkeleton(skeletonUpdate);
						}
					}
				}
			}
		}

		~IkConnector()
		{
			// In case Disconnect hasn't been called
			if (IsConnected)
			{
				Debug.LogWarning("IkConnector wasn't disconnected before being destroyed");
				Disconnect();
			}
		}
	}
}