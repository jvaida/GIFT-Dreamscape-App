using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim.Monitoring.Utils
{
	public class AvatarTransformsMetrics : TransformsMetricsBase
	{
		[SerializeField]
		Transform _root;

		[SerializeField]
		Transform _pelvis;

		[SerializeField]
		Transform _head;

		[SerializeField]
		Transform _leftHand;

		[SerializeField]
		Transform _rightHand;

		[SerializeField]
		Transform _leftFoot;

		[SerializeField]
		Transform _rightFoot;

		List<Transform> _transforms = new List<Transform>();
		string _instanceName = "";

		// Can't have a generic with Unity 2017 and use addressOf instance
		// so we duplicate the struct for each case
		struct AvatarPosEntry
		{
			public PosEntry root, head, pelvis, left_hand, right_hand, left_foot, right_foot, avatar_offset;

			public void Set(IList<Transform> transforms, bool global)
			{
				root.Set(transforms[0], global);
				head.Set(transforms[1], global);
				pelvis.Set(transforms[2], global);
				left_hand.Set(transforms[3], global);
				right_hand.Set(transforms[4], global);
				left_foot.Set(transforms[5], global);
				right_foot.Set(transforms[6], global);
				avatar_offset.Set(transforms[7], global);
			}
		}

		struct AvatarRotEntry
		{
			public RotEntry root, head, pelvis, left_hand, right_hand, left_foot, right_foot, avatar_offset;

			public void Set(IList<Transform> transforms, bool global)
			{
				root.Set(transforms[0], global);
				head.Set(transforms[1], global);
				pelvis.Set(transforms[2], global);
				left_hand.Set(transforms[3], global);
				right_hand.Set(transforms[4], global);
				left_foot.Set(transforms[5], global);
				right_foot.Set(transforms[6], global);
				avatar_offset.Set(transforms[7], global);
			}
		}

		struct AvatarPosRotEntry
		{
			public PosRotEntry root, head, pelvis, left_hand, right_hand, left_foot, right_foot, avatar_offset;

			public void Set(IList<Transform> transforms, bool global)
			{
				root.Set(transforms[0], global);
				head.Set(transforms[1], global);
				pelvis.Set(transforms[2], global);
				left_hand.Set(transforms[3], global);
				right_hand.Set(transforms[4], global);
				left_foot.Set(transforms[5], global);
				right_foot.Set(transforms[6], global);
				avatar_offset.Set(transforms[7], global);
			}
		}

		struct AvatarPosRotScaleEntry
		{
			public PosRotScaleEntry root, head, pelvis, left_hand, right_hand, left_foot, right_foot, avatar_offset;

			public void Set(IList<Transform> transforms, bool global)
			{
				root.Set(transforms[0], global);
				head.Set(transforms[1], global);
				pelvis.Set(transforms[2], global);
				left_hand.Set(transforms[3], global);
				right_hand.Set(transforms[4], global);
				left_foot.Set(transforms[5], global);
				right_foot.Set(transforms[6], global);
				avatar_offset.Set(transforms[7], global);
			}
		}

		protected override void Initialize()
        {
			base.Initialize();

			_transforms.Add(_root);
			_transforms.Add(_pelvis);
			_transforms.Add(_head);
			_transforms.Add(_leftHand);
			_transforms.Add(_rightHand);
			_transforms.Add(_leftFoot);
			_transforms.Add(_rightFoot);

			var avatarController = GetComponentInParent<AvatarController>();
			if (avatarController != null)
			{
				var player = avatarController.RuntimePlayer.Player;
				_instanceName = (player.UserSessionId != System.Guid.Empty ? player.UserSessionId : player.ComponentId).ToString();
				_transforms.Add(avatarController.RuntimePlayer.AvatarOffset);
			}
			else
			{
				Debug.LogErrorFormat("Couldn't find AvatarController for {0}", name);
				_transforms.Add(null);
			}
		}

		protected override IMetricsWrapper CreateMetrics(MetricsParams metricsParams)
		{
			switch (MembersToLog)
			{
				case EMetricsType.Position:
					return new MetricsWrapper<AvatarPosEntry>(metricsParams, SendPosEntry);
				case EMetricsType.Rotation:
					return new MetricsWrapper<AvatarRotEntry>(metricsParams, SendRotEntry);
				case EMetricsType.PositionRotation:
					return new MetricsWrapper<AvatarPosRotEntry>(metricsParams, SendPosRotEntry);
				case EMetricsType.PositionRotationScale:
					return new MetricsWrapper<AvatarPosRotScaleEntry>(metricsParams, SendPosRotScaleEntry);
				default:
					throw new System.InvalidOperationException("Unexpected MembersToLog value: " + MembersToLog);
			}
		}

		protected override MetricsParams GetMetricsParams()
		{
			return new MetricsParams { TemplateName = "AvatarTransforms", InstanceName = _instanceName };
		}

        protected override IList<Transform> GetTransforms()
		{
			return _transforms;
		}

		#region Specialized metrics sender (for optimization)

		static void SendPosEntry(MetricsChannel<AvatarPosEntry> metrics, IList<Transform> transforms, Space space)
		{
			var data = new AvatarPosEntry(); // Struct
			data.Set(transforms, space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendRotEntry(MetricsChannel<AvatarRotEntry> metrics, IList<Transform> transforms, Space space)
		{
			var data = new AvatarRotEntry(); // Struct
			data.Set(transforms, space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendPosRotEntry(MetricsChannel<AvatarPosRotEntry> metrics, IList<Transform> transforms, Space space)
		{
			var data = new AvatarPosRotEntry(); // Struct
			data.Set(transforms, space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendPosRotScaleEntry(MetricsChannel<AvatarPosRotScaleEntry> metrics, IList<Transform> transforms, Space space)
		{
			var data = new AvatarPosRotScaleEntry(); // Struct
			data.Set(transforms, space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		#endregion
	}
}