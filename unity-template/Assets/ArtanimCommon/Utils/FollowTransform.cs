using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    [AddComponentMenu("Artanim/Follow Transform")]
    public class FollowTransform : MonoBehaviour {

        [Tooltip("The transform to follow")]
        public Transform target;

        [Tooltip("Whether or not to update the position of this game object to match the one of the target")]
        public bool UpdatePosition;

        [Tooltip("Whether or not to update the rotation of this game object to match the one of the target")]
        public bool UpdateRotation;

        [Tooltip("Whether or not to update the scale of this game object to match the one of the target")]
        public bool UpdateScale;

        // Update is called once per frame
        void Update()
        {
            // Update position if required
            if (target && UpdatePosition)
                transform.localPosition = target.localPosition;
            
            // Update rotation if required
            if (target && UpdateRotation)
                transform.localRotation = target.localRotation;

            // Update rotation if required
            if (target && UpdateScale)
                transform.localScale = target.localScale;
        }

    }
}
