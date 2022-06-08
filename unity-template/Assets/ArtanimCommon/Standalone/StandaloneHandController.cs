using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	[RequireComponent(typeof(AvatarBodyPart))]
	public class StandaloneHandController : MonoBehaviour
	{
        public bool ShowHideHands = false;

		private StandalonePickupable OverPickupable;
		private StandalonePickupable RegisteredPickup;

		private AvatarBodyPart AvatarBodyPart;
		private StandaloneHandAnimation HandAnimation;

		private void Start()
		{
			AvatarBodyPart = GetComponent<AvatarBodyPart>();
			HandAnimation = GetComponentInChildren<StandaloneHandAnimation>();
		}

		private void OnTriggerEnter(Collider other)
		{
			var pickup = GetPickup(other);
			if (pickup)
			{
				OverPickupable = pickup;
			}
		}

		private void OnTriggerExit(Collider other)
		{
			var pickup = GetPickup(other);
			if(pickup)
			{
				OverPickupable = null;
			}
		}

		private void Update()
		{
			//Update hand animations
			if(HandAnimation)
			{
				if (RegisteredPickup)
                {
                    if (ShowHideHands) HandAnimation.gameObject.SetActive(true);
					HandAnimation.ShowPickedUp(true);
                }
				else if (OverPickupable)
                {
                    if (ShowHideHands) HandAnimation.gameObject.SetActive(true);
                    HandAnimation.ShowPickupable(true);
                }
				else
                {
                    if (ShowHideHands) HandAnimation.gameObject.SetActive(false);
                    HandAnimation.ShowPickupable(false);
                }
			}

			var pickupPressed = PickupButtonPressed();
			if (!RegisteredPickup && pickupPressed && OverPickupable)
			{
				//Pickup
				RegisterPickup(OverPickupable);
			}
			else if(RegisteredPickup && !pickupPressed)
			{
				//Let down
				UnRegisterPickup();
			}

		}

		private void RegisterPickup(StandalonePickupable pickup)
		{
			if (!RegisteredPickup && !pickup.RegisteredParent)
			{
				//Debug.LogErrorFormat("Registering pickup: {0} to {1}", pickup.name, name);
				RegisteredPickup = pickup;
				RegisteredPickup.RegisterParent(transform);
			}
		}

		private void UnRegisterPickup()
		{
			if (RegisteredPickup && RegisteredPickup.RegisteredParent == transform)
			{
				//Debug.LogErrorFormat("Unregistering pickup: {0} to {1}", RegisteredPickup.name, name);
				RegisteredPickup.UnregisterParent();
				RegisteredPickup = null;
			}
		}

		private StandalonePickupable GetPickup(Collider other)
		{
			if(other)
			{
				return other.GetComponentInParent<StandalonePickupable>();
			}
			return null;
		}

		private bool PickupButtonPressed()
		{
			return DevelopmentMode.GetAxis(
				AvatarBodyPart.BodyPart ==
					Location.Messages.EAvatarBodyPart.LeftHand ? DevelopmentMode.AXIS_STANDALONE_PICKUP_LEFT : DevelopmentMode.AXIS_STANDALONE_PICKUP_RIGHT) == 1f;
		}
	}
}