using UnityEngine;
using System.Collections;
using System.IO;
using Artanim.LogSystem;
using System.Collections.Generic;
using System.Threading;
using System;

namespace Artanim
{
	// Initialize our logging system.
	// It's recommended to give script a high priority so it runs as soon as possible in order
	// to ensure that even early Unity logs are forwarded to our logger
	public class LogController : MonoBehaviour
	{
		#region Internals
		private const string RVLoggerMessagerHeader = "RVLogger: ";
		private const string UnityLoggerKeyName = "Unity.Log";
		private const string UnityLoggerName = "Unity";
		private const string UnityConsoleName = "Unity.Console";

		private Artanim.LogSystem.Logger logger;
		private static LogController instance;
		private ILogHandler _defaultlogger;
		private IEnumerator _logcoroutine = null;

		internal static LogController Instance { get { return instance; } }

		void Awake()
		{
			instance = this;

#if UNITY_5
			_defaultlogger = Debug.logger.logHandler;
#else
			_defaultlogger = Debug.unityLogger.logHandler;
#endif

			if (Debug.isDebugBuild)
			{
				LogManager.Instance.Init(UnityConsoleName, activatePolling: true);

				_logcoroutine = LogConsoleCoroutine();

				StartCoroutine(_logcoroutine);
			}
			else
			{
				LogManager.Instance.Init(UnityConsoleName);
			}

			logger = LogManager.Instance.GetLogger(UnityLoggerKeyName, UnityLoggerName);

			// We want to log every message from now on, so we never unsubscribe from this event
			Application.logMessageReceivedThreaded += HandleLogCallback;

			//
			// Log some useful info that we want to see early in the logs
			//

			// Log ARTANIM_HOME path
			if (ConfigService.VerboseSdkLog)
			{
				Debug.LogFormat("{0} is set to: {1}", Base.Constants.ArtanimHomeEnv, Environment.GetEnvironmentVariable(Base.Constants.ArtanimHomeEnv));

				// Log mono/.net runtime version
				bool isMono = Type.GetType("Mono.Runtime") != null;
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("Running with {0} runtime version {1}", isMono ? "Mono" : ".NET", Environment.Version);
			}
		}

		void OnDestroy()
		{
			Shutdown();
		}

		public static void Shutdown()
		{
			if (instance != null)
            {
				instance._Shutdown();
				instance = null;
			}
		}

		private void _Shutdown()
		{
			if (_logcoroutine != null)
			{
				StopCoroutine(_logcoroutine);
				_logcoroutine = null;
			}

			//Application.logMessageReceivedThreaded -= HandleLogCallback;
		}

		private void HandleLogCallback(string msg, string stackTrace, LogType type)
		{
			if (LogManager.HasInstance)
			{
				// ignore messages written in LogConsoleCoroutine
				if (msg.StartsWith(RVLoggerMessagerHeader))
				{
					return;
				}

				// Remove color tags
				if (msg.StartsWith("<color=") && msg.EndsWith("</color>"))
				{
					int endTag = msg.IndexOf('>');
					if (endTag > 7)
					{
						msg = msg.Substring(endTag + 1, msg.Length - endTag - 9);
					}
				}

				switch (type)
				{
					case LogType.Log:
					case LogType.Assert:
						if (logger.IsInfoEnabled)
							logger.Info(msg);
						break;
					case LogType.Warning:
						if (logger.IsWarnEnabled)
							logger.Warn(msg);
						break;
					case LogType.Error:
						if (logger.IsErrorEnabled)
							logger.Error(msg);
						break;
					case LogType.Exception:
						if (logger.IsExceptionEnabled)
							logger.Exception(stackTrace, msg);
						break;
				}
			}
		}

		// This callback allows Console.Write messages to be pushed on Unity console.
		private void LogConsoleCallback(string loggerkeyname, string loggername, int level, string message)
		{
			//lock(_logMessageQueue)
			//{
			//	_logMessageQueue.Enqueue(new _logMessage() { loggername = loggername, level = level, message = message });
			//}
		}

		private IEnumerator LogConsoleCoroutine()
		{
			while (true)
			{
				while (true)
				{
					LogManager.LogEntry logentry = LogManager.Instance.PopMessage();

					if (logentry == null)
						break;

					// ignore messages written in HandleLogCallback
					if (logentry.loggerKeyName == UnityLoggerKeyName)
					{
						break;
					}

					if (CheckConsoleLoggerLevel(logentry.level))
					{
						LogType type = LogType.Log;
						switch (logentry.level)
						{
							case LogManager.Levels.DEBUG:
							case LogManager.Levels.INFO:
								type = LogType.Log;
								break;
							case LogManager.Levels.WARNING:
								type = LogType.Warning;
								break;
							case LogManager.Levels.ERROR:
							case LogManager.Levels.EXCEPTION:
								type = LogType.Error;
								break;
						}

						_defaultlogger.LogFormat(type, this, "{0}{1} - {2} - {3}", new string[] { RVLoggerMessagerHeader, logentry.loggerName, logentry.level.ToString(), logentry.message });
					}
				}

				// Wait until next frame
				yield return null;
			}
		}
		#endregion

		#region EditorOption
#if UNITY_EDITOR
		private const string MENU_LOG_EDITOR_BASE = "Artanim/Show SDK Logs in Editor/";
		private const string MENU_LOG_EDITOR_DEBUG = MENU_LOG_EDITOR_BASE + "DEBUG";
		private const string MENU_LOG_EDITOR_INFO = MENU_LOG_EDITOR_BASE + "INFO";
		private const string MENU_LOG_EDITOR_WARNING = MENU_LOG_EDITOR_BASE + "WARNING";
		private const string MENU_LOG_EDITOR_ERROR = MENU_LOG_EDITOR_BASE + "ERROR";
		private const string MENU_LOG_EDITOR_EXCEPTION = MENU_LOG_EDITOR_BASE + "EXCEPTION";
		private const string MENU_LOG_EDITOR_NONE = MENU_LOG_EDITOR_BASE + "NONE";

		private const string KEY_LOGGER_EDITOR = "ArtanimLoggerConsoleLevel";
#endif

		private static bool CheckConsoleLoggerLevel(LogManager.Levels level)
		{
			return level >= Level;
		}

		private static int _level = -1;
		private static LogManager.Levels Level
		{
			get
            {
				if(_level == -1)
                {
#if UNITY_EDITOR
					_level = UnityEditor.EditorPrefs.GetInt(KEY_LOGGER_EDITOR, (int)LogManager.Levels.ERROR);
#else
					_level = (int)LogManager.Levels.ERROR;
#endif
				}

				return (LogManager.Levels) _level;
			}

            set
            {
				_level = (int) value;
#if UNITY_EDITOR
				UnityEditor.EditorPrefs.SetInt(KEY_LOGGER_EDITOR, _level);
#endif
			}
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(MENU_LOG_EDITOR_DEBUG, false, 30)]
		public static void DoShowDebugLogsInEditor()
		{
			Level = LogManager.Levels.DEBUG;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_DEBUG, true)]
		public static bool ValidateShowDebugLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.DEBUG);
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_INFO, false, 31)]
		public static void DoShowInfoLogsInEditor()
		{
			Level = LogManager.Levels.INFO;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_INFO, true)]
		public static bool ValidateShowInfoLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.INFO);
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_WARNING, false, 32)]
		public static void DoShowWarningLogsInEditor()
		{
			Level = LogManager.Levels.WARNING;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_WARNING, true)]
		public static bool ValidateShowWarningLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.WARNING);
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_ERROR, false, 33)]
		public static void DoShowErrorLogsInEditor()
		{
			Level = LogManager.Levels.ERROR;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_ERROR, true)]
		public static bool ValidateShowErrorLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.ERROR);
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_EXCEPTION, false, 34)]
		public static void DoShowExceptionLogsInEditor()
		{
			Level = LogManager.Levels.EXCEPTION;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_EXCEPTION, true)]
		public static bool ValidateShowExceptionLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.EXCEPTION);
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_NONE, false, 34)]
		public static void DoShowNoneLogsInEditor()
		{
			Level = LogManager.Levels.NONE;
		}

		[UnityEditor.MenuItem(MENU_LOG_EDITOR_NONE, true)]
		public static bool ValidateShowNoneLogsInEditor()
		{
			return CheckMenuItem(LogManager.Levels.NONE);
		}

		private static bool CheckMenuItem(LogManager.Levels level)
        {
			UnityEditor.Menu.SetChecked(MENU_LOG_EDITOR_BASE + level.ToString(), level == Level);
			return true;
        }
#endif

		#endregion
	}

}