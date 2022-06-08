using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Artanim
{
	public class KeepMainScreenActive : MonoBehaviour
	{
		[DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
		static extern bool ovrp_SetAppIgnoreVrFocus(bool value);

		void Awake()
		{
			ovrp_SetAppIgnoreVrFocus(true);
		}
	}
}
