using Artanim.Algebra;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{
	public class AvatarOffsetController : SingletonBehaviour<AvatarOffsetController>
	{
		public enum ESyncMode { Synced, Unsynced }

		#region Events

		public delegate void OnPlayerOffsetRegisteredHandler(Guid playerId, Transform registeredOffsetTransform, Transform avatarOffsetTransform, ESyncMode syncMode, Guid owner);
		public event OnPlayerOffsetRegisteredHandler OnPlayerOffsetRegistered;

		public delegate void OnPlayerOffsetUnregisteredHandler(Guid playerId, Transform registeredOffsetTransform, Transform avatarOffsetTransform, ESyncMode syncMode, Guid owner);
		public event OnPlayerOffsetUnregisteredHandler OnPlayerOffsetUnregistered;

		#endregion

		[ReadOnlyProperty]
		public int NumRegistrations;

		[ReadOnlyProperty] [SerializeField]
		private bool OwnsAnyTransform;

		private Dictionary<Guid, OffsetRegistration> RegisteredTransforms = new Dictionary<Guid, OffsetRegistration>();

		private DateTime timeRef = DateTime.UtcNow;

		private const float SYNCED_OFFSET_DELAY = 1.0f / 30.0f;
		//adjustment from time stamps received from server to current time on client
		private float timeOffset = 0.0f;
		private bool timeOffsetInited = false;

		/// <summary>
		/// Main players avatar offset or null of no offset is registered.
		/// </summary>
		public Transform MainPlayerOffset
		{
			get
			{
				Transform playerTransform = null;
				var currentPlayer = GameController.Instance.CurrentPlayer;
				if (currentPlayer != null)
				{
					playerTransform = currentPlayer.AvatarOffset;
				}
				return playerTransform;
			}
		}

		#region Unity events

		void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<AvatarOffsetsStateUpdate>(NetworkMessage_AvatarOffsetsStateUpdate);
			NetworkInterface.Instance.Subscribe<AvatarOffsetsUpdate>(NetworkMessage_SyncedAvatarOffsetsUpdate);
			NetworkInterface.Instance.Subscribe<ClientSyncedAvatarOffsetsUpdate>(NetworkMessage_ClientSyncedAvatarOffsetsUpdate);

			if (GameController.Instance)
				GameController.Instance.OnLeftSession += Instance_OnLeftSession;

			if (NetworkInterface.Instance.IsServer)
				GameController.Instance.OnSessionPlayerLeft += Instance_OnSessionPlayerLeft;
		}

		void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<AvatarOffsetsStateUpdate>(NetworkMessage_AvatarOffsetsStateUpdate);
			NetworkInterface.SafeUnsubscribe<AvatarOffsetsUpdate>(NetworkMessage_SyncedAvatarOffsetsUpdate);
			NetworkInterface.SafeUnsubscribe<ClientSyncedAvatarOffsetsUpdate>(NetworkMessage_ClientSyncedAvatarOffsetsUpdate);

			if (GameController.Instance)
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;

			if (NetworkInterface.Instance.IsServer)
				GameController.Instance.OnSessionPlayerLeft -= Instance_OnSessionPlayerLeft;
		}

		private List<AvatarTransform> AvatarOffsetsCache;
		private AvatarOffsetsUpdate AvatarOffsetsUpdateCache;
		private ClientSyncedAvatarOffsetsUpdate ClientSyncedAvatarOffsetsUpdateCache;

		void LateUpdate()
		{
#if EXP_PROFILING
			ExpProfiling.MarkAvOffStart();
#endif

			if (OwnsAnyTransform && RegisteredTransforms.Count > 0 && GameController.Instance.CurrentSession != null)
			{
				if (AvatarOffsetsCache == null)
                {
					AvatarOffsetsCache = new List<AvatarTransform>();
					if (NetworkInterface.Instance.IsServer)
					{
						AvatarOffsetsUpdateCache = new AvatarOffsetsUpdate { AvatarOffsets = AvatarOffsetsCache };
					}
					else if (NetworkInterface.Instance.IsTrueClient)
					{
						ClientSyncedAvatarOffsetsUpdateCache = new ClientSyncedAvatarOffsetsUpdate { AvatarOffsets = AvatarOffsetsCache };
					}
				}

				//Broadcast offset message
				Guid myId = SharedDataUtils.MySharedId;
				var invalidOffsets = new List<Guid>();

				foreach (var avatarOffset in RegisteredTransforms)
				{
					if (avatarOffset.Value.SourceId == myId && avatarOffset.Value.SyncMode == ESyncMode.Synced)
					{
						if(avatarOffset.Value.RegisteredOffsetTransform)
						{
							//Send the current transform of the registered offset. 
							AvatarOffsetsCache.Add(new AvatarTransform
							{
								PlayerId = avatarOffset.Key,
								Position = avatarOffset.Value.RegisteredOffsetTransform.localPosition.ToVect3f(),
								Orientation = avatarOffset.Value.RegisteredOffsetTransform.localRotation.ToQuatf(),
							});
						}
						else
						{
							invalidOffsets.Add(avatarOffset.Key);
						}
					}
				}

				if (AvatarOffsetsCache.Count > 0)
                {
					if (AvatarOffsetsUpdateCache != null)
                    {
						NetworkInterface.Instance.SendMessage(AvatarOffsetsUpdateCache);
					}
					else if (ClientSyncedAvatarOffsetsUpdateCache != null)
					{
						NetworkInterface.Instance.SendMessage(ClientSyncedAvatarOffsetsUpdateCache);
					}
					AvatarOffsetsCache.Clear();
				}

				//Clean invalid offsets
				if (invalidOffsets.Count > 0)
				{
					foreach(var invalidOffset in invalidOffsets)
					{
						Debug.LogWarningFormat("Unregistering invalid avatar offset for player={0}", invalidOffset);
						UnregisterAvatarOffset(invalidOffset);
					}
				}
			}

			//All components unsynced offsets or development modes where client is also server (driver of the offset) or if we're the source of offset
			if (RegisteredTransforms.Count > 0 && GameController.Instance.CurrentSession != null)
			{
				var myId = SharedDataUtils.MySharedId;
				foreach (var avatarOffset in RegisteredTransforms)
				{
                    //On client, interpolate from recent synced offset update
                    if(avatarOffset.Value.SyncMode == ESyncMode.Synced && avatarOffset.Value.SourceId != myId)
                    {
                        Vector3 pos;
                        Quaternion rot;
                        avatarOffset.Value.InterpolateTransform(Time.time - SYNCED_OFFSET_DELAY, out pos, out rot);
                        avatarOffset.Value.RegisteredOffsetTransform.localPosition = pos;
                        avatarOffset.Value.RegisteredOffsetTransform.localRotation = rot;
                    }
					//move the actual avatar offset transform
				    UpdateAvatarOffsetTransform(avatarOffset.Value);
				}
			}

#if EXP_PROFILING
			ExpProfiling.MarkAvOffEnd();
#endif
		}

		#endregion

		#region Public interface

		/// <summary>
		/// Registers an offset transform for an avatar.
		/// This method can now be called either from the server or the client (but not both for a same avatar!)
		/// </summary>
		/// <param name="playerId">Player id to add the offset</param>
		/// <param name="avatarOffset">Offset that the player will follow</param>
		/// <param name="moveAvatarToTransform">Whether or not to move the player on the avatar offset</param>
		/// <param name="syncMode">Whether or not to sync between server and clients</param>
		/// <param name="allowClientRegistration">Whether or not to do something when called from a client</param>
		public void RegisterAvatarOffset(Guid playerId, AvatarOffset avatarOffset, bool moveAvatarToTransform = false, ESyncMode syncMode = ESyncMode.Synced, bool allowClientRegistration = false)
		{
			if (allowClientRegistration || NetworkInterface.Instance.IsServer)
			{
				if (playerId != Guid.Empty)
				{
					if (avatarOffset != null && !string.IsNullOrEmpty(avatarOffset.ObjectId))
					{
						//Start registration
						var player = GameController.Instance.GetPlayerByPlayerId(playerId);
						if (player != null)
						{
							Debug.LogFormat("Sending avatar offset registration for: PlayerId={0}, OffsetId={1}, Position={2}, Orientation={3}", playerId, avatarOffset.ObjectId, avatarOffset.transform.localPosition, avatarOffset.transform.localRotation.eulerAngles);

							//Send registration update
							NetworkInterface.Instance.SendMessage(new AvatarOffsetsStateUpdate
							{
								Action = AvatarOffsetsStateUpdate.EAction.Register,
								SyncMode = (int)syncMode,
								OffsetObjectId = avatarOffset.ObjectId,
								MoveToTransform = moveAvatarToTransform,
								InitialTransform = new AvatarTransform
								{
									PlayerId = playerId,
									Position = avatarOffset.transform.localPosition.ToVect3f(),
									Orientation = avatarOffset.transform.localRotation.ToQuatf(),
								},
							});
						}
						else
						{
							Debug.LogErrorFormat("Failed to register avatar offset for playerId={0}, OffsetId={1}. Player was not found.", playerId, avatarOffset.ObjectId);
						}
					}
					else
					{
						Debug.LogErrorFormat("Cannot register avatar offset for playerId={0}. AvatarOffset is invalid.", playerId);
					}
				}
				else
				{
					Debug.LogError("Cannot register avatar offset. PlayerId is empty.");
				}
			}
		}

		/// <summary>
		/// Unregisters an offset transform for a avatar.
		/// </summary>
		/// <param name="avatarOffset">Avatar offset to remove</param>
		/// <param name="resetToZero">Reset the player to zero offset position and rotation</param>
		public void UnregisterAvatarOffset(AvatarOffset avatarOffset, bool resetToZero = true)
		{
			var registeredTransform = RegisteredTransforms.FirstOrDefault(r => r.Value.RegisteredOffsetTransform == avatarOffset.transform);
			if(registeredTransform.Key != Guid.Empty)
			{
				UnregisterAvatarOffset(registeredTransform.Key, resetToZero);
			}
		}


		/// <summary>
		/// Unregisters an offset transform for a avatar.
		/// </summary>
		/// <param name="playerId">Player id to remove the offset</param>
		/// <param name="resetToZero">Reset the player to zero offset position and rotation</param>
		public void UnregisterAvatarOffset(Guid playerId, bool resetToZero = true)
		{
			if (playerId != Guid.Empty && RegisteredTransforms.ContainsKey(playerId))
			{
				Debug.LogFormat("Unregistering avatar offset for player: {0}", playerId.ToString());
				
				var offsetTransform = RegisteredTransforms[playerId];
				if(offsetTransform != null && offsetTransform.AvatarOffsetTransform) //The transform can be invalid when if not properly unregistered by the experience
				{
					//Reset transform to origin?
					if(resetToZero && offsetTransform.AvatarOffsetTransform)
					{
						offsetTransform.AvatarOffsetTransform.localPosition = Vector3.zero;
						offsetTransform.AvatarOffsetTransform.localRotation = Quaternion.identity;
					}
				}

				//Notify others
				if(NetworkInterface.Instance.SessionId != Guid.Empty)
				{
					NetworkInterface.Instance.SendMessage(new AvatarOffsetsStateUpdate
					{
						Action = AvatarOffsetsStateUpdate.EAction.Unregister,
						InitialTransform = new AvatarTransform
						{
							PlayerId = playerId,
							Position = offsetTransform.AvatarOffsetTransform ? offsetTransform.AvatarOffsetTransform.localPosition.ToVect3f() : Vect3f.Zero,
							Orientation = offsetTransform.AvatarOffsetTransform ? offsetTransform.AvatarOffsetTransform.localRotation.ToQuatf() : Quatf.Identity,
						},
					});
				}
			}
		}

		/// <summary>
		/// Removes all avatar offsets.
		/// </summary>
		public void UnregisterAllAvatarOffsets(bool resetToZero = true)
		{
			foreach (var avatarOffsetKey in RegisteredTransforms.Keys.ToArray())
			{
				UnregisterAvatarOffset(avatarOffsetKey, resetToZero);
			}
		}

		/// <summary>
		/// Returns whether or not the player with the given player id has an avatar offset
		/// </summary>
		/// <param name="playerId">The id of the player to check</param>
		/// <returns>Whether or not player has an avatar offset</returns>
		public bool IsPlayerRegistered(Guid playerId)
		{
			return RegisteredTransforms.ContainsKey(playerId);
		}

		/// <summary>
		/// Returns the transform of the AvatarOffset assigned to the player with the given id, or null if none
		/// </summary>
		/// <param name="playerId">The id of the player</param>
		/// <returns>The player AvatarOffset transform</returns>
		public Transform GetPlayerAvatarOffset(Guid playerId)
		{
			OffsetRegistration offsetRegistration = null;
			if (RegisteredTransforms.TryGetValue(playerId, out offsetRegistration))
			{
				return offsetRegistration.AvatarOffsetTransform;
			}
			return null;
		}

		/// <summary>
		/// Returns the registered avatar offset transform used to register an offset (with AvatarOffset), or null if none
		/// </summary>
		/// <param name="playerId">The id of the player</param>
		/// <returns>The registered player AvatarOffset transform</returns>
		public Transform GetPlayerRegisteredAvatarOffset(Guid playerId)
        {
			OffsetRegistration offsetRegistration = null;
			if (RegisteredTransforms.TryGetValue(playerId, out offsetRegistration))
			{
				return offsetRegistration.RegisteredOffsetTransform;
			}
			return null;
		}

		#endregion

		#region Network events

		private void NetworkMessage_AvatarOffsetsStateUpdate(AvatarOffsetsStateUpdate args)
		{
			//Register offset
			if (args.Action == AvatarOffsetsStateUpdate.EAction.Register)
			{
                var player = GameController.Instance.GetPlayerByPlayerId(args.InitialTransform.PlayerId);
                if(player != null)
                {
					//Remove existing offset
					if (RegisteredTransforms.ContainsKey(player.Player.ComponentId))
					{
						RegisteredTransforms.Remove(player.Player.ComponentId);
						RegisteredTransformsUpdated();
					}

                    //Search local AvatarOffset with corresponding ID
                    var registeredOffset = FindObjectsOfType<AvatarOffset>().FirstOrDefault(o => o.ObjectId == args.OffsetObjectId);
                    if (registeredOffset)
                    {
                        Debug.LogFormat("Registering avatar offset: PlayerId={0}, OffsetId={1}, SourceId={2}, SyncMode={3}, MoveToTransform={4}",
							player.Player.ComponentId, registeredOffset.ObjectId, args.SenderId, args.SyncMode, args.MoveToTransform);

                        //Set the registered avatar offset to the initial position if its synced
                        if ((ESyncMode)args.SyncMode == ESyncMode.Synced)
                        {
                            registeredOffset.transform.localPosition = args.InitialTransform.Position.ToUnity();
                            registeredOffset.transform.localRotation = args.InitialTransform.Orientation.ToUnity();
                        }

                        //Do we need to move the avatar to the registered transform?
                        if (args.MoveToTransform)
                        {
                            player.AvatarOffset.position = registeredOffset.transform.position;
                            player.AvatarOffset.rotation = registeredOffset.transform.rotation;
                        }

						//Create registration
						var offsetRegistration = new OffsetRegistration
						{
							SyncMode = (ESyncMode)args.SyncMode,
							SourceId = args.SenderId,
							MoveToTransform = args.MoveToTransform,
							
                            RegisteredOffsetTransform = registeredOffset.transform,
                            AvatarOffsetTransform = player.AvatarOffset,

                            StartParentPosition = registeredOffset.transform.position,
                            StartParentRotation = registeredOffset.transform.rotation,

                            StartAvatarPosition = player.PlayerInstance.transform.position,
                            StartAvatarRotation = player.PlayerInstance.transform.rotation,
                        };

						Debug.LogFormat("OffsetRegistration: RegisteredOffsetTransform={0}, AvatarOffsetTransform={1}, StartParentPosition={2}, StartParentRotation={3}, StartAvatarPosition={4}, StartAvatarRotation={5}",
							offsetRegistration.RegisteredOffsetTransform, offsetRegistration.AvatarOffsetTransform, offsetRegistration.StartParentPosition, offsetRegistration.StartParentRotation, offsetRegistration.StartAvatarPosition, offsetRegistration.StartAvatarRotation);

                        //Register new offset.
                        RegisteredTransforms.Add(player.Player.ComponentId, offsetRegistration);
						RegisteredTransformsUpdated();
					}
					else
                    {
                        Debug.LogErrorFormat("Failed to register AvatarOffset. AvatarOffset not found with ID: {0}", args.OffsetObjectId);
                    }

                    //Notify event
                    if (OnPlayerOffsetRegistered != null)
                        OnPlayerOffsetRegistered(player.Player.ComponentId, registeredOffset.transform, player.AvatarOffset, (ESyncMode)args.SyncMode, args.SenderId);
                }
                else
                {
                    Debug.LogErrorFormat("Failed to register avatar offset. Unable to find avatar with playerId={0}.", args.InitialTransform.PlayerId);
                }
            }

			//Unregister offset
			else if (args.Action == AvatarOffsetsStateUpdate.EAction.Unregister)
			{
				//This is the case when running in developer mode client/server
				//In this case we want the client to run as server and bypass the state updates
				//to avoid clearing the server registrations
				OffsetRegistration offsetRegistration = null;
				if(RegisteredTransforms.TryGetValue(args.InitialTransform.PlayerId, out offsetRegistration))
				{
					Debug.LogFormat("AvatarOffset status update: Unregister player {0}", args.InitialTransform.PlayerId);

					//Remove registration
					RegisteredTransforms.Remove(args.InitialTransform.PlayerId);
					RegisteredTransformsUpdated();
						
					//When synced, reset to given position and rotation. Depending on unregistration this will leave the avatar at the current position or reset to zero.
					//When position is zero reset to avatar offset locally even when unsynced.
					var resetPosition = args.InitialTransform.Position.ToUnity();
					var resetRotation = args.InitialTransform.Orientation.ToUnity();
					if (offsetRegistration.SyncMode == ESyncMode.Synced || resetPosition == Vector3.zero)
					{
                        var player = GameController.Instance.GetPlayerByPlayerId(args.InitialTransform.PlayerId);
                        if(player != null)
                        {
                            player.AvatarOffset.localPosition = resetPosition;
                            player.AvatarOffset.localRotation = resetRotation;
                        }
                    }

					//Notify event
					if (OnPlayerOffsetUnregistered != null)
						OnPlayerOffsetUnregistered(
							args.InitialTransform.PlayerId,
							offsetRegistration.RegisteredOffsetTransform,
							offsetRegistration.AvatarOffsetTransform,
							offsetRegistration.SyncMode,
							offsetRegistration.SourceId);
				}
			}
		}

		private void NetworkMessage_SyncedAvatarOffsetsUpdate(AvatarOffsetsUpdate args)
		{
			if (args.SenderId != SharedDataUtils.MySharedId)
			{
				RecordTransforms(args.AvatarOffsets, args.ReceptionTime);
			}
		}

		private void NetworkMessage_ClientSyncedAvatarOffsetsUpdate(ClientSyncedAvatarOffsetsUpdate args)
        {
			if (args.SenderId != SharedDataUtils.MySharedId)
			{
				RecordTransforms(args.AvatarOffsets, args.ReceptionTime);
			}
		}

		private void RecordTransforms(IEnumerable<AvatarTransform> avatarOffsets, DateTime receptionTime)
		{
			//TODO would probably be better to use args.SendTime here since that would eliminate any discontinuities due to network latency
			//     this may introduce a problem  with clock skew between client and server though
			float rawTimeStamp = (float)(receptionTime - timeRef).TotalSeconds;

			if (!timeOffsetInited)  //TODO probably need to do more to account for clock skew
			{
				timeOffset = rawTimeStamp - Time.time;
				timeOffsetInited = true;
			}

			float adjustedTimeStamp = rawTimeStamp - timeOffset;

			//Update avatar offsets
			foreach (var avatarOffset in avatarOffsets)
			{
				OffsetRegistration registeredOffset;
				if (RegisteredTransforms.TryGetValue(avatarOffset.PlayerId, out registeredOffset))
				{
					//records current transform along with time it was received
					registeredOffset.ReceiveSyncedUpdate(
						avatarOffset.Position.ToUnity(),
						avatarOffset.Orientation.ToUnity(),
						adjustedTimeStamp);
				}
			}
		}

		private void UpdateAvatarOffsetTransform(OffsetRegistration offsetRegistration)
		{
			if(offsetRegistration.RegisteredOffsetTransform && offsetRegistration.AvatarOffsetTransform)
			{
				if (offsetRegistration.MoveToTransform)
				{
					//Directly move offset to transform
					offsetRegistration.AvatarOffsetTransform.position = offsetRegistration.RegisteredOffsetTransform.position;
					offsetRegistration.AvatarOffsetTransform.rotation = offsetRegistration.RegisteredOffsetTransform.rotation;
				}
				else
				{
					//Calculate the position and rotation the avatar as if it was child of the registered offset
					var parentMatrix = Matrix4x4.TRS(
							offsetRegistration.RegisteredOffsetTransform.position,
							offsetRegistration.RegisteredOffsetTransform.rotation * Quaternion.Inverse(offsetRegistration.StartParentRotation),
							offsetRegistration.RegisteredOffsetTransform.lossyScale);

					offsetRegistration.AvatarOffsetTransform.position = parentMatrix.MultiplyPoint3x4(offsetRegistration.StartAvatarPosition - offsetRegistration.StartParentPosition);
					offsetRegistration.AvatarOffsetTransform.rotation = (offsetRegistration.RegisteredOffsetTransform.rotation * Quaternion.Inverse(offsetRegistration.StartParentRotation)) * offsetRegistration.StartAvatarRotation;
				}
            }
		}

		private void Instance_OnLeftSession()
		{
            //We left session, clear all registrations now!
            //-> If we're server do the same. Since the session is cleared all session members will clear the registrations too.
            foreach (var key in RegisteredTransforms.Keys.ToArray())
            {
                //Notify event
                if (OnPlayerOffsetUnregistered != null)
                    OnPlayerOffsetUnregistered(
                        key,
                        RegisteredTransforms[key] != null ? RegisteredTransforms[key].RegisteredOffsetTransform : null,
                        RegisteredTransforms[key] != null ? RegisteredTransforms[key].AvatarOffsetTransform : null,
                        RegisteredTransforms[key] != null ? RegisteredTransforms[key].SyncMode : ESyncMode.Synced,
                        RegisteredTransforms[key] != null ? RegisteredTransforms[key].SourceId : Guid.Empty);
            }
            RegisteredTransforms.Clear();
            RegisteredTransformsUpdated();
        }

        private void Instance_OnSessionPlayerLeft(Location.Data.Session session, Guid playerId)
        {
            //Only server, unregister player that left
            UnregisterAvatarOffset(playerId);
        }

		#endregion

		#region Internal Methods

		private void RegisteredTransformsUpdated()
		{
			var myId = SharedDataUtils.MySharedId;
			NumRegistrations = RegisteredTransforms.Count();
			OwnsAnyTransform = RegisteredTransforms.Values.Any(offReg => offReg.SourceId == myId);
		}

		#endregion

		#region Internal Classes

		private class OffsetRegistration
		{
            public ESyncMode SyncMode { get; set; }
			public Guid SourceId { get; set; }
			public bool MoveToTransform { get; set; }

			public Transform RegisteredOffsetTransform { get; set; }
			public Transform AvatarOffsetTransform { get; set; }

			//Position and orientation of the parent at registration time
			public Vector3 StartParentPosition { get; set; }
			public Quaternion StartParentRotation { get; set; }

			//Position and orientation of the avatar at registration time
			public Vector3 StartAvatarPosition { get; set; }
			public Quaternion StartAvatarRotation { get; set; }

            //cache of recent synced update values to interpolate between
            private const int NUM_UPDATES_TO_CACHE = 90;
            private Vector3[] positionUpdates = new Vector3[NUM_UPDATES_TO_CACHE];
            private Quaternion[] rotationUpdates = new Quaternion[NUM_UPDATES_TO_CACHE];
            private float[] updateTimeStamps = new float[NUM_UPDATES_TO_CACHE];
            public void InitCache()
            {
                for(int i = 0; i < NUM_UPDATES_TO_CACHE; i++)
                {
                    positionUpdates[i] = StartParentPosition;
                    rotationUpdates[i] = StartParentRotation;
                    updateTimeStamps[i] = Time.time;
                }
            }

            public void ReceiveSyncedUpdate(Vector3 position, Quaternion rotation, float timeStamp)
            {
                if(timeStamp <= updateTimeStamps[0])
                {
                    return;
                }
                for(int  i = NUM_UPDATES_TO_CACHE - 1; i > 0; i--)
                {
                    positionUpdates[i] = positionUpdates[i - 1];
                    rotationUpdates[i] = rotationUpdates[i - 1];
                    updateTimeStamps[i] = updateTimeStamps[i - 1];
                }

                positionUpdates[0] = position;
                rotationUpdates[0] = rotation;
                updateTimeStamps[0] = timeStamp;
            }

            public void InterpolateTransform(float time, out Vector3 position, out Quaternion rotation)
            {
                int i = 0;

                //request time is before the oldest update we have cached - just return the oldest update
                if (time < updateTimeStamps[NUM_UPDATES_TO_CACHE - 1])
                {
                    position = positionUpdates[NUM_UPDATES_TO_CACHE - 1];
                    rotation = rotationUpdates[NUM_UPDATES_TO_CACHE - 1];
                    Debug.LogWarning("AvatarOffsetController: NUM_UPDATES_TO_CACHE is too small. This may cause jittery motion.");
					Debug.LogWarningFormat("Request time '{0}', oldest is '{1}'", time, updateTimeStamps[NUM_UPDATES_TO_CACHE - 1]);
                }
                else
                {
                    if( time > updateTimeStamps[0]) //request time is later than the newest update received - should probably never happen
                    {
                        Debug.LogWarning("AvatarOffsetController: SYNCED_OFFSET_DELAY is too small. This may cause jittery motion.");
                    }
                    while (i < NUM_UPDATES_TO_CACHE - 1 && time < updateTimeStamps[i + 1])
                    {
                        i++;
                    }

                    float t = (time - updateTimeStamps[i + 1]) / (updateTimeStamps[i] - updateTimeStamps[i + 1]);

                    position = Vector3.Lerp(positionUpdates[i + 1], positionUpdates[i], t);
                    rotation = Quaternion.Slerp(rotationUpdates[i + 1], rotationUpdates[i], t);
                }


            }
		}

		#endregion 
	}

}