using UnityEngine;
using Artanim.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Artanim.Tracking;

namespace Artanim.Monitoring.Utils
{
	public class PositionMetrics : MonoBehaviour
	{
		private Transform _transform;

		// Metrics
		struct MetricsData
		{
			public Vector3 pos;
			public Vector3 rot;
		}

		MetricsChannel<MetricsData> _metricsPosition;
		float _metricsPositionPeriod = 1.0f;

		long _lastTime = 0;

		private void OnEnable()
		{
		}

		private void OnDisable()
		{
		}

		public void Initialize(Transform transform, string name)
		{
			if (_metricsPosition == null)
			{
				// Create metrics channel
				_metricsPosition = MetricsManager.Instance.GetChannelInstance<MetricsData>(MetricsAction.Create, "Player Position", name);

				if(_metricsPosition != null)
					_metricsPositionPeriod = (float)_metricsPosition.GetParamValue("time period", _metricsPositionPeriod);
			}

			_transform = transform;
			_lastTime = 0;
		}

		// Never called, Garbage collector can handle this.
		public void Shutdown()
		{
			_lastTime = 0;
			_transform = null;

			if (_metricsPosition != null)
			{
				_metricsPosition.Dispose();
				_metricsPosition = null;
			}
		}

		private void LateUpdate()
		{
			if (_transform != null && _metricsPosition != null)
			{
				if (_lastTime == 0)
				{
					_lastTime = Profiling.CPProfiler.Timestamp;
				}
				else
				{
					float diff = Profiling.CPProfiler.DurationSince(_lastTime);

					if (diff >= _metricsPositionPeriod)
					{
						var position = new MetricsData()
						{
							pos = _transform.position,
							rot = _transform.eulerAngles
						};

						unsafe
						{
							_metricsPosition.SendRawData(new System.IntPtr(&position));
						}

						_lastTime = Profiling.CPProfiler.Timestamp;
					}
				}
			}
		}
	}
}