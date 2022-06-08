using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artanim.Location.Network;

namespace Artanim
{
	// This behaviour exists only to shutdown destroy the managed singletons that keep the artanim-native DLL loaded.
	// By destroying those singletons, we ensure that the artanim-native DLL is unloaded when exiting play mode.
	// We do this for 2 reasons:
	// - be able to update artanim-native when not in play mode (otherwise the file is locked)
	// - properly shutdown the native libraries so they can finish their job
	//   (and as a consequence they are not to be used by some editor code)
	public class NativeController
		: MonoBehaviour
	{
		void Awake()
		{
			// Show the console:
			//Utils.WinKernel.ShowConsole();

			// Add the libs directory into the current path so native DLLs are found
#if UNITY_EDITOR
			string dllPath = Path.Combine(ArtanimCommon.EditorCommonDir, "Libs");
#elif UNITY_2019_1_OR_NEWER
			// In a build, all our native DLLs are copied into the Plugins folder
			string dllPath = Path.Combine(Application.dataPath, "Plugins\\x86_64");
#else
			string dllPath = Path.Combine(Application.dataPath, "Plugins");
#endif
			string currentPath = Environment.GetEnvironmentVariable("PATH");
			if (!currentPath.Contains(dllPath))
			{
				Environment.SetEnvironmentVariable("PATH", dllPath + Path.PathSeparator + currentPath);
			}

			var streamingAssetsHomePath = Path.Combine(Application.streamingAssetsPath, ".home");
			string localHome = CommandLineUtils.GetValue("LocalHome", streamingAssetsHomePath, null);
			if ((localHome != null) && (!Directory.Exists(localHome)))
			{
				Utils.OperatingSystem.WinErrorMessage(string.Format("Invalid {0} path", Base.Constants.ArtanimHomeEnv), localHome);
				Application.Quit();
			}

#if UNITY_EDITOR
			if (Directory.Exists(streamingAssetsHomePath) || (localHome != null))
#else
			if (RemoteSessionController.Instance.IsDesktopClient || (localHome != null))
#endif
			{
				string home = localHome != null ? localHome : streamingAssetsHomePath;
				Debug.LogWarningFormat("Setting '{0}' environment variable to: {1}", Base.Constants.ArtanimHomeEnv, home);
				Environment.SetEnvironmentVariable(Base.Constants.ArtanimHomeEnv, home);
			}

			// Check that the native DLL can load
			string errorMsg = null;
			string moduleName = Utils.NativeModulesList.ArtanimNativeDllName;
			try
            {
				int error = Utils.NativeModulesList.CheckCanLoadModule(moduleName);
				if (error != 0)
				{
					errorMsg = string.Format("Got error code: {0}", error);
					Debug.LogErrorFormat("Error loading {0}: {1}", moduleName, errorMsg);
				}
			}
			catch (Exception e)
            {
				Debug.LogException(e);
				errorMsg = string.Format("Got exception of type {0} with message: {1}", e.GetType(), e.Message);
			}
			if (errorMsg != null)
            {
				Utils.OperatingSystem.WinErrorMessage(string.Format("Error loading {0}", moduleName), errorMsg);
				Application.Quit();
			}

#if UNITY_EDITOR
			// We want to get notified when we exit play mode
			SubscribePlayModeStateChanged();
#endif
		}

		public static void DisposeAllSingletons()
        {
			if (ConfigService.VerboseSdkLog) Debug.Log("Disposing all singletons");

            Location.SharedData.SharedDataUtils.Shutdown();
			if(NetworkInterface.HasInstance)
            {
				NetworkInterface.Instance.Disconnect();
			}
            Location.Helpers.NetworkSetup.ShutdownNetBus();
            Profiling.CPProfiler.ReleaseNative();

			LogController.Shutdown();

			Utils.OneTimeDisposableSingletonManager.DisposeAll();

            CrashHandler.CrashHandler.Shutdown();
            Journal.Journal.ForceUnloadNative();
			Utils.OperatingSystem.ForceUnloadNative();

			// We need to release every native resource for the native DLL to properly unload
			string nativeDllName = Utils.NativeModulesList.ArtanimNativeDllName;
            bool hasUnloadedModule;
            string fullPathname = Utils.NativeModulesList.ForceUnloadModule(nativeDllName, out hasUnloadedModule);
            if (hasUnloadedModule)
            {
                Debug.LogErrorFormat("Some native modules for '{0}' were not explicitly unloaded", fullPathname);
            }

			int maxAttempts = 50;
			while (IsNativeDllLoaded(fullPathname) && (maxAttempts >= 0))
            {
				System.Threading.Thread.Sleep(100);
				--maxAttempts;
			}

			if (IsNativeDllLoaded(fullPathname))
            {
                Debug.LogErrorFormat("Native DLL '{0}' not unloaded!", fullPathname);
            }
        }

        static bool IsNativeDllLoaded(string dllPathname)
        {
            return (!string.IsNullOrEmpty(dllPathname)) && (Utils.WinKernel.GetModuleHandle(dllPathname) != IntPtr.Zero);
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Artanim/Tools/Unload Native DLL")]
		static void UnloadNativeDLL()
		{
			string nativeDllName = Utils.NativeModulesList.ArtanimNativeDllName;

			// Dispose the managed singletons that use the artanim-native DLL
			Debug.Log("Shutting down " + nativeDllName + " DLL");

            // Manually dispose some of our managed singletons
            // Already done in RuntimeLocationComponent, but when in the editor we might not even have instantiated this behaviour
            NativeController.DisposeAllSingletons();
		}

#if UNITY_5 || UNITY_2017_1

		static void SubscribePlayModeStateChanged()
		{
			UnityEditor.EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
		}

		static void PlaymodeStateChanged()
		{
			// Check that we are exiting play mode
			if (!UnityEditor.EditorApplication.isPlaying)
			{
				try
				{
					UnloadNativeDLL();
				}
				finally
				{
					// Unsubscribe from callback
					UnityEditor.EditorApplication.playmodeStateChanged -= PlaymodeStateChanged;
				}
			}
		}

#else

		static void SubscribePlayModeStateChanged(bool unsubscribe = false)
		{
			UnityEditor.EditorApplication.playModeStateChanged += PlaymodeStateChanged;
		}

		static void PlaymodeStateChanged(UnityEditor.PlayModeStateChange state)
		{
			// Check that we are exiting play mode
			if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
			{
				try
				{
					UnloadNativeDLL();
				}
				finally
				{
					// Unsubscribe from callback
					UnityEditor.EditorApplication.playModeStateChanged -= PlaymodeStateChanged;
				}
			}
		}
#endif

#endif
    }
}
