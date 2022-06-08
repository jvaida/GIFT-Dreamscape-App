using Artanim.Monitoring;
using UnityEngine;
using System.Collections;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRNode = UnityEngine.VR.VRNode;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

using FpsData = Artanim.Data.FPS;

namespace Artanim
{

	public class FPSMetrics : System.IDisposable
	{
		// Public fields
		public static float FpsAvg { get; private set; }
		public static float MinFps { get; private set; }
		public static float MaxFps { get; private set; }

		// Private fields
		long _lastTicks, _markedFrameTime;
		int _markedFrameCount;
		float _currentMinFps, _currentMaxFps;

		// Metrics
		// struct FpsData
		// {
		// 	public float Instant, Average, Min, Max;
		// }

		MetricsChannel<FpsData> _metricsFPS;
		float _metricsFpsPeriod = 1.0f;
        float _metricsRefreshPeriod, _metricsLastRefresh = float.MinValue;

		public FPSMetrics()
		{
		}

		public void Reset()
		{
			_markedFrameTime = Profiling.CPProfiler.Timestamp; //System.DateTime.Now.Ticks is not precise enough: ~0.5ms with Unity 5.6.2
			_markedFrameCount = Time.frameCount;
			_currentMinFps = float.MaxValue;
			_currentMaxFps = 0;
		}

		public void Update()
		{
			long ticks = Profiling.CPProfiler.Timestamp; //System.DateTime.Now.Ticks is not precise enough: ~0.5ms with Unity 5.6.2

			// If we were enabled this frame then the FPS value is not correct
			if (_lastTicks == 0)
			{
				_lastTicks = ticks;
				Reset();
				return;
			}
			else if ((ticks - _lastTicks) < 10)
			{
				// Sometimes we get called twice even though Time.frameCount has increased
				return;
			}

			// Compute current FPS
			float fps = 1f / Profiling.CPProfiler.DurationSince(_lastTicks);
			_lastTicks = ticks;

			// Update min/max
			if (fps < _currentMinFps) _currentMinFps = fps;
			if (fps > _currentMaxFps) _currentMaxFps = fps;

			// Check if it's time to update display
			float diff = Profiling.CPProfiler.DurationSince(_markedFrameTime);

			if (diff >= _metricsFpsPeriod)
			{
				// Compute fps average
				FpsAvg = (Time.frameCount - _markedFrameCount) / diff;

				// Store min/max
				MinFps = _currentMinFps;
				MaxFps = _currentMaxFps;

				// Reset variables
				Reset();
			}

			if (_metricsFPS == null)
			{
				// Create metrics (Update might be called before the component is known)
				var componentType = Location.Network.NetworkInterface.Instance.ComponentType;
				if (componentType != Location.Data.ELocationComponentType.Undefined)
				{
#if UNITY_EDITOR
					string instanceName = "editor";
#else
					string instanceName = componentType.ToString().ToLowerInvariant();
					if ((componentType == Location.Data.ELocationComponentType.ExperienceClient) && (!XRSettings.enabled))
					{
						instanceName += "_novr";
					}
#endif
					// Create metrics channels
					_metricsFPS = MetricsManager.Instance.GetChannelInstance<FpsData>(MetricsAction.Create, "FPS", instanceName);
					_metricsFpsPeriod = (float)_metricsFPS.GetParamValue("time period", _metricsFpsPeriod);
                    _metricsRefreshPeriod = (float)_metricsFPS.GetParamValue("refresh period", 1);
				}
			}

			if ((_metricsFPS != null) && ((Time.realtimeSinceStartup - _metricsLastRefresh) > _metricsRefreshPeriod))
			{
                _metricsLastRefresh = Time.realtimeSinceStartup;

				var fpsData = new FpsData()
				{
					Instant = fps,
					Average = FpsAvg,
					Min = MinFps,
					Max = MaxFps,
				};
				unsafe
				{
					_metricsFPS.SendRawData(new System.IntPtr(&fpsData));
				}
			}
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_lastTicks = 0;

					// Destroy metrics channels
					if (_metricsFPS != null)
					{
						_metricsFPS.Dispose();
						_metricsFPS = null;
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