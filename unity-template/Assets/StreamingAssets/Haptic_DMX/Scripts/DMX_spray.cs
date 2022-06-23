using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	public class DMX_spray : DMX_1ChannelDevice
	{
		public new void Reset()
		{
			base.Reset();
			dmxDevice.deviceType = "DMX_spray";
		}
	}

}