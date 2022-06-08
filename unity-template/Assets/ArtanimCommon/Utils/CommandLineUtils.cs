using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace Artanim
{

	public static class CommandLineUtils
	{
		/// <summary>
		/// The syntax for command line arguments "-switch[=value]" (the part in brackets being optional)
		/// </summary>
		/// <typeparam name="T">Desired type for the switch value (without a leading dash)</typeparam>
		/// <param name="cmdSwitch">The command line switch we are looking for</param>
		/// <param name="valueIfPresent">The value returned in the switch is found but without any assigned value</param>
		/// <param name="valueIfMissing">The value returned if the switch is missing</param>
		/// <returns>The value of the switch, or valueIfMissing if the switch isn't found</returns>
		public static T GetValue<T>(string cmdSwitch, T valueIfPresent, T valueIfMissing = default(T))
		{
			if (string.IsNullOrEmpty(cmdSwitch) || cmdSwitch.Any(char.IsWhiteSpace))
			{
				throw new ArgumentException("key");
			}

			// The string will be searching for
			string switch_ = "-" + cmdSwitch.Trim().ToLowerInvariant();

			// Iterate on command line arguments
			bool found = false;
			T val = valueIfMissing;
			foreach(var arg in Environment.GetCommandLineArgs())
			{
				var keyValue = arg.Split('=');
				if (keyValue[0].Trim().ToLowerInvariant() == switch_)
				{
					found = true;
					val = valueIfPresent;
					if (keyValue.Length > 1)
					{
						try
						{
							var converter = TypeDescriptor.GetConverter(typeof(T));
							val = (T)converter.ConvertFromString(keyValue[1]);
							Debug.LogFormat("Command line argument {0} found with value: {1}", cmdSwitch, val);
						}
						catch
						{
							Debug.LogWarningFormat("Failed to parse value in command line argument {0}, defaulting to: {1}", arg, val);
						}
					}
					else
                    {
						Debug.LogFormat("Command line argument {0} found without a value, defaulting to: {1}", cmdSwitch, val);
					}
					break;
				}
			}

			if (!found)
            {
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("Command line argument {0} not found, defaulting to: {1}", cmdSwitch, val);
			}

			return val;
		}
	}
}