using Artanim.Location.Network;
using Artanim.Tracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    [RequireComponent(typeof(AvatarController))]
    public class DesktopAvatarControllerNoVR : MonoBehaviour
	{
        public float PlayerSpeed = 2.0f;
        public float TurnSpeed = 4.0f;
        public float KbTurnSpeed = 1.0f;

        private AvatarController AvatarController;
        private float HeadHeight;

        private void Start()
        {
            AvatarController = GetComponent<AvatarController>();

            // Disable self
            if (!AvatarController.IsMainPlayer)
            {
                enabled = false;
                return;
            }

            //Disable IK listener
            GetComponent<IKListener>().enabled = false;

            HeadHeight = MainCameraController.Instance.PlayerCamera.transform.position.y - AvatarController.AvatarAnimator.transform.position.y;

#if !UNITY_EDITOR
            // Hide and lock mouse cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
#endif
        }

        private void Update()
        {
            // Mouse lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.LogWarning("Escape pressed, showing and unlocking mouse cursor");

                // Show and unlock mouse cursor
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // Hide and lock mouse cursor
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Get transform
            var transf = AvatarController.AvatarAnimator.transform;

            // Mouse aiming
            transf.eulerAngles += (Input.GetAxis("Mouse X") * TurnSpeed + Input.GetAxis("Desktop Avatar Look") * KbTurnSpeed) * Vector3.up;

            // Move
            Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            transf.position += Time.deltaTime * PlayerSpeed * (transf.rotation * move);

            // Camera follows avatar
            MainCameraController.Instance.PlayerCamera.transform.position = transf.position + new Vector3(0, HeadHeight, 0);
            MainCameraController.Instance.PlayerCamera.transform.rotation = transf.rotation;
        }
    }
}