using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals.Animators
{
    [ExecuteInEditMode]
    public class Rotation : MonoBehaviour, IHapticDeviceAnimator
    {
        [SerializeField]
        [Range(0f, 1f)]
        float _value = 0f;

        public Vector3 RotationLocalAxis = Vector3.forward;

        public float AnimationSpeed = 1;

        public float Value
        {
            get { return _value; }
            set { _value = value; }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void LateUpdate()
        {
            float angle = AnimationSpeed * _value * Time.deltaTime;
            transform.Rotate(RotationLocalAxis, angle);
        }
    }
}
