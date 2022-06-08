using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Config
{
    [Serializable]
    public class HandSetup
    {
        [Header("Hand axis")]
        [Tooltip("Local hand rotation axis.")]
        public Vector3 RotationAxis = Vector3.right;
        [Tooltip("Local hand forward axis.")]
        public Vector3 HandForward = Vector3.up;
        [Tooltip("Local palm facing axis.")]
        public Vector3 PalmFacing = Vector3.forward;
        [Tooltip("Hand alignment axis.")]
        public Vector3 HandAlignmentAxis = Vector3.right;

        [MinMaxRange(-60f, 60f)]
        [Tooltip("Deviation angle range based on the hand forward axis.")]
        public Vector2 DeviationAngleRange = new Vector2(-25, 25f);

        [Header("Fingers")]
        public ThumbSetup Thumb = new ThumbSetup();
        public FingerSetup Index = new FingerSetup();
        public FingerSetup Middle = new FingerSetup();
        public FingerSetup Ring = new FingerSetup();
        public FingerSetup Pinky = new FingerSetup();

    }
}