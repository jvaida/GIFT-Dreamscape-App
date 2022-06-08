using Artanim.Algebra;
using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation
{
#if !NET_4_6
	public class Tuple<T1, T2>
	{
		public T1 Item1 { get; private set; }
		public T2 Item2 { get; private set; }
		internal Tuple(T1 first, T2 second)
		{
			Item1 = first;
			Item2 = second;
		}
	}
#endif

    /// <summary>
    /// A simple component to sync local transform rotations over wifi
    /// In this particular case to sync hand/finger joint rotations obtained
    /// from a Leap Motion or procedural animation on avatars other than 
    /// the main player. Note that this component handles both send and receive
    /// scenarios
    /// </summary>
    [RequireComponent(typeof(AvatarController))]
    [RequireComponent(typeof(AvatarHandController))]
    public class HandTransformStream : MonoBehaviour
	{
        private Animator _AvatarAnimator;
		private Animator AvatarAnimator
        {
            get
            {
                if (!_AvatarAnimator)
                    _AvatarAnimator = GetComponent<AvatarController>().AvatarAnimator;
                return _AvatarAnimator;
            }
        }

        private AvatarHandController _AvatarHandController;
        private AvatarHandController AvatarHandController
        {
            get
            {
                if (!_AvatarHandController)
                    _AvatarHandController = GetComponent<AvatarHandController>();
                return _AvatarHandController;
            }
        }


        private List<Transform> _HandTransforms;

		private HandTransformData _ReceivedData;

		private float _InterpolationDelay; //The amount of time we "lag" behind the current time to allow for smooth interpolation
		private float _StartTime; 

		private TimedRingBuffer<HandTransformData> _HandTransformBuffer; //Ring buffer of received HandTransformData frames
		private DateTime? _OriginalDateTime = null; //The DateTime timestamp of the first message we received. This is our reference for our current time. 

		private float _UpdateDeltaTime;
		private float _LastSendTime;

#pragma warning disable 414 
		//Editor specific ifdefs cause the Guid not to be used in editor. This causes a warning. Disable and restore
		private Guid _AvatarGuid;
#pragma warning restore 414

		private bool _IsSender = false;

		private bool _IsInitialized = false;

		private bool _AlreadyWarnedAboutSubscription = false;

		private void Awake()
		{
			_HandTransforms = new List<Transform>();
			foreach (var index in HandUpdateBones.Rotation)
			{
				_HandTransforms.Add(AvatarAnimator.GetBoneTransform((HumanBodyBones)index));
			}

			if (AvatarHandController.HandDefinition.InterpolateIfReceiver)
			{
				_HandTransformBuffer = new TimedRingBuffer<HandTransformData>(ConfigService.Instance.ExperienceSettings.HandUpdatesPerSecond);
				_InterpolationDelay = 2.0f / ConfigService.Instance.ExperienceSettings.HandUpdatesPerSecond;
			}
		}

		private void OnEnable()
		{
			//Initialization happens in the first update. We subscribe to the stream there as well
			//If we have initialized already, it means we were re-enabled. Make sure to subscribe again.
			if (_IsInitialized)
			{
				_OriginalDateTime = null;
				_HandTransformBuffer.Clear();
				NetworkInterface.Instance.Subscribe<AvatarHandsUpdate>(OnHandTransformData);
			}
		}

		public void OnDisable()
		{
			if (_IsInitialized)
			{
				NetworkInterface.SafeUnsubscribe<AvatarHandsUpdate>(OnHandTransformData);
			}
		}

		public void LateUpdate()
		{
			//Initializing in the first LateUpdate call, because doing it in Awake/Start/Enable caused issues
			if (!_IsInitialized && NetworkInterface.Instance != null)
			{
				AvatarController controller = GetComponent<AvatarController>();
				if (controller != null)
				{
					_AvatarGuid = controller.PlayerId;


                    if (NetworkInterface.Instance.ComponentType != ELocationComponentType.ExperienceObserver)
                    {
                        _IsSender = controller.IsMainPlayer;
                    }
                    else
                    {
                        _IsSender = false;
                    }
				}

				if (NetworkInterface.Instance != null)
				{
					NetworkInterface.Instance.Subscribe<AvatarHandsUpdate>(OnHandTransformData);
					_UpdateDeltaTime = 1.0f / ConfigService.Instance.ExperienceSettings.HandUpdatesPerSecond;
					_IsInitialized = true;
				}
				else if(!_AlreadyWarnedAboutSubscription)
				{
					Debug.LogError("[HandTransformStream] Couldn't subscribe to AvatarHandsUpdate. NetworkInterface was null");
					_AlreadyWarnedAboutSubscription = true;
				}
			}

			if (_IsSender)
			{
				if (Time.realtimeSinceStartup - _LastSendTime > _UpdateDeltaTime)
				{
					SendHandTransformData();
					_LastSendTime = Time.realtimeSinceStartup;
				}
			}
			else //We're a receiver
			{
				if (AvatarHandController.HandDefinition.InterpolateIfReceiver)
				{
					if (_HandTransformBuffer.BufferCount > 0)
					{
						Tuple<float, HandTransformData> before_data;
						Tuple<float, HandTransformData> after_data;

						float currentTime = (Time.realtimeSinceStartup - _StartTime) - _InterpolationDelay;

						if (_HandTransformBuffer.GetItemsAroundTime(currentTime, out before_data, out after_data))
						{
							float normalized_time = (currentTime - before_data.Item1) / (after_data.Item1 - before_data.Item1);
							HandleHandTransformStream(normalized_time, before_data.Item2, after_data.Item2);
						}
					}
				}
				else
				{
					//Just straight apply the data we received.
					if (_ReceivedData != null)
					{
						HandleHandTransformStream(_ReceivedData);
					}
				}
			}
		}

        private HandTransformData handTransformData;
		public void SendHandTransformData()
		{
            if(handTransformData == null)
            {
                handTransformData = new HandTransformData()
                {
                    HandRotations = new Quatf[_HandTransforms.Count]
                };
            }
			

			for(int i = 0; i < _HandTransforms.Count; ++i)
			{
				handTransformData.HandRotations[i] = _HandTransforms[i].localRotation.ToQuatf();
			}

			if (NetworkInterface.Instance != null)
			{
				NetworkInterface.Instance.SendMessage(new AvatarHandsUpdate()
				{
					Data = handTransformData
				});
			}
		}

		public void OnHandTransformData(AvatarHandsUpdate message)
		{
			if (message.SenderId == _AvatarGuid && message.SenderId != SharedDataUtils.MySharedId)
			{
				if (!AvatarHandController.HandDefinition.InterpolateIfReceiver)
				{
					_ReceivedData = message.Data;
				}
				else
				{
					if(!_OriginalDateTime.HasValue)
					{
						_OriginalDateTime = message.SendTime;
						_HandTransformBuffer.Add(0.0f, message.Data);
						_StartTime = Time.realtimeSinceStartup;
					}
					else
					{
						float time = (float)((DateTime.UtcNow - _OriginalDateTime.Value).TotalMilliseconds) * 0.001f;
						_HandTransformBuffer.Add(time, message.Data);
					}
				}
			}
		}

		public void HandleHandTransformStream(HandTransformData data)
		{
			if (_HandTransforms.Count != data.HandRotations.Length)
			{
				Debug.LogError("The transform data count received does not match the number of transforms set up for the avatar!");
				return;
			}

			for (int i = 0; i < _HandTransforms.Count; ++i)
			{
				_HandTransforms[i].localRotation = data.HandRotations[i].ToUnity();
			}
		}

		/// <summary>
		/// Performs a slerp between hand joint rotations at a given timestamp
		/// A normalized time stamp is assumed within the interval of the two datasets. 
		/// </summary>
		/// <param name="normalized_time">A time value between 0 (use the before_data) and 1 (use the after_data)</param>
		/// <param name="before_data">HandstreamData from a time before our current time</param>
		/// <param name="after_data">HandStreamData from a time after our current time</param>
		public void HandleHandTransformStream(float normalized_time, HandTransformData before_data, HandTransformData after_data)
		{
			if (_HandTransforms.Count != before_data.HandRotations.Length)
			{
				Debug.LogError("The transform data count received does not match the number of transforms set up for the avatar!");
				return;
			}

            //Skip wrist rotation (first two indexes) if tracker is on hands. In that case the wrist is controlled by IK.
            var startIndex = ConfigService.Instance.ExperienceSettings.HandTrackerPosition == ExperienceSettingsSO.EHandTrackerPosition.Hand ? 2 : 0;
            for (int i = startIndex; i < _HandTransforms.Count; ++i)
			{
				var before = before_data.HandRotations[i].ToUnity();
				var after = after_data.HandRotations[i].ToUnity();
				_HandTransforms[i].localRotation = Quaternion.Slerp(before, after, normalized_time);
			}
		}
	}
}