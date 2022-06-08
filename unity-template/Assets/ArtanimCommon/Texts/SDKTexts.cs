using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public static class SDKTexts
	{
		public static string ID_TRACKING_LOST = "SDK_Tracking_Lost";

		/// <summary>
		/// <TextId, DefaultText>: Default texts used by the SDK. 
		/// </summary>
		public static readonly Dictionary<string, string> SDK_DEFAULT_TEXTS = new Dictionary<string, string>
		{
			{ "SDK_Tracking_Lost", "We are experiencing technical issues.\nPlease standby and wait for the local staff to assist you." },
		};
	}
}