using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Artanim.HandAnimation.Procedural
{
	[CustomEditor(typeof(HandAnimationCollider))]
	public class HandAnimationColliderEditor : Editor
	{
		private static GUIContent _CreateOrUpdateColliderDataAsset = new GUIContent("Create or update collider data asset", "Creates a collider data asset in the same folder as the mesh, or updates the existing asset");

		private static GUILayoutOption _MiniButtonWidth = GUILayout.Width(50f);
		private static bool _EditAlignmentEdges = false;

		private static GUIContent _EditButtonContent = new GUIContent("edit", "Edit hand alignment edges");
		private static GUIContent _AddButtonContent = new GUIContent("add", "Add hand alignment edge");
		private static GUIContent _DeleteButtonContent = new GUIContent("delete", "Delete hand alignment edge");

		private static int _SelectedEdge = -1;

		private void OnDisable()
		{
			_SelectedEdge = -1;
		}

		public override void OnInspectorGUI()
		{
			HandAnimationCollider targetCollider = target as HandAnimationCollider;

			DrawDefaultInspector();

			if(GUILayout.Button(_CreateOrUpdateColliderDataAsset))
			{
				CreateOrUpdateColliderDataAsset(targetCollider);
			}
		}

		public void CreateOrUpdateColliderDataAsset(HandAnimationCollider targetCollider)
		{
			if(targetCollider.Mesh == null)
			{
				Debug.LogError("[HandAnimationCollider] No Mesh assigned to the HandAnimationCollider. Please assign a mesh before creating or updating the collider data");
				return;
			}

			if(targetCollider.ColliderData == null)
			{
				string path = AssetDatabase.GetAssetPath(targetCollider.Mesh);
				string folder = Path.GetDirectoryName(path);
				string fileName = Path.GetFileNameWithoutExtension(path);

				string assetName = Path.Combine(folder, fileName + "_HandAnimationCollider.asset");

				HandAnimationColliderData colliderAsset = ScriptableObject.CreateInstance<HandAnimationColliderData>();

				AssetDatabase.CreateAsset(colliderAsset, assetName);
				AssetDatabase.SaveAssets();

				targetCollider.ColliderData = colliderAsset;
			}

			targetCollider.ColliderData.CreateFromMesh(targetCollider.Mesh);
			EditorUtility.SetDirty(targetCollider.ColliderData);
			EditorUtility.SetDirty(targetCollider);
			AssetDatabase.SaveAssets();
		}


		protected virtual void OnSceneGUI()
		{
			HandAnimationCollider geometryController = (HandAnimationCollider)target;

			var e = Event.current;

			//Draw toolbar
			Handles.BeginGUI();

			bool deleteEdge = false;
			bool addEdge = false;

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				var doEdit = GUILayout.Toggle(_EditAlignmentEdges, _EditButtonContent, EditorStyles.miniButtonLeft, _MiniButtonWidth);
				if(doEdit != _EditAlignmentEdges)
				{
					_EditAlignmentEdges = doEdit;
					if(!_EditAlignmentEdges)
					{
						_SelectedEdge = -1;
					}
				}
				GUI.enabled = _EditAlignmentEdges;
				addEdge = GUILayout.Button(_AddButtonContent, EditorStyles.miniButtonMid, _MiniButtonWidth);

				if (geometryController.HandAlignmentEdges == null || geometryController.HandAlignmentEdges.Count == 0 || _SelectedEdge == -1)
				{
					GUI.enabled = false;
				}
				deleteEdge = GUILayout.Button(_DeleteButtonContent, EditorStyles.miniButtonRight, _MiniButtonWidth);
				GUI.enabled = true;

				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();

			Handles.EndGUI();

			if(addEdge)
			{
				Undo.RecordObject(geometryController, "Add alignment edge");
				HandAlignmentEdge edge = new HandAlignmentEdge()
				{
					VertexA = new Vector3(-0.01f, 0.0f, 0.0f),
					VertexB = new Vector3(0.01f, 0.0f, 0.0f)
				};
				if(geometryController.HandAlignmentEdges == null)
				{
					geometryController.HandAlignmentEdges = new List<HandAlignmentEdge>();
				}
				geometryController.HandAlignmentEdges.Add(edge);
				_SelectedEdge = geometryController.HandAlignmentEdges.Count - 1;
			}
			else if(deleteEdge && _SelectedEdge != -1)
			{
				Undo.RecordObject(geometryController, "Remove alignment edge");
				geometryController.HandAlignmentEdges.RemoveAt(_SelectedEdge);
				_SelectedEdge = -1;
			}

			//Edit edges
			if (!_EditAlignmentEdges || geometryController.HandAlignmentEdges == null || geometryController.HandAlignmentEdges.Count == 0)
			{
				return;
			}

			Transform controllerTransform = geometryController.transform;
			Handles.matrix = Matrix4x4.TRS(controllerTransform.position, controllerTransform.rotation, Vector3.one);

			for(int i = 0; i < geometryController.HandAlignmentEdges.Count; ++i)
			{
				HandAlignmentEdge alignmentEdge = geometryController.HandAlignmentEdges[i];
				Vector3 vertexAPosition = alignmentEdge.VertexA;
				Vector3 vertexBPosition = alignmentEdge.VertexB;

				if (i == _SelectedEdge)
				{
					Handles.color = Color.blue;
				}
				else
				{
					Handles.color = new Color(0.5f, 0.5f, 0.8f);
				}

				GUI.changed = false;

				if (Handles.Button(vertexAPosition, Quaternion.identity, 0.01f, 0.01f, Handles.SphereHandleCap))
				{
					_SelectedEdge = i;
				}
				if (Handles.Button(vertexBPosition, Quaternion.identity, 0.01f, 0.01f, Handles.SphereHandleCap))
				{
					_SelectedEdge = i;
				}

				GUI.changed = false;
				EditorGUI.BeginChangeCheck();
				{
					if (i == _SelectedEdge)
					{
						vertexAPosition = Handles.PositionHandle(vertexAPosition, Quaternion.identity);
						vertexBPosition = Handles.PositionHandle(vertexBPosition, Quaternion.identity);
					}
				}
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(geometryController, "Moved alignment edge vertex");
					alignmentEdge.VertexA = vertexAPosition;
					alignmentEdge.VertexB = vertexBPosition;
				}

				Handles.DrawAAPolyLine(5.0f, new Vector3[] { alignmentEdge.VertexA, alignmentEdge.VertexB });

				Handles.color = Color.white;
			}
		}
	}
}