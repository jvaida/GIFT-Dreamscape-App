using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public class AvatarDisplayController : MonoBehaviour
	{
		public virtual void InitializePlayer(string initials) { }

		/// <summary>
		/// Shows the avatar body.
		/// </summary>
		public virtual void ShowAvatar() { }

		/// <summary>
		/// Hides the avatar body.
		/// </summary>
		public virtual void HideAvatar() { }

		/// <summary>
		/// Shows the avatar head.
		/// </summary>
		public virtual void ShowHead() { }

		/// <summary>
		/// Hides the avatar head.
		/// </summary>
		public virtual void HideHead() { }

	}
}