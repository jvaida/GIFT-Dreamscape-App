using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artanim.Monitoring;
using Artanim.Monitoring.Utils;

namespace Artanim
{
	public class TrackingQualityMetrics : IDisposable
	{
		// We create one metrics instance (channel) for each rigidbody
		class QualityMetrics
		{
			MetricsChannel<float> _channel;
			double _cumulatedQuality;
			int _cumulQualityCount;
			long _cumulQualityStartTime;

			public QualityMetrics(MetricsChannel<float> channel)
			{
				_channel = channel;
				_cumulQualityStartTime = Profiling.CPProfiler.Timestamp;
			}

			// Monitor the quality overtime
			public void Monitor(float quality, float timePeriod)
			{
				// Accumulate quality values
				_cumulatedQuality += quality;
				++_cumulQualityCount;

				// After a certain time, compute average
				float diff = Profiling.CPProfiler.DurationSince(_cumulQualityStartTime);
				if (diff >= timePeriod)
				{
					float average = (float)(_cumulatedQuality / _cumulQualityCount);
					unsafe
					{
						_channel.SendRawData(new IntPtr(&average));
					}

					// Reset values
					_cumulQualityStartTime = Profiling.CPProfiler.Timestamp;
					_cumulatedQuality = 0;
					_cumulQualityCount = 0;
				}
			}

			public void Close()
			{
				_channel.Dispose();
			}
		}

		Dictionary<string, QualityMetrics> _mapQualityMetrics = new Dictionary<string, QualityMetrics>();
		MetricsChannelTemplate<float> _metricChannelTemplate = MetricsManager.Instance.GetChannelTemplate<float>(MetricsAction.Create, "Tracking Quality");

		// Config values
		float _qualityThreshold = float.NaN;
		uint _timePeriod;

		public TrackingQualityMetrics()
		{
		}

		// Check the tracking quality for the given rigidbody and trigger a quality metrics event when tracking isn't good enough
		public bool CheckQuality(string rigidbodyName, float quality)
		{
			// Get metrics for the rigidbody
			var qm = GetQualityMetrics(rigidbodyName);

			// And
			qm.Monitor(quality, _timePeriod);

			// Returns whether or not the quality is good enough
			return (_qualityThreshold <= 0) || (quality < _qualityThreshold);
		}

		QualityMetrics GetQualityMetrics(string rigidbodyName)
		{
			QualityMetrics qualityMetrics;
			_mapQualityMetrics.TryGetValue(rigidbodyName, out qualityMetrics);
			if (qualityMetrics == null)
			{
				var instance = _metricChannelTemplate.GetInstance(MetricsAction.Create, rigidbodyName);
				qualityMetrics = new QualityMetrics(instance);
				_mapQualityMetrics.Add(rigidbodyName, qualityMetrics);

				// Initialize config values once
				if (float.IsNaN(_qualityThreshold))
				{
					_qualityThreshold = (float)instance.GetParamValue("threshold", 3);
					_timePeriod = (uint)instance.GetParamValue("time period", 1);
				}
			}
			return qualityMetrics;
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (var qm in _mapQualityMetrics)
					{
						qm.Value.Close();
					}

					_mapQualityMetrics.Clear();

					if (_metricChannelTemplate != null)
					{
						_metricChannelTemplate.Dispose();
						_metricChannelTemplate = null;
					}
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}