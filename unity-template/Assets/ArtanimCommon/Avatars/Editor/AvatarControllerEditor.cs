using Artanim.Location.Messages;
using Artanim.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
	[CustomEditor(typeof(AvatarController))]
	[CanEditMultipleObjects]
	public class AvatarControllerEditor : Editor
	{
        private const int DEFAULT_SDK_AVATAR_LAYER = 21;

        private static Dictionary<EAvatarBodyPart, CapsuleSetting> BODY_PART_COLLIDER_SETTINGS = new Dictionary<EAvatarBodyPart, CapsuleSetting>
        {
            { EAvatarBodyPart.Head, new CapsuleSetting { Center = new Vector3(0f, 0.07f, 0f), Radius = 0.18f, Height = 0f, Direction = CapsuleSetting.EDirection.YAxis, } },
            { EAvatarBodyPart.LeftFoot, new CapsuleSetting { Center = new Vector3(0f, -0.03f, 0f), Radius = 0.05f, Height = 0.25f, Direction = CapsuleSetting.EDirection.YAxis, } },
            { EAvatarBodyPart.RightFoot, new CapsuleSetting { Center = new Vector3(0f, -0.03f, 0f), Radius = 0.05f, Height = 0.25f, Direction = CapsuleSetting.EDirection.YAxis, } },
            { EAvatarBodyPart.LeftHand, new CapsuleSetting { Center = new Vector3(0f, 0.1f, 0f), Radius = 0.05f, Height = 0.2f, Direction = CapsuleSetting.EDirection.YAxis, } },
            { EAvatarBodyPart.RightHand, new CapsuleSetting { Center = new Vector3(0f, 0.1f, 0f), Radius = 0.05f, Height = 0.2f, Direction = CapsuleSetting.EDirection.YAxis, } },
        };


        private Vector3 HandBodyPartOffset = new Vector3(0f, 0.1f, 0f);

        public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			DrawDefaultInspector();


            //Selection
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            if (GUILayout.Button("Select Head")) SelectBodyPart(EAvatarBodyPart.Head);
            if (GUILayout.Button("Select Hand Left")) SelectBodyPart(EAvatarBodyPart.LeftHand);
            if (GUILayout.Button("Select Hand Right")) SelectBodyPart(EAvatarBodyPart.RightHand);
            if (GUILayout.Button("Select Foot Left")) SelectBodyPart(EAvatarBodyPart.LeftFoot);
            if (GUILayout.Button("Select Foot Right")) SelectBodyPart(EAvatarBodyPart.RightFoot);


            //Setup AvatarController
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

            //Collider options
            HandBodyPartOffset = EditorGUILayout.Vector3Field("Hand Bodypart Offset", HandBodyPartOffset);

            if (GUILayout.Button("Setup AvatarController"))
            {
                foreach (var ac in targets.OfType<AvatarController>())
                    SetupAvatarController(ac);
            }

            //Init body parts...
            if (GUILayout.Button("Setup Body Parts"))
            {
                foreach(var ac in targets.OfType<AvatarController>())
                    SetupBodyParts(ac);
            }

            //Setup layer
            if (GUILayout.Button("Setup Render Layer"))
            {
                foreach (var ac in targets.OfType<AvatarController>())
                    SetupLayer(ac);
            }

            //Setup renderers
            if (GUILayout.Button("Setup Renderers"))
            {
                foreach (var ac in targets.OfType<AvatarController>())
                    SetupRenderers(ac);
            }

            if(!string.IsNullOrEmpty(Log))
            {
                EditorGUILayout.HelpBox(Log, MessageType);
            }

            serializedObject.ApplyModifiedProperties();
		}

        #region Internals

        private void SelectBodyPart(EAvatarBodyPart bodyPart)
        {
            Selection.activeTransform = GetBodyPartTransform(bodyPart, target as AvatarController);
        }


        private string Log;
        private MessageType MessageType;
        private void SetActionResult(string message, MessageType messageType)
        {
            Log = message;
            MessageType = messageType;
        }

        private void SetupAvatarController(AvatarController avatarController)
        {
            var log = "";
            var animator = GetAvatarAnimator(avatarController);

            //Check/setup animator
            if (!animator)
            {
                //Use first human animator in childs
                animator = avatarController.transform.GetComponentsInChildren<Animator>().Where(a => a.avatar && a.avatar.isHuman).FirstOrDefault();
                if(animator)
                {
                    avatarController.GetComponent<IKListener>().AvatarAnimator = animator;
                    log += string.Format("Set {0} as avatar animator.\n", animator.name);
                }
                else
                {
                    Debug.LogErrorFormat("Failed to find animator for avatar: {0}", avatarController.name);
                    return;
                }
            }
            else
            {
                if (!animator.avatar.isHuman)
                {
                    SetActionResult(string.Format("Set avatar in animator is not of human type. Avatar={0}", animator.avatar.name), MessageType.Error);
                    return;
                }
            }

            //Setup controller
            if (avatarController)
            {
                //Set head bone
                avatarController.HeadBone = animator.GetBoneTransform(HumanBodyBones.Head);
                if (avatarController.HeadBone)
                    log += string.Format("Set {0} as head bone.", avatarController.HeadBone.name);
                else
                    Debug.LogErrorFormat("Unable to set head bone for avatar: {0}", avatarController.name);
            }

            SetActionResult(log, MessageType.None);
        }

        
        private void SetupBodyParts(AvatarController avatarController)
        {
            var log = "";
            foreach(EAvatarBodyPart bodyPart in Enum.GetValues(typeof(EAvatarBodyPart)))
            {
                var avatarBone = GetBodyPartBone(bodyPart);
                var bodyPartTransform = GetBodyPartTransform(bodyPart, avatarController);
                if(bodyPartTransform)
                {
                    SetupBodyPart(bodyPart, bodyPartTransform);
                    log += string.Format("Added or updated body part: {0} to {1}\n", bodyPart, bodyPartTransform.name);
                }
                else
                {
                    //No transform found, avatar might not be setup imported properly. Check Mecanim rig
                    log = string.Format("Failed to setup body part:{0}. The avatar animator did not return a transform for bone:{1}. Check the avatar Mecanim rig in the import settings.\n", bodyPart, avatarBone);
                    SetActionResult(log, MessageType.Error);
                }
            }
            SetActionResult(log, MessageType.Info);
        }

        private void SetupBodyPart(EAvatarBodyPart bodyPart, Transform boneTransform)
        {
            //Setup rigidbody
            var rigidbody = GetOrCreateComponent<Rigidbody>(boneTransform);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            //Clear existing colliders
            foreach(var c in boneTransform.GetComponents<Collider>())
                DestroyImmediate(c);

            //Setup collider
            var collider = GetOrCreateComponent<CapsuleCollider>(boneTransform);
            collider.center = BODY_PART_COLLIDER_SETTINGS[bodyPart].Center;
            collider.radius = BODY_PART_COLLIDER_SETTINGS[bodyPart].Radius;
            collider.height = BODY_PART_COLLIDER_SETTINGS[bodyPart].Height;
            collider.direction = (int)BODY_PART_COLLIDER_SETTINGS[bodyPart].Direction;
            collider.isTrigger = true;

            //Setup offset if needed
            if (bodyPart == EAvatarBodyPart.LeftHand || bodyPart == EAvatarBodyPart.RightHand)
                collider.center = HandBodyPartOffset;

            //Setup body part
            var avatarBodyPart = GetOrCreateComponent<AvatarBodyPart>(boneTransform);
            avatarBodyPart.BodyPart = bodyPart;
        }


        private void SetupLayer(AvatarController avatarController)
        {
            var rootLayer = avatarController.gameObject.layer;
            if (LayerMask.LayerToName(rootLayer) == "Default")
                rootLayer = DEFAULT_SDK_AVATAR_LAYER;

            //Apply layer to all childs
            SetChildLayer(avatarController.transform, rootLayer);

            SetActionResult(string.Format("Set all children to layer {0}", LayerMask.LayerToName(rootLayer)), MessageType.Info);
        }

        private void SetupRenderers(AvatarController avatarController)
        {
            foreach(var renderer in avatarController.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.allowOcclusionWhenDynamic = false;
                renderer.updateWhenOffscreen = true;
            }
            SetActionResult("Updated SkinnedMeshRenderers:\n allowOcclusionWhenDynamic = false\n updateWhenOffscreen = true", MessageType.Info);
        }

        private void SetChildLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;

            //Recursive on childs
            if(root.childCount > 0)
            {
                for (var i = 0; i < root.childCount; ++i)
                    SetChildLayer(root.GetChild(i), layer);
            }
        }

        private Animator GetAvatarAnimator(AvatarController avatarController)
        {
            return avatarController.AvatarAnimator;
        }

        private HumanBodyBones GetBodyPartBone(EAvatarBodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case EAvatarBodyPart.LeftFoot: return HumanBodyBones.LeftToes;
                case EAvatarBodyPart.RightFoot: return HumanBodyBones.RightToes;
                case EAvatarBodyPart.LeftHand: return HumanBodyBones.LeftHand;
                case EAvatarBodyPart.RightHand: return HumanBodyBones.RightHand;
                case EAvatarBodyPart.Head: return HumanBodyBones.Head;
                default: return HumanBodyBones.LastBone;
            }
        }

		private Transform GetBodyPartTransform(EAvatarBodyPart bodyPart, AvatarController avatarController)
		{
            var humanBone = GetBodyPartBone(bodyPart);

			if (humanBone != HumanBodyBones.LastBone)
				return GetAvatarAnimator(avatarController).GetBoneTransform(humanBone);
			else
				return null;
		}

        private T GetOrCreateComponent<T>(Transform root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
                component = root.gameObject.AddComponent<T>();
            return component;
        }

        #endregion

        private class CapsuleSetting
        {
            public enum EDirection { XAxis, YAxis, ZAxis, }
            public Vector3 Center { get; set; }
            public float Radius { get; set; }
            public float Height { get; set; }
            public EDirection Direction { get; set; }
        }

    }
}