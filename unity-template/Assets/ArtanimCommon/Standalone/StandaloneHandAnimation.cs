using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(Animator))]
	public class StandaloneHandAnimation : MonoBehaviour
	{
		private const string BOOL_SHOW_PICKKUP = "Show Pickup";

		public Renderer[] IndexRenderers;

		private Animator Animator;

		void Start()
		{
			Animator = GetComponent<Animator>();
		}

		public void ShowPickupable(bool show)
		{
            if(isActiveAndEnabled)
            {
                Animator.SetBool(BOOL_SHOW_PICKKUP, show);
                SetIndexColor(show ? Color.green : Color.white);
            }
		}

		public void ShowPickedUp(bool show)
		{
            if (isActiveAndEnabled)
            {
                Animator.SetBool(BOOL_SHOW_PICKKUP, false);
                SetIndexColor(show ? Color.red : Color.white);
            }
		}

		private void SetIndexColor(Color color)
		{
			if(IndexRenderers != null && IndexRenderers.Length > 0)
			{
				foreach(var renderer in IndexRenderers)
				{
					renderer.material.color = color;
				}
			}
		}

	}
}