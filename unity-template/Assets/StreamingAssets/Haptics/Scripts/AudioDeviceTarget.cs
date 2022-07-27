using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics
{
	[RequireComponent(typeof(BoxCollider))]
	public class AudioDeviceTarget : MonoBehaviour
	{
		public enum EBoundsReferential { World, Local, GlobalOffset }

		public bool IgnoreCollider;
		public EBoundsReferential BoundsReferential;

		// Note: we don't store a reference to BoxCollider so it's stays dynamic

		public bool HasCollider
        {
			get { return (!IgnoreCollider) && (GetComponent<BoxCollider>() != null); }
        }

		public Bounds Bounds
        {
			get
            {
				if (HasCollider)
				{
					var boxCollider = GetComponent<BoxCollider>();
					switch (BoundsReferential)
                    {
						default:
						case EBoundsReferential.World:
							return boxCollider.bounds;
						case EBoundsReferential.Local:
							return new Bounds(transform.parent.InverseTransformPoint(boxCollider.bounds.center), ComputeBoundsSize(boxCollider));
						case EBoundsReferential.GlobalOffset:
							return GlobalMocapOffset.Instance.HasOffset ? new Bounds(GlobalMocapOffset.Instance.UnOffsetPosition(boxCollider.bounds.center), ComputeBoundsSize(boxCollider)) : boxCollider.bounds;
					}
				}
				else
				{
					return new Bounds();
				}
			}
        }

        Vector3 ComputeBoundsSize(BoxCollider boxCollider)
        {
			var size = boxCollider.size;
			var scale = transform.lossyScale;
			size.x *= scale.x;
			size.y *= scale.y;
			size.z *= scale.z;
			return size;
		}
	}
}