using Artanim.Haptics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals.Animators
{
    public class AnimatedScale : MonoBehaviour, IHapticDeviceAnimator
    {
        public float MinFrequency = 1f;
        public float MaxFrequency = 1f;
        public float MinScale = 0.9f;
        public float MaxScale = 1f;

        Vector3 _initialScale;
        float _time = 0;

        public float Value { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            _initialScale = transform.localScale;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Value <= 0)
            {
                transform.localScale = _initialScale;
                _time = 0;
            }
            else
            {
                float freq = Mathf.Lerp(MinFrequency, MaxFrequency, Value);
                _time = (Time.deltaTime * freq + _time) % 1f;
                float max = Mathf.Lerp(MinScale, MaxScale, Value);
                float v = Mathf.LerpUnclamped(MinScale, max, Mathf.Cos(2 * Mathf.PI * _time));
                transform.localScale = v * _initialScale;
            }
        }
    }
}
