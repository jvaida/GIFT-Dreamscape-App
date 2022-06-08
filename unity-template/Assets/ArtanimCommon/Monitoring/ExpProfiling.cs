using Artanim.Monitoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Artanim.Monitoring.Utils
{
	public static class ExpProfiling
	{
		public struct Timings
		{
			public float CaptureLatency;
			public float IkLatency;
			public float MessageLatency;

			public float VcStart;
			public float VcEnd;

			public float RbUpdateStart;
			public float RbUpdateEnd;

			public float GmCtrlStart;
			public float GmCtrlEnd;

			public float AvOffStart;
			public float AvOffEnd;

			public float LateUpdateEnd;

			public void Clear()
			{
				CaptureLatency = IkLatency = 0;
				VcStart = VcEnd = 0;
				RbUpdateStart = RbUpdateEnd = 0;
				GmCtrlStart = GmCtrlEnd = 0;
				AvOffStart = AvOffEnd = 0;
				LateUpdateEnd = 0;
			}
		}

		public static Timings Frame { get { return _timings; } }

		public static long StartTimestamp { get { return _startTimestamp; } }

		public static Timings _timings = new Timings();
		static long _startTimestamp, _lastRbUpdateTimestamp;

		public static void StartFrame()
		{
			_startTimestamp = GetTimestamp();
			_lastRbUpdateTimestamp = 0;

			_timings.Clear();
		}

		public static void SetIkUpdateLatency(float captureLatency, float ikLatency, float messageLatency)
		{
			_timings.CaptureLatency = captureLatency;
			_timings.IkLatency = ikLatency;
			_timings.MessageLatency = messageLatency;
		}

		public static void MarkVcStart()
		{
			_timings.VcStart = GetDuration();
		}

		public static void MarkVcEnd()
		{
			_timings.VcEnd = GetDuration();
		}

		public static void MarkRbUpdateStart()
		{
			if (_timings.RbUpdateStart == 0)
			{
				_timings.RbUpdateStart = GetDuration();
				_lastRbUpdateTimestamp = 0;
			}
			else
			{
				_lastRbUpdateTimestamp = GetTimestamp();
			}
		}

		public static void MarkRbUpdateEnd()
		{
			_timings.RbUpdateEnd += GetRbUpdateDuration();
		}

		public static void MarkGmCtrlStart()
		{
			_timings.GmCtrlStart = GetDuration();
		}

		public static void MarkGmCtrlEnd()
		{
			_timings.GmCtrlEnd = GetDuration();
		}

		public static void MarkAvOffStart()
		{
			_timings.AvOffStart = GetDuration();
		}

		public static void MarkAvOffEnd()
		{
			_timings.AvOffEnd = GetDuration();
		}

		public static void MarkLateUpdateEnd()
		{
			_timings.LateUpdateEnd = GetDuration();
		}

		static long GetTimestamp()
		{
			// DateTime.Now.Ticks is not precise enough: ~0.5 ms on Unity 5.6 (Mono 2)
			return Profiling.CPProfiler.Timestamp;
		}

		static float GetDurationSince(long timestamp)
		{
			//return GetTimestamp() - timestamp;
			return Profiling.CPProfiler.DurationSince(timestamp);
		}

		static float GetDuration()
		{
			if (_startTimestamp > 0)
			{
				return GetDurationSince(_startTimestamp);
			}
			else
			{
				return 0;
			}
		}

		static float GetRbUpdateDuration()
		{
			if (_lastRbUpdateTimestamp == 0)
			{
				return GetDuration();
			}
			else
			{
				return GetDurationSince(_lastRbUpdateTimestamp);
			}
		}
	}
}