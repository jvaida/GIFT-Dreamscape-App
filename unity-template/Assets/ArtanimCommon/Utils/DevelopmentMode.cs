using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public enum EDevelopmentMode { None, ClientServer, Standalone }

	public static class DevelopmentMode
	{
		public const string KEY_DEVELOPMENT_MODE = "ArtanimDevelopmentMode";

        //Navigation
		public const string AXIS_STANDALONE_MOVE_FORWARD = "Standalone Move Forward";
		public const string AXIS_STANDALONE_MOVE_STRAFING = "Standalone Move Strafing";
		public const string AXIS_STANDALONE_ROTATION = "Standalone Rotate";

        //Pickup
		public const string AXIS_STANDALONE_PICKUP_LEFT = "Standalone Pickup Left";
		public const string AXIS_STANDALONE_PICKUP_RIGHT = "Standalone Pickup Right";

        //Right Hand HUD
        public const string AXIS_STANDALONE_POPUP_RIGHT = "Standalone Popup Right";
        public const string AXIS_STANDALONE_POPUP_RIGHT_VERTICAL = "Standalone Popup Right Vertical";
        public const string BUTTON_STANDALONE_POPUP_RIGHT_SELECT = "Standalone Popup Right Select";

        //Left Hand HUD
        public const string AXIS_STANDALONE_POPUP_LEFT = "Standalone Popup Left";
        public const string AXIS_STANDALONE_POPUP_LEFT_VERTICAL = "Standalone Popup Left Vertical";
        public const string BUTTON_STANDALONE_POPUP_LEFT_SELECT = "Standalone Popup Left Select";

        //Additional actions
        public const string BUTTON_STANDALONE_RECALIBRATE = "Standalone Recalibrate";

#if !UNITY_EDITOR
		private static bool DevelopmentModeInitialized;
		private static EDevelopmentMode RuntimeDevelopmentMode = EDevelopmentMode.None;
#endif

		/// <summary>
		/// Current development mode. If not running in Unity editor, EDevelopmentMode.None is returned.
		/// </summary>
		public static EDevelopmentMode CurrentMode
		{
			get
			{
#if UNITY_EDITOR
				return (EDevelopmentMode)UnityEditor.EditorPrefs.GetInt(KEY_DEVELOPMENT_MODE, 1);
#else
				if(!DevelopmentModeInitialized)
				{
					//Try read from command line. If not set, no dev mode active
					foreach (var argument in Environment.GetCommandLineArgs())
					{
						string devMode = "DevelopmentMode=".ToLowerInvariant();
						if (argument.ToLowerInvariant().StartsWith(devMode))
						{
							//Dev mode set in command line
							try
							{
								RuntimeDevelopmentMode = (EDevelopmentMode)Enum.Parse(typeof(EDevelopmentMode), argument.Trim().Substring(devMode.Length), true);
								break;
							}
							catch (Exception)
							{
								RuntimeDevelopmentMode = EDevelopmentMode.None;
								break;
							}
						}
					}
					DevelopmentModeInitialized = true;
				}

				return RuntimeDevelopmentMode;
#endif
			}
		}

		/// <summary>
		/// Reads input axis. If standalone inputs are not setup, 0f is returned.
		/// </summary>
		/// <param name="name">Input axis name</param>
		/// <returns></returns>
		public static float GetAxis(string name)
		{
            return IsStandaloneInputsSetup ? Input.GetAxis(name) : 0f;
		}

        /// <summary>
        /// Checks if the given axis is fully pressed.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsAxisDown(string name)
        {
            return GetAxis(name) == 1f;
        }

		/// <summary>
		/// Reads input button. If standalone inputs are not setup, false is retunred.
		/// </summary>
		/// <param name="name">Button name</param>
		/// <returns></returns>
		public static bool GetButtonUp(string name)
		{
            return IsStandaloneInputsSetup ? Input.GetButtonUp(name) : false;
		}

		private static bool? InputsValid = null;
		/// <summary>
		/// Checks if the standalone inputs are setup.
		/// </summary>
		public static bool IsStandaloneInputsSetup
		{
			get
			{
				if(!InputsValid.HasValue)
				{
					try
					{
						//Axis
						Input.GetAxis(AXIS_STANDALONE_MOVE_FORWARD);
						Input.GetAxis(AXIS_STANDALONE_MOVE_STRAFING);
						Input.GetAxis(AXIS_STANDALONE_ROTATION);
						Input.GetAxis(AXIS_STANDALONE_POPUP_RIGHT);
						Input.GetAxis(AXIS_STANDALONE_POPUP_RIGHT_VERTICAL);

						//Buttons
						Input.GetButton(BUTTON_STANDALONE_RECALIBRATE);
						Input.GetButton(BUTTON_STANDALONE_POPUP_RIGHT_SELECT);

						InputsValid = true;
					}
					catch
					{
						Debug.LogError("Standalone inputs are not setup in your project. Setup inputs using Artanim->Inputs menu in the editor.");
						InputsValid = false;
					}
				}
				return InputsValid.Value;
			}
		}
	}
}