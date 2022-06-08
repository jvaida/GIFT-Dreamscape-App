using UnityEngine;
using System.Collections;

namespace Artanim
{

	public class MenuCameraController : MonoBehaviour
	{
		public Transform LookAtTarget;
		public float Speed = 15f;

		void Start ()
		{
	
		}
	
		void Update()
		{
			if(LookAtTarget)
			{
				transform.RotateAround(LookAtTarget.position, Vector3.up, Speed * Time.smoothDeltaTime);
				transform.LookAt(LookAtTarget);
			}
		}
	}

}