using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System;

namespace Artanim
{
	// Initialize our Crash Handler system.
	// It's recommended to give script a high priority so it runs as soon as possible in order
	// to ensure that even early Unity logs are forwarded to our logger
	public class CrashHandlerController : MonoBehaviour
	{
		void Awake()
		{
			// Initialize native lib load
			CrashHandler.CrashHandler.Initialize();
		}

		void OnDestroy()
		{
		}
	}
}