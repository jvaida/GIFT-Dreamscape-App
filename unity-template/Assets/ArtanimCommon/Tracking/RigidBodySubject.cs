using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Artanim.Location.Config;
using System;
using Artanim.Location.Data;

namespace Artanim.Tracking
{
	public class RigidBodySubject
	{
        const int NUM_FRAMES_THRESHOLD_FOR_RESTART = 180; // ~1 second

        public string Name { get; private set; }
		public string SkeletonName { get; private set; }
		public bool IsSkeletonMainSubject { get; private set; }
		public bool IsSkeletonSubject { get; private set; }
        public bool IsSkeletonSubjectClassified { get; private set; }
		public ESkeletonSubject Subject { get; private set; }

        //===================================================================
        // Temp parallel dynamic classification validation.
        // Todo: Remove once dynamic classification is activated in prod
        //===================================================================
        public string PDC_SkeletonName;
        public ESkeletonSubject PDC_Subject;
        public bool PDC_IsSkeletonSubjectClassified;
        //===================================================================
        //===================================================================

        public bool CreateVisual { get; set; }

		private DateTime _LastUpdate;
		public DateTime LastUpdate
		{ 
			get
			{
				return !IsVirtual ? _LastUpdate : DateTime.UtcNow;
			}

			private set
            {
				_LastUpdate = value;
            }
		} 

		public uint LastUpdateFrameNumber { get; private set; }
		public bool IsTracked { get; private set; }
        public float TrackingQuality { get; private set; }

        /// <summary>
        /// Result translation with offset and smoothing.
        /// (This value is in Unity space)
        /// </summary>
        public Vector3 GlobalTranslation { get; private set; }

		/// <summary>
		/// Result rotation with offset and smoothing
		/// (This value is in Unity space)
		/// </summary>
		public Quaternion GlobalRotation { get; private set; }

		/// <summary>
		/// Unsmoothed translation with offset
		/// (This value is in Unity space)
		/// </summary>
		public Vector3 UnsmoothedTranslation { get; private set; }

		/// <summary>
		/// Unsmoothed rotation
		/// (This value is in Unity space)
		/// </summary>
		public Quaternion UnsmoothedRotation { get; private set; }


		/// <summary>
		/// Translation as received by the tracking system but converted to Unity space. (no offset, no smoothing)
		/// </summary>
		public Vector3 RawTranslation { get; private set; }

		/// <summary>
		/// Rotation as received by the tracking system but converted to Unity space. (no offset, no smoothing)
		/// </summary>
		public Quaternion RawRotation { get; private set; }

		/// <summary>
		/// Translation received by the tracking system in the tracking systems space.
		/// </summary>
		public Vector3 SourcePosition { get; private set; }

		/// <summary>
		/// Rotation received by the tracking system in the tracking systems space.
		/// </summary>
		public Quaternion SourceRotation { get; private set; }

        /// <summary>
		/// Rotation received by the tracking system in the tracking systems space.
		/// </summary>
		public Quaternion LastSourceRotation { get; private set; }

        /// <summary>
        /// Indicates if the rigidbody is smoothed
        /// (Independent of translation or rotation smoothing)
        /// </summary>
        public bool IsSmoothed { get; private set; }

		/// <summary>
		/// Whether or not the position/rotation were recently updated 
		/// </summary>
		public bool IsUpToDate { get; private set; }

		public bool IsVirtual { get; set; }

		public string RigidbodyConfigPrefix { get { return RigidbodyConfig != null ? RigidbodyConfig.NamePrefix : ""; } }

		public RigidbodyConfig RigidbodyConfig { get; private set; }

		public RigidBodySubject(string name, bool applyRigidbodyConfig)
		{
			Name = name;
			IsSkeletonMainSubject = SkeletonConstants.IsSkeletonMainSubject(Name);
			IsSkeletonSubject = SkeletonConstants.IsSkeletonRigidbody(Name);

			if(applyRigidbodyConfig)
				RigidbodyConfig = GetConfig();
		}

		// Use this method to reset frame number, when Tracker's loop a trial for examples
		public void ResetFrameNumber()
		{
			LastUpdateFrameNumber = 0;
		}

        public void ResetIsUpToDate()
		{
			if(!IsVirtual)
            {
				IsUpToDate = false;
				IsTracked = false;
			}
		}

        public void ClassifyAsBodyPart(string skeletonName, ESkeletonSubject bodyPartSubject, bool pdc = false)
        {
            if (!pdc)
            {
                SkeletonName = skeletonName;
                Subject = bodyPartSubject;
                IsSkeletonSubjectClassified = true;
            }
            else
            {
                PDC_SkeletonName = skeletonName;
                PDC_Subject = bodyPartSubject;
                PDC_IsSkeletonSubjectClassified = true;
            }
        }

        public void ResetBodyPartClassification(bool pdc = false)
        {
            if(!pdc)
            {
                IsSkeletonSubjectClassified = false;
                SkeletonName = null;

                PDC_IsSkeletonSubjectClassified = false;
                PDC_SkeletonName = null;
            }
            else
            {
                PDC_IsSkeletonSubjectClassified = false;
                PDC_SkeletonName = null;
            }
        }

        // This method is only used by the Vicon connector to additionally store the Vicon given values for recording
        public void UpdateTransform(uint frameNumber, Vector3 position, Quaternion rotation, bool isTracked, Vector3 sourcePosition, Quaternion sourceRotation, float trackingQuality = 0f)
		{
			SourcePosition = sourcePosition;
			SourceRotation = sourceRotation;
			UpdateTransform(frameNumber, position, rotation, isTracked, trackingQuality: trackingQuality);
		}


        public void UpdateTransform(uint frameNumber, Vector3 position, Quaternion rotation, bool isTracked, float trackingQuality = 0f)
		{
			//Check if virtual
			if(!IsVirtual)
            {
				bool isUpToDate = false;
				IsTracked = isTracked;
				TrackingQuality = trackingQuality;

				// Check that we have a valid frame number
				// (the counter gets reset to 0 every time Tracker is looping a trial and that we are using the SDK connector
				// and we also have another check for looping in case we missed the frame 0, which can happen with the broadcast connector)
				if ((frameNumber == 0) || (frameNumber > LastUpdateFrameNumber) || ((frameNumber + NUM_FRAMES_THRESHOLD_FOR_RESTART) < LastUpdateFrameNumber))
				{
					bool isNullPosition = (position == Vector3.zero);

					// Position at origin as signals an invalid value
					if (!isNullPosition)
					{
						// Temp fix to make sure we don't have discontinuities in quaternion components 
						// before applying smoothing them. This should probably be handled by Vicon directly.
						if (LastSourceRotation != Quaternion.identity)
						{
							if (DetectFlippedQuaternion(rotation, LastSourceRotation))
							{
								rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
							}
						}

						GlobalTranslation = RawTranslation = position;
						GlobalRotation = RawRotation = LastSourceRotation = UnsmoothedRotation = rotation;

						//Smoothing based on config
						//Apply rotation smoothing before offset
						ApplyRotationSmoothing();

						//Offset based on config after smoothing!
						ApplyOffset();
						UnsmoothedTranslation = GlobalTranslation;

						//Apply translation smoothing
						ApplyTranslationSmoothing();

						LastUpdate = DateTime.UtcNow;
						LastUpdateFrameNumber = frameNumber;
						isUpToDate = true;
					}

					//Debug.LogFormat("Updated rigidbody: Name={0}, Active={1}, Translation({2}, {3}, {4}), Rotation=({5}, {6}, {7})",
					//    Name, Active,
					//    Segment.globalTranslation.x, Segment.globalTranslation.y, Segment.globalTranslation.z,
					//    Segment.globalRotation.x, Segment.globalRotation.y, Segment.globalRotation.z);
				}
				else if (frameNumber != LastUpdateFrameNumber)
				{
					Debug.LogWarningFormat("Wrong update sequence. CurrentFrameNumber={0}, LastUpdateFrameNumber={1}", frameNumber, LastUpdateFrameNumber);
				}

				// Store tracking behavior
				IsUpToDate = isUpToDate;
			}
		}

		#region Rigidbody offset

		private bool OffsetInitialized;
		private bool HasOffset;
		private Matrix4x4 OffsetMatrix;

		private void ApplyOffset()
		{
			//Initialize
			if(!OffsetInitialized)
			{
				if (RigidbodyConfig != null)
				{
					if(!RigidbodyConfig.PositionOffset.IsZero || !RigidbodyConfig.RotationOffset.IsZero)
					{
						//Init offset matrix
						OffsetMatrix = Matrix4x4.TRS(
							new Vector3(RigidbodyConfig.PositionOffset.X, RigidbodyConfig.PositionOffset.Y, RigidbodyConfig.PositionOffset.Z),
							Quaternion.Euler(RigidbodyConfig.RotationOffset.X, RigidbodyConfig.RotationOffset.Y, RigidbodyConfig.RotationOffset.Z),
							Vector3.one);
						HasOffset = true;
					}
					else
					{
						HasOffset = false;
					}
				}
				else
				{
					HasOffset = false;
				}

				OffsetInitialized = true;
			}
			
			if(HasOffset)
			{
				//Apply configured offset
				var targetMatrix = Matrix4x4.TRS(GlobalTranslation, GlobalRotation, Vector3.one) * OffsetMatrix;
				GlobalTranslation = UnsmoothedTranslation = UnityUtils.ExtractMatrixPosition(targetMatrix);
				GlobalRotation = UnsmoothedRotation = UnityUtils.ExtractMatrixRotation(targetMatrix);
			}
		}

		#endregion

		#region Rigidbody smoothing

		private OneEuroFilter<Quaternion> RotationFilter;
		private OneEuroFilter<Vector3> TranslationFilter;

		private void ApplyRotationSmoothing()
		{
			//Initialize
			if (RigidbodyConfig != null && RigidbodyConfig.SmoothingRotation.Enabled && RotationFilter == null)
			{
				RotationFilter = new OneEuroFilter<Quaternion>(
					RigidbodyConfig.SmoothingRotation.Frequency,
					RigidbodyConfig.SmoothingRotation.MinCutoff,
					RigidbodyConfig.SmoothingRotation.Beta,
					RigidbodyConfig.SmoothingRotation.DCutoff);

				IsSmoothed = true;
			}

			//Filter rotation
			if (RotationFilter != null)
			{
				// The output quaternion of the filter is not always normalized...
				GlobalRotation = UnityUtils.GetNormalized(RotationFilter.Filter(UnsmoothedRotation));
			}
		}

		private void ApplyTranslationSmoothing()
		{
			//Initialize
			if (RigidbodyConfig != null && RigidbodyConfig.SmoothingTranslation.Enabled && TranslationFilter == null)
			{
				TranslationFilter = new OneEuroFilter<Vector3>(
					RigidbodyConfig.SmoothingTranslation.Frequency,
					RigidbodyConfig.SmoothingTranslation.MinCutoff,
					RigidbodyConfig.SmoothingTranslation.Beta,
					RigidbodyConfig.SmoothingTranslation.DCutoff);

				IsSmoothed = true;
			}

			//Filter position
			if (TranslationFilter != null)
			{
				GlobalTranslation = TranslationFilter.Filter(UnsmoothedTranslation);
			}
		}

		#endregion

		#region Internals

		private RigidbodyConfig GetConfig()
		{
			if(ConfigService.Instance.Config != null)
			{
				var configs = ConfigService.Instance.RigidbodiesConfig.Configs;

                //Sort rigidbody name prefixes by length to catch the longest possible first
                var sortedConfigs = configs.OrderByDescending(c => c.NamePrefix.Length);

				var config = sortedConfigs.FirstOrDefault(c => Name.StartsWith(c.NamePrefix));

				if (config != null)
					Debug.LogFormat("Found rigidbody config for rigidbody={0}, config={1}", Name, config.NamePrefix);
				else
					Debug.LogFormat("Did not find rigidbody config for {0}", Name);


				return config;
			}
			return null;
		}

        // Detect if two quaternions are flipped (negative dot product)
        private bool DetectFlippedQuaternion(Quaternion a, Quaternion b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w < -0.0f;
        }

        #endregion
    }
}
