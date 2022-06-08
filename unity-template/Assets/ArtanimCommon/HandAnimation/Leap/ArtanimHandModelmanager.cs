using Artanim.Location.Data;
using Artanim.Location.Network;
using Leap;
using Leap.Unity;
using System.IO;
using UnityEngine;

namespace Artanim.HandAnimation.Leap
{
	/// <summary>
	/// A HandModel manager derived from Leap's HandModelManager. 
	/// In our case we're only interested in managing the hands from a single user's avatar
	/// rather than the leap case of managing a pool of individual hands. 
	/// ArtanimRiggedHands will automatically register themselves with this manager to
	/// receive Leap Motion hand data. 
	/// </summary>
    public class ArtanimHandModelmanager : SingletonBehaviour<ArtanimHandModelmanager>
    {
        [SerializeField]
        private LeapProvider _LeapProvider;
        public LeapProvider LeapProvider
        {
            get { return _LeapProvider; }
            set
            {
                _LeapProvider = value;
            }
        }

		[HideInInspector] //data gets loaded from json. No need to expose it in the inspector
		public LeapConfig LeapConfig; 

        public ArtanimRiggedHand LeftHandModel;
        public ArtanimRiggedHand RightHandModel;

		public bool FilterUnlikelyHands = true;

		[Tooltip("Distance in meters beyond which we will reject a recognized hand as belonging to the client's avatar")]
		public float HandRejectionDistance = 0.15f;

		private bool _LeftHandSeen = false;
		private bool _RightHandSeen = false;

		public void Awake()
		{
            if (NetworkInterface.Instance != null)
            {
                if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
                {
                    return;
                }
            }

			string path = Path.Combine(Application.streamingAssetsPath, "leap_config.json");
			if(File.Exists(path))
			{
				string json = File.ReadAllText(path);
				LeapConfig = JsonUtility.FromJson<LeapConfig>(json);
				Debug.LogWarning("[ArtanimHandModelManager] Loaded leap config");
			}
			else
			{
				Debug.LogWarning("[ArtanimHandModelManager] No leap_config.json found in streaming assets. Loading default settings.");
				LeapConfig = new LeapConfig();
				string json = JsonUtility.ToJson(LeapConfig, true);
				File.WriteAllText(path, json);
			}
		}

		public void OnEnable()
        {
			_LeapProvider = FindObjectOfType<LeapServiceProvider>();
			if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				if(_LeapProvider != null)
				{
					_LeapProvider.enabled = false;
				}
				return;
			}

            if (_LeapProvider != null)
            {
                _LeapProvider.OnUpdateFrame += OnUpdateFrame;
            }
        }

        public void OnDisable()
        {
			if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				return;
			}

			if (_LeapProvider != null)
            {
                _LeapProvider.OnUpdateFrame -= OnUpdateFrame;
            }
        }

		public void RegisterHandModel(ArtanimRiggedHand handModel)
        {
			if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				Debug.LogWarning("[ArtanimHandmanager] Hand registration attempted, but experience is running as server or observer");
				return;
			}

			if (handModel.Handedness == Chirality.Left)
            {
				if(LeftHandModel != null)
				{
					Debug.LogWarning("[ArtanimHandModelmanager] A left hand had already been registered. Overwriting previous hand.");
				}
                LeftHandModel = handModel;
            }
            else
            {
				if (RightHandModel != null)
				{
					Debug.LogWarning("[ArtanimHandModelmanager] A right hand had already been registered. Overwriting previous hand.");
				}

				RightHandModel = handModel;
            }
        }

        protected virtual void OnUpdateFrame(Frame frame)
        {
			if (NetworkInterface.Instance.IsServer || NetworkInterface.Instance.ComponentType == ELocationComponentType.ExperienceObserver)
			{
				return;
			}

			bool foundLeft = false;
			bool foundRight = false;

			if (FilterUnlikelyHands)
			{
				Hand bestLeftHand = null;
				Hand bestRightHand = null;
				float bestLeftDistance = HandRejectionDistance;
				float bestRightDistance = HandRejectionDistance;

				foreach (var hand in frame.Hands)
				{
					if (hand.IsLeft && LeftHandModel != null)
					{
						float distance = Vector3.Distance(hand.WristPosition.ToVector3(), LeftHandModel.transform.position);

						if (distance < bestLeftDistance)
						{
							foundLeft = true;
							_LeftHandSeen = true;
							bestLeftHand = hand;
							bestLeftDistance = distance;
						}
					}
					else if (hand.IsRight && RightHandModel != null)
					{
						float distance = Vector3.Distance(hand.WristPosition.ToVector3(), RightHandModel.transform.position);

						if (distance < bestRightDistance)
						{
							foundRight = true;
							_RightHandSeen = true;
							bestRightHand = hand;
							bestRightDistance = distance;
						}
					}
				}

				if (bestLeftHand != null)
				{
					LeftHandModel.SetLeapHand(bestLeftHand);
				}
				if (bestRightHand != null)
				{
					RightHandModel.SetLeapHand(bestRightHand);
				}
			}
			else
			{
				foreach (var hand in frame.Hands)
				{
					if (hand.IsLeft && LeftHandModel != null)
					{
						foundLeft = true;
						_LeftHandSeen = true;
						LeftHandModel.SetLeapHand(hand);
					}
					else if (hand.IsRight && RightHandModel != null)
					{
						foundRight = true;
						_RightHandSeen = true;
						RightHandModel.SetLeapHand(hand);
					}
				}
			}

			if (!foundLeft && _LeftHandSeen)
			{
				LeftHandModel.SetHandTrackingLost();
				_LeftHandSeen = false;
			}

			if(!foundRight && _RightHandSeen)
			{
				RightHandModel.SetHandTrackingLost();
				_RightHandSeen = false;
			}
        }
	}
}