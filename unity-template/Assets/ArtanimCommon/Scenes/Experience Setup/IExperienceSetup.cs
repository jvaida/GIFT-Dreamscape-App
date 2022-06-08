using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public class ExperienceSetupSettings
	{
		public uint DomainId = uint.MaxValue;
		public List<System.Net.IPAddress> ComponentsIps = null;
	}

	/// <summary>
	/// Interface to be implemented by the prefab set as the ExperienceSetupTemplate in the experience settings
	/// </summary>
	public interface IExperienceSetup
	{
		/// <summary>
		/// The method will be called once the Experience Setup Scene is loaded, and when the later has completed the experience will be started.
		/// The implementation of this method should never complete in case of a setup error that should prevent the experience from running.
		/// </summary>
		/// <param name="outSettings">Optional settings that will be use to initialize the SDK</param>
		/// <returns>An enumerator</returns>
		IEnumerator Run(ExperienceSetupSettings outSettings);
	}
}