using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tools
{
    public class AvatarCopyPoseWindow : EditorWindow
    {
        private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header copy avatar pose.jpg";

        [MenuItem("Artanim/Tools/Copy Avatar Pose...", priority = 2)]
        static void Open()
        {
            var avatarPoseWindow = (AvatarCopyPoseWindow)GetWindow(typeof(AvatarCopyPoseWindow), focus: true, title: "Copy Avatar Pose", utility: true);
            avatarPoseWindow.minSize = new Vector2(600f, 600f);
            avatarPoseWindow.maxSize = new Vector2(600f, 600f);
            avatarPoseWindow.Show();
        }

        private Texture2D ImageHeader;


        public AvatarController SourceAvatar;
        public AvatarController[] TargetAvatars;

        public bool CopyBody = true;
        public bool CopyLegs = true;
        public bool CopyArms = true;
        public bool CopyHands = true;

        private void Awake()
        {
            var mainFolder = EditorUtils.GetSDKAssetFolder();
            if (!string.IsNullOrEmpty(mainFolder))
            {
                ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));
            }

            //Pre-select source if one is already selected
            if(Selection.activeGameObject)
            {
                SourceAvatar = Selection.activeGameObject.GetComponent<AvatarController>();
            }
        }

        private Vector2 _scrollPos = Vector2.zero;
        private void OnGUI()
        {
            ScriptableObject target = this;
            SerializedObject serializedObject = new SerializedObject(target);

            if (ImageHeader)
                GUILayout.Label(ImageHeader);

            EditorGUIUtility.labelWidth = 250f;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                //Source avatar
                EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceAvatar"));
                EditorGUILayout.Space();

                //Target avatars
                EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

                if (GUILayout.Button("Set selection as targets"))
                    TargetAvatars = Selection.gameObjects.Where(go => go.GetComponent<AvatarController>() != null).Select(go => go.GetComponent<AvatarController>()).Where(ac => ac != SourceAvatar).ToArray();

                if (TargetAvatars != null && TargetAvatars.Length > 0)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("TargetAvatars"), true);

                EditorGUILayout.Space();

                //Copy settings
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CopyBody"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CopyLegs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CopyArms"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CopyHands"));
                EditorGUILayout.Space();

                //Copy avatar...
                GUI.enabled = SourceAvatar && SourceAvatar.AvatarAnimator && SourceAvatar.isActiveAndEnabled && TargetAvatars != null && TargetAvatars.Length > 0;

                if (GUILayout.Button("Apply Avatar Pose"))
                    ApplyAvatarPose();

                if (GUILayout.Button("Apply Avatar Prefabs"))
                    ApplyAvatarPrefabs();

                GUI.enabled = true;

                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyAvatarPose()
        {

            foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if(NeedCopy(bone))
                {
                    var sourceTransform = SourceAvatar.AvatarAnimator.GetBoneTransform(bone);
                    foreach(var targetAvatar in TargetAvatars)
                    {
                        var targetTransform = targetAvatar.AvatarAnimator.GetBoneTransform(bone);
                        targetTransform.localRotation = sourceTransform.localRotation;
                    }
                }
            }
        }

        private void ApplyAvatarPrefabs()
        {
            foreach(var targetAvatar in TargetAvatars)
            {
                PrefabUtility.ReplacePrefab(targetAvatar.gameObject, PrefabUtility.GetPrefabParent(targetAvatar), ReplacePrefabOptions.ConnectToPrefab);
            }
        }

        private bool NeedCopy(HumanBodyBones bone)
        {
            if (CopyBody && BODY_BONES.Contains(bone))
                return true;
            else if (CopyLegs && LEG_BONES.Contains(bone))
                return true;
            else if (CopyArms && ARM_BONES.Contains(bone))
                return true;
            else if (CopyHands && HAND_BONES.Contains(bone))
                return true;
            else
                return false;
        }

        #region Body part definitions

        private static HumanBodyBones[] BODY_BONES = new HumanBodyBones[]
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.Jaw,
            HumanBodyBones.UpperChest,
            HumanBodyBones.LeftEye,
            HumanBodyBones.RightEye,
        };

        private static HumanBodyBones[] LEG_BONES = new HumanBodyBones[]
        {
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightToes,
        };

        private static HumanBodyBones[] ARM_BONES = new HumanBodyBones[]
        {
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm,
        };

        private static HumanBodyBones[] HAND_BONES = new HumanBodyBones[]
        {
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftThumbProximal,
            HumanBodyBones.LeftThumbIntermediate,
            HumanBodyBones.LeftThumbDistal,
            HumanBodyBones.LeftIndexProximal,
            HumanBodyBones.LeftIndexIntermediate,
            HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftMiddleIntermediate,
            HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal,
            HumanBodyBones.LeftRingIntermediate,
            HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal,
            HumanBodyBones.LeftLittleIntermediate,
            HumanBodyBones.LeftLittleDistal,
            HumanBodyBones.RightThumbProximal,
            HumanBodyBones.RightThumbIntermediate,
            HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal,
            HumanBodyBones.RightIndexIntermediate,
            HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.RightMiddleIntermediate,
            HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal,
            HumanBodyBones.RightRingIntermediate,
            HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal,
            HumanBodyBones.RightLittleIntermediate,
            HumanBodyBones.RightLittleDistal,
        };

        #endregion

    }
}