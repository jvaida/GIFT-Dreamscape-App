using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Config
{
    [Serializable]
    public class ThumbSetup : FingerSetup
    {
        public enum EThumbPose { Neutral, Abducted, Adducted }

        [Tooltip("Neutral thumb orientation.")]
        public Quaternion NeutralOrientation;

        [Tooltip("Abducted thumb orientation.")]
        public Quaternion AbductedOrientation;

        [Tooltip("Adducted thumb orientation.")]
        public Quaternion AdductedOrientation;

        public Quaternion GetPoseRotation(EThumbPose pose)
        {
            switch (pose)
            {
                case EThumbPose.Neutral: return NeutralOrientation;
                case EThumbPose.Abducted: return AbductedOrientation;
                case EThumbPose.Adducted: return AdductedOrientation;
                default: return Quaternion.identity;
            }
        }
    }
}