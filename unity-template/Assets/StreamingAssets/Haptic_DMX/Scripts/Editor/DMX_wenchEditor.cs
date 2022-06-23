using UnityEditor;
using System.Collections;
using UnityEngine;

namespace Dreamscape
{
	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(DMX_wench))]
	public class DMX_wenchEditor : DMX_deviceEditor
	{

	}
}