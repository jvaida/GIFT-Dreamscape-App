using Artanim.Location.Data;
using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public class DreamscapeSEChairConfig : BaseSeatedExperienceChairConfig
    {
        public const string HANDLE_PATTERN = "Handle_{0}";

        // Seated experience geometric config
        private static readonly Vector3 OA = new Vector3(0.0f, 0.3873562f, -0.07619365f);
        private static readonly Vector3 OB = new Vector3(0.0f, 0.3365524f, 0.07995675f);
        private static readonly Vector3 PA = new Vector3(0.0f, -0.1464438f, -0.7143936f);
        private static readonly Vector3 PB = new Vector3(0.0f, -0.1972476f, -0.5582432f);

        public override void EstimateChairRootTransform(ECalibrationMode calibrationMode, Vector3 chairTrackerPosition, Quaternion chairTrackerRotation, out Vector3 chairRootPosition, out Quaternion chairRootRotation)
        {
            //Normal seated experience chair
            if(calibrationMode == ECalibrationMode.SeatedExperience)
            {
                var chairTrackerMatrix = Matrix4x4.TRS(chairTrackerPosition, chairTrackerRotation, Vector3.one);
                chairTrackerMatrix.m13 -= ChairFloorOffset;

                Vector3 A2 = chairTrackerMatrix.MultiplyPoint3x4(PA);
                Vector3 B2 = chairTrackerMatrix.MultiplyPoint3x4(PB);

                float cosRY = Mathf.Clamp((B2.z - A2.z) / (OB.z - OA.z), -1f, 1f);
                float sinRY = Mathf.Clamp((B2.x - A2.x) / (OB.z - OA.z), -1f, 1f);

                float X = A2.x - sinRY * OA.z;
                float Z = A2.z - cosRY * OA.z;

                float ry = Mathf.Acos(cosRY);
                if (sinRY < 0.0f) ry *= -1.0f;

                chairRootPosition = new Vector3(X, ChairFloorOffset, Z);
                chairRootRotation = Quaternion.Euler(0.0f, ry * Mathf.Rad2Deg, 0.0f);
            }

            //Wheelchair
            else if(calibrationMode == ECalibrationMode.SeatedExperienceWheelchair)
            {
                //Root rotation
                chairRootRotation = Quaternion.Euler(0f, chairTrackerRotation.eulerAngles.y, 0f);

                //Root position
                chairRootPosition = chairTrackerPosition + chairTrackerRotation * SeatTrackerToRootOffset;

                //Pelvis position
                PelvisTarget.transform.localPosition = SeatTrackerToPelvisOffset;
            }
            else
            {
                Debug.LogErrorFormat("Failed to estimate seated experience chair root. Invalid calibration mode: {0}", calibrationMode);
                chairRootPosition = Vector3.zero;
                chairRootRotation = Quaternion.identity;
            }

            //Apply calculated root to local model
            RootTarget.position = chairRootPosition;
            RootTarget.rotation = chairRootRotation;
        }

        public override void AssignPlayer(SkeletonConfig skeleton)
        {
            var pelvisName = skeleton.SkeletonSubjectNames[(int)ESkeletonSubject.Pelvis];
            if (SeatTracker)
                SeatTracker.ResetRigidbodyName(pelvisName);

            var skeletonPostfix = pelvisName.Substring(pelvisName.IndexOf("_") + 1);
            if (HandleTracker)
                HandleTracker.ResetRigidbodyName(string.Format(HANDLE_PATTERN, skeletonPostfix));
        }
    }
}