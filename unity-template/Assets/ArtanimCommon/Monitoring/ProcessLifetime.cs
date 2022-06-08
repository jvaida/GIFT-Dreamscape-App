using Artanim.Location.Monitoring;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public class ProcessLifetime : MonoBehaviour
	{
		bool _shutdownRequested;
		DateTime _nextTimeDump;

		void Start()
		{
			// Attach to process extension termination signal
			Utils.ProcessExtension.AnnounceProcessWithNotify("Experience", () => _shutdownRequested = true, null);
		}

		void Update()
		{
			// Handle shutdown request
			if (_shutdownRequested)
			{
				Debug.Log("Shutdown requested by process notify. Shutting down...");
				Application.Quit();
			}

			// Heartbeat
			var heartbeat = ProcessHeartbeat.Instance;
			if (heartbeat != null)
            {
				heartbeat.Tick();
			}

			// Log time (liveliness check)
			if (_nextTimeDump <= DateTime.UtcNow)
            {
				_nextTimeDump = DateTime.UtcNow.AddMinutes(1);
				if (ConfigService.VerboseSdkLog)
                {
					Debug.LogFormat("Timings: Time.realtimeSinceStartup={0}, Time.unscaledTime={1}, Time.time={2}, Time.fixedUnscaledTime={3}, Time.fixedTime={4}",
						Time.realtimeSinceStartup, Time.unscaledTime, Time.time, Time.fixedUnscaledTime, Time.fixedTime);
				}
			}
		}
	}
}