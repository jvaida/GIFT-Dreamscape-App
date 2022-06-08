using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(AudioSource))]
	public class ClientScreenshotController : MonoBehaviour
	{
		public AudioClip ShutterSound;

		private AudioSource _AudioSource;
		private AudioSource AudioSource
		{ 
			get
			{
				if (!_AudioSource)
					_AudioSource = GetComponent<AudioSource>();
				return _AudioSource;
			}
		}

		public void DoScreenshot()
		{
			if (ShutterSound)
				AudioSource.PlayOneShot(ShutterSound);

			if (ScreenshotController.Instance)
				ScreenshotController.Instance.TakeScreenshot(gameObject.name);
		}

		void OnTriggerEnter(Collider other)
		{
			var bodyPart = other.GetComponent<AvatarBodyPart>();
			if (bodyPart && (bodyPart.BodyPart == Location.Messages.EAvatarBodyPart.LeftHand || bodyPart.BodyPart == Location.Messages.EAvatarBodyPart.RightHand))
			{
				var ac = bodyPart.GetComponentInParent<AvatarController>();
				if(ac && ac.IsMainPlayer)
				{
					DoScreenshot();
				}
			}
		}
	}
}