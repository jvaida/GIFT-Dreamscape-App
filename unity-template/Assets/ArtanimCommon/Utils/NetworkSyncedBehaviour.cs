using UnityEngine;
using System.Collections;
using Artanim.Location.Network;

namespace Artanim
{

    /// <summary>
    /// Base class for all network synced objects
    /// </summary>
    public abstract class NetworkSyncedBehaviour : MonoBehaviour
	{
		public enum ESyncMode { ClientAndServer, Client, Server }

        /// <summary>
		/// ID this behavior reacts to.
		/// </summary>
        [ObjectId]
        [Tooltip("ID this behavior reacts to.")]
        public string ObjectId;

        [Tooltip("Mode defining on which component type the actions are triggered.")]
		public ESyncMode SyncMode = ESyncMode.ClientAndServer;

		protected bool NeedTrigger(string objectId)
		{
			if(ObjectId == objectId)
			{
				switch (SyncMode)
				{
					case ESyncMode.ClientAndServer:
						return true;
					case ESyncMode.Client:
						if (NetworkInterface.Instance.IsClient)
							return true;
						break;
					case ESyncMode.Server:
						if (NetworkInterface.Instance.IsServer)
							return true;
						break;
				}
			}
			
			return false;
		}

		protected bool ValidateObjectId()
		{
			if (string.IsNullOrEmpty(ObjectId))
			{
				Debug.LogWarningFormat("NetworkSyncedBehaviour does not have an ObjectId assigned! Assign an ObjectId value. NetworkSyncedBehaviour name={0}", name);
				return false;
			}
			else
			{
				return true;
			}
		}
	}

}