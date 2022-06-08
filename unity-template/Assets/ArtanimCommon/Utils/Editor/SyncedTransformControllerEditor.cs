using Artanim;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Artanim
{
	[CustomEditor(typeof(SyncedTransformController))]
	public class SyncedTransformControllerEditor : Editor
	{
		bool ShowTransforms = true;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			//Default layout
			DrawDefaultInspector();

			//Show registered transforms
			ShowTransforms = EditorGUILayout.Foldout(ShowTransforms, "Registered Transforms");
			if (ShowTransforms)
			{
				var controller = serializedObject.targetObject as SyncedTransformController;

				if (controller)
				{

					foreach (var syncedTransform in controller.RegisteredTransforms)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField(syncedTransform.ObjectId);

						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.ObjectField(syncedTransform.transform, typeof(Transform), true);
						EditorGUI.EndDisabledGroup();

						EditorGUILayout.EndHorizontal();
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}