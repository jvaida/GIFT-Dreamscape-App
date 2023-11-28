using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tracking
{
    [CustomEditor(typeof(TrackingRigidbody))]
    public class TrackingRigidbodyEditor : Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var rigidbody = serializedObject.targetObject as TrackingRigidbody;
            if(rigidbody && rigidbody.RigidbodySubject != null)
            {
                var subject = rigidbody.RigidbodySubject;

                GUI.enabled = false;
                EditorGUILayout.LabelField("RigidBodySubject:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.TextField("Name", subject.Name);
                    EditorGUILayout.TextField("SkeletonName", subject.SkeletonName);
                    EditorGUILayout.Toggle("IsSkeletonMainSubject", subject.IsSkeletonMainSubject);
                    EditorGUILayout.Toggle("IsSkeletonSubject", subject.IsSkeletonSubject);
                    EditorGUILayout.Toggle("IsSkeletonSubjectClassified", subject.IsSkeletonSubjectClassified);
                    EditorGUILayout.EnumPopup("Subject", subject.Subject);
                    EditorGUILayout.Toggle("IsVirtual", subject.IsVirtual);
                    EditorGUILayout.TextField("LastUpdate", subject.LastUpdate.ToLongTimeString());
                    EditorGUILayout.IntField("LastUpdateFrameNumber", (int)subject.LastUpdateFrameNumber);
                    EditorGUILayout.Toggle("IsTracked", subject.IsTracked);
                }
                EditorGUILayout.EndVertical();
                GUI.enabled = true;
            }
        }
    }
}