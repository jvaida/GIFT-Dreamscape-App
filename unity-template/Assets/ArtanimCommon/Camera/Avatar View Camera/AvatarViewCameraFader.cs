using UnityEngine;
using System.Collections;
using Artanim.Location.Messages;

namespace Artanim
{
	public class AvatarViewCameraFader : VRCameraFader
	{
		public override IEnumerator DoFadeAsync(Transition transition, string customTransitionName = null)
		{
			yield return true;
		}

		public override IEnumerator DoFadeInAsync()
		{
			yield return true;
		}

		public override void SetFaded(Transition transition, string customTransitionName = null)
		{
		}
	}

}