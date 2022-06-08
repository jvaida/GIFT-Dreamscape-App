using Artanim.Location.Data;
using Artanim.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim
{
    public abstract class BaseSeatedExperienceChairConfig : ChairConfig
    {
        [Header("Seated Experience Chair")]
        public TrackingRigidbody SeatTracker;
        public TrackingRigidbody HandleTracker;

        [Tooltip("Heigh of the base of the chair")]
        public float ChairFloorOffset = 0.0f;

        [Tooltip("Defines if the chair root (player feet) moves with the chair seat tracker.")]
        public bool ChairRootMovesWithSeatTracker;

        [Header("Wheelchair")]
        [Tooltip("Wheelchair offset from the seat tracker to the chair root")]
        public Vector3 SeatTrackerToRootOffset = new Vector3(0f, -0.5f, -0.5f);

        [Tooltip("Wheelchair offset from the seat tracker to the the players pelvis")]
        public Vector3 SeatTrackerToPelvisOffset = new Vector3(0f, 0.2f, -0.6f);


        public abstract void EstimateChairRootTransform(ECalibrationMode calibrationMode, Vector3 chairTrackerPosition, Quaternion chairTrackerRotation, out Vector3 chairRootPosition, out Quaternion chairRootRotation);

        public abstract override void AssignPlayer(SkeletonConfig skeleton);

    }
}
