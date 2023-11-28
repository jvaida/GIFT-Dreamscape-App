using Artanim.Location.Config;
using Artanim.Location.Data;
using Artanim.Location.Hostess;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artanim.Tracking;
using Artanim.Location.Messages;

namespace Artanim
{
	/// <summary>
	/// Controller instantiated by the GameController when running in standalone dev mode.
	/// It creates a new session (using the HostessController), adds the current player to it and starts the session with the
	/// selected scene from the main menu.
	/// </summary>
	public class StandaloneController : MonoBehaviour
	{
        public GameObject DefaultStandaloneAvatarTemplate;

		private ExperienceClient ThisClient;
        private Session ThisSession;
        private List<ExperienceClient> SpawnedClients = new List<ExperienceClient>();

		void Start()
		{
            if(SceneController.Instance)
                SceneController.Instance.OnSceneLoaded += Instance_OnSceneLoaded;


            //Init
			if (SharedDataUtils.IsInitialized)
				Initialize();
			else
				SharedDataUtils.Initialized += Initialize;
		}

        void OnDestroy()
        {
            if (SceneController.HasInstance)
                SceneController.Instance.OnSceneLoaded -= Instance_OnSceneLoaded;
        }

        public GameObject GetAvatarTemplate()
        {
            var template = ConfigService.Instance.ExperienceSettings.StandaloneAvatarTemplate;

            if (template && !template.GetComponent<AvatarController>())
            {
                Debug.LogErrorFormat("The standalone avatar template configured does not have an AvatarController set. Template: {0}", template.name);
                template = null;
            }

            if (!template)
                template = DefaultStandaloneAvatarTemplate;

            return template;
        }

        void Initialize()
		{
			ThisClient = SharedDataUtils.GetMyComponent<ExperienceClient>();

			if(ThisClient != null)
			{
				StartCoroutine(InitializeStandaloneSession());
			}
			else
			{
				Debug.LogError("Unable to initialize standalone development mode. Component must be of type client");
			}
		}

        void OnApplicationQuit()
        {
            //Clear spawned clients
            foreach(var spawnedClient in SpawnedClients)
            {
                SharedDataUtils.RemoveFakeExperienceClient(spawnedClient);
            }
        }


        private void Instance_OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.Scene scene, bool isMainScene)
        {
            //Set spawned clients current scene
            foreach (var spawnedClient in SpawnedClients)
            {
                spawnedClient.LoadedExperienceSceneNames = sceneName;
            }
        }

        /// <summary>
        /// Create a "fake" session, add the current player to it and start the session with the selected scene from the PlayerPrefs (set by the main menu)
        /// </summary>
        /// <returns></returns>
        private IEnumerator InitializeStandaloneSession()
		{
            //Create a new session
            ThisSession = SessionManager.PrepareNewSession();
            ThisSession.Experience = ConfigService.Instance.ExperienceConfig.ExperienceName;
            ThisSession.ExperienceServerId = SharedDataUtils.MySharedId;

            //Create fake player
            var player = SessionManager.PrepareSessionPlayer(ThisClient, false);

			//Fake initialize player
			player.Status = EPlayerStatus.Calibrated;
			player.ShowSubtitles = PlayerPrefs.GetInt(MainMenu.KEY_SHOW_SUBTITLES, 0) == 1;
            player.Language = ConfigService.Instance.ExperienceSettings.EditorLanguage;
			player.Avatar = "Standalone";

            //Wait for ReadyForSession
            yield return new WaitUntil(() => SharedDataUtils.GetMyComponent<ExperienceClient>().Status == ELocationComponentStatus.ReadyForSession);

			//Join the session
			SessionManager.RequestPlayerJoinSession(ThisSession, player);

            //Wait for PreparingSession
            yield return new WaitUntil(() => SharedDataUtils.GetMyComponent<ExperienceClient>().Status == ELocationComponentStatus.PreparingSession);

            //Find the scene to start
            if (ConfigService.Instance.ExperienceConfig.StartScenes.Count > 0)
			{
				Scene startScene = null;
				var startSceneName = PlayerPrefs.GetString(MainMenu.KEY_LAST_SELECTED_SCENE);
				if(!string.IsNullOrEmpty(startSceneName))
				{
					//Search for the start scene
					startScene = ConfigService.Instance.ExperienceConfig.StartScenes.FirstOrDefault(s => s.SceneName == startSceneName);
				}

				if(startScene == null)
				{
					Debug.LogWarningFormat("Failed to find start scene with name {0} (set by the menu) in experience config. Loading first scene.", startSceneName);
					startScene = ConfigService.Instance.ExperienceConfig.StartScenes[0];
				}

                //Start the session
                ThisSession.StartScene = startScene.SceneName;
				var result = SessionManager.StartSession(ThisSession);

                if (result != StartSessionStatus.SessionStarted)
				{
					Debug.LogErrorFormat("Failed to start in standalone mode. Error: {0}", result);
				}
			}
			else
			{
				Debug.LogError("Failed to start in standalone mode. No experience config found used for the start scene. Please add an experience settings asset to your project and create an experience config for this project.");
			}

			yield return null;
		}

        public void SpawnPlayer(Location.Config.Avatar avatar)
        {
            StartCoroutine(SpawnPlayerRoutine(avatar));
        }

        private IEnumerator SpawnPlayerRoutine(Location.Config.Avatar avatar)
        {
            Debug.LogFormat("Spawning new player with avatar: {0}", avatar.Name);

            GameController.Instance.CurrentSession.Status = ESessionStatus.Started;

            //Create new fake client
            var fakeClient = SharedDataUtils.CreateFakeExperienceClient();
            fakeClient.Status = ELocationComponentStatus.ReadyForSession;
            fakeClient.LoadedExperienceSceneNames = ThisClient.LoadedExperienceSceneNames;
            SpawnedClients.Add(fakeClient);

            //Create fake player
            var player = SessionManager.PrepareSessionPlayer(fakeClient, false);

            //Fake initialize player
            player.Status = EPlayerStatus.Initializing;
            player.SkeletonId = Guid.NewGuid();
            player.Avatar =  avatar.Name;

            //Join the session
            SessionManager.RequestPlayerJoinSession(ThisSession, player, true);
            yield return null;

            //Send fake joined message
            fakeClient.SessionId = ThisSession.SharedId;
            NetworkInterface.Instance.SendMessage(new ComponentJoinedSession
            {
                SenderId = fakeClient.SharedId,
                Result = ComponentJoinedSession.EJoinSessionResult.Success,
                SessionId = ThisSession.SharedId,
            }, keepSenderId: true);

            yield return null;

            InitFakeAvatar(player.ComponentId);
        }

        private void InitFakeAvatar(Guid playerId)
        {
            //Update player status
            var player = GameController.Instance.GetPlayerByPlayerId(playerId);
            player.Player.Status = EPlayerStatus.Calibrated;

            var avatarTransform = player.AvatarController.AvatarAnimator.transform;

            //Position avatar in front of player
            var playerHead = MainCameraController.Instance.HeadRigidBody.transform;
            var avatarPosition = playerHead.position + MainCameraController.Instance.PlayerCamera.transform.forward * 1f;
            avatarPosition.y = avatarTransform.position.y;
            avatarTransform.position = avatarPosition;

            //Rotate avatar towards player
            var rotation = Quaternion.LookRotation(MainCameraController.Instance.PlayerCamera.transform.forward * -1f, playerHead.up).eulerAngles;
            rotation.x = rotation.z = 0f;
            avatarTransform.rotation = Quaternion.Euler(rotation);

            //Set avatar as standalone pickupable
            var rigidbody = player.AvatarController.AvatarAnimator.gameObject.AddComponent<TrackingRigidbody>();
            rigidbody.RigidbodyName = player.Player.ComponentId.ToString();

            //Show avatar
            player.AvatarController.ShowAvatar(true, forceShowNow:true);
        }
    }
}