using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Artanim
{

    public class FaceStateEditorWindow : EditorWindow
    {
        private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/face state editor.jpg";

        [MenuItem("Artanim/Tools/Face State Editor...", priority = 1)]
        public static void Open()
        {
            var configWindow = (FaceStateEditorWindow)GetWindow(typeof(FaceStateEditorWindow), true, "Face State Editor", true);
            configWindow.minSize = new Vector2(800f, 800f);
            configWindow.maxSize = new Vector2(800f, 1200f);
            configWindow.Show();
        }

        public static void Open(FaceState faceState)
        {
            var configWindow = (FaceStateEditorWindow)GetWindow(typeof(FaceStateEditorWindow), true, "Face State Editor", true);
            configWindow.minSize = new Vector2(800f, 800f);
            configWindow.maxSize = new Vector2(800f, 1200f);
            configWindow.Show();

            configWindow.FaceState = faceState;
        }

        private Texture2D ImageHeader;

        private AvatarFaceController _FaceController;
        private AvatarFaceController FaceController
        {
            get { return _FaceController; }
            set
            {
                if(FaceController != value)
                {
                    //Reset old one first
                    if (FaceController)
                    {
                        FaceController.ResetPreviewFaceState();
                    }

                    _FaceController = value;

                    //Apply state if available
                    if (FaceState && FaceController)
                        FaceController.PreviewFaceState(FaceState);
                }
            }
        }

        private FaceState _FaceState;
        private FaceState FaceState
        {
            get { return _FaceState; }
            set
            {
                if(FaceState != value)
                {
                    _FaceState = value;

                    //Apply to face
                    if(FaceController)
                    {
                        if (FaceState)
                            FaceController.PreviewFaceState(FaceState);
                        else
                            FaceController.ResetPreviewFaceState();
                    }

                    //Cleanup prev editor
                    if(FaceStateEditor)
                    {
                        FaceStateEditor.RemoveUnusedBlendShapes();
                        FaceStateEditor = null;
                    }

                    //Create editor
                    FaceStateEditor = FaceState ? Editor.CreateEditor(FaceState) as FaceStateEditor : null;
                }
            }
        }

        private FaceStateEditor FaceStateEditor;

        private void Awake()
        {
            var mainFolder = EditorUtils.GetSDKAssetFolder();
            if (!string.IsNullOrEmpty(mainFolder))
            {
                ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));
            }

            //Preselect avatar if selected
            if(Selection.activeGameObject && Selection.activeGameObject.activeInHierarchy && Selection.activeGameObject.GetComponent<AvatarFaceController>())
            {
                FaceController = Selection.activeGameObject.GetComponent<AvatarFaceController>();
            }
            else
            {
                //Try search in scene
                foreach(var faceController in FindObjectsOfType<AvatarFaceController>())
                {
                    if (faceController.gameObject.activeInHierarchy)
                    {
                        FaceController = faceController;
                        break;
                    }
                }
            }

            //Preselect face state if selection
            if(Selection.activeObject && Selection.activeObject is FaceState)
            {
                FaceState = Selection.activeObject as FaceState;
            }
        }

        private void OnDisable()
        {
            FaceController = null;
            FaceState = null;
        }

        private void OnGUI()
        {
            if (ImageHeader)
                GUILayout.Label(ImageHeader);

            //FaceController and FaceState selection
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            FaceController = (AvatarFaceController)EditorGUILayout.ObjectField("FaceController (Avatar)", FaceController, typeof(AvatarFaceController), true);

            //Validate FaceController
            if(!FaceController.FaceDefinition)
            {
                EditorGUILayout.HelpBox("The selected avatar FaceController does not have a FaceDefinition set.", MessageType.Warning);
                return;
            }


            EditorGUILayout.BeginHorizontal();
            {
                FaceState = (FaceState)EditorGUILayout.ObjectField("Face State", FaceState, typeof(FaceState), true);
                if(GUILayout.Button("Create New", GUILayout.Width(150)))
                {
                    CreateFaceState();
                }
            }
            EditorGUILayout.EndHorizontal();

            //FaceState view
            if(FaceStateEditor)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Face State", EditorStyles.boldLabel);

                if(FaceController)
                {
                    var allBlendShapes = FaceController.GetAllBlendshapeNames();
                    FaceStateEditor.RenderWithAllBlendshapes(allBlendShapes);
                }
            }

            //Preview
            if(FaceController && FaceState)
            {
                FaceController.PreviewFaceState(FaceState);
            }
            else
            {
                EditorGUILayout.HelpBox("Select an avatar with a FaceController of your scene and a FaceState to preview and edit face states.", MessageType.Info);
            }
        }

        private void CreateFaceState()
        {
            var path = EditorUtility.SaveFilePanel(
                "Create Face State",
                FaceState ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, Path.GetDirectoryName(AssetDatabase.GetAssetPath(FaceState))) : "",
                "",
                "asset");

            if (!string.IsNullOrEmpty(path))
            {
                //Remove data path for AssetDatabase
                var assetsPath = Application.dataPath.Replace("Assets", "");
                path = path.Replace(assetsPath, "");

                var faceState = FaceState.CreateInstance<FaceState>();
                if (faceState != null)
                {
                    AssetDatabase.CreateAsset(faceState, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    FaceState = faceState;
                }
            }
        }

    }
}