using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Config
{
    [CreateAssetMenu(fileName = "AvatarHandDefinition", menuName = "Artanim/Avatar Hand Definition", order = 1)]
    public class AvatarHandDefinition : ScriptableObject
    {
        public enum ESide { Left, Right, }

        //==============================================================================
        // Streaming and Smoothing
        //==============================================================================
        [Header("Streaming and Smoothing")]
        [Tooltip("Whether or not we want to smoothly interpolate between received datasets. Primarily useful at low data framerates (e.g. < 30)")]
        public bool InterpolateIfReceiver = true;
        [Tooltip("When enabled, new animation data will be partially applied to a hand to filter out high-frequency changes")]
        public bool ApplySmoothing = true;
        [Range(0.0f, 1.0f)]
        [Tooltip("If it's 0, new animation data is ignored, and if it's set to 1, new data is directly applied as is (giving the same result as one would get with Apply Smoothing disabled).")]
        public float SmoothingFactor = 0.5f;

        //==============================================================================
        // Solver / interpolation
        //==============================================================================
        [Header("Solver (Only when multiply solver methods are used)")]
        [Tooltip("Time (in seconds) to interpolate between two solving methods (e.g. procedural -> Leap Motion).")]
        public float StartInterpolationDuration = 1.0f;

        //==============================================================================
        // Reach
        //==============================================================================
        [Header("Reach")]
        [Tooltip("The Reach of a hand describes the length of the hand. It is used to determine what objects are within reach of the hand to be interacted with. Objects beyond this reach will be ignored.")]
        public float Reach = 0.15f;
        [Range(0f, 1f)]
        [Tooltip("Determines how the hand reaches for objects located on the vertical axis of the hand compared to the forward reaching. (1: vertical=Reach, <1: percentage of the forward reach)")]
        public float ReachStickiness = 0.4f;

        //==============================================================================
        // Forearm tracking specific
        //==============================================================================
        [Header("Forearm tracking specific")]
        [MinMaxRange(-160f, 160f)]
        [Tooltip("Wrist rotation angle range.")]
        public Vector2 WristRotationRange = new Vector2(-90f, 90f);
        [Tooltip("Rest position wrist rotation angle.")]
        public float RestRotation = 0f;
        [Tooltip("When enabled, hand twist rotation will be exaggerated to compensate for under rotation due to markers being placed on the forearm.")]
        public bool ExaggerateTwist;
        [Tooltip("Forward axis the twist exaggeration is applied to the wrist.")]
        public Vector3 TwistAxis = Vector3.up;
        [Tooltip("1: Leaves the twist unmodified, >1: Exaggerate the twist, <1: dampens the twist")]
        public float ExaggerationFactor = 1.2f;

        
        //==============================================================================
        // Specific hand and finger setup
        //==============================================================================
        [Header("Hand specific setup")]
        public HandSetup LeftHandSetup = new HandSetup();
        public HandSetup RightHandSetup = new HandSetup();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="side"></param>
		/// <returns></returns>
		public HandSetup GetHandSetup(ESide side)
        {
            switch (side)
            {
                case ESide.Left:
                    return LeftHandSetup;
                case ESide.Right:
                    return RightHandSetup;
                default: return null;
            }
        }

    }
}