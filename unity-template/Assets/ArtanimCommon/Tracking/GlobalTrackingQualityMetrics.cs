using Artanim.Location.Monitoring;
using Artanim.Location.Monitoring.OpTicketsTypes.System;
using Artanim.Location.Monitoring.OpTicketsTypes.Tracking;
using Artanim.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim
{
	public class GlobalTrackingQualityMetrics : IDisposable
	{
		private MetricsChannel<float> Channel;
		private MetricsChannelTemplate<float> ChannelTemplate = MetricsManager.Instance.GetChannelTemplate<float>(MetricsAction.Create, "Global Tracking Quality");

		private readonly int Sma;
        private readonly float RefreshPeriod;
        private float LastRefresh = float.MinValue;
#if OP_TICKET
		private float TicketOpenThreshold;
		private float TicketCloseThreshold;
#endif
        private float[] FrameQualityValues;
		private int FrameIndex;
		private bool BufferFilled;

		public GlobalTrackingQualityMetrics()
		{
			Channel = ChannelTemplate.GetInstance(MetricsAction.Create);

			Sma = (int)Channel.GetParamValue("sma", 180);
            RefreshPeriod = (float)Channel.GetParamValue("refresh period", 1);
#if OP_TICKET
			TicketOpenThreshold = (float)Channel.GetParamValue("ticket open threshold", 1.5f);
			TicketCloseThreshold = (float)Channel.GetParamValue("ticket close threshold", 1.2f);

			if (TicketOpenThreshold < TicketCloseThreshold)
			{
				Debug.LogWarningFormat("Ticket open threshold is larger than ticket close threshold. Setting both values to ticket open threshold: open threshold={0}, close threshold={1}", TicketOpenThreshold, TicketCloseThreshold);
				TicketCloseThreshold = TicketOpenThreshold;
			}
#endif
			FrameQualityValues = new float[Sma];
		}

		private float SlidingSum;
		public void AddFrameQualityAverage(float value)
		{
			//Validate
			if (float.IsNaN(value) || float.IsInfinity(value))
				return;

			//Update sum
			SlidingSum -= FrameQualityValues[FrameIndex];
			SlidingSum += value;
			FrameQualityValues[FrameIndex] = value;

			if (!BufferFilled && FrameIndex == FrameQualityValues.Length - 1)
			{
				BufferFilled = true;

				SlidingSum = 0f;
				for (var i = 0; i < FrameQualityValues.Length; ++i)
					SlidingSum += FrameQualityValues[i];
			}

			if (BufferFilled)
			{
				var sma = SlidingSum / FrameQualityValues.Length;

                if ((Time.realtimeSinceStartup - LastRefresh) > RefreshPeriod)
                {
                    LastRefresh = Time.realtimeSinceStartup;
                    unsafe
                    {
                        Channel.SendRawData(new IntPtr(&sma));
                    }
                }
				

#if UNITY_EDITOR && IK_SERVER
				DebugGraph.MultiLog("Global Tracking Quality", Color.green, sma, string.Format("SMA{0}", Sma));
				DebugGraph.MultiLog("Global Tracking Quality", Color.blue, value, "Raw");
#endif

#if OP_TICKET
				if (sma > TicketOpenThreshold)
					OpenTicket(sma);
				else if(sma < TicketCloseThreshold)
					CloseTicket();
#endif
			}

			FrameIndex = (FrameIndex + 1) % FrameQualityValues.Length;
		}

#if OP_TICKET
		#region Global Tracking Quality Ticket

		private OperationalTickets.IOpTicket GlobalTrackingQualityTicket;

		private void OpenTicket(float quality)
		{
			if (GlobalTrackingQualityTicket == null)
			{
				var data = new GlobalTrackingQuality { averageTrackingQuality = quality, };
				GlobalTrackingQualityTicket = OperationalTickets.Instance.OpenTicket(data);
			}
		}

		private void CloseTicket()
		{
			if(GlobalTrackingQualityTicket != null)
			{
				GlobalTrackingQualityTicket.Close();
				GlobalTrackingQualityTicket = null;
			}
		}

		#endregion
#endif

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
#if OP_TICKET
					CloseTicket();
#endif
					Channel.Dispose();
					Channel = null;

					ChannelTemplate.Dispose();
					ChannelTemplate = null;
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
