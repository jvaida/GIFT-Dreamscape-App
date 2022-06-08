using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Artanim
{

	public class FollowOrbitCamera : MonoBehaviour
	{
        private const float USER_INPUT_FACTOR = 50f;

        public enum EMouseButton { Left, Right }

        [Header("Camera Target")]
        public Transform Target;
        public Vector3 TargetOffset;

        [Header("Interaction")]

        public EMouseButton DragMouseButton;

        [Range(0.1f, 1f)]
        public float DragSpeed = 1f;

        [Range(0.1f, 1f)]
        public float ZoomSpeed = 1f;

        [Range(1f, 20f)]
        public float MovementSpeed = 10f;

        [Range(0.7f, 0.99f)]
        public float VelocityDamping = 0.95f;

        [Header("Limits")]
        public Vector2 DistanceRange = new Vector2(1f, 10f);
        public Vector2 VerticalAngleRange = new Vector2(-20f, 85f);

        private float Distance = 5f;

        private Vector3 MouseVelocity;
        private Vector2 Rotation = Vector2.zero;
        private Vector3 EffectiveTarget;
        private Vector3 ManualOffset;

        private void Start()
        {
            Distance = Target ? Vector3.Distance(transform.position, Target.position) : Distance;

            Vector3 angles = transform.eulerAngles;
            Rotation = new Vector2(angles.x, angles.y);

            EffectiveTarget = (Target ? Target.position : Vector3.zero) + TargetOffset;
        }

        private void LateUpdate()
        {
            UpdateTarget();
            UpdateManualOffset();
            UpdateEffectiveTarget();

            Vector3 CurrentMouseVelocity;
            if (UpdateMouseVelocity(out CurrentMouseVelocity))
                MouseVelocity += CurrentMouseVelocity;

            //Apply damping
            MouseVelocity *= VelocityDamping; 

            //Calc effectinve movement
            Rotation.y -= MouseVelocity.x * DragSpeed * USER_INPUT_FACTOR;
            Rotation.x += MouseVelocity.y * DragSpeed * USER_INPUT_FACTOR;
            Distance -= MouseVelocity.z * ZoomSpeed * USER_INPUT_FACTOR;

            //Clamp values
            Distance = Mathf.Clamp(Distance, DistanceRange.x, DistanceRange.y);
            Rotation.x = ClampAngle(Rotation.x, VerticalAngleRange.x, VerticalAngleRange.y);

            //Reposition
            var negDistance = new Vector3(0.0f, 0.0f, -Distance);
            transform.rotation = Quaternion.Euler(Rotation.x, Rotation.y, 0);
            transform.position = transform.rotation * negDistance + EffectiveTarget;
            transform.LookAt(EffectiveTarget, Vector3.up);
        }

        

        private Transform PrevTarget;
        private void UpdateTarget()
        {
            if (PrevTarget != Target)
            {
                ResetManualOffset();
            }
            PrevTarget = Target;
        }

        private void UpdateManualOffset()
        {
            Vector3 offset = Vector3.zero;
            if (Input.GetKey(KeyCode.RightArrow)) offset += Vector3.right;
            if (Input.GetKey(KeyCode.LeftArrow)) offset += Vector3.left;
            if (Input.GetKey(KeyCode.PageUp)) offset += Vector3.up;
            if (Input.GetKey(KeyCode.PageDown)) offset += Vector3.down;
            if (Input.GetKey(KeyCode.UpArrow)) offset += Vector3.forward;
            if (Input.GetKey(KeyCode.DownArrow)) offset += Vector3.back;

            if (Input.GetKeyDown(KeyCode.Home))
                ResetManualOffset();
            else if (offset != Vector3.zero)
                ManualOffset += Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0) * offset * MovementSpeed * Time.smoothDeltaTime;
        }

        public void ResetManualOffset()
        {
            ManualOffset = Vector3.zero;
        }

        private void UpdateEffectiveTarget()
        {
#if !IK_SERVER
            EffectiveTarget = (Target ? Target.position : GlobalMocapOffset.Instance.GlobalPositionOffset) + TargetOffset + ManualOffset;
#else
            EffectiveTarget = (Target ? Target.position : Vector3.zero) + TargetOffset + ManualOffset;
#endif
        }

        private Vector3 LastMouseVelocity;
        private bool UpdateMouseVelocity(out Vector3 mouseVelocity)
        {
            //Dragging
            var currentMouseVelocity = Vector3.zero;
            if (Input.GetMouseButton((int)DragMouseButton))
            {
                currentMouseVelocity.x = Input.mousePosition.x / Screen.width;
                currentMouseVelocity.y = Input.mousePosition.y / Screen.height;
            }
            else
            {
                currentMouseVelocity.x = currentMouseVelocity.y = 0f;
            }

            //Init last mouse velocity
            if (LastMouseVelocity == Vector3.zero || currentMouseVelocity == Vector3.zero)
                LastMouseVelocity = currentMouseVelocity;

            //Calculate the delta from the last frame
            mouseVelocity = LastMouseVelocity - currentMouseVelocity;

            //Zoom (wheel delta is only 0 or 1 given from unity, no need to recalculate the delta in this case)
            mouseVelocity.z = Input.mouseScrollDelta.y * Time.smoothDeltaTime;// 0.05f;

            LastMouseVelocity = currentMouseVelocity;
            return mouseVelocity != Vector3.zero;
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

    }

}