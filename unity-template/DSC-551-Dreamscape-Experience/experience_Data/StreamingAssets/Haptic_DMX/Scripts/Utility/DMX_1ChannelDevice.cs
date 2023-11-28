using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{
	[ExecuteInEditMode]
	public class DMX_1ChannelDevice : DMX_device
	{
		public new void Reset()
		{
			base.Reset();
			dmxDevice.deviceName = gameObject.name;
			if (dmxDevice.dmxChannel.Count != 1)
			{
				dmxDevice.dmxChannel.Clear();
				dmxDevice.addChannel(1, 101, 0.0f, gameObject.name);
				dmxDevice.deviceType = "DMX_1ChannelDevice";
			}
			initSetup();
		}

		public void initSetup()
		{
			updateSubDeviceList();
			dmxDevice.dmxChannel[0].value = 100.0f;      // TH1_m1_X_hi
		}

		public new void AdjustDeviceChannelName(string newName)
		{
			Debug.Log("DMX_1ChannelDevice.AdjustDeviceChannelName");
			dmxDevice.setChannelName(0, newName);
		}

		public override void CheckForEditorNameChange()
		{
			if (name != oldName)
			{
				dmxDevice.updateName(name, true);
				oldName = name;
			}
		}

		//public override void updateChannelName(string Name)
		//{
		//    // sic
		//    // override this in your subclass of the channel is supposed to inherit the device name
		//}

	}

}