using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artanim.Location.Config;

namespace Artanim.Tracking
{
	// Connection statistics
	public interface ITrackingConnectorStats
	{
		uint FrameNumber { get; }
		DateTime FrameCaptureTime { get; }
		long FrameProcessTimestamp { get; }
		float FrameCaptureLatency { get; }
		float FrameProcessLatency { get; }
	}

	public interface ITrackingConnector
	{
		bool IsConnected { get; }

		string Endpoint { get; }

		string Version { get; }

		ITrackingConnectorStats Stats { get; }

		void Connect();

		void Disconnect();

		bool UpdateRigidBodies();
	}
}
