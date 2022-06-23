using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	[ExecuteInEditMode]
	[RequireComponent(typeof(ParticleSystem))]
	public class DMX_fanParticals : DMX_subDevice
	{

		[Range(0f, 1.0f)]
		public float min_s_life = 0.5f;
		[Range(0f, 1.0f)]
		public float max_s_Life = 0.5f;

		[Range(0f, 1.0f)]
		public float min_s_speed = 0.05f;
		[Range(0f, 10.0f)]
		public float max_s_speed = 10.0f;

		[Range(0f, 1.0f)]
		public float min_sim_speed = 0.05f;
		[Range(0f, 1.0f)]
		public float max_sim_speed = 0.75f;

		[Range(0f, 1.0f)]
		public float min_Erate_o_time = 0.0f;
		[Range(0f, 50.0f)]
		public float max_Erate_o_time = 50.0f;

		[Range(0f, -0.7f)]
		public float zForceOverLifetime = -0.625f;

		public ParticleSystem[] SubSystems;

		private ParticleSystem ps;


		// Use this for initialization
		public new void Start()
		{
			base.Start();
			ps = gameObject.GetComponent<ParticleSystem>();
		}

		// Update is called once per frame
		public new void Update()
		{
			base.Update();
			float normalSpeed = _speed / 255.0f;
			var main = ps.main;
			var emission = ps.emission;
			var forceOverLife = ps.forceOverLifetime;
			var sizeOverLifetime = ps.sizeOverLifetime;
			emission.enabled = true;

			//Start subsystems
			if (SubSystems != null)
			{
				foreach (var subSystem in SubSystems)
				{
					if (normalSpeed > 0f && !subSystem.isEmitting)
						subSystem.Play();
					else if (normalSpeed == 0f && subSystem.isEmitting)
						subSystem.Stop();
				}
			}

			//Modulate particle system based on fan speed
			main.startLifetime = min_s_life + (max_s_Life - min_s_life) * normalSpeed;
			main.startSpeed = min_s_speed + (max_s_speed - min_s_speed) * normalSpeed;
			main.simulationSpeed = min_sim_speed + (max_sim_speed - min_sim_speed) * normalSpeed;
			emission.rateOverTime = min_Erate_o_time + (max_Erate_o_time - min_Erate_o_time) * normalSpeed;
			forceOverLife.zMultiplier = zForceOverLifetime * normalSpeed;

		}

	}

}