using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	public class DMX_scent : DMX_1ChannelDevice
	{
		public new void Reset()
		{
			base.Reset();
			dmxDevice.deviceType = "DMX_scent";
		}
	}

}