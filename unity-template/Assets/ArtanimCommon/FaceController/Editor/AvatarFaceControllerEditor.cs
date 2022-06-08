using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomEditor(typeof(AvatarFaceController))]
	[CanEditMultipleObjects]
    public class AvatarFaceControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            if(GUILayout.Button("Reset Face"))
            {
                foreach(var fc in targets.OfType<AvatarFaceController>())
                    fc.ResetPreviewFaceState();
            }

            EditorGUILayout.Space();
            if(GUILayout.Button("Add all blendshape renderers"))
            {
                foreach (var fc in targets.OfType<AvatarFaceController>())
                {

                    if (fc != null)
                        fc.FaceRenderers = fc.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0).ToList();
                }
            }

            EditorGUILayout.Space();
            if(GUILayout.Button("Add/update Eye Hotspot"))
            {
                foreach (var fc in targets.OfType<AvatarFaceController>())
                    AddEyeHotspot(fc);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddEyeHotspot(AvatarFaceController faceController)
        {
            var avatarController = faceController.GetComponent<AvatarController>();
            if(avatarController)
            {
                var head = avatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.Head);
                if(head)
                {
                    var eyeHotspot = head.transform.GetComponentInChildren<EyesHotSpot>();

                    //Remove first?
                    if(eyeHotspot && eyeHotspot.gameObject.name == "Face Eye Hotspot")
                    {
                        DestroyImmediate(eyeHotspot.gameObject);
                        eyeHotspot = null;
                    }

                    if(!eyeHotspot)
                    {
                        //Setup new one...
                        //Create root
                        var hotspotRoot = new GameObject("Face Eye Hotspot");
                        hotspotRoot.transform.parent = head.transform;

                        //Position to eye center
                        var leftEye = avatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.LeftEye).position;
                        var rightEye = avatarController.AvatarAnimator.GetBoneTransform(HumanBodyBones.RightEye).position;
                        var eyeCenter = leftEye - ((leftEye - rightEye) / 2);
                        hotspotRoot.transform.position = eyeCenter;
                        hotspotRoot.transform.localRotation = Quaternion.identity;

                        //Add trigger
                        var trigger = hotspotRoot.AddComponent<BoxCollider>();
                        trigger.isTrigger = true;
                        trigger.center = new Vector3(0f, 0f, 0.75f);
                        trigger.size = new Vector3(1.5f, 0.5f, 1.5f);

                        //Add hotspot
                        eyeHotspot = hotspotRoot.AddComponent<EyesHotSpot>();
                        eyeHotspot.Priority = 1;
                        eyeHotspot.MinHorizontalAngle = -30f;
                        eyeHotspot.MaxHorizontalAngle = 30f;
                        eyeHotspot.MinVerticalAngle = -20f;
                        eyeHotspot.MaxVerticalAngle = 20f;
                    }
                    else
                    {
                        Debug.LogError("Unable to setup eye hotspot. The avatar head already has a custom EyeHotSpot.");
                    }
                }
                else
                {
                    Debug.LogError("Unable to setup eye hotspot. No head bone found on avatar.");
                }
            }
            else
            {
                Debug.LogError("Unable to setup eye hotspot. No AvatarController found.");
            }
        }
        
    }
}