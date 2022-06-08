using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artanim.Experience;
using Artanim.Location.Network;
using Artanim.Location.Messages;

namespace Artanim
{
	[RequireComponent(typeof(GameController))]
	public class GameSessionController : SingletonBehaviour<GameSessionController>
	{
		public RectTransform GameSessionDebugPanel;

		#region Events

		public delegate void OnValueUpdatedHandler(string key, object value, bool playerValue = false, bool isInitializing = false);
		public event OnValueUpdatedHandler OnValueUpdated;

        #endregion

        #region Public methods

		/// <summary>
		/// Check whether or not there is a value for the specified key and, optionally, for a specific player
		/// </summary>
		/// <param name="key">Name of the key to check</param>
		/// <param name="playerId">Optional player id</param>
		/// <returns>True if there is a value</returns>
        public bool HasValue(string key, Guid playerId = default(Guid))
		{
			if(_gameSession != null)
			{
				return _gameSession.HasValue(key, playerId);
			}
			return false;
		}

		/// <summary>
		/// Get the value associated with the specified key and, optionally, for a specific player
		/// </summary>
		/// <typeparam name="T">Type of the value</typeparam>
		/// <param name="key">Name of the key</param>
		/// <param name="defaultValue">Default value to use</param>
		/// <param name="playerId">Optional player id</param>
		/// <returns>The value for the given key, or <paramref name="defaultValue"/> if not found</returns>
		public T GetValue<T>(string key, T defaultValue, Guid playerId = default(Guid))
		{
			if (_gameSession != null)
			{
				return _gameSession.GetValue(key, defaultValue, playerId);
			}
			return defaultValue;
		}

		/// <summary>
		/// Set the value associated for the specified key and, optionally, only for a specific player
		/// </summary>
		/// <typeparam name="T">Type of the value</typeparam>
		/// <param name="key">Name of the key</param>
		/// <param name="value">Value to set</param>
		/// <param name="playerId">Optional player id</param>
		public void SetValue<T>(string key, T value, Guid playerId = default(Guid))
		{
			if (_gameSession == null)
			{
				// This is to show when SetValue is called before the game session is ready
				// but it will also happen when the component has left the session (and in this case we don't really care)
				Debug.LogWarning("<color=teal>Cannot set a Game Session Value because controller is not yet ready</color>");
			}
			else
			{
				if(NetworkInterface.Instance.IsServer)
				{
					if(!string.IsNullOrEmpty(key))
					{
						_gameSession.SetValue(key, value, playerId);
					}
					else
					{
						Debug.LogWarning("<color=teal>Cannot set game session value. The given key was empty or null.</color>");
					}
				}
			}
		}

		/// <summary>
		/// Shows all the game session values on screen
		/// </summary>
		public void DumpGameSession()
		{
			if (GameSessionDebugPanel)
			{
				GameSessionDebugPanel.gameObject.SetActive(true);
				var text = GameSessionDebugPanel.GetComponentInChildren<Text>();
				if (text != null)
				{
					var sessionDump = "N/A";
					if (_gameSession != null)
					{
						sessionDump = _gameSession.DebugSessionState;
						if (string.IsNullOrEmpty(sessionDump))
						{
							sessionDump = "<empty session>";
						}
					}
						
					text.text = sessionDump;
				}
			}
		}

		#endregion

		#region Internals

		GameSessionValues _gameSession;

		void OnEnable()
		{
			GameController.Instance.OnJoinedSession += GameController_OnJoinedSession;
			GameController.Instance.OnLeftSession += GameController_OnLeftSession;
		}

		void OnDisable()
		{
			GameController.Instance.OnJoinedSession -= GameController_OnJoinedSession;
			GameController.Instance.OnLeftSession -= GameController_OnLeftSession;
		}

		void OnDestroy()
		{
			// Clear
			GameController_OnLeftSession();
		}

		void GameController_OnJoinedSession(Location.Data.Session session, Guid playerId)
		{
			//Create / init game session
			if (_gameSession != null)
			{
                //Leave session first
				_gameSession.OnValueUpdated -= GameSession_OnValueUpdated;
				_gameSession.Dispose();
				_gameSession = null;
			}
			_gameSession = new GameSessionValues(session.SharedId, playerId);
			_gameSession.OnValueUpdated += GameSession_OnValueUpdated;
		}

		void GameController_OnLeftSession()
		{
			//Clear Game Session
			if (_gameSession != null)
			{
				_gameSession.OnValueUpdated -= GameSession_OnValueUpdated;
				_gameSession.Dispose();
				_gameSession = null;
			}
		}

		void GameSession_OnValueUpdated(string key, object value, bool playerValue = false, bool isInitializing = false)
		{
			if (OnValueUpdated  != null)
			{
				OnValueUpdated(key, value, playerValue, isInitializing);
			}
		}

		#endregion
	}
}