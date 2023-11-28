using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	[ExecuteInEditMode]
	public class DMX_wench : DMX_device
	{
		public new void Reset()
		{
			base.Reset();
			Debug.Log("Reset");
			dmxDevice.deviceName = gameObject.name;
			if (dmxDevice.dmxChannel.Count != 7)
			{
				dmxDevice.dmxChannel.Clear();
				dmxDevice.addChannel(1, 101, 0.0f, "TH1_m1_X_hi");
				dmxDevice.addChannel(1, 102, 0.0f, "TH1_m1_X_lo");
				dmxDevice.addChannel(1, 103, 0.0f, "TH1_m1_V");
				dmxDevice.addChannel(1, 104, 0.0f, "TH1_m1_TopDown");
				dmxDevice.addChannel(1, 105, 0.0f, "TH1_m1_BottomUp");
				dmxDevice.addChannel(1, 106, 0.0f, "TH1_m1_vup");
				dmxDevice.addChannel(1, 107, 0.0f, "TH1_m1_vdn");
				dmxDevice.deviceType = "DMX_wench";
			}
			initSetup();
		}

		public void initSetup()
		{
			Debug.Log("initSetup");
			updateSubDeviceList();
			dmxDevice.dmxChannel[0].value = 100.0f;      // TH1_m1_X_hi
			dmxDevice.dmxChannel[1].value = 100.0f;      // TH1_m1_X_lo
			dmxDevice.dmxChannel[2].value = 16.0f;      // TH1_m1_V
			dmxDevice.dmxChannel[3].value = 0.0f;      // TH1_m1_TopDown
			dmxDevice.dmxChannel[4].value = 97.0f;      // TH1_m1_BottomUp
			dmxDevice.dmxChannel[5].value = 0.0f;      // TH1_m1_vup
			dmxDevice.dmxChannel[6].value = 0.0f;      // TH1_m1_vdn
		}
	}

}