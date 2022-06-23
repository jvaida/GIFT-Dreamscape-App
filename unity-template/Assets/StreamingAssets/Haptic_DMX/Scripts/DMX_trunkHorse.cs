using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamscape
{

	public class DMX_trunkHorse : DMX_wench
	{

		public new void Reset()
		{
			base.Reset();
			dmxDevice.deviceType = "DMX_trunkHorse";
		}

	}

}