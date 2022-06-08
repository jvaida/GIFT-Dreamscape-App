using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals
{
    public class HapticDevice : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        float _value = 0.0f;

        Visuals.IHapticDeviceAnimator[] _childDevices;

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
            if (_childDevices == null)
            {
                _childDevices = transform.GetComponentsInChildren<Visuals.IHapticDeviceAnimator>(includeInactive: true);
            }
            foreach (var d in _childDevices)
            {
                d.Value = _value;
            }
        }
    }
}
