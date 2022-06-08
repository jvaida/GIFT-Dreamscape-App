using Artanim.Location.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{ 
    public class ChairConfig : MonoBehaviour
	{
        [Header("Root and Pelvis")]
        public Transform RootTarget;
        public Transform PelvisTarget;

		[Header("Feet")]
        public Transform RightFootTarget;
        public Transform LeftFootTarget;

		[Header("Leg Bend Goals")]
		[Tooltip("Target knee direction. If left empty the knee direction is calculated by the IK.")]
		public Transform RightLegBendGoal;
		[Tooltip("Target knee direction. If left empty the knee direction is calculated by the IK.")]
		public Transform LeftLegBendGoal;

        public GameObject[] ChairVisuals;
        public Renderer[] ColorRenderers;

        public void EnableWheelChair()
        {
			if(ChairVisuals.Length > 0)
            {
                foreach (var visual in ChairVisuals)
                    visual.SetActive(true);
            }
        }

        public void SetColor(Color color)
        {
            if(ColorRenderers.Length > 0)
            {
                foreach (var renderer in ColorRenderers)
                    renderer.material.color = color;
            }
        }

        public virtual void AssignPlayer(SkeletonConfig skeleton) { }

    }
}
