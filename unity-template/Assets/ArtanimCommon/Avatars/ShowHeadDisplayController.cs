using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Artanim
{
	public class ShowHeadDisplayController : AvatarDisplayController
	{
		[Tooltip("The avatars visual root used to show/hide the avatars body")]
		public GameObject[] AvatarVisuals;

        public override void InitializePlayer(string initials)
        {
            
        }

        public override void ShowAvatar()
		{
			if (AvatarVisuals != null && AvatarVisuals.Length > 0)
			{
				foreach (var avatarVisual in AvatarVisuals)
				{
					if(avatarVisual)
						avatarVisual.SetActive(true);
				}
			}
			else
			{
				Debug.LogError("Failed to show avatar. No AvatarVisualRoot set.");
			}
		}

		public override void HideAvatar()
		{
			if (AvatarVisuals != null && AvatarVisuals.Length > 0)
			{
				foreach (var avatarVisual in AvatarVisuals)
				{
					if (avatarVisual)
						avatarVisual.SetActive(false);
				}
			}
			else
			{
				Debug.LogError("Failed to hide avatar. No AvatarVisualRoot set.");
			}
		}

		public override void ShowHead()
		{
			//Do nothing
		}

		public override void HideHead()
		{
			//DoNothing
		}
	}
}