using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Artanim.Tools
{
	[InitializeOnLoad]
	public static class DevelopmentModeEditor
	{
		private const string MENU_CLIENT_SERVER = "Artanim/Development Mode/Client and Server";
		private const string MENU_STANDALONE = "Artanim/Development Mode/Standalone";

		#region Editor menu

		/// <summary>
		/// Client / Server mode
		/// </summary>
		[MenuItem(MENU_CLIENT_SERVER)]
		private static void ToggleModeClientServer()
		{
			ToggleMode(EDevelopmentMode.ClientServer);
		}

		[MenuItem(MENU_CLIENT_SERVER, true)]
		private static bool ValidateToggleModeClientServer()
		{
			Menu.SetChecked(MENU_CLIENT_SERVER, DevelopmentMode.CurrentMode == EDevelopmentMode.ClientServer);
			return !EditorApplication.isPlaying;
		}

		/// <summary>
		/// Standalone mode
		/// </summary>
		[MenuItem(MENU_STANDALONE)]
		private static void ToggleModeStandalone()
		{
			ToggleMode(EDevelopmentMode.Standalone);
		}

		[MenuItem(MENU_STANDALONE, true)]
		private static bool ValidateToggleModeStandalone()
		{
			Menu.SetChecked(MENU_STANDALONE, DevelopmentMode.CurrentMode == EDevelopmentMode.Standalone);
			return !EditorApplication.isPlaying;
		}

		#endregion

		/// <summary>
		/// Toggle a mode on or off
		/// </summary>
		/// <param name="mode"></param>
		public static void ToggleMode(EDevelopmentMode toggleMode)
		{
			if (toggleMode != DevelopmentMode.CurrentMode)
			{
				//Toggle on
				EditorPrefs.SetInt(DevelopmentMode.KEY_DEVELOPMENT_MODE, (int)toggleMode);
			}
			else
			{
				//Clear mode
				EditorPrefs.SetInt(DevelopmentMode.KEY_DEVELOPMENT_MODE, (int)EDevelopmentMode.None);
			}
		}
	}
}