using Artanim.Location.Messages;
using Artanim.Location.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Artanim
{
	[AddComponentMenu("Artanim/Network Synced Timeline")]
	[RequireComponent(typeof(PlayableDirector))]
	public class NetworkSyncedTimeline : MonoBehaviour
	{
        [ObjectId]
		[Tooltip("ID this behavior reacts to.")]
		public string ObjectId;

		[Tooltip("The frequency (in seconds) the server sends sync updated to the clients.")]
		public float SyncFrequencySeconds = 2f;

		[Tooltip("The maximal speed increase / decrease factor clients apply to the timeline to sync with the server time.")]
		public float MaxSyncSpeedFactor = 0.2f;

		[Tooltip("Sync offsets larger than this value in seconds (+/-) will apply the given MaxSyncSpeedFactor to recover from the offset.")]
		public float MaxSyncSpeedRangeSeconds = 0.5f;

		public float CurrentSpeedFactor = 1f;

		#region TO REVMOVE
		[Tooltip("UI text element to display client sync infos. Currently used for development and testing. Will be removed in the future.")]
		public Text TextEstimatedDelta;
		#endregion

		private PlayableDirector TimelineDirector;
		
		private PlayState _currentPlaystate;
		private PlayState CurrentPlaystate
		{
			get { return _currentPlaystate; }
			set
			{
				if(_currentPlaystate != value)
				{
					OnPlayStateChanging(value);
				}
				_currentPlaystate = value;
			}
		}

		private TimelineSync LastTimelineSync;

		#region Unity events

		private void Awake()
		{
			TimelineDirector = GetComponent<PlayableDirector>();
			CurrentPlaystate = TimelineDirector.state;
		}

		private void OnEnable()
		{
			if(NetworkInterface.Instance.IsClient)
			{
				NetworkInterface.Instance.Subscribe<TimelineSync>(OnTimelineSync);
			}
		}

		private void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<TimelineSync>(OnTimelineSync);
		}

		private void Update()
		{
			//Update playstate
			CurrentPlaystate = TimelineDirector.state;

			//Send sync?
			if(NetworkInterface.Instance.IsServer)
			{
				UpdateTimelineSync();
			}
			else
			{
				AdjustTimelineSpeed();
			}
		}

		#endregion

		#region Location events

		private void OnTimelineSync(TimelineSync timelineSync)
		{
			if(timelineSync.ObjectId == ObjectId)
			{
				Debug.LogFormat("Recieved new timeline sync: state={0}, time={1}", ((PlayState)timelineSync.PlayState).ToString(), timelineSync.TimelineTime);

				LastTimelineSync = timelineSync;

				//Update timeline play state. This will trigger play and stop
				CurrentPlaystate = (PlayState)LastTimelineSync.PlayState;
			}
		}

		#endregion

		#region Internals

		/// <summary>
		/// Is called when the play state of the timeline changed.
		/// Server: Send sync update with the updated play state.
		/// Client: Change timeline playstate.
		/// </summary>
		/// <param name="newPlayState"></param>
		private void OnPlayStateChanging(PlayState newPlayState)
		{
			//Debug.LogFormat("Play state changing to : {0}", newPlayState.ToString());

			if (NetworkInterface.Instance.IsServer)
			{
				//Send sync event
				SendTimelineSync();
			}
			else //Client and observer
			{
				//Update play state
				if (newPlayState == PlayState.Paused)
				{
					TimelineDirector.Stop();
				}
				else
				{
					//On play set the server timeline time
					if(LastTimelineSync != null)
						TimelineDirector.time = LastTimelineSync.TimelineTime;
					TimelineDirector.Play();
				}
			}
		}


		private float lastSyncTime;
		/// <summary>
		/// Checks if a sync message has to be sent. 
		/// </summary>
		private void UpdateTimelineSync()
		{
			if(CurrentPlaystate == PlayState.Playing)
			{
				if (lastSyncTime == 0f || Time.realtimeSinceStartup > lastSyncTime + SyncFrequencySeconds)
				{
					SendTimelineSync();
				}
			}
		}

		/// <summary>
		/// Adjust speed of the timeline based on last sync
		/// </summary>
		private void AdjustTimelineSpeed()
		{
			var estimatedDelta = GetEstimatedServerDelta();

			//Adjust speed based on estimated offset
			var adjustedSpeedFactor = Mathf.Lerp(MaxSyncSpeedFactor * 0.1f, MaxSyncSpeedFactor, Math.Abs((float)estimatedDelta / MaxSyncSpeedRangeSeconds)); //Range between 10%-100% of max speed
			CurrentSpeedFactor = estimatedDelta > 0 ? 1f - adjustedSpeedFactor : 1f + adjustedSpeedFactor;

			//Debug
			if (TextEstimatedDelta)
				TextEstimatedDelta.text = string.Format("Estimated delta: {0:+0.00000;-0.00000}s, current speed: {1:+0.00000;-0.00000}", estimatedDelta, CurrentSpeedFactor);

			//PlayerController
			if (CurrentPlaystate == PlayState.Playing)
				TimelineDirector.time += Time.deltaTime * CurrentSpeedFactor;

			//Check for looping timelines
			if (TimelineDirector.time > TimelineDirector.duration && TimelineDirector.extrapolationMode == DirectorWrapMode.Loop)
			{
				TimelineDirector.time -=  TimelineDirector.duration;
			}
		}

		private void SendTimelineSync()
		{
			if (NetworkInterface.Instance.SessionId != Guid.Empty && !string.IsNullOrEmpty(ObjectId))
			{
				lastSyncTime = Time.realtimeSinceStartup;

				//Send sync message
				NetworkInterface.Instance.SendMessage(new TimelineSync
				{
					ObjectId = ObjectId,
					PlayState = (int)TimelineDirector.state,
					TimelineTime = TimelineDirector.time,
				});
			}
			else
			{
				Debug.LogWarningFormat("Cannot sync timeline. The NetworkSyncedTimeline does not have a valid ObjectId set. Timeline object: {0}", name);
			}
		}

		/// <summary>
		/// Calculates the estimated sync offset between this client and the last sync message received by the server.
		/// It also takes in account looping timelines. In that case it also finds the shorted sync delta passing through the timeline looping point.
		/// </summary>
		/// <returns></returns>
		private double GetEstimatedServerDelta()
		{
			if(LastTimelineSync != null)
			{
				//This assumes that the network sync event was not delayed. Replaced old calculation that was talking in account the time delay between
				//server and client, which assumed a very precise clock synchronization between all the components. 
				var timeSinceSync = DateTime.UtcNow - LastTimelineSync.ReceptionTime; // + (LastTimelineSync.SendTime - LastTimelineSync.ReceptionTime)
				var estimatedServerTime = LastTimelineSync.TimelineTime + timeSinceSync.TotalSeconds;

				//Check for estimated server loop
				if(estimatedServerTime > TimelineDirector.duration && TimelineDirector.extrapolationMode == DirectorWrapMode.Loop)
				{
					//Server looped
					estimatedServerTime -= TimelineDirector.duration;
				}

				var estimatedDelta = TimelineDirector.time - estimatedServerTime;

				//Check if theres a loop between server and client
				if(TimelineDirector.extrapolationMode == DirectorWrapMode.Loop)
				{
					var loopedDelta = TimelineDirector.duration - Mathf.Abs((float)estimatedDelta);
					if (loopedDelta < TimelineDirector.duration / 2f)
					{
						//Loop between server and client, reverse direction and take the shorter way
						return estimatedDelta > 0f ? -loopedDelta : loopedDelta;
					}
				}

				return TimelineDirector.time - estimatedServerTime;
			}
			return 0;
		}

		#endregion

	}

}