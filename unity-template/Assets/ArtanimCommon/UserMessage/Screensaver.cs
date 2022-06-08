using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public class Screensaver : MonoBehaviour
    {
        private const float MovementThreshold = 5f;
        
        public GameObject PanelScreensaver;

        private Quaternion PrevRotation;
        private float LastMovementTime;
        private int IdleTime;

        void Start()
        {
            LastMovementTime = Time.time;
            IdleTime = ConfigService.Instance.Config.Location.Client.ScreensaverIdleSecs;

            if (GameController.Instance)
            {
                GameController.Instance.OnJoinedSession += Instance_OnJoinedSession;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession;
                enabled = GameController.Instance.CurrentSession == null;
            }

        }

        void OnDestroy()
        {
            if (GameController.HasInstance)
            {
                GameController.Instance.OnJoinedSession -= Instance_OnJoinedSession;
                GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
            }
        }

        private void Instance_OnJoinedSession(Location.Data.Session session, System.Guid playerId)
        {
            enabled = false;
            if (PanelScreensaver)
                PanelScreensaver.SetActive(false);
        }

        private void Instance_OnLeftSession()
        {
            enabled = true;
        }

        void Update()
        {
            if(PanelScreensaver)
            {
                var camRotation = MainCameraController.Instance.ActiveCamera.transform.rotation;
                var movement = 0f;
                if (PrevRotation != Quaternion.identity)
                {
                    var delta = camRotation * Quaternion.Inverse(PrevRotation);
                    var deltaEuler = new Vector3(Mathf.DeltaAngle(0, delta.eulerAngles.x), Mathf.DeltaAngle(0, delta.eulerAngles.y), Mathf.DeltaAngle(0, delta.eulerAngles.z)) / Time.deltaTime;
                    movement = deltaEuler.magnitude;
                }

                LastMovementTime = movement > MovementThreshold ? Time.time : LastMovementTime;

                //Enable disable screensaver object
                PanelScreensaver.SetActive(Time.time - LastMovementTime > IdleTime);

                PrevRotation = camRotation;
            }
        }
    }
}