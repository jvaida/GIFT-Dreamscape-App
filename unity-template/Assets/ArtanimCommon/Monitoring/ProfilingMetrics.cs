using Artanim.Monitoring;
using UnityEngine;
using System.Collections;
using Artanim.Monitoring.Utils;
using System;

namespace Artanim
{
	public class ProfilingMetrics : MonoBehaviour
	{
		FPSMetrics _fpsMetrics;

		void OnEnable()
		{
			_fpsMetrics = new FPSMetrics();
		}

		void OnDisable()
		{
			// Destroy FPS metrics
			if (_fpsMetrics != null)
			{
				_fpsMetrics.Dispose();
				_fpsMetrics = null;
			}

			Dispose();
		}

		IEnumerator Start()
		{
			// Wait a few seconds before starting FPS, because Unity might still be loading stuff
			yield return new WaitForSecondsRealtime(5);

			var waitEndOfFrame = new WaitForEndOfFrame();
			while (true)
			{
				yield return waitEndOfFrame;
				if (enabled)
				{
					_fpsMetrics.Update();
				}
			}
		}

#if IK_PROFILING
		void Dispose()
		{
		}

		void LateUpdate()
		{
			IKProfiling.MarkLateUpdateEnd();
		}
#elif EXP_PROFILING
		MetricsChannel<ExpProfiling.Timings> _expTimingsMetrics;

		void Dispose()
		{
			// Destroy metrics channels
			if (_expTimingsMetrics != null)
			{
				_expTimingsMetrics.Dispose();
				_expTimingsMetrics = null;
			}
		}

		void LateUpdate()
		{
			ExpProfiling.MarkLateUpdateEnd();

			if (_expTimingsMetrics == null)
			{
				_expTimingsMetrics = MetricsManager.Instance.GetChannelInstance<ExpProfiling.Timings>(MetricsAction.Create, "Exp Profiling");
			}
			var frame = ExpProfiling._timings;
			unsafe
			{
				_expTimingsMetrics.SendRawData(new IntPtr(&frame));
			}
		}
#else
		void Dispose()
		{
		}
#endif

	public static void MarkVcEnd()
		{
#if IK_PROFILING
			IKProfiling.MarkVcEnd();
#endif
#if EXP_PROFILING
			ExpProfiling.MarkVcEnd();
#endif
		}

		public static void MarkRbUpdateStart()
		{
#if IK_PROFILING
			IKProfiling.MarkRbUpdateStart();
#endif
#if EXP_PROFILING
			ExpProfiling.MarkRbUpdateStart();
#endif
		}

		public static void MarkRbUpdateEnd()
		{
#if IK_PROFILING
			IKProfiling.MarkRbUpdateEnd();
#endif
#if EXP_PROFILING
			ExpProfiling.MarkRbUpdateEnd();
#endif
		}
	}
}