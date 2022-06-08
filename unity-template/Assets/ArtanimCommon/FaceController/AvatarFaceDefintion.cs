using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Artanim
{
    [CreateAssetMenu(fileName = "CharacterFaceDefintion", menuName = "Artanim/Avatar Face Definition", order = 1)]
    public class AvatarFaceDefintion : ScriptableObject
    {
        //==========================================================================================
        [Header("Jaw Settings")]
        //==========================================================================================
        [Tooltip("Local jaw axis used to open/close the jaw.")]
        public Vector3 JawOpenAxis;
        [Tooltip("Local jaw axis used to move the jaw left/right.")]
        public Vector3 JawLeftRightAxis;

        [Range(0f, 60f)]
        [Tooltip("Max. jaw open angle. [euler]")]
        public float JawMaxOpenAngle = 30f;

        [Range(0f, 30f)]
        [Tooltip("Max. jaw left/right angle. [euler]")]
        public float JawMaxLeftRightAngle = 15f;

        //==========================================================================================
        [Header("Eye Settings")]
        //==========================================================================================

        [Tooltip("Local eye forward vector.")]
        public Vector3 EyeForward = Vector3.forward;

        [Range(0f, 75f)]
        [Tooltip("Defines the max. horizontal angle the eyes can move. Also used for hotspots. If a hotspot is not within this viewangle, the hotspot is ignored.")]
        public float HorizontalAngle = 40f;

        [Range(0f, 45f)]
        [Tooltip("Defines the max. vertical angle the eyes can move. Also used for hotspots. If a hotspot is not within this viewangle, the hotspot is ignored.")]
        public float VerticalAngle = 10f;

        [Range(1f, 20f)]
        [Tooltip("Eye movement speed. A high value will make the eyes move quicker.")]
        public float EyeMotionSpeed = 10f;

        //==========================================================================================
        [Header("Random Eye Motion")]
        //==========================================================================================
        [Tooltip("Enable random eye movements.")]
        public bool EnableRandomEyeMotion = true;

        [Tooltip("Random time range between random eye motions. [seconds]")]
        [MinMaxRange(0.1f, 30f)]
        public Vector2 RandomEyeMotionTimeRange = new Vector2(1f, 4f);

        [Range(0f, 20f)]
        [Tooltip("Max. eye angles used for random eye movements used for vertical and horizontal. [euler]")]
        public float RandomEyeMaxAngle = 10f;

        //==========================================================================================
        [Header("Random Eye Motion (Hotspot)")]
        //==========================================================================================
        [Tooltip("Enable random eye movements on active hotspots.")]
        public bool EnableHotspotRandomEyeMotion = true;

        [Tooltip("Random time range between random eye motions while a hotspot is active. [seconds]")]
        [MinMaxRange(0.1f, 30f)]
        public Vector2 RandomEyeMotionHotspotTimeRange = new Vector2(4f, 8f);


        [Tooltip("Random time range between random eye motions while a hotspot is active. [seconds]")]
        [MinMaxRange(0.1f, 10f)]
        public Vector2 RandomEyeMotionDurationHotspotTimeRange = new Vector2(1f, 2f);

        [Range(0f, 20f)]
        [Tooltip("Max. eye angles used for random eye movements while a hotspot is active. This value is used for vertical and horizontal. [euler]")]
        public float RandomEyeMaxAngleHotspot = 10;

        //==========================================================================================
        [Header("Eye Blinking")]
        //==========================================================================================
        [Tooltip("Enable random eye blinking.")]
        public bool EnableEyeBlinking = true;

        [Tooltip("Random time range btween blinking. [seconds]")]
        [MinMaxRange(0.1f, 30f)]
        public Vector2 BlinkTimeRange;

        [Range(0f, 100f)]
        [Tooltip("Double blink chance. [percent]")]
        public float DoubleBlinkChancePercent = 10.0f;

        [Tooltip("FaceState used for blinking. The given FaceState must be set to PingPong1 lifecycle.")]
        public FaceState EyeBlinkingFaceState;

        //==========================================================================================
        [Header("Lip Sync")]
        //==========================================================================================
        [Range(0f, 2f)]
        [Tooltip("Global weight applied to lipsync visime face states. 1: normal, full visime face state weight, 0: no visime face states applied.")]
        public float VisemeWeightScale = 1f;

        [Tooltip("Smoothing of 1 will yield only the current predicted viseme, 100 will yield an extremely smooth viseme response.")]
        public int LipSyncSmoothAmount = 50;

        [Range(0.1f, 1f)]
        [Tooltip("Smoothing between blendshape values of the last frame to the current frame. Higher values will weight the current frame higher.")]
        [HideInInspector]
        public float SmoothingWeight = 0.5f;

        //Viseme's
        public FaceState SilAsset;
        public FaceState PPAsset;
        public FaceState FFAsset;
        public FaceState THAsset;
        public FaceState DDAsset;
        public FaceState kkAsset;
        public FaceState CHAsset;
        public FaceState SSAsset;
        public FaceState NnAsset;
        public FaceState RRAsset;
        public FaceState AaAsset;
        public FaceState EAsset;
        public FaceState IhAsset;
        public FaceState OhAsset;
        public FaceState OuAsset;

        //==========================================================================================
        [Header("Random Face States During Lipsync")]
        //==========================================================================================
        [Tooltip("Enable random facestates blended in during lipsync.")]
        public bool EnableRandomFaceStatesDuringLipsync = true;

        [Tooltip("Time range between random face states during lipsync. [seconds]")]
        [MinMaxRange(0.1f, 10f)]
        public Vector2 RandomFaceStatesTimeRange = new Vector2(0.5f, 2f);

        [Tooltip("List of random face states blended in during lipsync.")]
        public FaceState[] RandomFaceStateDuringLipSync;

        
        public FaceState GetVisemeState(OVRLipSync.Viseme viseme)
        {
            switch (viseme)
            {
                case OVRLipSync.Viseme.sil:
                    return SilAsset;
                case OVRLipSync.Viseme.PP:
                    return PPAsset;
                case OVRLipSync.Viseme.FF:
                    return FFAsset;
                case OVRLipSync.Viseme.TH:
                    return DDAsset;
                case OVRLipSync.Viseme.DD:
                    return DDAsset;
                case OVRLipSync.Viseme.kk:
                    return kkAsset;
                case OVRLipSync.Viseme.CH:
                    return CHAsset;
                case OVRLipSync.Viseme.SS:
                    return SSAsset;
                case OVRLipSync.Viseme.nn:
                    return NnAsset;
                case OVRLipSync.Viseme.RR:
                    return RRAsset;
                case OVRLipSync.Viseme.aa:
                    return AaAsset;
                case OVRLipSync.Viseme.E:
                    return EAsset;
                case OVRLipSync.Viseme.ih:
                    return IhAsset;
                case OVRLipSync.Viseme.oh:
                    return OhAsset;
                case OVRLipSync.Viseme.ou:
                    return OuAsset;
                default:
                    return null;
            }
        }
    }
}