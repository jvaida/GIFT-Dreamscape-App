using Artanim.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim.Monitoring.Utils
{
	public abstract class TransformsMetricsBase : MonoBehaviour
	{
		public enum EMetricsType { Position, PositionRotation, PositionRotationScale, Rotation };

		[Tooltip("Transform members to log")]
		[SerializeField]
		EMetricsType _membersToLog = EMetricsType.PositionRotation;

		[Tooltip("Whether to log local or global coordinates")]
		[SerializeField]
		Space _space;

		[Tooltip("Logging frequency in Hz, leave to 0 to generate a new entry every frame")]
		[SerializeField]
		float _frequency = 10;

		// Private data
		bool _initialized;
		float _period;
		long _lastLogIter;
		IMetricsWrapper _metrics;

		public EMetricsType MembersToLog
        {
			get
            {
				return _membersToLog;
            }
        }


		protected struct MetricsParams
		{
			public string TemplateName;
			public string InstanceName;
		}

		#region Metrics classes

		protected interface IMetricsEntry
		{
			void Set(Transform transf, bool global);
		}

		protected struct PosEntry : IMetricsEntry
		{
			public byte global;
			public Vector3 pos;
			public void Set(Transform transf, bool global)
			{
				this.global = (byte)(global ? 1 : 0);
				pos = transf == null ? Vector3.zero : (global ? transf.position : transf.localPosition);
			}
		}

		protected struct RotEntry : IMetricsEntry
		{
			public byte global;
			public Vector3 rot;
			public void Set(Transform transf, bool global)
			{
				this.global = (byte)(global ? 1 : 0);
				rot = transf == null ? Vector3.zero : (global ? transf.rotation.eulerAngles : transf.localRotation.eulerAngles);
			}
		}

		protected struct PosRotEntry : IMetricsEntry
		{
			public byte global;
            public Vector3 pos;
            public Vector3 rot;
            public void Set(Transform transf, bool global)
			{
				this.global = (byte)(global ? 1 : 0);
				pos = transf == null ? Vector3.zero : (global ? transf.position : transf.localPosition);
				rot = transf == null ? Vector3.zero : (global ? transf.rotation.eulerAngles : transf.localRotation.eulerAngles);
			}
		}

		protected struct PosRotScaleEntry : IMetricsEntry
		{
			public byte global;
			public Vector3 pos;
			public Vector3 rot;
			public Vector3 scale;
			public void Set(Transform transf, bool global)
			{
				this.global = (byte)(global ? 1 : 0);
				pos = transf == null ? Vector3.zero : (global ? transf.position : transf.localPosition);
				rot = transf == null ? Vector3.zero : (global ? transf.rotation.eulerAngles : transf.localRotation.eulerAngles);
				scale = transf == null ? Vector3.zero : (global ? transf.lossyScale : transf.localScale);
			}
		}

		protected interface IMetricsWrapper : System.IDisposable
		{
			void Log(IList<Transform> transforms, Space space);
		}

		protected class MetricsWrapper<T> : IMetricsWrapper
			 where T : struct
		{
			public delegate void SendDataHandler(MetricsChannel<T> metrics, IList<Transform> transforms, Space space); // Optimization to be able to use unsafe{} to send metrics data

			MetricsChannel<T> _metrics;
			SendDataHandler _sendDataFunc;

			public MetricsWrapper(MetricsParams metricsParams, SendDataHandler sendDataFunc)
			{
				_sendDataFunc = sendDataFunc;
				_metrics = MetricsManager.Instance.GetChannelInstance<T>(MetricsAction.Create, metricsParams.TemplateName, metricsParams.InstanceName);
			}

			public void Log(IList<Transform> transforms, Space space)
			{
				if (_metrics == null)
				{
					throw new System.ObjectDisposedException(GetType().Name);
				}

				_sendDataFunc(_metrics, transforms, space);
			}

			public void Dispose()
			{
				if (_metrics != null)
				{
					_metrics.Dispose();
					_metrics = null;
				}
			}
		}

		#endregion

		#region Unity events

		void Start()
		{
			_period = _frequency <= 0 ? _frequency : 1 / _frequency;
			enabled = _period >= 0;
		}

		void Update()
		{
			if (GameController.Instance && GameController.Instance.CurrentSession != null)
			{
				if (_period == 0)
				{
					LogNow();
				}
				else if (_period > 0)
				{
					// We want all of these metrics to be logged at the same frame
					// if they use the same frequency
					long iter = (long)(Time.unscaledTime / _period);
					if (iter > _lastLogIter)
					{
						_lastLogIter = iter;
						LogNow();
					}
				}
			}
		}

		void OnDisable()
		{
			if (_metrics != null)
			{
				_metrics.Dispose();
				_metrics = null;
			}
		}

		#endregion

		public void LogNow()
		{
			if (!_initialized)
            {
				Initialize();
				_initialized = true; // Just in case
			}

			var transforms = GetTransforms();
			if ((transforms != null) && (transforms.Count > 0))
			{
				if (_metrics == null)
				{
					var metricsParams = GetMetricsParams();
					metricsParams.TemplateName = Paths.ReplaceInvalidCharacters(metricsParams.TemplateName + "-" + MembersToLog, "_").Replace(" ", "_");
					metricsParams.InstanceName = string.IsNullOrEmpty(metricsParams.InstanceName) ? null : Paths.ReplaceInvalidCharacters(metricsParams.InstanceName, "_").Replace(" ", "_");
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("Creating transforms metrics with template '{0}' and instance name '{1}'", metricsParams.TemplateName, metricsParams.InstanceName);
					_metrics = CreateMetrics(metricsParams);
				}

				_metrics.Log(transforms, _space);
			}
		}

		protected virtual void Initialize()
        {
			_initialized = true;
		}

		protected virtual IMetricsWrapper CreateMetrics(MetricsParams metricsParams)
		{
			switch (MembersToLog)
			{
				case EMetricsType.Position:
					return new MetricsWrapper<PosEntry>(metricsParams, SendPosEntry);
				case EMetricsType.Rotation:
					return new MetricsWrapper<RotEntry>(metricsParams, SendRotEntry);
				case EMetricsType.PositionRotation:
					return new MetricsWrapper<PosRotEntry>(metricsParams, SendPosRotEntry);
				case EMetricsType.PositionRotationScale:
					return new MetricsWrapper<PosRotScaleEntry>(metricsParams, SendPosRotScaleEntry);
				default:
					throw new System.InvalidOperationException("Unexpected MembersToLog value: " + MembersToLog);
			}
		}

		protected virtual MetricsParams GetMetricsParams()
		{
			return new MetricsParams { TemplateName = GetType().Name, InstanceName = string.Empty };
		}

		protected abstract IList<Transform> GetTransforms();

		#region Specialized metrics sender (for optimization)

		static void SendPosEntry(MetricsChannel<PosEntry> metrics, IList<Transform> transforms, Space space)
        {
			var data = new PosEntry(); // struct
			data.Set(transforms[0], space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendRotEntry(MetricsChannel<RotEntry> metrics, IList<Transform> transforms, Space space)
        {
			var data = new RotEntry(); // struct
			data.Set(transforms[0], space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendPosRotEntry(MetricsChannel<PosRotEntry> metrics, IList<Transform> transforms, Space space)
        {
			var data = new PosRotEntry(); // struct
			data.Set(transforms[0], space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

		static void SendPosRotScaleEntry(MetricsChannel<PosRotScaleEntry> metrics, IList<Transform> transforms, Space space)
        {
			var data = new PosRotScaleEntry(); // struct
			data.Set(transforms[0], space == Space.World);
			unsafe { metrics.SendRawData(new System.IntPtr(&data)); }
		}

        #endregion
    }
}