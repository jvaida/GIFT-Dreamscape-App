using UnityEditor;
using System.Collections;
using UnityEngine;

namespace Dreamscape
{

	// Custom Editor using SerializedProperties.
	// Automatic handling of multi-object editing, undo, and prefab overrides.
	[CustomEditor(typeof(DMX_1ChannelDevice))]
	public class DMX_1ChannelDeviceEditor : DMX_deviceEditor
	{

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			string deviceNameOriginal = deviceName.stringValue;
			base.OnInspectorGUI();
			string deviceNameNew = deviceName.stringValue;
			if (deviceNameOriginal != deviceNameNew)
			{
				var dmx_device = target as DMX_1ChannelDevice;
				dmx_device.AdjustDeviceChannelName(deviceNameNew);
				Debug.Log("Device name has changed");
			}
		}
	}

}