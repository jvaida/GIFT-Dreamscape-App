using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artanim.Monitoring;

namespace Artanim
{

	public class FPS : MonoBehaviour
	{
		// Public properties
		public float UpdateRefreshRate = 1f;
		public Text AverageFpsText;
		public Text DeltaFpsText;

		long _markedFrameDisplayTime;
		// Optimize string construction
		StringBuilder _stringBuilder = new StringBuilder();

		// A string describing the average FPS
		public string AverageFps
		{
			get
			{
				_stringBuilder.Length = 0;
				_stringBuilder.Append(Mathf.RoundToInt(FPSMetrics.FpsAvg));
				_stringBuilder.Append(" FPS");
				return _stringBuilder.ToString();
			}
		}

		// A string describing the variation of FPS in percent (respectively to the average FPS)
		public string DeltaPercent
		{
			get
			{
				_stringBuilder.Length = 0;
				_stringBuilder.Append(Mathf.RoundToInt(100 / 2 * (FPSMetrics.MaxFps - FPSMetrics.MinFps) / FPSMetrics.FpsAvg));
				_stringBuilder.Append("%");
				return _stringBuilder.ToString();
			}
		}

		private void Update()
		{
			long ticks = Profiling.CPProfiler.Timestamp;

			if (_markedFrameDisplayTime == 0)
			{
				_markedFrameDisplayTime = ticks;
			}
			else if (Profiling.CPProfiler.DurationSince(_markedFrameDisplayTime) >= UpdateRefreshRate)
			{
				// Update display
				if (AverageFpsText != null)
				{
					AverageFpsText.text = AverageFps;
				}
				if (DeltaFpsText != null)
				{
					DeltaFpsText.text = DeltaPercent;
				}

				_markedFrameDisplayTime = Profiling.CPProfiler.Timestamp;
			}

		}

		void OnEnable()
		{
			_markedFrameDisplayTime = 0;
		}

		void OnDisable()
		{
		}
	}
}