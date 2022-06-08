using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	public class DMX_subDevice : MonoBehaviour
	{

		[Range(0f, 255.0f)]
		public float speed = 0.0f;
		protected float _speed = 0.0f;

		public void Reset()
		{

		}

		// Use this for initialization
		public void Start()
		{

		}

		// Update is called once per frame
		public void Update()
		{
			_speed = speed * 255.0f;

		}
	}

}