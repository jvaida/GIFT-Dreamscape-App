using Artanim.Location.Data;
using Artanim.Location.Network;
using Artanim.Location.SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Artanim.Experience
{
	class GameSessionValues : IDisposable
	{
		public delegate void OnValueUpdatedHandler(string key, object value, bool playerValue = false, bool isInitializing = false);
		public event OnValueUpdatedHandler OnValueUpdated;

		private GameSession _serverGameSession;
		private Guid _sessionId;
		private readonly bool _ownsGameSession;

		public string DebugSessionState
		{
			get
			{
				if (_serverGameSession == null)
				{
					return "(empty)";
				}
				else
				{
					return string.Join("\n", _serverGameSession.GameSessionValues.Select(v => v.ToString()).ToArray());
				}
			}
		}

		public Guid ObservedPlayerId { get; private set; }

		/// <summary>
		/// Either creates or connects to the shared data object for the GameSession
		/// </summary>
		/// <param name="sessionId">The session id of the game session</param>
		/// <param name="observedPlayerId">The player id for which to trigger events when a session value is changed</param>
		public GameSessionValues(Guid sessionId, Guid observedPlayerId)
		{
			ObservedPlayerId = observedPlayerId;
			_sessionId = sessionId;
			_ownsGameSession = NetworkInterface.Instance.IsServer;

			GameSession gameSession = null;
			if (_ownsGameSession)
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=teal>Creating game session shared data</color>");

				// Game server always creates game session
				gameSession = SharedDataController.Instance.CreateSharedData<GameSession>(sessionId);
			}
			else
			{
				// Watch for new game session (even if we find one right now, it could go away and come back when a network disconnect/reconnect happens)
				SharedDataController.Instance.SharedDataAdded += Instance_SharedDataAdded;

				// If client, retrieve game session
				gameSession = SharedDataUtils.FindSharedData<GameSession>(s => s.SessionId == sessionId);
				if (gameSession == null)
				{
					if (ConfigService.VerboseSdkLog) Debug.Log("<color=teal>Failed to retrieved game session shared data, waiting on update...</color>");
				}
			}

			// Subscribe to list changes event in server mode, so we always get the value updated event
			if (gameSession != null)
			{
				SetServerGameSession(gameSession);
			}
		}

		private void Instance_SharedDataAdded(BaseSharedData sharedData)
		{
			var gameSession = sharedData as GameSession;
			if ((gameSession != null) && (gameSession.SessionId == _sessionId))
			{
				SetServerGameSession(gameSession);
			}
		}

		private void SetServerGameSession(GameSession gameSession)
		{
			if (_serverGameSession != null)
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=teal>Cleaning previous connection to game session shared data</color>");

				// Unsubscribe from events
				_serverGameSession.GameSessionValues.SharedDataListChanged -= GameSessionValues_SharedDataListChanged;
				foreach (var sessionValue in _serverGameSession.GameSessionValues)
				{
					sessionValue.PropertyChanged -= GameSession_PropertyChanged;
				}
			}

			_serverGameSession = gameSession;

			if (_serverGameSession != null)
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("<color=teal>Connecting to game session shared data</color>");

				// Subscribe to list changes
				_serverGameSession.GameSessionValues.SharedDataListChanged += GameSessionValues_SharedDataListChanged;

				// Subscribe to value changes
				foreach (var sessionValue in _serverGameSession.GameSessionValues)
				{
					sessionValue.PropertyChanged += GameSession_PropertyChanged;
				}

				// Notify for initial value states
				foreach (var sessionValue in _serverGameSession.GameSessionValues)
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Received game session value initial state {0} of type {1}</color>", sessionValue, sessionValue.Value == null ? "null" : sessionValue.Value.GetType().ToString());
					NotifyUpdate(sessionValue.Key, sessionValue.Value, sessionValue.CollectionKey);
				}
			}
		}

		private void GameSessionValues_SharedDataListChanged(object sender, SharedDataListChangedEventArgs e)
		{
			switch (e.Action)
			{
				// A value has been added or changed
				case SharedDataListChangedAction.Add:
				case SharedDataListChangedAction.Replace:
					// Notify and register for new session value
					foreach (var item in e.NewItems)
					{
						var sessionValue = item as GameSessionValue;
						if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Received game session value added {0} of type {1}</color>", sessionValue, sessionValue.Value == null ? "null" : sessionValue.Value.GetType().ToString());
						NotifyUpdate(sessionValue.Key, sessionValue.Value, sessionValue.CollectionKey);

						// Subscribe to changes
						sessionValue.PropertyChanged += GameSession_PropertyChanged;
					}
					break;

				// We never remove any value, so this case shouldn't occur
				case SharedDataListChangedAction.Remove:
					foreach (var item in e.OldItems)
					{
						var sessionValue = item as GameSessionValue;
						if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Received game session value removed {0} of type {1}</color>", sessionValue, sessionValue.Value == null ? "null" : sessionValue.Value.GetType().ToString());
						NotifyUpdate(sessionValue.Key, null, sessionValue.CollectionKey);

						// Unsubscribe from changes
						sessionValue.PropertyChanged -= GameSession_PropertyChanged;
					}
					break;
			}
		}

		private void GameSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Value")
			{
				var sessionValue = sender as GameSessionValue;
				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Received game session value change {0} of type {1}</color>", sessionValue, sessionValue.Value == null ? "null" : sessionValue.Value.GetType().ToString());
				NotifyUpdate(sessionValue.Key, sessionValue.Value, sessionValue.CollectionKey);
			}
		}

		private void NotifyUpdate(string key, object value, Guid playerId, bool isInitializing = false)
		{
			//Notify listeners only if session value or playerId matches current player
			if ((OnValueUpdated != null) && (playerId == Guid.Empty || ObservedPlayerId == playerId))
			{
				try
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Notifying game session value with key '{0}' {1} to '{2}' for player {2} {3})</color>", key, value, playerId, isInitializing ? "initialized" : "changed");
					OnValueUpdated(key, value, playerId != Guid.Empty, isInitializing);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		public bool HasValue(string key, Guid playerId = default(Guid))
		{
			return GetGameSessionValue(playerId, key) != null;
		}

		public T GetValue<T>(string key, T defaultValue, Guid playerId = default(Guid))
		{
			if (typeof(T).IsClass && (typeof(T) != typeof(string)))
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("Reading game session value of non trivial type " + typeof(T));
			}

			GameSessionValue sessionValue;
			if (_ownsGameSession)
			{
				// For server, always create value
				sessionValue = GetOrCreateGameSessionValue(playerId, key, defaultValue);
			}
			else
			{
				// For client, try to get the session value
				sessionValue = GetGameSessionValue(playerId, key);

				if (sessionValue == null)
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Game session value not found with key '{0}' and value '{1}' for player {2}</color>", key, defaultValue, playerId);
				}
				else
				{
					if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Reading game session value with key '{0}' and value '{1}' for player {2}</color>", key, sessionValue.Value, playerId);
				}
			}

			if (sessionValue != null)
			{
				return (T)sessionValue.Value;
			}
			else
			{
				return defaultValue;
			}
		}

		public void SetValue<T>(string key, T value, Guid playerId = default(Guid))
		{
			if (typeof(T).IsClass && (typeof(T) != typeof(string)))
			{
				if (ConfigService.VerboseSdkLog) Debug.Log("Sending game session value of non trivial type " + typeof(T));
			}

			if (!_ownsGameSession)
			{
				throw new InvalidOperationException("Game session value can only be set by server");
				// We'll have to check if _serverGameSession is not null when we'll allow clients to change some game session values
			}

			// Note: we never remove a key
			GetOrCreateGameSessionValue(playerId, key, value).Value = value;

			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Set game session value with key '{0}' to value '{1}' for player {2}</color>", key, value, playerId);
		}

		private GameSessionValue GetOrCreateGameSessionValue<T>(Guid playerId, string key, T defaultValue)
		{
			//Get existing session value for the given key
			var sessionValue = GetGameSessionValue(playerId, key);

			if ((sessionValue == null) && (_serverGameSession != null))
			{
				//Create and add game session value
				sessionValue = new GameSessionValue { CollectionKey = playerId, Key = key, Value = defaultValue, };
				_serverGameSession.GameSessionValues.Add(sessionValue);

				if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=teal>Created new game session value with key '{0}' with default value '{1}' for player {2}</color>", key, defaultValue, playerId);
			}

			return sessionValue;
		}

		private GameSessionValue GetGameSessionValue(Guid playerId, string key)
		{
			if (_serverGameSession != null)
			{
				return _serverGameSession.GameSessionValues.FirstOrDefault(v => v.CollectionKey == playerId && v.Key == key);
			}
			else
			{
				Debug.LogWarningFormat("<color=teal>Game session not yet received</color>");
				return null;
			}
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					try
					{
						var sharedDataController = SharedDataController.Instance;
						if (sharedDataController != null)
						{
							sharedDataController.SharedDataAdded -= Instance_SharedDataAdded;
							if (_ownsGameSession)
							{
								sharedDataController.RemoveSharedData(_serverGameSession);
							}
							SetServerGameSession(null);
						}
					}
					catch
					{
						// Dispose should never throw
					}
					_serverGameSession = null;
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}
