using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artanim
{
	public static class EditorUtils
	{
		private static readonly string SDK_MAIN_FOLDER = "Assets/ArtanimCommon/";

		public static string GetSDKAssetFolder()
		{
			if (Directory.Exists(SDK_MAIN_FOLDER))
				return SDK_MAIN_FOLDER;
			else
				return null;
		}

	}
}