using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tracking
{
    [CustomEditor(typeof(IKListener))]
    [CanEditMultipleObjects]
    public class IKListenerEditor : Editor
    {
        private const string LEFT_TWIST_BONE = "lowerarm_twist_01_l";
        private const string RIGHT_TWIST_BONE = "lowerarm_twist_01_r";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Setup CC Arm Twists"))
            {
                foreach (var ikl in targets.OfType<IKListener>())
                    SetCCArmTwists(ikl);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetCCArmTwists(IKListener ikListener)
        {
            var leftTwist = UnityUtils.GetChildByName(LEFT_TWIST_BONE, ikListener.transform);
            if(!leftTwist)
            {
                Debug.LogErrorFormat("Unable to find CC armtwist: {0}", LEFT_TWIST_BONE);
                return;
            }

            var rightTwist = UnityUtils.GetChildByName(RIGHT_TWIST_BONE, ikListener.transform);
            if (!rightTwist)
            {
                Debug.LogErrorFormat("Unable to find CC armtwist: {0}", LEFT_TWIST_BONE);
                return;
            }

            var twistBones = new List<AdditionalBone>()
            {
                new AdditionalBone { Bone = ERigBones.LeftArmRoll, BoneTransform = leftTwist.transform, },
                new AdditionalBone { Bone = ERigBones.RightArmRoll, BoneTransform = rightTwist.transform, },
            };
            ikListener.AdditionalBones = twistBones.ToArray();

        }
    }
}