using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	[RequireComponent(typeof(Animator))]
	public class DefaultUserMessageDisplayer : MonoBehaviour, IUserMessageDisplayer
	{
		private const string BOOL_SHOW_USER_MESSAGE = "ShowUserMessage";

		public Text TextUserMessage;
		public Image ImageIcon;

		public Sprite IconInfo;
		public Sprite IconWarning;
		public Sprite IconError;

		private Animator UserMessageAnimator;

		void Start()
		{
			UserMessageAnimator = GetComponent<Animator>();
		}

		public void DisplayUserMessage(EUserMessageType type, string message, bool isTextId = false, string defaultText = null)
		{
			if (!string.IsNullOrEmpty(message) && UserMessageAnimator)
			{
				//Translate?
				if (isTextId)
					message = TextService.Instance.GetText(message, defaultText);

				//Display message
				if (TextUserMessage)
					TextUserMessage.text = message;

				if (ImageIcon)
					ImageIcon.sprite = GetMessageTypeIcon(type);

				UserMessageAnimator.SetBool(BOOL_SHOW_USER_MESSAGE, true);
			}
		}

		public void HideUserMessage()
		{
			if (UserMessageAnimator)
				UserMessageAnimator.SetBool(BOOL_SHOW_USER_MESSAGE, false);
		}

		private Sprite GetMessageTypeIcon(EUserMessageType messageType)
		{
			switch (messageType)
			{
				case EUserMessageType.Info:
					return IconInfo;
				case EUserMessageType.Warning:
					return IconWarning;
				case EUserMessageType.Error:
					return IconError;
				default:
					return IconInfo;
			}
		}
	}
}
