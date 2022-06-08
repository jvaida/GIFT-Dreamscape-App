using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
	public enum EUserMessageType { Info, Warning, Error }

	public interface IUserMessageDisplayer
	{
		/// <summary>
		/// Display a message to the user of the given type.
		/// </summary>
		/// <param name="type">Type of the message</param>
		/// <param name="message"></param>
		void DisplayUserMessage(EUserMessageType type, string message, bool isTextId = false, string defaultText = null);

		/// <summary>
		/// Hide the previously displayed user message.
		/// </summary>
		void HideUserMessage();
	}

}
