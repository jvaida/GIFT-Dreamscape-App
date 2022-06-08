using UnityEngine;
using System.Collections;
using Artanim.Location.Messages;

namespace Artanim
{

	[RequireComponent(typeof(CameraFilterPack_Colors_Brightness))]
	[AddComponentMenu("Artanim/VR Camera Fader")]
	public class VRCameraFader : MonoBehaviour, ICameraFader
	{
		public const float WHITE_VALUE = 2f;
		public const float NORMAL_VALUE = 1f;
		public const float BLACK_VALUE = 0f;

		public float FadeSpeed = 5f;

		private CameraFilterPack_Colors_Brightness _cameraFilter;
		public CameraFilterPack_Colors_Brightness CameraFilter
		{
			get
			{
				if (_cameraFilter == null)
					_cameraFilter = GetComponent<CameraFilterPack_Colors_Brightness>();
				return _cameraFilter;
			}
		}

		public float TargetFadeValue = 1f;
		protected Transition TargetTransition { get; set; }

		public Transition GetTragetTransition()
		{
			return TargetTransition;
		}

		public virtual IEnumerator DoFadeAsync(Transition transition, string customTransitionName = null)
		{
			if (ConfigService.VerboseSdkLog)
				Debug.LogFormat("<color=yellow>Setting async fading to {0} on {1} (custom transition={2})</color>",
					transition.ToString(), name, !string.IsNullOrEmpty(customTransitionName) ? customTransitionName : "none");

			TargetTransition = transition;
			TargetFadeValue = GetTransitionTragetValue(TargetTransition);

			while(CameraFilter._Brightness != TargetFadeValue)
			{
				//Wait next frame
				yield return null;
			}

			if (ConfigService.VerboseSdkLog) Debug.Log("<color=yellow>Screen faded. Passing control back to controller</color>");

			yield return true;
		}

		public virtual IEnumerator DoFadeInAsync()
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=yellow>Setting async fade in on {0}</color>", name);

			TargetFadeValue = NORMAL_VALUE;
			TargetTransition = Transition.None;

			while (CameraFilter._Brightness != TargetFadeValue)
			{
				//Wait next frame
				yield return null;
			}

			if (ConfigService.VerboseSdkLog) Debug.Log("<color=yellow>Screen faded in. Passing control back to controller</color>");

			yield return true;
		}

		public virtual void SetFaded(Transition transition, string customTransitionName = null)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=yellow>Setting faded to {0} on {1}</color>", transition.ToString(), name);

			TargetTransition = transition;
			TargetFadeValue = GetTransitionTragetValue(TargetTransition);
			CameraFilter._Brightness = TargetFadeValue;
			//Debug.LogErrorFormat("<color=yellow>Set target fade value: Value={0}, Camera={1}</color>", TargetFadeValue, name);
		}

		void Update()
		{
			if(CameraFilter._Brightness != TargetFadeValue)
			{
				CameraFilter._Brightness = Mathf.Lerp(CameraFilter._Brightness, TargetFadeValue, Time.smoothDeltaTime * FadeSpeed);

				//Debug.LogFormat("<color=yellow>Fader value: {0}, camera={1}</color>", CameraFilter._Brightness, name);

				//Stop condition
				if (Mathf.Abs(CameraFilter._Brightness - TargetFadeValue) < 0.05f)
				{
					//Debug.Log("<color=yellow>Screen faded. Notifying SceneController</color>");
					CameraFilter._Brightness = TargetFadeValue;
				}
			}
		}

		private float GetTransitionTragetValue(Transition transition)
		{
			switch (transition)
			{
				case Transition.FadeWhite:
					return WHITE_VALUE;
				case Transition.FadeBlack:
					return BLACK_VALUE;
				case Transition.None:
					return NORMAL_VALUE;
				default:
					Debug.LogWarning("VRCameraFader does not support custom transitions. Using default: None");
					return NORMAL_VALUE;
			}
		}
	}

}