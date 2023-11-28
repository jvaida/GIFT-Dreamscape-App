using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals.Animators
{
    public class SwitchTransform : MonoBehaviour, IHapticDeviceAnimator
    {
        public bool _switchPosition;
        public Vector3 _position = Vector3.zero;
        public bool _switchedRotation;
        public Vector3 _rotation = Vector3.zero;
        public bool _switchScale;
        public Vector3 _scale = Vector3.one;

        public float Value { get; set; }

        Vector3 _initialPosition, _initialScale;
        Quaternion _initialRotation;

        // Start is called before the first frame update
        void Start()
        {
            _initialPosition = transform.localPosition;
            _initialRotation = transform.localRotation;
            _initialScale = transform.localScale;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Value <= 0)
            {
                transform.localPosition = _initialPosition;
                transform.localRotation = _initialRotation;
                transform.localScale = _initialScale;
            }
            else
            {
                if (_switchPosition)
                {
                    transform.localPosition = _position;
                }
                if (_switchedRotation)
                {
                    transform.localRotation = Quaternion.Euler(_rotation);
                }
                if (_switchScale)
                {
                    transform.localScale = _scale;
                }

            }
        }
    }
}
