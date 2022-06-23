using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	[ExecuteInEditMode]
	public class DMX_powerArrow : DMX_subDevice
	{
		[Range(0f, 100.0f)]
		public float phase_1 = 25f;



		//int blendShapeCount;
		SkinnedMeshRenderer skinnedMeshRenderer;
		Mesh skinnedMesh;

		int noPower_blendIndex = 0;
		int lowPower_blendIndex = 0;
		int highPower_blendIndex = 0;


		void Awake()
		{
			skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			skinnedMesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
			if (phase_1 < 1f)
			{
				phase_1 = 1f;
			}
		}

		// Use this for initialization
		public new void Start()
		{
			base.Start();
			//blendShapeCount = skinnedMesh.blendShapeCount;
			noPower_blendIndex = skinnedMesh.GetBlendShapeIndex("No_power");
			lowPower_blendIndex = skinnedMesh.GetBlendShapeIndex("Low_power");
			highPower_blendIndex = skinnedMesh.GetBlendShapeIndex("High_Power");
		}

		// Update is called once per frame
		public new void Update()
		{
			base.Update();
			float speed_normal = _speed / 2.55f;
			if (speed_normal <= 0.0)
			{
				skinnedMeshRenderer.SetBlendShapeWeight(noPower_blendIndex, 100.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(lowPower_blendIndex, 0.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(highPower_blendIndex, 0.0f);
			}
			else if (speed_normal < phase_1)
			{
				float normalValue = 100.0f * speed_normal / phase_1;
				skinnedMeshRenderer.SetBlendShapeWeight(noPower_blendIndex, 100.0f - normalValue / 2.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(lowPower_blendIndex, 0.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(highPower_blendIndex, 0.0f);

			}
			else if (speed_normal < 100.0f)
			{
				float normalValue = 100.0f * (speed_normal - phase_1) / (100.0f - phase_1);
				skinnedMeshRenderer.SetBlendShapeWeight(noPower_blendIndex, 50.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(lowPower_blendIndex, 0.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(highPower_blendIndex, normalValue);
			}
			else
			{
				skinnedMeshRenderer.SetBlendShapeWeight(noPower_blendIndex, 50.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(lowPower_blendIndex, 0.0f);
				skinnedMeshRenderer.SetBlendShapeWeight(highPower_blendIndex, 100.0f);
			}


		}
	}

}