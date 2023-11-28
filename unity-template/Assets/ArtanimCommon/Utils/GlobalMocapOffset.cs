using UnityEngine;
using System.Collections;

namespace Artanim
{

	public class GlobalMocapOffset : SingletonBehaviour<GlobalMocapOffset>
	{
		private Transform MocapOffsetTarget;

		public bool HasOffset
        {
			get { return MocapOffsetTarget != null; }
		}

		public Vector3 GlobalPositionOffset
		{
			get { return MocapOffsetTarget != null ? MocapOffsetTarget.position : Vector3.zero; }
		}

		public Quaternion GlobalRotationOffset
		{
			get { return MocapOffsetTarget != null ? MocapOffsetTarget.rotation : Quaternion.identity; }
		}

		public void OffsetTransform(Transform toOffset)
		{
			if(toOffset && MocapOffsetTarget)
			{
				var offsetMatrix = MocapOffsetTarget.localToWorldMatrix * toOffset.localToWorldMatrix;
				toOffset.position = new Vector3(offsetMatrix.m03, offsetMatrix.m13, offsetMatrix.m23);
			}
		}

		public Vector3 UnOffsetPosition(Vector3 worldPos)
		{
			return MocapOffsetTarget ? MocapOffsetTarget.transform.InverseTransformPoint(worldPos) : worldPos;
		}

		public Vector3 UnOffsetDirection(Vector3 direction)
		{
			return MocapOffsetTarget ? MocapOffsetTarget.transform.InverseTransformDirection(direction) : direction;
		}

		void LateUpdate()
		{
			//Global offset
			if (MocapOffsetTarget)
			{
				transform.position = MocapOffsetTarget.position;
				transform.rotation = MocapOffsetTarget.rotation;
			}
			else
			{
				transform.position = Vector3.zero;
				transform.rotation = Quaternion.identity;
			}
		}

		/// <summary>
		/// Registers a transform as global mocap offset
		/// </summary>
		/// <param name="offset"></param>
		public void RegisterGlobalMocapOffset(Transform offset)
		{
			MocapOffsetTarget = offset;
		}

		/// <summary>
		/// Unregisters a transform as global mocap offset.
		/// If the transform has not been registered before, the transform is not unregistered.
		/// </summary>
		/// <param name="offset"></param>
		public void UnRegisterGlobalMocapOffset(Transform offset)
		{
			if (MocapOffsetTarget == offset)
			{
				MocapOffsetTarget = null;
				transform.position = Vector3.zero;
				transform.rotation = Quaternion.identity;
			}
		}	
	}

}