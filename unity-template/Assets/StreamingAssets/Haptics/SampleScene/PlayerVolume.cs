using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.SampleScene
{
	[RequireComponent(typeof(HapticAudioEffect))]
	public class PlayerVolume : MonoBehaviour
	{
		public AnimationCurve Volume = AnimationCurve.Constant(0, 1, 1);

		public float Duration;

		float _startTime;
		HapticAudioEffect _effect;

		// Use this for initialization
		void Start()
		{
			_startTime = Time.time;
			_effect = GetComponent<HapticAudioEffect>();
		}

		// Update is called once per frame
		void Update()
		{
			if (_effect)
			{
				float t = Mathf.Repeat((Time.time - _startTime) / Duration, 1);
				float volume = Volume.Evaluate(t);
				foreach (var player in GameController.Instance.RuntimePlayers)
                {
					_effect.SetPlayerVolume(player, volume);
				}
			}
		}
	}
}
