using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[AddComponentMenu("Artanim/Network Synced Transform")]
	public class NetworkSyncedTransform : MonoBehaviour
	{
		public enum ESyncSpace { Global, Local }

		[ObjectId]
		[Tooltip("ID of the synced transform. This value must be set and unique.")]
		public string ObjectId;

		[Tooltip("Specifies if this transform should be synced as soon as it is enabled.")]
		public bool RegisterOnEnable = true;

		[Tooltip("Space this transform should be synced. (Default is global)")]
		public ESyncSpace SyncSpace;

		[Tooltip("Specifies if the transforms position should be synced.")]
		public bool SyncPosition = false;

		[Tooltip("Specifies if the transforms rotation should be synced.")]
		public bool SyncRotation = false;

		[Tooltip("Specifies if the transforms scale should be synced.")]
		public bool SyncScale = false;

		[Tooltip("Specifies the owner that will control the transform (empty guid for server).")]
		public System.Guid Owner = System.Guid.Empty;

		/// <summary>
		/// Registers this transform to by synced.
		/// </summary>
		/// <returns>True if the registration was successful</returns>
		public bool Register()
		{
			if (!string.IsNullOrEmpty(ObjectId))
			{
				if (SyncedTransformController.Instance)
					return SyncedTransformController.Instance.RegisterTransform(this);
			}
			else
			{
				Debug.LogErrorFormat("NetworkSyncedTransform must have a valid ObjectId assigned. The GameObject will be disabled now. Name={0}", name);
				gameObject.SetActive(false);
			}
			return false;
		}

		/// <summary>
		/// Unregisters this transform from sync.
		/// </summary>
		/// <returns>True if the unregistration was successful</returns>
		public bool Unregister()
		{
			if (!string.IsNullOrEmpty(ObjectId) && SyncedTransformController.Instance && SyncedTransformController.Instance.IsRegistered(ObjectId))
			{
				return SyncedTransformController.Instance.UnregisterTransform(ObjectId);
			}
			return false;
		}


		private void Start()
		{
			if (RegisterOnEnable)
			{
				Register();
			}
		}

		private void OnDestroy()
		{
			Unregister();
		}

	}
}