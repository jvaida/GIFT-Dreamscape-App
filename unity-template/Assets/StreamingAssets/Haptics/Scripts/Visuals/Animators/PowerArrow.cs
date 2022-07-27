using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals.Animators
{
	[ExecuteInEditMode]
	public class PowerArrow : MonoBehaviour, IHapticDeviceAnimator
	{
		[SerializeField]
		[Range(0f, 1f)]
		float _targetValue = 0f;

		public float AnimationSpeed = 1f;
		public float AnimationScale = 1f;

		float _currentValue;
		SkinnedMeshRenderer skinnedMeshRenderer;
		int noPowerBlendIndex, lowPowerBlendIndex, highPowerBlendIndex;

		public float Value
		{
			get { return _targetValue; }
			set { _targetValue = value; }
		}

		// Use this for initialization
		void Start()
		{
			skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			noPowerBlendIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("No_power");
			lowPowerBlendIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Low_power");
			highPowerBlendIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("High_Power");
		}

		// Update is called once per frame
		void LateUpdate()
		{
			_targetValue = Mathf.Clamp01(_targetValue);
			_currentValue = Mathf.Clamp01(Mathf.Lerp(_currentValue, _targetValue, Time.deltaTime * AnimationSpeed));

			float val = AnimationScale * _currentValue;
			skinnedMeshRenderer.SetBlendShapeWeight(noPowerBlendIndex, 100f * (1 - val));
			skinnedMeshRenderer.SetBlendShapeWeight(lowPowerBlendIndex, 0.0f);
			skinnedMeshRenderer.SetBlendShapeWeight(highPowerBlendIndex, 100f * val);
		}
	}
}
