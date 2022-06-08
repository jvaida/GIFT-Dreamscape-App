using Artanim.Location.Data;
using MsgPack.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Artanim.Tools
{
    public class IKPlayerControlWindow : EditorWindow
    {
        private const float HEADER_HEIGHT = 25f;
        private const string DEFAULT_HOST = "127.0.0.1";
        private const int DEFAULT_PORT = 802;
        private const string KEY_IKPLAYER_HOST = "SDK_IKPLAYER_HOST";
        private const string KEY_IKPLAYER_PORT = "SDK_IKPLAYER_PORT";
		private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header ikplayer remote.jpg";

        private Texture2D ImageHeader;
        private Texture2D ImagePlay;
        private Texture2D ImagePause;
        private Texture2D ImageRestart;
        private Texture2D ImageNext;
        private Texture2D ImagePrev;
        private Texture2D ImagePlayOne;

        private string Host;
        private int Port;
        
        [MenuItem("Artanim/Tools/IK Player Control...", priority = 3)]
        static void Init()
        {
            var ikPlayerWindow = (IKPlayerControlWindow)GetWindow(typeof(IKPlayerControlWindow), false, "IK Player Remote", true);
            ikPlayerWindow.Show();
        }

        private void Awake()
        {
            var mainFolder = EditorUtils.GetSDKAssetFolder();
            if (!string.IsNullOrEmpty(mainFolder))
            {
                ImageHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, HEADER_IMAGE_PATH));

                ImagePlay = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/play.png"));
                ImagePause = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/pause.png"));
                ImageRestart = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/restart.png"));
                ImageNext = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/next.png"));
                ImagePrev = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/prev.png"));
                ImagePlayOne = AssetDatabase.LoadAssetAtPath<Texture2D>(string.Concat(mainFolder, "UI/Images/Icons/playone.png"));
            }

            //Read prefs
            Host = EditorPrefs.GetString(KEY_IKPLAYER_HOST);
            if (string.IsNullOrEmpty(Host))
                Host = DEFAULT_HOST;

            Port = EditorPrefs.GetInt(KEY_IKPLAYER_PORT);
            if (Port == default(int))
                Port = DEFAULT_PORT;

            //Set initial state
            SendInitialState();
        }

        private void OnDestroy()
        {
            //Store prefs
            EditorPrefs.SetString(KEY_IKPLAYER_HOST, Host);
            EditorPrefs.SetInt(KEY_IKPLAYER_PORT, Port);
        }

        private void SendInitialState()
        {
            IKPlayerRemote.SetPlaybackMode(Host, Port, PlaybackMode);
            IKPlayerRemote.SetPlaybackSpeed(Host, Port, CurrentPlaybackSpeed);
            IKPlayerRemote.Play();
        }

        private float PlaybackSpeed = 0f;
        private float CurrentPlaybackSpeed = 0f;
        private DateTime LastPlaybackSpeedUpdateTime;
        private EPlaybackMode PlaybackMode = EPlaybackMode.Loop1;
        private void OnGUI()
        {
            if (ImageHeader)
            {
                GUI.DrawTexture(new Rect(0f, 0f, EditorGUIUtility.currentViewWidth, HEADER_HEIGHT), ImageHeader, ScaleMode.ScaleAndCrop);
                GUILayout.Space(HEADER_HEIGHT + 5f);
            }

            //Player connection header
            EditorGUILayout.LabelField("IK Player Connection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                var prevHost = Host;
                Host = EditorGUILayout.TextField("Host", Host, GUILayout.MinWidth(250));
                if (prevHost != Host)
                    SendInitialState();

                var editorLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50f;
                var prevPort = Port;
                Port = EditorGUILayout.IntField("Port", Port);
                if (prevPort != Port)
                    SendInitialState();
                EditorGUIUtility.labelWidth = editorLabelWidth;

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            //Playback mode
            var prevPlaybackMode = PlaybackMode;
            PlaybackMode = (EPlaybackMode)EditorGUILayout.EnumPopup("Playback Mode", PlaybackMode);
            if (PlaybackMode != prevPlaybackMode)
                IKPlayerRemote.SetPlaybackMode(Host, Port, PlaybackMode);


            //Playback speed
            EditorGUILayout.BeginHorizontal();
            {
                PlaybackSpeed = EditorGUILayout.Slider("Playback Speed", PlaybackSpeed, -1f, 1f);
                if (GUILayout.Button("Reset Speed")) { PlaybackSpeed = 0f; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();


            //Delay playback speed update to avoid sending too much
            if (PlaybackSpeed != CurrentPlaybackSpeed && (DateTime.UtcNow - LastPlaybackSpeedUpdateTime).TotalMilliseconds > 200)
            {
                IKPlayerRemote.SetPlaybackSpeed(Host, Port, PlaybackSpeed);
                CurrentPlaybackSpeed = PlaybackSpeed;
                LastPlaybackSpeedUpdateTime = DateTime.UtcNow;
            }
            EditorGUILayout.Space();


            //Playback buttons
            var buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.fixedWidth = buttonStyle.fixedHeight = 50f;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                if (GUILayout.Button(ImagePrev, buttonStyle)) { IKPlayerRemote.PrevRecording(Host, Port); }
                if (GUILayout.Button(ImagePlay, buttonStyle)) { IKPlayerRemote.Play(Host, Port); }
                if (GUILayout.Button(ImagePause, buttonStyle)) { IKPlayerRemote.Pause(Host, Port); }
                if (GUILayout.Button(ImageRestart, buttonStyle)) { IKPlayerRemote.RestartCurrentFile(Host, Port); }
                if (GUILayout.Button(ImagePlayOne, buttonStyle)) { IKPlayerRemote.NextFrame(Host, Port); }
                if (GUILayout.Button(ImageNext, buttonStyle)) { IKPlayerRemote.NextRecording(Host, Port); }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}