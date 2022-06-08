using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.Visuals.Animators
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(ParticleSystem))]
    public class Particles : MonoBehaviour, IHapticDeviceAnimator
    {
        [System.Serializable]
        public struct MinMax
        {
            public float Min, Max;
            public float GetValue(float x) { return Mathf.Lerp(Min, Max, x); }
        }

        [SerializeField]
        [Range(0f, 1f)]
        float _value = 0.0f;

        public MinMax StartLifetime;
        public MinMax StartSpeed;
        public MinMax SimulationSpeed;
        public MinMax RateOverTime;
        public MinMax ForceOverLife;

        private ParticleSystem _particleSystem;

        public float Value
        {
            get { return _value; }
            set { _value = value; }
        }

        // Start is called before the first frame update
        void Start()
        {
            _particleSystem = gameObject.GetComponent<ParticleSystem>();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            var main = _particleSystem.main;
            var emission = _particleSystem.emission;
            var forceOverLife = _particleSystem.forceOverLifetime;
            var sizeOverLifetime = _particleSystem.sizeOverLifetime;

            main.startLifetime = StartLifetime.GetValue(_value);
            main.startSpeed = StartSpeed.GetValue(_value);
            main.simulationSpeed = SimulationSpeed.GetValue(_value);
            emission.rateOverTime = RateOverTime.GetValue(_value);
            forceOverLife.zMultiplier = ForceOverLife.GetValue(_value);

            if (_value == 0)
            {
                //_particleSystem.Start
            }
        }
    }
}
