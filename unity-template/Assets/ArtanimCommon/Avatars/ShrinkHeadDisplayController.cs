using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Artanim
{
	public class ShrinkHeadDisplayController : AvatarDisplayController
	{
		private static readonly Vector3 SCALE_HEAD_SHRINK = new Vector3(0.1f, 0.1f, 0.1f);

		[Tooltip("The avatars visual root used to show/hide the avatars body")]
		public GameObject AvatarVisualRoot;

		[Tooltip("The avatars head bone used to shrink/hide the head")]
		public Transform HeadBone;

        public override void InitializePlayer(string initials)
        {

        }

        public override void ShowAvatar()
		{
			if (AvatarVisualRoot)
				AvatarVisualRoot.SetActive(true);
			else
				Debug.LogError("Failed to show avatar. No AvatarVisualRoot set.");
		}

		public override void HideAvatar()
		{
			if (AvatarVisualRoot)
				AvatarVisualRoot.SetActive(false);
			else
				Debug.LogError("Failed to hide avatar. No AvatarVisualRoot set.");
		}

		public override void ShowHead()
		{
			if (HeadBone)
				HeadBone.localScale = Vector3.one;
			else
				Debug.LogError("Failed to hide head. No HeadBone set.");
		}

		public override void HideHead()
		{
			if (HeadBone)
				HeadBone.localScale = SCALE_HEAD_SHRINK;
			else
				Debug.LogError("Failed to hide head. No HeadBone set.");
		}
	}
}