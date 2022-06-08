using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using Artanim.Location.Monitoring;
using Artanim.Location.Data;
using System;
using System.Linq;

namespace Artanim.Tracking
{

	[AddComponentMenu("Artanim/Tracking Rigidbody")]
    public class TrackingRigidbody : BaseTrackingRigidbody
	{
		[Header("Standalone")]
		public bool NotPickupable;

		private void OnEnable()
		{
#if !IK_SERVER
			//Create standalone pickup if needed
			SetupStandalonePickupable();

			//Check rigidbody name against the configured list for now just a warning
			if (!string.IsNullOrEmpty(RigidbodyName))
			{
				if (!ConfigService.Instance.ExperienceConfig.TrackedProps.Any(p => p.Name == RigidbodyName))
				{
					Debug.LogWarningFormat("Rigidbody with name {0} is not in the Tracked Props list in the config!", RigidbodyName);
				}
			}
#endif
		}
		
		#region Standalone pickup

		private void SetupStandalonePickupable()
		{
			if(DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone
				&& !NotPickupable && !ConfigService.Instance.ExperienceSettings.DisableStandalonePickupables)
			{
				if (!GetComponent<StandalonePickupable>() && !GetComponentInParent<MainCameraController>()) //Exclude camera controller rigidbody
				{
					gameObject.AddComponent<StandalonePickupable>();
				}
			}
		}

		#endregion
	}
}