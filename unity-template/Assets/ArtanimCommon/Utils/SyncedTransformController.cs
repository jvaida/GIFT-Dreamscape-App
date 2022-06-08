using Artanim.Algebra;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{
    public class SyncedTransformController : SingletonBehaviour<SyncedTransformController>
	{
		#region Events

		public delegate void OnTransformRegisteredHandler(NetworkSyncedTransform syncedTransform);
		public event OnTransformRegisteredHandler OnTransformRegistered;

		public delegate void OnTransformUnregisteredHandler(NetworkSyncedTransform syncedTransform);
		public event OnTransformUnregisteredHandler OnTransformUnregistered;

		#endregion

		private int MessageMaxSize;
		private Dictionary<string, NetworkSyncedTransform> SyncedTransforms = new Dictionary<string, NetworkSyncedTransform>();
		private bool OwnsAnyTransform;

		public NetworkSyncedTransform[] RegisteredTransforms
		{
			get
			{
				return SyncedTransforms.Values.ToArray();
			}
		}

		#region Public Interface

		/// <summary>
		/// Register or update a synced transform
		/// </summary>
		/// <param name="objectId">ObjectId of the synced transform. This value cannot be null.</param>
		/// <param name="transform">Transform to register.</param>
		/// <param name="player">The player (or really, its client process) that will control this transform, or null if it's the server.</param>
		/// <returns>True if registration was successful</returns>
		public bool RegisterTransform(NetworkSyncedTransform syncedTransform, RuntimePlayer player = null)
		{
			if ((player != null) && (player.Player != null))
			{
				syncedTransform.Owner = player.Player.ComponentId;
			}

			if (AddOrUpdateTransform(syncedTransform))
			{
				Debug.LogFormat("Registered synced transform. ObjectId={0}, Transform={1}", syncedTransform.ObjectId, syncedTransform.name);

				//Notify
				if (OnTransformRegistered != null)
					OnTransformRegistered(syncedTransform);

				return true;
			}
			return false;
		}

		/// <summary>
		/// Unregisters a synced transform
		/// </summary>
		/// <param name="objectId">ObjectId of the synced transform to be unregistered. This value cannot be null.</param>
		/// <returns>True if the unregistration was successful</returns>
		public bool UnregisterTransform(string objectId)
		{
			var removedTransform = RemoveTransform(objectId);
			if (removedTransform)
			{
				Debug.LogFormat("Unregistered or updated synced transform. ObjectId={0}, Transform={1}", objectId, removedTransform ? removedTransform.name : "<null>");

				//Notify
				if (OnTransformUnregistered != null)
					OnTransformUnregistered(removedTransform);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Unregisters all registered synced transforms 
		/// </summary>
		public void UnregisterAll()
		{
			foreach(var syncedTransform in SyncedTransforms.ToArray())
			{
				UnregisterTransform(syncedTransform.Key);
			}
		}

		/// <summary>
		/// Checks if the given objectId is already registered.
		/// </summary>
		/// <param name="objectId"></param>
		/// <returns>True if the objectId is registered as synced transform</returns>
		public bool IsRegistered(string objectId)
		{
			return SyncedTransforms.ContainsKey(objectId);
		}

		#endregion

		#region Unity Events

		private void OnEnable()
		{
			NetworkInterface.Instance.Subscribe<SyncedTransforms>(OnSyncedTransforms);
			NetworkInterface.Instance.Subscribe<ClientSyncedTransforms>(OnClientSyncedTransforms);
		}

		private void OnDisable()
		{
			NetworkInterface.SafeUnsubscribe<SyncedTransforms>(OnSyncedTransforms);
			NetworkInterface.SafeUnsubscribe<ClientSyncedTransforms>(OnClientSyncedTransforms);
		}

		private List<SyncedTransform> SyncedTransformsListCache;
		private SyncedTransforms SyncedTransformsCache;
		private ClientSyncedTransforms ClientSyncedTransformsCache;

		private void LateUpdate()
		{
			if(GameController.Instance.CurrentSession != null && OwnsAnyTransform)
			{
				bool isServer = NetworkInterface.Instance.IsServer;
				var myId = SharedDataUtils.MySharedId;
				if (SyncedTransformsListCache == null)
				{
					// Init list capacity with a reasonable size
					MessageMaxSize = (int)NetworkInterface.Instance.MessageMaxSize;
					int approximateSizeOfSyncedTransform = 16 /*ObjectId*/ + 7 * sizeof(float) /*pos + rot*/; // Assume no scaling
					SyncedTransformsListCache = new List<SyncedTransform>(MessageMaxSize / approximateSizeOfSyncedTransform);

					// And create sync message
					if (isServer)
                    {
                        SyncedTransformsCache = new SyncedTransforms { Transforms = SyncedTransformsListCache };
					}
					else if (NetworkInterface.Instance.IsTrueClient)
					{
                        ClientSyncedTransformsCache = new ClientSyncedTransforms { Transforms = SyncedTransformsListCache };
					}
					else
                    {
						Debug.LogError("Experience that's neither a client or a server is trying to send a SyncedTransform");
                    }
				}

				int msgSize = 0;
				var transfDst = new SyncedTransform();
				SyncedTransformsListCache.Clear();

				//Collect synced transforms
				foreach (var kv in SyncedTransforms)
				{
					var syncedTransf = kv.Value;

					if(syncedTransf && ((isServer && syncedTransf.Owner == System.Guid.Empty) || syncedTransf.Owner == myId)
						&& (syncedTransf.SyncPosition || syncedTransf.SyncRotation || syncedTransf.SyncScale))
					{
						bool doSend = false;
						var props = SyncedTransform.Properties.None;
						bool globalSpace = syncedTransf.SyncSpace == NetworkSyncedTransform.ESyncSpace.Global;
						var transSrc = syncedTransf.transform;

						//Position?
						if(syncedTransf.SyncPosition)
                        {
							transfDst.Position = globalSpace ? transSrc.position.ToVect3f() : transSrc.localPosition.ToVect3f();
							props |= SyncedTransform.Properties.Position;
							msgSize += 3 * sizeof(float);
							doSend = true;
						}

						//Rotation?
						if (syncedTransf.SyncRotation)
                        {
							transfDst.Rotation = globalSpace ? transSrc.rotation.ToQuatf() : transSrc.localRotation.ToQuatf();
							props |= SyncedTransform.Properties.Rotation;
							msgSize += 4 * sizeof(float);
							doSend = true;
						}

						//Scale?
						if(syncedTransf.SyncScale)
                        {
							transfDst.Scale = transSrc.localScale.ToVect3f();
							props|= SyncedTransform.Properties.Scale;
							msgSize += 3 * sizeof(float);
							doSend = true;
						}

						if(doSend)
                        {
							transfDst.ObjectId = syncedTransf.ObjectId;
							transfDst.ValidProperties = props;
							SyncedTransformsListCache.Add(transfDst);

							msgSize += transfDst.ObjectId.Length + 2; // Include null terminator and 1 extra byte for the enum

							//Send update when max message is about to be reached
							if(msgSize >= MessageMaxSize - 100)
                            {
								SendSyncTransforms();
								SyncedTransformsListCache.Clear();
								msgSize = 0;
							}
						}
					}
				}

				//Send update
				if (SyncedTransformsListCache.Count > 0)
				{
					SendSyncTransforms();
					SyncedTransformsListCache.Clear();
				}
			}
		}

        private void SendSyncTransforms()
        {
			if (SyncedTransformsCache != null)
			{
				NetworkInterface.Instance.SendMessage(SyncedTransformsCache);
			}
			else if (ClientSyncedTransformsCache != null)
			{
				NetworkInterface.Instance.SendMessage(ClientSyncedTransformsCache);
			}
		}

		#endregion

		#region Location Events

		private void OnSyncedTransforms(SyncedTransforms syncedTransforms)
		{
			if (!NetworkInterface.Instance.IsServer)
            {
                SyncTransforms(syncedTransforms.Transforms);
			}
        }

		private void OnClientSyncedTransforms(ClientSyncedTransforms syncedTransforms)
		{
			if (syncedTransforms.SenderId != SharedDataUtils.MySharedId)
			{
				SyncTransforms(syncedTransforms.Transforms);
			}
		}

		private void SyncTransforms(List<SyncedTransform> transforms)
        {
            if (transforms != null)
            {
                for (int i = 0, iMax = transforms.Count; i < iMax; ++i)
                {
                    var transfUpdate = transforms[i];

                    NetworkSyncedTransform localTransform;
                    if (SyncedTransforms.TryGetValue(transfUpdate.ObjectId, out localTransform))
                    {
                        bool globalSpace = localTransform.SyncSpace == NetworkSyncedTransform.ESyncSpace.Global;

						//Apply position
						if (transfUpdate.HasPosition)
                        {
                            if (globalSpace)
                                localTransform.transform.position = transfUpdate.Position.ToUnity();
                            else
                                localTransform.transform.localPosition = transfUpdate.Position.ToUnity();
                        }

                        //Apply rotation
                        if (transfUpdate.HasRotation)
                        {
                            if (globalSpace)
                                localTransform.transform.rotation = transfUpdate.Rotation.ToUnity();
                            else
                                localTransform.transform.localRotation = transfUpdate.Rotation.ToUnity();
                        }

                        //Apply scale
                        if (transfUpdate.HasScale)
                        {
                            localTransform.transform.localScale = transfUpdate.Scale.ToUnity();
                        }
                    }
                }
            }
        }

		#endregion

		#region Internals

		private bool AddOrUpdateTransform(NetworkSyncedTransform syncedTransform)
		{
			if(syncedTransform && !string.IsNullOrEmpty(syncedTransform.ObjectId))
			{
				if(transform)
				{
					if(!SyncedTransforms.ContainsKey(syncedTransform.ObjectId))
					{
						SyncedTransforms.Add(syncedTransform.ObjectId, syncedTransform);
						SyncedTransformsUpdated();
					}
					else
					{
						Debug.LogFormat("<color=red>Replaced registered synced transform for ObjectId={0}. Check the ObjectId's in the experience if you didn't wanted to replace it.</color>", syncedTransform.ObjectId);
						SyncedTransforms[syncedTransform.ObjectId] = syncedTransform;
					}
					return true;

				}
				else
				{
					Debug.LogWarning("Cannot register transform without a valid transform assigned.");
					return false;
				}
			}
			else
			{
				Debug.LogWarning("Cannot register transform without a valid ObjectId assigned.");
				return false;
			}
		}

		private NetworkSyncedTransform RemoveTransform(string objectId)
		{
			if(!string.IsNullOrEmpty(objectId))
			{
				if(SyncedTransforms.ContainsKey(objectId))
				{
					var syncedTransform = SyncedTransforms[objectId];
					SyncedTransforms.Remove(objectId);
					SyncedTransformsUpdated();
					return syncedTransform;
				}
				else
				{
					Debug.LogWarningFormat("Cannot unregister transform. Transform with ObjectId={0} was not registered.", objectId);
					return null;
				}
			}
			else
			{
				Debug.LogWarning("Cannot unregister transform without a valid ObjectId assigned.");
				return null;
			}
		}
		
		private void SyncedTransformsUpdated()
        {
			bool isServer = NetworkInterface.Instance.IsServer;
			var myId = SharedDataUtils.MySharedId;
			OwnsAnyTransform = SyncedTransforms.Values.Any(syncTransf => (isServer && syncTransf.Owner == System.Guid.Empty) || (syncTransf.Owner == myId));
		}

		#endregion
	}

}