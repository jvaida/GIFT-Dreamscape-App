using Artanim.Location.Messages;
using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
using XRNode = UnityEngine.VR.VRNode;
using XRNodeState = UnityEngine.VR.VRNodeState;
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace Artanim
{
	[RequireComponent(typeof(IKListener))]
	[RequireComponent(typeof(AvatarController))]
	public class StandaloneAvatarController : MonoBehaviour
	{
        public enum ERotationMode { Smooth, Snapped, }
        private const float FIXED_ROTATION_ANGLE = 30f;

        private const float DEFAULT_PLAYER_HEIGHT = 0f;
		private const float DEFAULT_OCULUS_PLAYER_HEIGHT = 1.6f;
        private const string OCULUS_DEVICE_NAME = "Oculus";

        private float PlayerHeight
        { 
            get
            {
                // Different offset for Oculus and OpenVR
                return XRSettings.loadedDeviceName != OCULUS_DEVICE_NAME ? DEFAULT_PLAYER_HEIGHT : DEFAULT_OCULUS_PLAYER_HEIGHT;
            }
        }


		public float MaxMovementSpeed = 5f;
		public float MaxRotationSpeed = 80f;

        public ERotationMode RotationMode;

		public Transform HandRoot;
		public Transform FeetRoot;

		public Transform AvatarRoot;

        public Vector3 TrackedHandToRigForward;
        public Vector3 TrackedHandToRigUp;

        private AvatarController AvatarController;
		
		private void Start()
		{
			AvatarController = GetComponent<AvatarController>();

			//Init VR
			if(XRSettings.enabled)
			{
				//Disable IK listener
				GetComponent<IKListener>().enabled = false;

				//Set player height (adjusted for seated experience)
				if (MainCameraController.Instance)
				{
					var camPos = MainCameraController.Instance.HeadRigidBody.transform.position;
					camPos.y = PlayerHeight;
					MainCameraController.Instance.HeadRigidBody.transform.position = camPos;
				}

				//Set hands to default height
				if (HandRoot)
					HandRoot.localPosition = new Vector3(0f, PlayerHeight, 0f);

				InputTracking.trackingAcquired += InputTracking_trackingAcquired;
                InputTracking.trackingLost += InputTracking_trackingLost;
			}
			else
			{
				Debug.LogError("No headset found. Use Oculus headset with Oculus Touch or a WMR setup to control the avatar in standalone mode.");
			}
        }


        private void InputTracking_trackingAcquired(XRNodeState node)
		{
            switch (node.nodeType)
            {
                case XRNode.Head:
                    XRUtils.Instance.Recenter();
                    break;
                case XRNode.LeftHand:
                    ShowHand(EAvatarBodyPart.LeftHand, true);
                    break;
                case XRNode.RightHand:
                    ShowHand(EAvatarBodyPart.RightHand, true);
                    break;
                default:
                    break;
            }
		}

        private void InputTracking_trackingLost(XRNodeState node)
        {
            switch (node.nodeType)
            {
                case XRNode.Head:
                    break;
                case XRNode.LeftHand:
                    ShowHand(EAvatarBodyPart.LeftHand, false);
                    break;
                case XRNode.RightHand:
                    ShowHand(EAvatarBodyPart.RightHand, false);
                    break;
                default:
                    break;
            }
        }

        private bool IsCalibrated;
		private void LateUpdate()
		{
			if (XRSettings.enabled)
			{
				if (MainCameraController.Instance && MainCameraController.Instance.HeadRigidBody && AvatarRoot)
				{
					//Update head
					UpdateHead(AvatarController.GetAvatarBodyPart(EAvatarBodyPart.Head).transform);

					//Update movement
					UpdateMovement();

                    //Inverse hand rotation vs. camera since we apply the local rotation to the avatar root
                    var x = HandRoot.localRotation.eulerAngles;
                    x.y = -XRUtils.Instance.GetNodeLocalRotation(XRNode.Head).eulerAngles.y;
                    HandRoot.localRotation = Quaternion.Euler(x);

                    //Update hands
                    UpdateHand(EAvatarBodyPart.LeftHand, XRNode.LeftHand);
					UpdateHand(EAvatarBodyPart.RightHand, XRNode.RightHand);

                    //Update feet position
                    UpdateFeet();

                    //Update calibration
                    if (DevelopmentMode.GetButtonUp(DevelopmentMode.BUTTON_STANDALONE_RECALIBRATE) || !IsCalibrated)
					{
                        XRUtils.Instance.Recenter();
                        IsCalibrated = true;
					}
				}
			}
		}

		/// <summary>
		/// Adjust head to the VR camera transform
		/// </summary>
		/// <param name="headTransform"></param>
		private void UpdateHead(Transform headTransform)
		{
			if(headTransform)
			{
                //Update body part
				headTransform.position = MainCameraController.Instance.PlayerCamera.transform.position;
				headTransform.rotation = MainCameraController.Instance.PlayerCamera.transform.rotation;
			}
		}

		/// <summary>
		/// Applies tracked hands to the avatar body parts
		/// </summary>
		/// <param name="handTransform"></param>
		/// <param name="node"></param>
		private void UpdateHand(EAvatarBodyPart bodyPart, XRNode node)
		{
            //Update bodypart
            var handTransform = AvatarController.GetAvatarBodyPart(bodyPart).transform;
			if(handTransform && handTransform.gameObject.activeInHierarchy)
			{
				handTransform.localPosition = XRUtils.Instance.GetNodeLocalPosition(node);
				handTransform.localRotation = XRUtils.Instance.GetNodeLocalRotation(node);
			}

            //Update avatar hands
            var avatarHandTransform = AvatarController.AvatarAnimator.GetBoneTransform(GetBodyPartBone(bodyPart));
            if(avatarHandTransform)
            {
                avatarHandTransform.position = handTransform.position + (handTransform.rotation * new Vector3(0f, 0f, -0.05f));
                avatarHandTransform.rotation = handTransform.rotation * Quaternion.LookRotation(TrackedHandToRigForward, TrackedHandToRigUp);
            }
        }

        /// <summary>
		/// 
		/// </summary>
		private void UpdateFeet()
        {
            if (FeetRoot)
            {
                //Set feet root to floor of head
                var newFeetPosition = AvatarController.GetAvatarBodyPart(EAvatarBodyPart.Head).transform.localPosition;
                newFeetPosition.y = 0f;
                FeetRoot.localPosition = newFeetPosition;

                //Update avatar feet (root with offsets)
                var foot = AvatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
                foot.position = FeetRoot.position + (FeetRoot.rotation * new Vector3(-0.2f, 0f, -0.15f));
                foot.rotation = FeetRoot.rotation * Quaternion.LookRotation(TrackedHandToRigForward, TrackedHandToRigUp);

                foot = AvatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
                foot.position = FeetRoot.position + (FeetRoot.rotation * new Vector3(0.2f, 0f, -0.15f));
                foot.rotation = FeetRoot.rotation * Quaternion.LookRotation(TrackedHandToRigForward, TrackedHandToRigUp);
            }
        }

        /// <summary>
        /// Show/hide hand
        /// </summary>
        /// <param name="bodyPart"></param>
        /// <param name="Show"></param>
        private void ShowHand(EAvatarBodyPart bodyPart, bool Show)
        {
            if(bodyPart == EAvatarBodyPart.LeftHand || bodyPart == EAvatarBodyPart.RightHand)
            {
                AvatarController.GetAvatarBodyPart(bodyPart).gameObject.SetActive(Show);
            }
        }


        private float ControllerRotation = 0f;
        private bool RotationClear = true;
		/// <summary>
		/// Updates the translation and the rotation using the left / right analog sticks
		/// </summary>
		private void UpdateMovement()
		{
            var movement = new Vector2(
                DevelopmentMode.GetAxis(DevelopmentMode.AXIS_STANDALONE_MOVE_STRAFING),
                DevelopmentMode.GetAxis(DevelopmentMode.AXIS_STANDALONE_MOVE_FORWARD)) * MaxMovementSpeed * Time.unscaledDeltaTime;

            //Rotation
            switch (RotationMode)
            {
                case ERotationMode.Smooth:
                    //Update perpetual controller rotation (input from controller to offset tracked rotation)
                    ControllerRotation += DevelopmentMode.GetAxis(DevelopmentMode.AXIS_STANDALONE_ROTATION) * MaxRotationSpeed * Time.unscaledDeltaTime;
                    break;

                case ERotationMode.Snapped:
                    var rotation = DevelopmentMode.GetAxis(DevelopmentMode.AXIS_STANDALONE_ROTATION);
                    if (RotationClear && rotation != 0f)
                    {
                        ControllerRotation += rotation > 0f ? FIXED_ROTATION_ANGLE : -FIXED_ROTATION_ANGLE;
                        RotationClear = false;
                    }
                    else if (rotation == 0f)
                    {
                        RotationClear = true;
                    }
                    break;
            }

            //Update controlled avatar position
            var newAvatarRotation = AvatarRoot.transform.localRotation.eulerAngles;
            newAvatarRotation.y = XRUtils.Instance.GetNodeLocalRotation(XRNode.Head).eulerAngles.y;
            newAvatarRotation.y += ControllerRotation;
            AvatarRoot.transform.localRotation = Quaternion.Euler(newAvatarRotation);

            //Translate avatar in look direction + controller rotation if left popup is not open
            if (!DevelopmentMode.IsAxisDown(DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT))
                AvatarRoot.Translate(movement.x, 0f, movement.y, Space.Self);

            //Apply this position to the main cam controller (simulate rigid body movement). Just offset X and Z since we don't navigate in vertical.
            MainCameraController.Instance.HeadRigidBody.transform.localPosition = new Vector3(
                AvatarRoot.localPosition.x, MainCameraController.Instance.HeadRigidBody.transform.localPosition.y, AvatarRoot.localPosition.z);

            //Apply controller rotation to camera. We're using the rigidbody since it's not used in standalone
            var cameraRigidbodyRotation = MainCameraController.Instance.HeadRigidBody.transform.localRotation.eulerAngles;
            cameraRigidbodyRotation.y = ControllerRotation;
            MainCameraController.Instance.HeadRigidBody.transform.localRotation = Quaternion.Euler(cameraRigidbodyRotation);
		}

        private HumanBodyBones GetBodyPartBone(EAvatarBodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case EAvatarBodyPart.LeftFoot: return HumanBodyBones.LeftFoot;
                case EAvatarBodyPart.RightFoot: return HumanBodyBones.RightFoot;
                case EAvatarBodyPart.LeftHand: return HumanBodyBones.LeftHand;
                case EAvatarBodyPart.RightHand: return HumanBodyBones.RightHand;
                case EAvatarBodyPart.Head: return HumanBodyBones.Head;
                default: return HumanBodyBones.LastBone;
            }
        }
	}
}