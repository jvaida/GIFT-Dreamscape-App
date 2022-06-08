using Artanim.Location.Messages;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim.Tools
{

	public class AvatarCheckWindow : EditorWindow
	{
        private static readonly string HEADER_IMAGE_PATH = "UI/Images/Icons/header avatar check.jpg";

        [MenuItem("Artanim/Tools/Check Avatars...", priority = 2)]
		static void Init()
		{
			var avatarCheckWindow = (AvatarCheckWindow)GetWindow(typeof(AvatarCheckWindow), focus: true, title: "Avatar Check", utility: true);
            avatarCheckWindow.minSize = new Vector2(600f, 800f);
            avatarCheckWindow.maxSize = new Vector2(800f, 1200f);
            avatarCheckWindow.Show();
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

        private List<Result> Results = new List<Result>();
		private Vector2 _scrollPos = Vector2.zero;
		private void OnGUI()
		{
            if (ImageHeader)
                GUILayout.Label(ImageHeader);

            if (Results.Count == 0)
				CheckAvatars();

			if (GUILayout.Button("Refresh"))
			{
				CheckAvatars();
			}

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
			{
				RenderResults();
			}
			EditorGUILayout.EndScrollView();
		}

		private void CheckAvatars()
		{
			Results.Clear();

			foreach (var avatar in ConfigService.Instance.ExperienceConfig.Avatars)
			{
				CheckAvatar(avatar);
			}
		}

		private void CheckAvatar(Location.Config.Avatar avatar)
		{
			var result = new Result()
			{
				AvatarName = avatar.Name,
				AvatarResource = avatar.AvatarResource,
				Message = "",
			};

			var avatarTemplate = Resources.Load(avatar.AvatarResource) as GameObject;
			if(avatarTemplate)
			{
				var isValid = true;

				//Check avatar controller
				var avatarController = avatarTemplate.GetComponent<AvatarController>();
				if(!avatarController)
				{
					result.Message += "No AvatarController found.\n";
					isValid = false;
				}

				//Check avatar controller head bone
				if(avatarController && !avatarController.HeadBone)
				{
					result.Message += "Head Bone is not set in the AvatarController.\n";
					isValid = false;
				}

				//Check body parts
				foreach (EAvatarBodyPart bodyPart in Enum.GetValues(typeof(EAvatarBodyPart)))
				{
					var foundBodyParts = avatarTemplate.GetComponentsInChildren<AvatarBodyPart>().Where(p => p.BodyPart == bodyPart);
					if (foundBodyParts.Count() > 1)
					{
						result.Message += string.Format("Found more than {0} body parts of type {1}\n", foundBodyParts.Count(), bodyPart);
						isValid = false;
					}
					else if (foundBodyParts.Count() == 0)
					{
						result.Message += string.Format("Found no body part of type {0}\n", bodyPart);
						isValid = false;
					}
				}

                //Check Face Controller
                var faceController = avatarTemplate.GetComponent<AvatarFaceController>();
                if (faceController)
                {
                    if(!faceController.FaceDefinition)
                    {
                        result.Message += "AvatarFaceController does not have a AvatarFaceDefinition set.\n";
                        isValid = false;
                    }
                }

                //Check for Hand Controller
                var handController = avatarTemplate.GetComponent<HandAnimation.AvatarHandController>();
                if(handController)
                {
                    if(!handController.HandDefinition)
                    {
                        result.Message += "AvatarHandController does not have a AvatarHandDefinition set.\n";
                        isValid = false;
                    }
                }

				result.IsValid = isValid;
				if(isValid)
					result.Message = "Is valid.";
			}
			else
			{
				result.Message = "Avatar not found in Resources.";
				result.IsValid = false;
			}

			Results.Add(result);
		}

		private void RenderResults()
		{
			foreach(var result in Results)
			{
				EditorGUILayout.LabelField(string.Format("{0} ({1})", result.AvatarName, result.AvatarResource), EditorStyles.boldLabel);
				EditorGUILayout.HelpBox(result.Message, result.IsValid? MessageType.Info : MessageType.Error);
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
		}

		private class Result
		{
			public string AvatarName { get; set; }
			public string AvatarResource { get; set; }
			public string Message { get; set; }
			public bool IsValid { get; set; }
		}

	}

}