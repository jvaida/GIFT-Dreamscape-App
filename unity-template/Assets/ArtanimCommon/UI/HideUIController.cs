using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(CanvasGroup))]
	public class HideUIController : MonoBehaviour
	{
		public float UITimeoutSecs = 5f;

		public bool HideOnServer;
		public bool HideOnClient;

		private CanvasGroup CanvasGroup;

		private float LastUserAction;

		void Start()
		{
			CanvasGroup = GetComponent<CanvasGroup>();

			//Do we need to hide the UI at all?
			if (NetworkInterface.Instance.IsServer && !HideOnServer)
				enabled = false;
			else if (NetworkInterface.Instance.IsClient && !HideOnClient)
				enabled = false;
		}

		void Update()
		{
			UpdateIdleTime();
			UpdateIdleAction();
		}

		private Vector3 LastMousePosition;
		private void UpdateIdleTime()
		{
			if (LastMousePosition != Input.mousePosition)
				LastUserAction = Time.time;
			else if (Input.anyKey)
				LastUserAction = Time.time;

			LastMousePosition = Input.mousePosition;
		}

		private void UpdateIdleAction()
		{
			CanvasGroup.alpha = Time.time > LastUserAction + UITimeoutSecs ? 0f : 1f;
		}
	}
}