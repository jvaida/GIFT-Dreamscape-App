using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Artanim
{

	public class MouseOrbit : MonoBehaviour
	{
        //Speed factor applied to avoid updating all the prefabs after applying delta time
        private const float SpeedFactor = 30f;

		public enum EMouseButton { Left, Right }

		public Vector3 LookAtOffset;

		public float SpeedHorizontal = 25.0f;
		public float SpeedVertical = 25.0f;

		public float VerticalMinLimit = -20f;
		public float VerticalMaxLimit = 90f;

		public float DistanceMin = 1f;
		public float DistanceMax = 7f;

		public float SlowdownTime = 5f;

		public EMouseButton MouseButton;
		public float Distance = 5.0f;

		public float translationSpeed = 0;

		private float RotationYAxis = 0.0f;
		private float RotationXAxis = 0.0f;

		private float VelocityX = 0.0f;
		private float VelocityY = 0.0f;

		private Vector2 LastMousePerc;
		private Vector3 LookAtTarget;
		private Vector3 InitialLookAtOffset;
		private Quaternion InitialRotation;

		public void Reset()
		{
			if (transform.localRotation == InitialRotation)
			{
				LookAtOffset = InitialLookAtOffset;
			}

			transform.localRotation = InitialRotation;
			Vector3 angles = transform.eulerAngles;
			RotationYAxis = angles.y;
			RotationXAxis = angles.x;

			VelocityX = VelocityY = 0;
			var camCtrl = MainCameraController.Instance;
			if (camCtrl)
				LookAtTarget = camCtrl.GlobalOffset.position;
		}

		// Use this for initialization
		void Start()
		{
			InitialLookAtOffset = LookAtOffset;
			InitialRotation = transform.localRotation;
			Reset();
		}

		void Update()
		{
			//Distance
			Distance += Input.mouseScrollDelta.y * -0.25f;
			Distance = Mathf.Clamp(Distance, DistanceMin, DistanceMax);

			if (Input.GetMouseButton((int)MouseButton))
			{
				//VelocityX += SpeedHorizontal * Input.GetAxis("Mouse X") * 0.02f;
				//VelocityY += SpeedVertical * Input.GetAxis("Mouse Y") * 0.02f;

				var mousePos = Input.mousePosition;
				var currentMousePerc = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);

				if (LastMousePerc == Vector2.zero)
					LastMousePerc = currentMousePerc;

				VelocityX += SpeedHorizontal * (currentMousePerc.x - LastMousePerc.x) * Time.smoothDeltaTime * SpeedFactor;
				VelocityY += SpeedVertical * (currentMousePerc.y - LastMousePerc.y) * Time.smoothDeltaTime * SpeedFactor; 

                LastMousePerc = currentMousePerc;
			}
			else
			{
				LastMousePerc = Vector2.zero;
			}

			RotationYAxis += VelocityX;
			RotationXAxis -= VelocityY;

			RotationXAxis = ClampAngle(RotationXAxis, VerticalMinLimit, VerticalMaxLimit);

			Quaternion rotation = Quaternion.Euler(RotationXAxis, RotationYAxis, 0);

			Vector3 negDistance = new Vector3(0.0f, 0.0f, -Distance);
			Vector3 position = rotation * negDistance + LookAtTarget + LookAtOffset;
			transform.localRotation = rotation;
			transform.localPosition = position;

			VelocityX = Mathf.Lerp(VelocityX, 0, Time.smoothDeltaTime * SlowdownTime);
			VelocityY = Mathf.Lerp(VelocityY, 0, Time.smoothDeltaTime * SlowdownTime);

			if (translationSpeed != 0)
			{
				UpdateTranslation();
			}
		}

		void UpdateTranslation()
		{
			Vector3 offset = Vector3.zero;
			if (Input.GetKey(KeyCode.RightArrow))
			{
				offset += Vector3.right;
			}
			if (Input.GetKey(KeyCode.LeftArrow))
			{
				offset += Vector3.left;
			}
			if (Input.GetKey(KeyCode.PageUp))
			{
				offset += Vector3.up;
			}
			if (Input.GetKey(KeyCode.PageDown))
			{
				offset += Vector3.down;
			}
			if (Input.GetKey(KeyCode.UpArrow))
			{
				offset += Vector3.forward;
			}
			if (Input.GetKey(KeyCode.DownArrow))
			{
				offset += Vector3.back;
			}
			if (Input.GetKeyDown(KeyCode.Home))
			{
				Reset();
			}
			else if (offset != Vector3.zero)
			{
				offset *= translationSpeed * Time.deltaTime;
				LookAtOffset += Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0) * offset;
			}
		}

		static float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;
			return Mathf.Clamp(angle, min, max);
		}
	}

}