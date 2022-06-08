using Artanim.Algebra;
using Artanim.HandAnimation;
using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using Artanim.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim
{

    /// <summary>
    /// Main controller for an actor.
    /// This class is responsible to init all actor and skeleton based functionality of an actor.
    /// </summary>
    [RequireComponent(typeof(IKListener))]
	[AddComponentMenu("Artanim/Avatar Controller")]
	public class AvatarController : MonoBehaviour
	{
		private const string SDK_WHEELCHAIR_RESOURCE = "Avatars/PFB_Generic User Wheelchair_Mixamo";
		private const string SDK_SEATED_EXP_CHAIR_RESOURCE = "Avatars/PFB_Generic DS Seated Exp Chair";

		//PlayerId is not used here but used in Real Virtuality 2 to store the player state
		public Guid PlayerId
		{
			get
			{
				return Player != null ? Player.ComponentId : Guid.Empty;
			}
		}

		public Guid SkeletonId
		{
			get
			{
				return Player != null ? Player.SkeletonId : Guid.Empty;
			}
		}

		public string Initials
		{
			get
			{
				return Player != null ? Player.Initials : string.Empty;
			}
		}

        public Animator AvatarAnimator
        {
            get
            {
                return IKListener.AvatarAnimator;
            }
        }

        public bool IsLipSyncAvatar
        {
            get { return GetComponent<AvatarLipSyncController>(); }
        }

        public bool IsHandAnimationAvatar
        {
            get { return GetComponent<AvatarHandController>(); }
        }


		public Transform HeadBone;

		[Header("Chair and wheelchair")]
		[Tooltip("Set this to override the default wheelchair template in the experience settings.")]
		public GameObject ChairTemplateOverride;

		[Header("Rig Calibration")]
		[Tooltip("")]
		public float BackpackToPelvisOffset = 0.14f;

		[Tooltip("")]
		public float DefaultArmStretchFactor = 1.0f;

		[Tooltip("")]
		public Vector3 HeadTargetOffset = new Vector3(0f, -0.07f, -0.20f);

		[Tooltip("")]
		public Vector3 HandTargetOffset = new Vector3(0f, -0.04f, 0.06f);

        [Tooltip("")]
        public Vector3 FootTargetOffset = new Vector3(0f, -0.04f, -0.015f);

        public List<Renderer> Renderers { get; private set; }


		/// <summary>
		/// True if the avatar is the clients main player.
		/// </summary>
		public bool IsMainPlayer { get; private set; }


		private IKListener _ikListener;
		private IKListener IKListener
		{
			get
			{
				if (_ikListener == null)
					_ikListener = GetComponent<IKListener>();
				return _ikListener;
			}
		}
		
		private bool ShowAvatarOnNextUpdate = false;

		public Player Player { get; private set; }
		public RuntimePlayer RuntimePlayer { get; private set; }

		private AvatarDisplayController _AvatarDisplayController;
		private AvatarDisplayController AvatarDisplayController
		{
			get
			{
				if (!_AvatarDisplayController)
				{
					_AvatarDisplayController = GetComponent<AvatarDisplayController>();
					if (!_AvatarDisplayController)
						Debug.LogErrorFormat("Avatar {0} is not setup correctly. No AvatarDisplayController found. Add an implementation of AvatarDisplayController to the root of the avatar.", name);
				}
				return _AvatarDisplayController;
			}
		}

		private List<BoneTransform> InitialBones;
		private bool Initialized;

		private AvatarBodyPart[] BodyParts;

		private GameObject ChairInstance;

		void Awake()
		{
			//Hide avatar
			//AvatarDisplayController.HideAvatar();
			BodyParts = FindAvatarBodyParts();

            //Init renderers
            Renderers = new List<Renderer>();
			Renderers.AddRange(GetComponentsInChildren<Renderer>());
		}

		private void OnDestroy()
		{
			if (Player != null)
			{
				//Detach player state update
				Player.PropertyChanged -= Player_PropertyChanged;
			}
		}

		#region Public interface

		/// <summary>
		/// 
		/// </summary>
		public void SetAsMainPlayer()
		{
			IsMainPlayer = true;
			InitAvatarVisuals();
		}

		/// <summary>
		/// 
		/// </summary>
		public void SetAsPassivePlayer()
		{
			IsMainPlayer = false;
			InitAvatarVisuals();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="playerId"></param>
		/// <param name="skeletonId"></param>
		/// <param name="isMainPlayer"></param>
		/// <param name="showAvatar"></param>
		public void InitAvatar(RuntimePlayer player, bool isMainPlayer, bool showAvatar = false)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("InitAvatar: playerId={0} isMainPlayer={1} showAvatar={2}", player.Player.ComponentId, isMainPlayer, showAvatar);

			//Detach player state update if needed
			if(Player != null)
			{
				Player.PropertyChanged -= Player_PropertyChanged;
			}
				
			IsMainPlayer = isMainPlayer;
			Player = player.Player;
			RuntimePlayer = player;

			//Attach player state udpates
			Player.PropertyChanged += Player_PropertyChanged;

			//Init IK listener
			IKListener.Init(SkeletonId, isMainPlayer);

			InitAvatarVisuals();

			ShowAvatar(showAvatar);

			Initialized = true;
		}

		public ChairConfig InitChair(EPlayerStatus playerStatus, ECalibrationMode calibrationMode)
		{
			// Instantiate chair template
			var chairTemplate = GetChairTemplate();
			if (chairTemplate)
			{
                var chairConfig = UnityUtils.InstantiatePrefab<ChairConfig>(chairTemplate, transform);
                if(chairConfig)
                {
                    ChairInstance = chairConfig.gameObject;

                    //Add chair to renderers to properly disable them in the server view
                    Renderers.AddRange(ChairInstance.GetComponentsInChildren<Renderer>());

                    switch (calibrationMode)
                    {
                        case ECalibrationMode.TrackedWheelchair:
                        case ECalibrationMode.UserWheelchair:

                            // Make the chair root follow the avatar root transform but ignores it's scale
							FollowTransform followTransform = ChairInstance.AddComponent<FollowTransform>();
                            followTransform.target = GetAvatarRoot();
                            followTransform.UpdatePosition = true;
                            followTransform.UpdateRotation = true;
                            followTransform.UpdateScale = false;

                            break;

                        case ECalibrationMode.SeatedExperience:
                        case ECalibrationMode.SeatedExperienceWheelchair:
                            //Nothing to do since the chair has already been initialized on calibration to send chair setup and root position to IK
                            break;

                        default:
                            Debug.LogWarning("Unrecognized chair calibration mode");
							return null;
                    }

                    ChairInstance.SetActive(playerStatus == EPlayerStatus.Calibrated); //Show chair if player is already calibrated.
					return chairConfig;

				}
                else
                {
                    Debug.LogErrorFormat("<color=lightblue>Failed to create chair. Could not instantiate chair resource={0}</color>", ChairInstance);
                }
			}
			else
            {
				Debug.LogErrorFormat("<color=lightblue>Failed to create chair. Could not find a chair template</color>");
            }
			return null;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="skeleton"></param>
		/// <param name="frameNumber"></param>
		public void UpdateSkeleton(SkeletonTransforms skeleton)
		{
			if (Initialized && Player != null && Player.Status == EPlayerStatus.Calibrated)
			{
				IKListener.UpdateSkeleton(skeleton);

				//Show avatar is needed
				if (ShowAvatarOnNextUpdate)
				{
					AvatarDisplayController.ShowAvatar();

                    // Add chair if requested
                    if (!ChairInstance && Player.CalibrationMode != ECalibrationMode.Normal)
                    {
                        InitChair(Player.Status, Player.CalibrationMode);
                    }

                    if (ChairInstance) ChairInstance.SetActive(true);

					ShowAvatarOnNextUpdate = false;
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="showOnNextUpdate"></param>
        public void ShowAvatar(bool showOnNextUpdate, bool forceShowNow = false)
		{
			if (AvatarDisplayController)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("ShowAvatar: showOnNextUpdate={0} forceShowNow={1}", showOnNextUpdate, forceShowNow);

				//Delay avatar display to next ik updates to avoid showing the avatar in T-pose until next update
				if (showOnNextUpdate)
				{
                    if(!forceShowNow)
					    ShowAvatarOnNextUpdate = true;
                    else
                        AvatarDisplayController.ShowAvatar();
                }
                else
				{ 
					AvatarDisplayController.HideAvatar();
					if (ChairInstance) ChairInstance.SetActive(false);
				}
			}
			else
            {
				if (ConfigService.VerboseSdkLog) Debug.Log("ShowAvatar: AvatarDisplayController is null");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void StartCalibration(bool sendRigSetup, Player player, string rigName, bool resetClassification)
		{
			//Client send RigSetup to IK server... needs to be the clients since in some cases no server is preset at calibration time!
			if (sendRigSetup)
			{
				if (InitialBones == null)
				{
					if (ConfigService.VerboseSdkLog) Debug.Log("Storing initial bones");

					//Initialize initial bones
					InitialBones = new List<BoneTransform>();
					foreach (ERigBones bone in Enum.GetValues(typeof(ERigBones)))
					{
						var boneTransform = IKListener.GetBoneTransform(bone);
						if (boneTransform != null)
						{
							InitialBones.Add(new BoneTransform
							{
								BoneIndex = (int)bone,
								LocalPosition = boneTransform.localPosition.ToVect3f(),
								LocalRotation = boneTransform.localRotation.ToQuatf(),
							});
						}
					}
				}

                //Send rig calibration message to IK with the avatar bone setup
                var msgRigCalibration = new RecalibrateRig
                {
                    ExperienceClientId = SharedDataUtils.MySharedId,
                    ResetClassification = resetClassification,

                    RigSetup = new RigSetup
                    {
						RigName = rigName,

                        BackpackToPelvisOffset = BackpackToPelvisOffset,
                        DefaultArmStretchFactor = DefaultArmStretchFactor,
                        HeadTargetOffset = HeadTargetOffset.ToVect3f(),
                        HandTargetOffset = HandTargetOffset.ToVect3f(),
                        FootTargetOffset = FootTargetOffset.ToVect3f(),

						Bones = InitialBones.ToArray(),
					},
				};


				//Calibrate with chair?
				if(player.CalibrationMode != ECalibrationMode.Normal)
				{
					// Add chair if needed
					ChairConfig chairConfig;
					if (!ChairInstance)
						chairConfig = InitChair(Player.Status, Player.CalibrationMode);
					else
						chairConfig = ChairInstance.GetComponent<ChairConfig>();

					if(chairConfig)
					{
						msgRigCalibration.ChairSetup =
							player.CalibrationMode == ECalibrationMode.SeatedExperience || player.CalibrationMode == ECalibrationMode.SeatedExperienceWheelchair ? 
							new ChairSetupSE() : 
							new ChairSetup();

						// Calibration mode
						msgRigCalibration.ChairSetup.CalibrationMode = player.CalibrationMode;

						//Chair setup
						msgRigCalibration.ChairSetup.PelvisPosition = chairConfig.PelvisTarget.localPosition.ToVect3f();
						msgRigCalibration.ChairSetup.PelvisRotation = chairConfig.PelvisTarget.localRotation.ToQuatf();

						msgRigCalibration.ChairSetup.LeftFootPosition = chairConfig.LeftFootTarget.localPosition.ToVect3f();
						msgRigCalibration.ChairSetup.LeftFootRotation = chairConfig.LeftFootTarget.localRotation.ToQuatf();

						msgRigCalibration.ChairSetup.RightFootPosition = chairConfig.RightFootTarget.localPosition.ToVect3f();
						msgRigCalibration.ChairSetup.RightFootRotation = chairConfig.RightFootTarget.localRotation.ToQuatf();

						msgRigCalibration.ChairSetup.LeftLegBendGoal = chairConfig.LeftLegBendGoal ? chairConfig.LeftLegBendGoal.localPosition.ToVect3f() : Vect3f.Zero;
						msgRigCalibration.ChairSetup.RightLegBendGoal = chairConfig.RightLegBendGoal ? chairConfig.RightLegBendGoal.localPosition.ToVect3f() : Vect3f.Zero;

						//Additional for SE
						var chairConfigSE = chairConfig as BaseSeatedExperienceChairConfig;
						if(chairConfigSE)
                        {
							var chairSetupSE = msgRigCalibration.ChairSetup as ChairSetupSE;

							//Base SE chair attributes
							chairSetupSE.ChairFloorOffset = chairConfigSE.ChairFloorOffset;
							chairSetupSE.ChairRootMovesWithSeatTracker = chairConfigSE.ChairRootMovesWithSeatTracker;

							//Chair root transform
							var skeletonConfig = SharedDataController.Instance.FindSharedDataById<SkeletonConfig>(Player.SkeletonId);
							if(skeletonConfig != null)
                            {
								//Assign player to chair
								chairConfigSE.AssignPlayer(skeletonConfig);

								var chairTrackerSubject = TrackingController.Instance.TryGetRigidBody(skeletonConfig.SkeletonSubjectNames[(int)ESkeletonSubject.Pelvis]);
								if (chairTrackerSubject != null)
								{
									Vector3 chairRootPosition;
									Quaternion chairRootRotation;
									chairConfigSE.EstimateChairRootTransform(player.CalibrationMode, chairTrackerSubject.GlobalTranslation, chairTrackerSubject.GlobalRotation, out chairRootPosition, out chairRootRotation);

									chairSetupSE.ChairRootPosition = chairRootPosition.ToVect3f();
									chairSetupSE.ChairRootRotation = chairRootRotation.ToQuatf();
								}
								else
                                {

                                }
							}
							else
                            {

                            }
						}
						
					}
					else
					{
						Debug.LogErrorFormat("<color=lightblue>Failed to calibrate player chair. Could not find a chair template</color>");
					}
				}

				//Send rig calibration to IK
				NetworkInterface.Instance.SendMessage(msgRigCalibration);
			}
		}


		/// <summary>
		/// Returns the requested body part of the avatar if available.
		/// </summary>
		/// <param name="bodyPart"></param>
		/// <returns>AvatarBodyPart instance. If not available, null.</returns>
		public AvatarBodyPart GetAvatarBodyPart(EAvatarBodyPart bodyPart)
		{
			return BodyParts[(int)bodyPart];
		}

		/// <summary>
		/// Return the root node of the avatar (i.e. where the root motion is applied)
		/// </summary>
		public Transform GetAvatarRoot()
		{
			return _ikListener.AvatarAnimator.transform;
		}

		/// <summary>
		/// Returns the tracked position of the given body part, which is the position in the physical space
		/// </summary>
		/// <param name="bodyPart"></param>
		/// <returns>Tracked position of the body part</returns>
		public Vector3 GetBodyPartTrackedPosition(EAvatarBodyPart bodyPart)
        {
			var part = BodyParts[(int)bodyPart];
			return (part != null) ? RuntimePlayer.AvatarOffset.InverseTransformPoint(part.transform.position) : Vector3.zero;
		}

		#endregion

		#region Events

		private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//Initials updated?
			if(e.PropertyName == "Initials" && AvatarDisplayController)
			{
				AvatarDisplayController.InitializePlayer(Player.Initials);
			}
			else if(e.PropertyName == "Status")
			{
                ShowAvatar(Player.Status == EPlayerStatus.Calibrated);
			}
		}

		#endregion

		#region Internals

		private GameObject GetChairTemplate()
		{
			//Use override if available
			var template = ChairTemplateOverride;

			//Use settings template if available
			if (!template && ConfigService.Instance.ExperienceSettings.DefaultChairTemplate)
				template = ConfigService.Instance.ExperienceSettings.DefaultChairTemplate.gameObject;

            //Use SDK template
            if (!template)
                template = !ConfigService.Instance.ExperienceConfig.SeatedExperience ?
                    ResourceUtils.LoadResources<GameObject>(SDK_WHEELCHAIR_RESOURCE) :
                    ResourceUtils.LoadResources<GameObject>(SDK_SEATED_EXP_CHAIR_RESOURCE);

            return template;
		}

		private void InitAvatarVisuals()
		{
			if (AvatarDisplayController)
			{
				//Initialize player
				var playerInitials = "";
				if (Player != null && !string.IsNullOrEmpty(Player.Initials))
					playerInitials = Player.Initials;
				AvatarDisplayController.InitializePlayer(playerInitials);
				
				if (IsMainPlayer)
					AvatarDisplayController.HideHead();
				else
					AvatarDisplayController.ShowHead();
			}
			else
			{
				Debug.LogError("No display controller");
			}
		}

		private AvatarBodyPart[] FindAvatarBodyParts()
		{
			var bodyParts = new AvatarBodyPart[Enum.GetValues(typeof(EAvatarBodyPart)).Length];
			foreach (EAvatarBodyPart bodyPart in Enum.GetValues(typeof(EAvatarBodyPart)))
			{
				var foundBodyParts = GetComponentsInChildren<AvatarBodyPart>().Where(p => p.BodyPart == bodyPart);
				if(foundBodyParts.Count() == 1)
				{
					bodyParts[(int)bodyPart] = foundBodyParts.First();
				}
				else if(foundBodyParts.Count() > 1)
				{
					Debug.LogErrorFormat("Found multiple AvatarBodyPart for {0} in avatar {1}. Using the first one!", bodyPart, name);
					bodyParts[(int)bodyPart] = foundBodyParts.First();
				}
				else if(foundBodyParts.Count() == 0)
				{
					Debug.LogErrorFormat("No AvatarBodyPart found for {0} in avatar {1}!", bodyPart, name);
				}
			}
			return bodyParts;
		}

		#endregion
	}

}