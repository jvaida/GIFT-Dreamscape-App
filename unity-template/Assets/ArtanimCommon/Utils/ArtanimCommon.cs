using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace Artanim
{
	public static class ArtanimCommon
	{
#if UNITY_EDITOR
		public static string EditorCommonDir
		{
			get
			{
				return Path.Combine(Application.dataPath, "ArtanimCommon");
			}
		}
#endif
	}
}