using Artanim.Location.Data;
using Artanim.Location.Hostess;
using Artanim.Location.Messages;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{
	public class ServerSessionUI : MonoBehaviour
	{
		public const string KEY_SERVER_SCENE = "ArtanimServerScene";

		[SerializeField]
		Dropdown DropdownScenes;

		[SerializeField]
		Button ButtonCreateOrStartSession;

		[SerializeField]
		Button ButtonTerminateSession;

		[SerializeField]
		RectTransform RemoteSessionPanel;

		[SerializeField]
		Text RemoteSessionText;

		bool RunAsClientAutoJoin
		{
			get
			{
#if UNITY_EDITOR
				return NetworkInterface.Instance.IsClient &&
					!string.IsNullOrEmpty(PlayerPrefs.GetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR));
#else
				return false;
#endif
			}
		}

		public void DoCreateOrStartSession()
		{
			if (GameController.Instance.CurrentSession == null)
            {
				PrepareSession();
			}
			else
            {
				Debug.LogFormat("ServerSessionUI: Starting session");

				NetworkInterface.Instance.SendMessage(new StartSession
				{
					SessionId = GameController.Instance.CurrentSession.SharedId,
					StartSceneOverride = DropdownScenes.options[DropdownScenes.value].text,
				});
			}
		}

		public void DoEndSession()
		{
			ButtonCreateOrStartSession.interactable = false;
			ButtonTerminateSession.interactable = false;

			Debug.LogFormat("ServerSessionUI: Terminating session");

			NetworkInterface.Instance.SendMessage(new TerminateSession
			{
				SessionId = GameController.Instance.CurrentSession.SharedId,
			});
		}

		public void DoSelectScene()
		{
			string scene = DropdownScenes.options[DropdownScenes.value].text;
			PlayerPrefs.SetString(KEY_SERVER_SCENE, DropdownScenes.options[DropdownScenes.value].text);

			if ((GameController.Instance.CurrentSession != null) && (GameController.Instance.CurrentSession.Status == ESessionStatus.Started))
            {
				GameController.Instance.LoadGameScene(scene, Transition.FadeBlack);
			}
		}

		void OnEnable()
		{
			if (NetworkInterface.Instance.IsServer && (DevelopmentMode.CurrentMode != EDevelopmentMode.Standalone))
			{
				//Attach session events
				GameController.Instance.OnJoinedSession += Instance_OnJoinedSession;
				GameController.Instance.OnSessionStarted += Instance_OnSessionStarted;
                GameController.Instance.OnLeftSession += Instance_OnLeftSession;
			}
			else
			{
				Destroy(gameObject);
			}
		}

        void OnDisable()
		{
			var gameController = GameController.Instance;
			if (gameController != null)
			{
				//Detach session events
				gameController.OnJoinedSession -= Instance_OnJoinedSession;
				gameController.OnSessionStarted -= Instance_OnSessionStarted;
                gameController.OnLeftSession -= Instance_OnLeftSession;
			}
		}

		// Use this for initialization
		IEnumerator Start()
		{
			ButtonCreateOrStartSession.interactable = true;
			ButtonTerminateSession.interactable = false;
			DropdownScenes.interactable = false;

			if (!UpdateRemoteText())
            {
				RemoteSessionPanel.gameObject.SetActive(false);
			}

			//Init scenes dropdown
			var scenes = ConfigService.Instance.ExperienceConfig.StartScenes.Select(s => s.SceneName).ToList();
			DropdownScenes.ClearOptions();
			DropdownScenes.AddOptions(scenes);

			int index = scenes.IndexOf(PlayerPrefs.GetString(KEY_SERVER_SCENE));
			if (index > 0)
            {
				DropdownScenes.value = index;
			}

			// Automatically create session in client+server mode and if auto join is activated
			if (RunAsClientAutoJoin)
			{
				var thisServer = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();

				//Wait for ready for session
				yield return new WaitUntil(() => thisServer.Status == ELocationComponentStatus.ReadyForSession);

				PrepareSession();
			}
		}

		void Update()
        {
			UpdateRemoteText();
		}

		bool UpdateRemoteText()
        {
			if (RemoteSessionController.Instance.IsRemoteServer)
            {
				var si = RemoteSessionController.Instance.SessionInfo;
				var delay = si.StartTime - System.DateTime.UtcNow;
				bool started = delay.TotalSeconds < 0;
				if (started)
                {
					delay = -delay;
				}
				RemoteSessionText.text = string.Format("{0} {1:D2}:{2:D2}\n{3} / {4} reserved", started ? "Started since" : "Starting in", delay.Minutes, delay.Seconds, si.SeatsReserved, si.MaximumSeats);
				return true;
			}
			return false;
		}

		void PrepareSession()
		{
			ButtonCreateOrStartSession.interactable = false;
			ButtonTerminateSession.interactable = false;

			Debug.LogFormat("ServerSessionUI: Preparing session with RunAsClientAutoJoin=" + RunAsClientAutoJoin);

			if (RunAsClientAutoJoin)
			{
				// Create a new session, it will be joined by the AutoJoin client code
				var session = SessionManager.PrepareNewSession();
				session.Experience = ConfigService.Instance.ExperienceConfig.ExperienceName;
				session.ExperienceServerId = SharedDataUtils.MySharedId;
			}
			else
			{
				NetworkInterface.Instance.SendMessage(new PrepareNewSession { RecipientId = SharedDataUtils.MySharedId });
			}
		}

		void Instance_OnJoinedSession(Session session, System.Guid playerId)
		{
			SetStartButtonText("Start Session");

			ButtonCreateOrStartSession.interactable = true;
			ButtonTerminateSession.interactable = true;
			DropdownScenes.interactable = true;
		}

		void Instance_OnSessionStarted()
		{
			SetStartButtonText("Start Scene");

			ButtonCreateOrStartSession.interactable = false;
			ButtonTerminateSession.interactable = true;
			DropdownScenes.interactable = true;
		}

		private void Instance_OnLeftSession()
		{
			SetStartButtonText("Create Session");

			ButtonCreateOrStartSession.interactable = true;
			ButtonTerminateSession.interactable = false;
			DropdownScenes.interactable = false;
		}
		
		void SetStartButtonText(string txt)
        {
			ButtonCreateOrStartSession.GetComponentInChildren<Text>().text = txt;
		}
	}
}