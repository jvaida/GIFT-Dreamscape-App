using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
	[CustomEditor(typeof(ExperienceSettingsSO))]
	public class ExperienceSettingsSOEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();
		}
	}
}