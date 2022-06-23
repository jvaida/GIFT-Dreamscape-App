using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Dreamscape
{

	[ExecuteInEditMode]
	public class DMX_arm : DMX_subDevice
	{
		//public GameObject[] rotateDevices;

		[Range(1f, 5.0f)]
		public float rotationFactor = 1.24f;

		//public Vector3andSpace moveUnitsPerSecond;
		//public Vector3andSpace rotateDegreesPerSecond;


		// Update is called once per frame
		public new void Update()
		{
			base.Update();
			float factor = ((255.0f - _speed) * 100.0f) / (rotationFactor * 255.0f);
			transform.localEulerAngles = new Vector3(factor, 0.0f, 0.0f);
		}

		[Serializable]
		public class Vector3andSpace
		{
			public Vector3 value;
			public Space space = Space.Self;
		}
	}

}