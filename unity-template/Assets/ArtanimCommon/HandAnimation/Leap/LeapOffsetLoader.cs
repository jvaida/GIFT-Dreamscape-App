using Artanim.Location.Network;
using Leap.Unity;
using System.IO;
using UnityEngine;

namespace Artanim.HandAnimation.Leap
{
	[System.Serializable]
	public class LeapOffsetData
	{
		public Quaternion Rotation;
		public Vector3 Translation;
	}


	/// <summary>
	/// Utility component to identify the transform used to indicate the offset between
	/// an avatar's camera, and the relative transform of the Leap Motion device with
	/// respect to this camera. Primarily used by the LeapMotionOffsetCalibration 
	/// </summary>
	public class LeapOffsetLoader : SingletonBehaviour<LeapOffsetLoader>
	{
		public LeapXRServiceProvider LeapXRServiceProvider;
		public Transform OffsetTransform;

		private string _OffsetDataPath;

		private Vector3 _PositionOffset;
		private Quaternion _RotationOffset;

		private void Awake()
		{
			if (NetworkInterface.Instance.IsClient)
			{
				_OffsetDataPath = Path.Combine(Application.persistentDataPath, "leapoffset.json");

				if (File.Exists(_OffsetDataPath))
				{
					Debug.LogWarning("[LeapOffsetLoader] Loading LeapOffsetData");
					string json = File.ReadAllText(_OffsetDataPath);
					LeapOffsetData offsetData = JsonUtility.FromJson<LeapOffsetData>(json);

					SetOffset(offsetData.Translation, offsetData.Rotation);

					UseManualOffset();
				}
				else
				{
					Debug.LogWarning("[LeapOffsetLoader] No Leap offet data found. Using default settings");
				}
			}
		}

		public void SetOffset(Vector3 translation, Quaternion rotation)
		{
			_PositionOffset = translation;
			_RotationOffset = rotation;
		}

		public void UseManualOffset()
		{
			LeapXRServiceProvider.deviceOffsetMode = LeapXRServiceProvider.DeviceOffsetMode.ManualHeadOffset;
			LeapXRServiceProvider.deviceOffsetYAxis = _PositionOffset.y;
			LeapXRServiceProvider.deviceOffsetZAxis = _PositionOffset.z;

			Vector3 eulerAngles = _RotationOffset.eulerAngles;
			LeapXRServiceProvider.deviceTiltXAxis = eulerAngles.x;

			Debug.LogWarning("Switched to manual Leap offset mode");
		}

		public void UseTransformOffset()
		{
			LeapXRServiceProvider.deviceOffsetMode = LeapXRServiceProvider.DeviceOffsetMode.Transform;
			OffsetTransform.localPosition = _PositionOffset;
			OffsetTransform.localRotation = _RotationOffset;
			LeapXRServiceProvider.deviceOrigin = OffsetTransform;

			Debug.LogWarning("Switched to Transform Leap offset mode");
		}
	}
}