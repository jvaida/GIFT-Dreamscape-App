using System.Collections.Generic;
using UnityEngine;

namespace Artanim.HandAnimation.Procedural
{
    public class SmoothMovement : MonoBehaviour
    {

        [Range(0.01f, 1)]
        public float smoothFactor;
        public bool smoothRotation = true;
        public bool smoothPosition = true;
        public bool smoothLocalTransform = true;
        public bool smoothChilds = true;

        public Transform[] rootTr;
        Transform[] allTr;
        Quaternion[] allTrSmoothRot;
        Vector3[] allTrSmoothPos;


        void AddChildRecursive(Transform root, List<Transform> addTo)
        {
            addTo.Add(root);
            for (int i = 0; i < root.childCount; i++)
            {
                AddChildRecursive(root.GetChild(i), addTo);
            }
        }

        void Start()
        {
            List<Transform> allTrTemp = new List<Transform>();

            for (int i = 0; i < rootTr.Length; i++)
            {
                if (smoothChilds)
                    AddChildRecursive(rootTr[i], allTrTemp);
                else
                    allTrTemp.Add(rootTr[i]);
            }
            
            allTr = allTrTemp.ToArray();
            allTrSmoothRot = new Quaternion[allTr.Length];
            allTrSmoothPos = new Vector3[allTr.Length];
            for (int i = 0; i < allTr.Length; i++)
            {
                allTrSmoothRot[i] = smoothLocalTransform ? allTr[i].localRotation : allTr[i].rotation;
                allTrSmoothPos[i] = smoothLocalTransform ? allTr[i].localPosition : allTr[i].position;
            }
        }

		private void OnEnable()
		{
			if(allTr == null || allTr.Length == 0)
			{
				return;
			}
			
			for (int i = 0; i < allTr.Length; i++)
			{
				allTrSmoothRot[i] = smoothLocalTransform ? allTr[i].localRotation : allTr[i].rotation;
				allTrSmoothPos[i] = smoothLocalTransform ? allTr[i].localPosition : allTr[i].position;
			}
		}

		// Update is called once per frame
		void LateUpdate()
        {
            for (int i = 0; i < allTr.Length; i++)
            {
                if (smoothRotation)
                {
                    Quaternion tgtRotation = smoothLocalTransform ? allTr[i].localRotation : allTr[i].rotation;
                    allTrSmoothRot[i] = Quaternion.Slerp(allTrSmoothRot[i], tgtRotation, smoothFactor);
                    if (smoothLocalTransform)
                        allTr[i].localRotation = allTrSmoothRot[i];
                    else
                        allTr[i].rotation = allTrSmoothRot[i];
                }
                if (smoothPosition)
                {
                    Vector3 tgtPosition = smoothLocalTransform ? allTr[i].localPosition : allTr[i].position;
                    allTrSmoothPos[i] = Vector3.Lerp(allTrSmoothPos[i], tgtPosition, smoothFactor);
                    if (smoothLocalTransform)
                        allTr[i].localPosition = allTrSmoothPos[i];
                    else
                        allTr[i].position = allTrSmoothPos[i];
                }
            }
        }
    }
}