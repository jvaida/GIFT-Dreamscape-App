using Artanim.Location.Data;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tools
{

    public class ExperienceTriggerWindow : EditorWindow
    {
        private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header experience triggers.jpg";
        private const float HEADER_HEIGHT = 25f;

        [MenuItem("Artanim/Tools/Experience Triggers...", priority = 2)]
        static void Init()
        {
            var experienceTriggersWindow = (ExperienceTriggerWindow)GetWindow(typeof(ExperienceTriggerWindow), focus: true, title: "Exp. Triggers", utility: false);
            experienceTriggersWindow.Show();
        }

        private Texture2D ImageHeader;
        private void Awake()
        {
            var mainFolder = EditorUtils.GetSDKAssetFolder();
            if (!string.IsNullOrEmpty(mainFolder))
            {
                ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));
            }
        }

        private Vector2 _scrollPos = Vector2.zero;


        //Called at 10fps
        void OnInspectorUpdate()
        {
            if(EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (ImageHeader)
            {
                GUI.DrawTexture(new Rect(0f, 0f, EditorGUIUtility.currentViewWidth, HEADER_HEIGHT), ImageHeader, ScaleMode.ScaleAndCrop);
                GUILayout.Space(HEADER_HEIGHT + 5f);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {

                if (EditorApplication.isPlaying && NetworkInterface.Instance != null && NetworkInterface.Instance.IsConnected)
                {
                    RenderExperienceTriggers();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode with the SDK running to see Experience Triggers", MessageType.Info);
                }
                

            }
            EditorGUILayout.EndScrollView();
        }

        private void RenderExperienceTriggers()
        {
            EditorGUILayout.BeginVertical();
            {
                var myComp = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
                for (var i = 0; i < myComp.Triggers.Count; ++i)
                {
                    var trigger = myComp.Triggers[i];

                    if (GUILayout.Button(trigger.TriggerName, GUILayout.Height(30)))
                    {
                        //Trigger
                        NetworkInterface.Instance.SendMessage(new ExperienceTrigger
                        {
                            TriggerName = trigger.TriggerName,
                            SessionId = GameController.Instance.CurrentSession != null ? GameController.Instance.CurrentSession.SharedId : Guid.Empty,
                        }); ;
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}