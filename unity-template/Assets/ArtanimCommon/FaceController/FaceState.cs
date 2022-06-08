using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Artanim
{
    [Serializable]
    public class BlendShapeValue
    {
        public string Name;

        [Range(0f, 1f)]
        public float Value;
    }

    [CreateAssetMenu(fileName = "FaceState", menuName = "Artanim/Avatar Face State", order = 1)]
    public class FaceState : ScriptableObject
    {
        public enum ELifeCycle { PingPong1, On, }

        [HideInInspector]
        public List<BlendShapeValue> BlendShapeValues = new List<BlendShapeValue>();

        [Header("Jaw")]
        [Range(0f, 1f)]
        public float JawOpen = 0f;

        [Range(-1f, 1f)]
        public float JawLeftRight = 0f;

        [Header("Animation and lifecycle")]
        public ELifeCycle LifeCycle = ELifeCycle.PingPong1;
        public float TransitionSecs = 0.5f;
        public AnimationCurve TransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
    }

}
