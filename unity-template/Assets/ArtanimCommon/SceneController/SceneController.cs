//#define TEST_CLIENT_LOAD_DELAY

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Linq;
using Artanim.Location.Network;
using Artanim.Location.Messages;
using Artanim.Location.Data;
using UnityEngine.UI;
using System.Text;
using Artanim.Location.SharedData;
using System.Threading;
using Artanim.Location.Monitoring;
using Artanim.Location.Monitoring.OpTicketsTypes.Experience;
using Artanim.Utils;

namespace Artanim
{

	/// <summary>
	/// Controls scene loading and screen fade between scene transitions.
	/// </summary>
	public class SceneController : SingletonBehaviour<SceneController>
	{
		private const string EMERGENCY_SCENE_NAME = "Emergency Scene";

		private const string SCENE_LOADING_FORMAT = "Loading scene: {0} ...";

		private const string ROOT_SCENE_NAME = "Main Scene";
		private const EScene DEFAULT_SCENE = EScene.MainMenu;


		public enum EScene
		{
			MainMenu,
			IK,
			ExperienceServer,
			ExperienceClient,
			ExperienceObserver,
		}

		public ELocationComponentType SceneComponentType
		{
			get
			{
				if (CurrentMainScene != null)
					return CurrentMainScene.ComponentType;
				else
					return ELocationComponentType.Undefined;
			}
		}

		private static readonly Dictionary<EScene, SceneConfig> SceneConfigs = new Dictionary<EScene, SceneConfig>()
		{
			{ EScene.MainMenu, new SceneConfig { SceneName = "Main Menu Scene", ComponentType = ELocationComponentType.Undefined } },

			{ EScene.IK, new SceneConfig { SceneName = "IK Scene", ComponentType = ELocationComponentType.IKServer } },

			{ EScene.ExperienceServer, new SceneConfig { SceneName = "Experience Controller Scene", ComponentType = ELocationComponentType.ExperienceServer } },
			{ EScene.ExperienceClient, new SceneConfig { SceneName = "Experience Controller Scene", ComponentType = ELocationComponentType.ExperienceClient } },
			{ EScene.ExperienceObserver, new SceneConfig { SceneName = "Experience Observer Scene", ComponentType = ELocationComponentType.ExperienceObserver, IsDeprecated = true } },
		};

		private static SceneConfig SetupSceneConfig = new SceneConfig { SceneName = "Experience Setup Scene", ComponentType = ELocationComponentType.Undefined };

		#region Events

		public delegate void OnSceneLoadedHandler(string sceneName, Scene scene, bool isMainScene);
		public event OnSceneLoadedHandler OnSceneLoaded;

		public delegate void OnSceneUnLoadedHandler(string sceneName, Scene scene, bool isMainScene);
		public event OnSceneUnLoadedHandler OnSceneUnLoaded;

		public delegate void OnSceneLoadFailedHandler(string sceneName);
		public event OnSceneLoadFailedHandler OnSceneLoadFailed;

		public delegate void OnExperienceSceneLoadedHandler(string sceneName);
		public event OnExperienceSceneLoadedHandler OnExperienceSceneLoaded;

		#endregion

		private SceneConfig CurrentMainScene;
		public string MainChildSceneName
		{
			get
			{
				var thisComponent = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
				if (thisComponent != null)
				{
					return thisComponent.LoadedExperienceSceneNames;
				}
				return string.Empty;
			}
		}

		private string SceneToActivate;

		//Loading coroutines
		private Coroutine CoroutineFadeIn;
		private Coroutine CoroutineFadeOut;
		private Coroutine CoroutineSceneSyncMonitor;
		private Coroutine CoroutineSceneSyncResult;
		private Coroutine CoroutineExperienceSceneLoading;

		private List<OperationalTickets.IOpTicket> OpenedOpTickets = new List<OperationalTickets.IOpTicket>();

		private void Awake()
		{
			//Setup game window
			SetupGameWindow();

			ExperienceSetupLoader.Initialize();
		}

		void Start()
		{
			SceneManager.sceneLoaded += SceneManager_sceneLoaded;
			SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

#if !UNITY_EDITOR
			// Reset those flags just to be sure we're not using some left over from a previous run
			PlayerPrefs.SetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR, null);
			PlayerPrefs.SetInt(ExperienceSetupLoader.KEY_RUN_EXPERIENCE_SETUP, 0);

			EScene scene = RemoteSessionController.Instance.IsDesktopClient ? EScene.ExperienceClient : EScene.MainMenu;
			// Note: we don't want to use Enum.Parse() to convert the string to a value, because if the given argument is a number it will always succeed
			var allScenes = Enum.GetNames(typeof(EScene)).Select(s => s.ToLowerInvariant()).ToArray();
			foreach (var argument in Environment.GetCommandLineArgs())
			{
				try
				{
					int index = Array.IndexOf(allScenes, argument.ToLowerInvariant());
					if (index >= 0)
					{
						var values = (int[])Enum.GetValues(typeof(EScene));
						scene = (EScene)values[index];
						break;
					}
				}
				catch (Exception)
				{
					continue;
				}
			}
			if (ExperienceSetupLoader.ShouldRunSetup)
			{
				LoadSetupScene(scene, Transition.None, true);
			}
			else
			{
				LoadMainScene(scene, Transition.None, true);
			}
#else
			if (RemoteSessionController.Instance.IsDesktopClient)
            {
				PlayerPrefs.SetString(AutoJoinSessionController.KEY_DESKTOP_AVATAR, AutoJoinSessionController.DEFAULT_AVATAR_NAME_NOVR);
				PlayerPrefs.SetInt(ExperienceSetupLoader.KEY_RUN_EXPERIENCE_SETUP, 1);
			}

			//Load default scene
			LoadDefaultScene(true);
#endif
		}

		private void OnDisable()
        {
			if (GameController.HasInstance)
            {
				GameController.Instance.OnLeftSession -= Instance_OnLeftSession;
			}
		}

		private bool IsSubscribedToOnLeftSession = false;

		private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (!IsSubscribedToOnLeftSession && GameController.Instance)
            {
				IsSubscribedToOnLeftSession = true;
				GameController.Instance.OnLeftSession += Instance_OnLeftSession;
			}

			if (!string.IsNullOrEmpty(SceneToActivate) && scene.name == SceneToActivate)
			{
				SceneManager.SetActiveScene(scene);
				SceneToActivate = null;
			}

			if (OnSceneLoaded != null)
				OnSceneLoaded(scene.name, scene, scene.name == MainChildSceneName);
		}

		private void SceneManager_sceneUnloaded(Scene scene)
		{
			if (OnSceneUnLoaded != null)
				OnSceneUnLoaded(scene.name, scene, scene.name == MainChildSceneName);
		}

		private void Instance_OnLeftSession()
		{
			Debug.Log("Closing all SceneLoadTimeout operational tickets");
			//Notify scene controller to clear all error reports opened
			foreach (var report in OpenedOpTickets)
				report.Close();
			OpenedOpTickets.Clear();
		}

		#region Public interface

		/// <summary>
		/// Starts the emergency procedure.
		/// </summary>
		public void DoEmergency()
		{
			if (!Emergency)
			{
				Emergency = true;
				StopAllCoroutines();
				StartCoroutine(StartEmergency());
			}
		}

		/// <summary>
		/// Loads the setup scene and then the given main scene
		/// </summary>
		/// <param name="scene">Scene name to load</param>
		/// <param name="transition">Scene transition</param>
		/// <param name="forceLoad">Force loading even if the given scene is already loaded</param>
		public void LoadSetupScene(EScene scene, Transition transition = Transition.FadeWhite, bool forceLoad = false)
        {
			if (Emergency)
				return;

			StartCoroutine(LoadSetupSceneAsync(scene, transition, forceLoad));
		}

		/// <summary>
		/// Loads the default scene. Currently the default scene is the main menu.
		/// </summary>
		/// <param name="forceLoad">Force loading even if the given scene is already loaded</param>
		public void LoadDefaultScene(bool forceLoad = false)
		{
			if (Emergency)
				return;

			//Unload all scenes but the main scene
			UnloadAllScenes();

			//Load default scene
			LoadMainScene(DEFAULT_SCENE, forceLoad: forceLoad);
		}

		/// <summary>
		/// Loads the given scene as main scene. Loading a new main scene will unload all previous experience child scenes.
		/// </summary>
		/// <param name="scene">Scene name to load</param>
		/// <param name="transition">Scene transition</param>
		/// <param name="forceLoad">Force loading even if the given scene is already loaded</param>
		public void LoadMainScene(EScene scene, Transition transition = Transition.FadeWhite, bool forceLoad = false)
		{
			if (Emergency)
				return;

			var mainSceneConfig = GetSceneConfig(scene);
			if (mainSceneConfig != null)
			{
				if (mainSceneConfig != CurrentMainScene)
				{
					StartCoroutine(LoadMainScene(mainSceneConfig));
				}
			}
			else
			{
				Debug.LogWarningFormat("<color=lime>Tried to load invalid scene: {0}. Ignoring scene loading</color>", scene.ToString());
			}
		}

		/// <summary>
		/// Loads a new child scene.
		/// </summary>
		/// <param name="sceneName">Child scene name to load</param>
		/// <param name="transition">Scene transition</param>
		/// <param name="unloadOtherChilds">Indicates if all other child scenes should be unloaded. Default is false.</param>
		/// <param name="forceReload">Indicated if the scene has to be loaded even if it's already loaded</param>
		public void LoadChildScene(string sceneName, Transition transition, ELoadSequence loadSequence = ELoadSequence.UnloadFirst, bool forceReload = false, bool awaitSceneSync = false, string customTransitionName = null)
		{
			if (Emergency)
				return;

			if (!string.IsNullOrEmpty(sceneName) && (!IsSceneLoaded(sceneName) || forceReload))
			{
				StartExperienceSceneLoad(sceneName, transition, loadSequence: loadSequence, setActiveScene: true, awaitSceneSync: awaitSceneSync, customTransitionName: customTransitionName);
			}
		}

		/// <summary>
		/// Returns a list of all currently loaded experience scenes.
		/// </summary>
		/// <returns></returns>
		public List<Scene> GetLoadedExperienceScenes()
		{
			var experienceScenes = new List<Scene>();
			for (var i = 0; i < SceneManager.sceneCount; ++i)
			{
				var scene = SceneManager.GetSceneAt(i);

				//Exclude unloaded scenes
				if (!scene.isLoaded)
					continue;

				//Exclude SDK scenes
				if (scene.name == ROOT_SCENE_NAME || scene.name == EMERGENCY_SCENE_NAME)
					continue;

				//Exclude component scene
				if (CurrentMainScene != null && CurrentMainScene.SceneName == scene.name)
					continue;

				experienceScenes.Add(scene);
			}
			return experienceScenes;
		}

		#endregion

		#region Internals

		private bool Emergency;
		private IEnumerator StartEmergency()
		{
			//Make sure the camera is faded in
			var cameraFader = GetCurrentVRCamFader();
			if (cameraFader != null)
				cameraFader.SetFaded(Transition.None);

			SceneManager.LoadScene(EMERGENCY_SCENE_NAME, LoadSceneMode.Additive);
			yield return UnloadAllExperienceScenes();
			yield return null;
		}

		private void StartExperienceSceneLoad(string sceneName, Transition transition,  ELoadSequence loadSequence = ELoadSequence.UnloadFirst, bool setActiveScene = true, bool awaitSceneSync = false, string customTransitionName = null)
		{
			//Check if scene can be loaded
			if(Application.CanStreamedLevelBeLoaded(sceneName))
			{
				//Stop already started scene transition routines... but keep all others running in case we have a main scene loading running in parallel
				StopCurrentExperienceSceneLoad();

				//Start async transition
				CoroutineExperienceSceneLoading = StartCoroutine(StartExperienceSceneTransitionAsync(sceneName, transition, loadSequence, awaitSceneSync: awaitSceneSync, customTransitionName: customTransitionName));
			}
			else
			{
				Debug.LogErrorFormat("Failed to load scene '{0}'. Make sure this scene is listed in the Unity build scenes!", sceneName);
				if (OnSceneLoadFailed != null)
					OnSceneLoadFailed(sceneName);
			}
		}

		private void StopCurrentExperienceSceneLoad()
		{
			ProcessHeartbeat.Instance.Resume();

			if (CoroutineExperienceSceneLoading != null)
			{
				StopCoroutine(CoroutineExperienceSceneLoading);
				CoroutineExperienceSceneLoading = null;
                if (ConfigService.VerboseSdkLog) Debug.Log("Stopped coroutine: CoroutineExperienceSceneLoading");
            }

			if (CoroutineSceneSyncMonitor != null)
			{
				StopCoroutine(CoroutineSceneSyncMonitor);
				CoroutineSceneSyncMonitor = null;
                if (ConfigService.VerboseSdkLog) Debug.Log("Stopped coroutine: CoroutineSceneSyncMonitor");
            }

            if (CoroutineSceneSyncResult != null)
			{
				StopCoroutine(CoroutineSceneSyncResult);
				CoroutineSceneSyncResult = null;
                if (ConfigService.VerboseSdkLog) Debug.Log("Stopped coroutine: CoroutineSceneSyncResult");
            }

            if (CoroutineFadeIn != null)
			{
				StopCoroutine(CoroutineFadeIn);
				CoroutineFadeIn = null;
                if (ConfigService.VerboseSdkLog) Debug.Log("Stopped coroutine: CoroutineFadeIn");
            }

            if (CoroutineFadeOut != null)
			{
				StopCoroutine(CoroutineFadeOut);
				CoroutineFadeOut = null;
                if (ConfigService.VerboseSdkLog) Debug.Log("Stopped coroutine: CoroutineFadeOut");
            }
        }

		private IEnumerator SetupScene()
		{
			//Search for SceneSetup object
			var sceneSetups = FindObjectsOfType<SceneSetup>();

			SceneSetup setupToLoad = null;

			//Validate
			if (sceneSetups != null)
			{
				if (sceneSetups.Length == 1)
					setupToLoad = sceneSetups[0];
				else if (sceneSetups.Length > 1)
					Debug.LogError("<color=lime>Found more than one SceneSetup! Only one can be present at a time</color>");
			}

			//Load setup
			if (setupToLoad)
			{
				//Setup camera
				if (MainCameraController.Instance)
					MainCameraController.Instance.ReplaceCamera(setupToLoad.CameraTemplate);

				//Call scene setup async call
				yield return setupToLoad.SetupScene();
			}
			else
			{
				//Load defaults
				//Setup camera
				if (MainCameraController.Instance)
					MainCameraController.Instance.ReplaceCamera(null);
			}

		}

		private void UnloadAllScenes()
		{
			var scenes = new List<Scene>();
			for (var i = 0; i < SceneManager.sceneCount; ++i)
				scenes.Add(SceneManager.GetSceneAt(i));

			foreach (var scene in scenes)
			{
				if (scene.name != ROOT_SCENE_NAME && scene.name != EMERGENCY_SCENE_NAME)
					SceneManager.UnloadSceneAsync(scene.name);
			}

			CurrentMainScene = null;
			Resources.UnloadUnusedAssets();
		}

		private ICameraFader GetCurrentVRCamFader()
		{
			if (MainCameraController.Instance)
				return MainCameraController.Instance.CameraFader;
			else
				return null;
		}

		private SceneConfig GetSceneConfig(EScene scene)
		{
			SceneConfig config = null;
			if (SceneConfigs.TryGetValue(scene, out config))
			{
				return config;
			}
			return null;
		}

		
		private bool IsSceneLoaded(string sceneName)
		{
			for (var i = 0; i < SceneManager.sceneCount; ++i)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.name == sceneName)
					return true;
			}
			return false;
		}

		private string GetJobListString(List<SceneLoadJob> jobs)
		{
			var jobListString = new StringBuilder();
			foreach (var job in jobs)
				jobListString.AppendFormat("{0}, ", job.ToString());
			return jobListString.ToString();
		}

		private List<SceneLoadJob> GetUnloadAllExperienceScenesJobs()
		{
			//Collect jobs
			var jobs = new List<SceneLoadJob>();
			for (var s = 0; s < SceneManager.sceneCount; ++s)
			{
				var scene = SceneManager.GetSceneAt(s);
				if (scene.name != ROOT_SCENE_NAME && scene.name != CurrentMainScene.SceneName)
				{
					jobs.Add(new SceneLoadJob
					{
						JobType = SceneLoadJob.ESceneJobType.Unload,
						Scene = scene,
					});
				}
			}
			return jobs;
		}

		private void SetComponentScene(string sceneName)
		{
			var thisComponent = SharedDataUtils.GetMyComponent<LocationComponentWithSession>();
			if (thisComponent != null)
			{
				thisComponent.LoadedExperienceSceneNames = sceneName;
			}
		}

		private const string UnityWindowClassName = "UnityWndClass";
		private void SetupGameWindow()
		{
#if !UNITY_EDITOR
            // Ignore "AlwaysOnTop" if environment variable is defined and has a value different than 0
            var ignore = Environment.GetEnvironmentVariable("ARTANIM_IGNORE_ON_TOP");
            if (((ignore == null) || (ignore == "0"))
                && CommandLineUtils.GetValue("AlwaysOnTop", true))
			{
				WinKernel.SetTopMost(true, UnityWindowClassName);
			}
#endif
        }

		#endregion

		#region Async handling

		private IEnumerator LoadSetupSceneAsync(EScene scene, Transition transition, bool forceLoad)
		{
			yield return LoadMainScene(SetupSceneConfig);

			var setupLoader = ExperienceSetupLoader.Instance;
			if (setupLoader)
            {
				yield return setupLoader.LoadMainSceneWhenDone(scene, transition, forceLoad);
			}
			else
            {
				Debug.LogError("Internal error, ExperienceSetupLoader not found");
            }
		}

		private IEnumerator LoadMainScene(SceneConfig mainScene)
		{
			if (mainScene != null)
			{
				if (mainScene.IsDeprecated)
                {
					Debug.LogError(mainScene.SceneName + " is deprecated");
				}

				//Unload all scenes
				yield return UnloadAllScenesButMain();

				//Set active
				SceneToActivate = mainScene.SceneName;

				//Load the new main scene
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Loading main scene: {0}</color>", mainScene);
				var async = SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Additive);
				if (async != null)
				{
					async.allowSceneActivation = true;
					CurrentMainScene = mainScene;

					//Wait for load to be done
					yield return new WaitUntil(() => async.isDone);

					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Main scene was loaded: {0}</color>", mainScene);
				}
			}
		}

		private IEnumerator StartExperienceSceneTransitionAsync(string sceneName, Transition transition, ELoadSequence loadSequence, bool awaitSceneSync = false, string customTransitionName = null)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Loading experience scene: sceneName={0}, transition={1}, loadSequence={2}, awaitSceneSync={3}</color>",
				sceneName, transition.ToString(), loadSequence.ToString(), awaitSceneSync);

			//Notify others using shared data that we're currently loading
			SetComponentScene(string.Format(SCENE_LOADING_FORMAT, sceneName));

			//Fade out
			CoroutineFadeOut = StartCoroutine(FadeOutAsync(transition, customTransitionName: customTransitionName));
			yield return CoroutineFadeOut;
			//yield return FadeOutAsync(transition);

			//Scene activation
			SceneToActivate = sceneName;

			//Collect unload jobs
			var sceneJobs = GetUnloadAllExperienceScenesJobs();

			//Add load job: if no transition, load first, otherwise unload all scenes first
			if (loadSequence == ELoadSequence.UnloadFirst)
				sceneJobs.Add(new SceneLoadJob { JobType = SceneLoadJob.ESceneJobType.Load, SceneName = sceneName, });
			else
				sceneJobs.Insert(0, new SceneLoadJob { JobType = SceneLoadJob.ESceneJobType.Load, SceneName = sceneName, });

			//Wait for session sync?
			var sceneSyncResult = new SceneSyncResult();
			if (awaitSceneSync && GameController.Instance && GameController.Instance.CurrentSession != null)
			{
				//Start checking for the scene load sync message
				CoroutineSceneSyncResult = StartCoroutine(MonitorSceneSyncMessage(sceneSyncResult, sceneName));

				//Start scene monitoring
				if (NetworkInterface.Instance.IsServer)
					CoroutineSceneSyncMonitor = StartCoroutine(MonitorSceneLoading(sceneName));
			}

#if TEST_CLIENT_LOAD_DELAY
			if (NetworkInterface.Instance.IsClient)
			{
				var sleepTime = new System.Random().Next(25000, 35000);
				Debug.LogErrorFormat("<color=lime>DEBUG: Client sleeping for {0}ms</color>", sleepTime);
				Thread.Sleep(sleepTime);
			}
#endif

			//Execute jobs sequential
			yield return ExecuteSceneJobsSequential(sceneJobs);
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=lime>Scene unloading/loading done.</color>");

			//Setup scene
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=lime>Setup scene</color>");
			yield return SetupScene();

#if UNITY_2019_3_OR_NEWER
            LightProbes.Tetrahedralize();
#endif
			SetComponentScene(sceneName);

            //Check or wait for scene sync message before fading in
            if (awaitSceneSync)
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=lime>Waiting for scene sync result...</color>");
				yield return WaitForSceneSyncResult(sceneSyncResult);
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Scene sync result is done: IsTimedOut={0}</color>", sceneSyncResult.IsTimedOut);
			}

			//Notify scene loaded in session if needed
			if (awaitSceneSync)
			{
				GameController.Instance.NotifySceneLoadedInSession(sceneName, sceneSyncResult.IsTimedOut);
			}

			//Fade in
			CoroutineFadeIn = StartCoroutine(FadeInAsync());
			yield return CoroutineFadeIn;

			CoroutineExperienceSceneLoading = null;

			//Notify scene load
			if (OnExperienceSceneLoaded != null)
				OnExperienceSceneLoaded.Invoke(sceneName);
		}

		private IEnumerator ExecuteSceneJobsParallel(List<SceneLoadJob> jobs)
		{
			if (jobs != null && jobs.Count > 0)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Executing scene load jobs parallel: {0}</color>", GetJobListString(jobs));

				//Start all jobs
				foreach (var job in jobs)
				{
					switch (job.JobType)
					{
						case SceneLoadJob.ESceneJobType.Load:
							job.Operation = SceneManager.LoadSceneAsync(job.SceneName, LoadSceneMode.Additive);
							break;
						case SceneLoadJob.ESceneJobType.Unload:
							job.Operation = SceneManager.UnloadSceneAsync(job.Scene);
							break;
					}
				}

				//Wait for all jobs done
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=lime>Waiting for all scene jobs to be executed...</color>");
				yield return new WaitWhile(() => jobs.Any(j => j.Operation != null && !j.Operation.isDone));
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=lime>All scene jobs done</color>");
			}

			yield return null;
		}

		private IEnumerator ExecuteSceneJobsSequential(List<SceneLoadJob> jobs)
		{
			if (jobs != null && jobs.Count > 0)
			{
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Executing scene load jobs sequential: {0}</color>", GetJobListString(jobs));

				ProcessHeartbeat.Instance.Suspend();

				try
				{
					foreach (var job in jobs)
					{
						switch (job.JobType)
						{
							case SceneLoadJob.ESceneJobType.Load:
								if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Waiting for scene job to be executed... {0}</color>", job.ToString());
                                try
                                {
								    job.Operation = SceneManager.LoadSceneAsync(job.SceneName, LoadSceneMode.Additive);
                                }
                                catch(Exception ex)
                                {
                                    Debug.LogErrorFormat("Unity error loading scene: {0}\n{1}", ex.Message, ex.StackTrace);
                                }

                                if(job.Operation != null)
                                {
                                    yield return new WaitUntil(() => job.Operation.isDone);
                                    if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Scene job is done: {0}</color>", job.ToString());
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Unity returned null async operation when trying to load {0}", job.SceneName);
                                }
								
								break;

							case SceneLoadJob.ESceneJobType.Unload:
								if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Waiting for scene job to be executed... {0}</color>", job.ToString());
                                try
                                {
                                    job.Operation = SceneManager.UnloadSceneAsync(job.Scene);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogErrorFormat("Unity error unloading scene: {0}\n{1}", ex.Message, ex.StackTrace);
                                }

                                if (job.Operation != null)
                                {
                                    yield return new WaitUntil(() => job.Operation.isDone);
                                    if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Scene job is done: {0}</color>", job.ToString());
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Unity returned null async operation when trying to unload {0}", job.SceneName);
                                }
                                
								break;
						}
					}
				}
				finally
				{
					ProcessHeartbeat.Instance.Resume();
				}
			}	
		}
		
		private IEnumerator WaitForSceneSyncResult(SceneSyncResult result)
		{
			while (!result.IsDone)
				yield return null;
		}

		private IEnumerator MonitorSceneSyncMessage(SceneSyncResult result, string sceneName)
		{
			SceneLoadedInSession lastMessage = null;

			var sceneLoadedAction = new Action<SceneLoadedInSession>((SceneLoadedInSession args) =>
			{
				if(args.Scene == sceneName)
					lastMessage = args;
			});

			//Subscribe sync message
			NetworkInterface.Instance.Subscribe(sceneLoadedAction);

			var timeoutTime = Time.realtimeSinceStartup + ConfigService.Instance.ExperienceSettings.SceneSyncTimeout + 2f; //Wait a bit more than the monitoring timeout before the local timeout kicks in
			int loop = 0;
			while (lastMessage == null && Time.realtimeSinceStartup < timeoutTime)
			{
				//Wait another frame...
				++loop;

				if (ConfigService.VerboseSdkLog && (loop % 100 == 0))
					Debug.Log("<color=lime>Waiting for sync message...</color>");

				yield return null;
			}
			
			if (lastMessage != null)
			{
				//We got the message
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Sync message received: Timeout={0}</color>", lastMessage.SceneLoadTimeout);
				result.IsTimedOut = lastMessage.SceneLoadTimeout;
				result.IsDone = true;
			}
			else
			{
				//We got timed out
				Debug.LogError("<color=lime>Local timeout waiting for scene sync event</color>");
				result.IsTimedOut = true;
				result.IsDone = true;

				//Report a ticket for the timeout
				OpenedOpTickets.Add(OperationalTickets.Instance.OpenTicket(new SceneLoadTimeout
				{
					ComponentId = SharedDataUtils.MySharedId.Guid,
					SceneName = sceneName,
					LocalTimeout = true,
				}));
			}

			//Unsubscribe sync message
			NetworkInterface.SafeUnsubscribe(sceneLoadedAction);

			CoroutineSceneSyncResult = null;
		}

		private IEnumerator UnloadAllExperienceScenes()
		{
			//Collect jobs
			var jobs = GetUnloadAllExperienceScenesJobs();
			yield return ExecuteSceneJobsParallel(jobs);
		}

		private IEnumerator UnloadAllScenesButMain()
		{
			//Collect jobs
			var jobs = new List<SceneLoadJob>();
			for (var s = 0; s < SceneManager.sceneCount; ++s)
			{
				var scene = SceneManager.GetSceneAt(s);
				if (scene.name != ROOT_SCENE_NAME)
				{
					jobs.Add(new SceneLoadJob
					{
						JobType = SceneLoadJob.ESceneJobType.Unload,
						Scene = scene,
					});
				}
			}

			yield return ExecuteSceneJobsParallel(jobs);
		}

		private IEnumerator FadeOutAsync(Transition transition, string customTransitionName = null)
		{
			var cameraFader = GetCurrentVRCamFader();
			if (cameraFader != null)
			{
				yield return cameraFader.DoFadeAsync(transition, customTransitionName: customTransitionName);
			}
			CoroutineFadeOut = null;
		}

		private IEnumerator FadeInAsync(/*Transition transition*/)
		{
			var cameraFader = GetCurrentVRCamFader();
			if (cameraFader != null)
			{
				yield return cameraFader.DoFadeInAsync();
			}
			CoroutineFadeIn = null;
		}

		private IEnumerator MonitorSceneLoading(string sceneToMonitor)
		{
			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Starting scene monitoring for scene: {0}</color>", sceneToMonitor);

			if (!string.IsNullOrEmpty(sceneToMonitor) && GameController.Instance && GameController.Instance.CurrentSession != null)
			{
				bool allLoaded = false;
				bool isTimeout = false;

				float timeOutTime = 0f;

				IEnumerable<LocationComponentWithSession> stillLoading = null;
				while (!allLoaded && !isTimeout)
				{
					yield return new WaitForSecondsRealtime(0.2f);

					var allSessionComponents = SharedDataUtils.Components.OfType<LocationComponentWithSession>().Where(c => c.SessionId == GameController.Instance.CurrentSession.SharedId);
					stillLoading = allSessionComponents.Where(c => c.LoadedExperienceSceneNames != sceneToMonitor);

					//Debug.LogErrorFormat("Loading monitor: #all={0}, #loading={1}, all={2}, stillLoading={3}",
					//	allSessionComponents.Count(),
					//	stillLoading.Count(),
					//	string.Join(", ", allSessionComponents.Select(c => c.SharedId.ToString()).ToArray()),
					//	string.Join(", ", stillLoading.Select(c => c.SharedId.ToString()).ToArray()));

					if (stillLoading.Count() != 0)
					{
						//Not all loaded... wait

						//Handle timeout
						if (timeOutTime == 0f && stillLoading.Count() < allSessionComponents.Count())
						{
							//At least one loaded... start timeout
							timeOutTime = Time.realtimeSinceStartup + ConfigService.Instance.ExperienceSettings.SceneSyncTimeout;
						}
						else if (timeOutTime > 0f)
						{
							//Check timeout
							isTimeout = Time.realtimeSinceStartup > timeOutTime;
						}

						//Debug.LogError("Waiting for scene sync");
					}
					else
					{
						//All done
						allLoaded = true;
					}
				}

				if (isTimeout)
				{
					var timedOutComponents = stillLoading.Select(c => c.SharedId).ToArray();
					Debug.LogErrorFormat("<color=lime>Scene loading terminated with timeout: timeout={0}sec, scenes={1}, still loading={2}</color>",
						ConfigService.Instance.ExperienceSettings.SceneSyncTimeout,
						sceneToMonitor,
						stillLoading != null ? string.Join(", ", timedOutComponents.Select(g => g.ToString()).ToArray()) : "null");

					//Report a ticket for all timed out components
					foreach(var timedOutComponent in timedOutComponents)
					{
						OpenedOpTickets.Add(OperationalTickets.Instance.OpenTicket( new SceneLoadTimeout { ComponentId = timedOutComponent, SceneName = sceneToMonitor, LocalTimeout = false, } ));
					}
				}
				else
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=lime>Scene loading done. All components loaded in time. scenes={0}</color>", sceneToMonitor);
				}


				//Send sync event if needed
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("Sending scene sync message to session for Scenes: {0}, SceneLoadTimeout={1}", sceneToMonitor, isTimeout);
				NetworkInterface.Instance.SendMessage(new SceneLoadedInSession()
				{
					Scene = sceneToMonitor,
					SceneLoadTimeout = isTimeout,
				});
			}
			
			CoroutineSceneSyncMonitor = null;
		}

#endregion

		#region Classes

		private class SceneConfig
		{
			public ELocationComponentType ComponentType;
			public string SceneName;
			public bool IsDeprecated;

			public override string ToString()
			{
				return string.Format("Scene config: ComponentType={0}, SceneName={1}", ComponentType.ToString(), SceneName);
			}
		}

		private class SceneLoadJob
		{
			public enum ESceneJobType { Load, Unload }

			public ESceneJobType JobType { get; set; }

			public string SceneName { get; set; }

			private Scene _Scene;
			public Scene Scene
			{
				get { return _Scene; }
				set { _Scene = value; SceneName = _Scene.name; }
			}

			public AsyncOperation Operation { get; set; }

			public override string ToString()
			{
				return string.Format("{0}: {1}", JobType.ToString(), SceneName);
			}
		}

		private class SceneSyncResult
		{
			public bool IsDone { get; set; }
			public bool IsTimedOut { get; set; }
		}

		#endregion
	}

}