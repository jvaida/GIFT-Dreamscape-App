using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Dreamscape
{

	[ExecuteInEditMode]
	public class DMX_rotate : DMX_subDevice
	{

		public Vector3andSpace moveUnitsPerSecond;
		public Vector3andSpace rotateDegreesPerSecond;
		public bool ignoreTimescale;
		private float m_LastRealTime;

		[Range(1f, 255.0f)]
		public float rotationFactor = 200.0f;

		public new void Reset()
		{
			base.Reset();
			rotateDegreesPerSecond.value = new Vector3(360.0f, 0.0f, 0.0f);
			m_LastRealTime = Time.realtimeSinceStartup;
		}

		// Use this for initialization
		public new void Start()
		{
			base.Start();
			m_LastRealTime = Time.realtimeSinceStartup;
		}

		// Update is called once per frame
		public new void Update()
		{
			base.Update();
			float deltaTime = Time.deltaTime;
			if (ignoreTimescale)
			{
				deltaTime = (Time.realtimeSinceStartup - m_LastRealTime);
				m_LastRealTime = Time.realtimeSinceStartup;
			}

			float factor = deltaTime * _speed / rotationFactor;

			transform.Rotate(rotateDegreesPerSecond.value * factor, moveUnitsPerSecond.space);

		}

		[Serializable]
		public class Vector3andSpace
		{
			public Vector3 value;
			public Space space = Space.Self;
		}
	}

}