using Artanim.HandAnimation.Procedural;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim.HandAnimation.Config
{
    [CustomEditor(typeof(AvatarHandDefinition))]
    public class AvatarHandDefinitionEditor : Editor
    {
        private AvatarHandController PreviewAvatar;

        private bool _ShowReach;
        private bool ShowReach { get { return _ShowReach; } set { _ShowReach = value; SceneView.RepaintAll(); } }

        private bool _ShowHandRotation;
        private bool ShowHandRotation { get { return _ShowHandRotation; } set { _ShowHandRotation = value; SceneView.RepaintAll(); } }

        private bool _ShowHandForward;
        private bool ShowHandForward { get { return _ShowHandForward; } set { _ShowHandForward = value; SceneView.RepaintAll(); } }

        private bool _ShowPalmFacing;
        private bool ShowPalmFacing { get { return _ShowPalmFacing; } set { _ShowPalmFacing = value; SceneView.RepaintAll(); } }

        private bool _ShowHandAlignment;
        private bool ShowHandAlignment { get { return _ShowHandAlignment; } set { _ShowHandAlignment = value; SceneView.RepaintAll(); } }

        private bool _ShowDeviation;
        private bool ShowDeviation { get { return _ShowDeviation; } set { _ShowDeviation = value; SceneView.RepaintAll(); } }


        public override void OnInspectorGUI()
        {
            var avatarHandDefinition = target as AvatarHandDefinition;

            //Avatar preview
            PreviewAvatar = EditorGUILayout.ObjectField("Preview Avatar", PreviewAvatar, typeof(AvatarHandController), true) as AvatarHandController;

            GUI.enabled = PreviewAvatar;
            {
                ShowReach = EditorGUILayout.Toggle("Show Reach", ShowReach);
                ShowHandRotation = EditorGUILayout.Toggle("Show Hand Rotation", ShowHandRotation);
                ShowHandForward = EditorGUILayout.Toggle("Show Hand Forward", ShowHandForward);
                ShowPalmFacing = EditorGUILayout.Toggle("Show Palm Facing", ShowPalmFacing);
                ShowHandAlignment = EditorGUILayout.Toggle("Show Hand Alignment", ShowHandAlignment);
                ShowDeviation = EditorGUILayout.Toggle("Show Deviation", ShowDeviation);

            }
            GUI.enabled = true;
            EditorGUILayout.Space();


            //Default inspector
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();


            //Thumb poses
            GUI.enabled = PreviewAvatar;
            {
                EditorGUILayout.Space();

                //Left Thumb
                EditorGUILayout.LabelField("Left Thumb Poses", EditorStyles.boldLabel);
                DrawThumbPose(ThumbSetup.EThumbPose.Neutral, AvatarHandDefinition.ESide.Left, avatarHandDefinition);
                DrawThumbPose(ThumbSetup.EThumbPose.Abducted, AvatarHandDefinition.ESide.Left, avatarHandDefinition);
                DrawThumbPose(ThumbSetup.EThumbPose.Adducted, AvatarHandDefinition.ESide.Left, avatarHandDefinition);

                //Right Thumb
                EditorGUILayout.LabelField("Right Thumb Poses", EditorStyles.boldLabel);
                DrawThumbPose(ThumbSetup.EThumbPose.Neutral, AvatarHandDefinition.ESide.Right, avatarHandDefinition);
                DrawThumbPose(ThumbSetup.EThumbPose.Abducted, AvatarHandDefinition.ESide.Right, avatarHandDefinition);
                DrawThumbPose(ThumbSetup.EThumbPose.Adducted, AvatarHandDefinition.ESide.Right, avatarHandDefinition);
            }
            GUI.enabled = true;
        }

        void OnEnable()
        {
            //Try to find an avatar in scene
            PreviewAvatar = FindObjectOfType<AvatarHandController>();

            Debug.LogFormat("activeObject: {0}", Selection.activeObject ? Selection.activeObject.name : "none");
            Debug.LogFormat("activeGameObject: {0}", Selection.activeGameObject ? Selection.activeGameObject.name : "none");
            Debug.LogFormat("activeTransform: {0}", Selection.activeTransform ? Selection.activeTransform.name : "none");

            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private static readonly HumanBodyBones[] HAND_BONES = new HumanBodyBones[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand, };
        void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type == EventType.Repaint && PreviewAvatar)
            {
                var avatarHandDefinition = target as AvatarHandDefinition;
                if (avatarHandDefinition)
                {
                    Color handleStartColor = Handles.color;

                    Vector3 handPosition = Vector3.zero;
                    float axisLength = 0.10f;
                    float arcRadius = 0.04f;

                    Matrix4x4 current = Handles.matrix;

                    foreach(var hand in HAND_BONES)
                    {
                        var handSetup = avatarHandDefinition.GetHandSetup(hand == HumanBodyBones.LeftHand ? AvatarHandDefinition.ESide.Left : AvatarHandDefinition.ESide.Right);
                        var handTransform = PreviewAvatar.GetComponent<AvatarController>().AvatarAnimator.GetBoneTransform(hand);
                        Handles.matrix = handTransform.transform.localToWorldMatrix;

                        if (ShowReach)
                        {
                            Vector3 direction = handSetup.HandForward;
                            Handles.color = Color.yellow;
                            Handles.DrawLine(handPosition, handPosition + avatarHandDefinition.Reach * direction);
                        }

                        if (ShowHandRotation)
                        {
                            Vector3 direction = handSetup.RotationAxis;
                            Vector3 end = handPosition + axisLength * direction;
                            Handles.color = Color.red;
                            DrawRotationAxisHandle(handPosition, end, axisLength, arcRadius);
                        }

                        if (ShowHandForward)
                        {
                            Vector3 direction = handSetup.HandForward;
                            Vector3 end = handPosition + axisLength * direction;
                            Handles.color = Color.green;
                            DrawRotationAxisHandle(handPosition, end, axisLength, arcRadius);
                        }
                        if (ShowPalmFacing)
                        {
                            Vector3 direction = handSetup.PalmFacing;
                            Vector3 end = handPosition + axisLength * direction;
                            Handles.color = Color.blue;
                            DrawRotationAxisHandle(handPosition, end, axisLength, arcRadius);
                        }
                        if (ShowHandAlignment)
                        {
                            Vector3 direction = handSetup.HandAlignmentAxis;
                            Vector3 end = handPosition + axisLength * direction;
                            Handles.color = Color.cyan;
                            DrawAxisHandle(handPosition, end);
                        }
                        if (ShowDeviation)
                        {
                            Vector3 reachVector = avatarHandDefinition.Reach * handSetup.HandForward;
                            Handles.color = Color.magenta;
                            Handles.DrawLine(Vector3.zero, Quaternion.AngleAxis(handSetup.DeviationAngleRange.x, handSetup.PalmFacing) * reachVector);
                            Handles.DrawLine(Vector3.zero, Quaternion.AngleAxis(handSetup.DeviationAngleRange.y, handSetup.PalmFacing) * reachVector);
                        }
                    }


                    Handles.matrix = current;
                    Handles.color = handleStartColor;
                }

            }
        }

        //private HandSetup GetControllerHandSetup(AvatarHandDefinition handDefinition, HandController handController)
        //{
        //    var animationManager = handController.GetComponent<HandAnimationManager>();
        //    if (animationManager)
        //    {
        //        switch (animationManager.Handedness)
        //        {
        //            case AvatarHandDefinition.ESide.Left:
        //                return handDefinition.GetHandSetup(AvatarHandDefinition.ESide.Left);
        //            case AvatarHandDefinition.ESide.Right:
        //                return handDefinition.GetHandSetup(AvatarHandDefinition.ESide.Right);
        //        }
        //    }
        //    return null;
        //}

        private void DrawAxisHandle(Vector3 from, Vector3 to)
        {
            Handles.ArrowHandleCap(0, to, Quaternion.LookRotation((to - from).normalized), 0.05f, EventType.Repaint);
            Handles.DrawLine(from, to);
        }

        private void DrawRotationAxisHandle(Vector3 from, Vector3 to, float length, float radius)
        {
            Vector3 direction = (to - from).normalized;
            Handles.ArrowHandleCap(0, to, Quaternion.LookRotation(direction), 0.05f, EventType.Repaint);
            Handles.DrawLine(from, to);
            Handles.DrawWireArc(from, direction, MathUtils.GetPerpendicularVector(direction).normalized, 360f, radius);
        }

        //private HandController[] GetHandControllers(AvatarHandController root)
        //{
        //    return root.transform.GetComponentsInChildren<HandController>();
        //}

        private Transform GetAvatarBoneTransform(AvatarHandController handController, HumanBodyBones bone)
        {
            if(handController)
            {
                return handController.GetComponent<AvatarController>().AvatarAnimator.GetBoneTransform(bone);
            }
            return null;
        }

        private void DrawThumbPose(ThumbSetup.EThumbPose pose, AvatarHandDefinition.ESide side, AvatarHandDefinition handDefinition)
        {
            var bone = GetAvatarBoneTransform(PreviewAvatar, side == AvatarHandDefinition.ESide.Left ?
                HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(pose.ToString());
                if (GUILayout.Button("Preview"))
                {
                    bone.localRotation = handDefinition.GetHandSetup(side).Thumb.GetPoseRotation(pose);
                }

                if (GUILayout.Button("Save"))
                {
                    serializedObject.Update();

                    switch (pose)
                    {
                        case ThumbSetup.EThumbPose.Neutral:
                            handDefinition.GetHandSetup(side).Thumb.NeutralOrientation = bone.localRotation;
                            break;
                        case ThumbSetup.EThumbPose.Abducted:
                            handDefinition.GetHandSetup(side).Thumb.AbductedOrientation = bone.localRotation;
                            break;
                        case ThumbSetup.EThumbPose.Adducted:
                            handDefinition.GetHandSetup(side).Thumb.AdductedOrientation = bone.localRotation;
                            break;
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}