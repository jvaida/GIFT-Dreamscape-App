using Artanim.Location.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	[RequireComponent(typeof(Animator))]
	public class DreamscapeUserMessageDisplayer : MonoBehaviour, IUserMessageDisplayer
	{
		private const string BOOL_SHOW_USER_MESSAGE = "ShowUserMessage";

		public GameObject PanelMessage;
		public GameObject PanelDreamscape;

		public Text TextUserMessage;
		public Image ImageIcon;

		public Sprite IconInfo;
		public Sprite IconWarning;
		public Sprite IconError;

		private Animator UserMessageAnimator;
		
		void Awake()
		{
			UserMessageAnimator = GetComponent<Animator>();
		}

		private void Start()
		{
			//Show logo on clients if not in a session
			if (NetworkInterface.Instance.ComponentType == Location.Data.ELocationComponentType.ExperienceClient)
			{
				if (GameController.Instance)
				{
					GameController.Instance.OnJoinedSession += Instance_OnJoinedSession;
					GameController.Instance.OnLeftSession += Instance_OnLeftSession;
				}

				if (GameController.Instance.CurrentSession == null)
				{
					ShowDreamscape();
					ShowMessagePanel(true);
				}
			}
		}

		void OnDestroy()
		{
			if (GameController.Instance)
			{
				GameController.Instance.OnJoinedSession -= Instance_OnJoinedSession;
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
			}
		}

		public void DisplayUserMessage(EUserMessageType type, string message, bool isTextId = false, string defaultText = null)
		{
			if (!string.IsNullOrEmpty(message) && UserMessageAnimator)
			{
				//Check for Dreamscape message:
				//If the message is tracking lost and the player is not calibrated yet, display only the Dreamscape panel.
				if(isTextId && message == SDKTexts.ID_TRACKING_LOST)
				{
					if (GameController.Instance && GameController.Instance.CurrentPlayer != null && GameController.Instance.CurrentPlayer.Player.Status != Location.Data.EPlayerStatus.Calibrated)
					{
						//Player is not calibrated yet. Just show the Dreamscape logo.
						ShowDreamscape();
					}
					else
					{
						//Player is calibrated, show normal message
						ShowMessageNormal(type, message, isTextId, defaultText);
					}

				}
				else
				{
					ShowMessageNormal(type, message, isTextId, defaultText);
				}

				ShowMessagePanel(true);
			}
		}

		public void HideUserMessage()
		{
			ShowMessagePanel(false);
		}

		#region Location events

		private void Instance_OnLeftSession()
		{
			ShowDreamscape();
			ShowMessagePanel(true);
		}

		private void Instance_OnJoinedSession(Location.Data.Session session, System.Guid playerId)
		{
			ShowMessagePanel(false);
		}

		#endregion

		#region Internals

		private void ShowMessagePanel(bool show)
		{
			if(UserMessageAnimator)
			{
				//Start display animation
				UserMessageAnimator.SetBool(BOOL_SHOW_USER_MESSAGE, show);
			}
		}

		private void ShowMessageNormal(EUserMessageType type, string message, bool isTextId = false, string defaultText = null)
		{
            //Translate?
            if (isTextId)
				message = TextService.Instance.GetText(message, defaultText);

            if (ConfigService.VerboseSdkLog)
                Debug.LogFormat("Displaying user message: type={0}, message={1}", type.ToString(), message);

            //Display message
            if (TextUserMessage)
				TextUserMessage.text = message;

			if (ImageIcon)
				ImageIcon.sprite = GetMessageTypeIcon(type);

			//Enable text panel
			if (PanelMessage) PanelMessage.SetActive(true);
			if (PanelDreamscape) PanelDreamscape.SetActive(false);
		}

		private void ShowDreamscape()
		{
            if (ConfigService.VerboseSdkLog)
                Debug.Log("Displaying Dreamscape logo to user");

            //Enable Dreamscape panel only
            if (PanelMessage) PanelMessage.SetActive(false);
			if (PanelDreamscape) PanelDreamscape.SetActive(true);
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

		#endregion
	}
}
