using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{

	[InitializeOnLoad]
	public static class TextServiceEditor
	{

		private const string MENU_RELOAD_TEXTS = "Artanim/Tools/Texts/Reload Texts";
		private const string MENU_RELOAD_PROVIDER = "Artanim/Tools/Texts/Reload Provider";

		[MenuItem(MENU_RELOAD_TEXTS)]
		public static void DoReloadTexts()
		{
			TextService.Instance.ReloadTexts();
		}

		[MenuItem(MENU_RELOAD_PROVIDER)]
		public static void DoReloadProvider()
		{
			TextService.Instance.ReloadProvider();
		}

	}

}