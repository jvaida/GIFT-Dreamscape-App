using Leap.Unity;
using UnityEngine;

namespace Artanim.HandAnimation.Leap
{
	public class LeapServiceProviderAttacher : ClientSideBehaviour
	{
		void Start()
		{
			var provider = FindObjectOfType<LeapXRServiceProvider>();

			if (provider != null)
			{
				transform.parent = provider.transform;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
			}
		}
	}
}