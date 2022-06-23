using UnityEditor;
using System.Collections;
using UnityEngine;

namespace Dreamscape
{

	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(DMX_fan))]
	public class DMX_FanDeviceEditor : DMX_wenchEditor
	{

	}

}