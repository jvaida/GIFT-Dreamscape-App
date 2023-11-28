using Artanim.Algebra;
using Artanim.Location.AudioHaptics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics
{
	public class AudioDevicesController : MonoBehaviour
	{
		struct PlayerInfo
        {
			public Player HapticPlayer;
			public RuntimePlayer RuntimePlayer;
		}

		GameController _gameController;
		AudioHapticsController _ctrl;
		float _unmuteGain;

		List<Sound> _sounds = new List<Sound>();
		List<PlayerInfo> _playersInfo = new List<PlayerInfo>();

		/// <summary>
		/// Whether or not to send sounds to the haptic devices
		/// </summary>
		public bool IsMuted
        {
			get
			{
				return (_ctrl != null) && (_ctrl.Gain <= 0);
			}
			set
			{
				if (_ctrl != null)
                {
					if (_unmuteGain == 0)
					{
						_unmuteGain = _ctrl.Gain;
						if (_unmuteGain == 0)
						{
							_unmuteGain = 1;
						}
					}
					if (value)
					{
						_ctrl.Gain = 0;
					}
					else
					{
						_ctrl.Gain = _unmuteGain;
					}
				}
			}
        }

		/// <summary>
		/// All available haptic elements
		/// </summary>
		public Element[] AllElements
        {
			get
            {
				return _ctrl == null ? null : _ctrl.Elements.ToArray();
			}
        }

		/// <summary>
		/// All available haptic players
		/// </summary>
		public Player[] AllPlayers
        {
			get
            {
				return _playersInfo.Select(i => i.HapticPlayer).ToArray();
			}
        }

		/// <summary>
		/// Creates a haptic audio sound object that will play for all players everywhere
		/// </summary>
		/// <param name="name">Name for the sound</param>
		/// <param name="ignoreMuteWhenUnoccupiedFlag">Set this flag to true to have the sound play regardless of a collision with a player</param>
		/// <returns>The haptic sound</returns>
		public Sound CreateGlobalSound(string name, bool ignoreMuteWhenUnoccupiedFlag = false)
		{
			var sound = _ctrl.CreateSound(name, ignoreMuteWhenUnoccupiedFlag);
			_sounds.Add(sound);

			sound.SetGlobal();

			if (ConfigService.VerboseSdkLog) Debug.LogFormat(
				"<color=magenta>AudioDevicesController: initializing global sound {0}</color>", name);

			sound.Initialize();

			return sound;
		}

		/// <summary>
		/// Creates a haptic audio sound object for specific elements or players.
		/// </summary>
		/// <param name="name">Name for the sound</param>
		/// <param name="elements">Elements on which the sound will play (all of them in null)</param>
		/// <param name="bounds">Restrict playing the sound on elements which volume intersect with the bounds</param>
		/// <param name="players">Players for which the sound will play (all of them in null) => required to control volume by player</param>
		/// <param name="ignoreMuteWhenUnoccupiedFlag">Set this flag to true to have the sound play regardless of a collision with a player</param>
		/// <returns>The haptic sound</returns>
		public Sound CreateSound(string name, IEnumerable<Element> elements = null, Bounds? bounds = default(Bounds?), IEnumerable<Player> players = null, bool ignoreMuteWhenUnoccupiedFlag = false)
		{
			// We can't have both elements and players at the moment
			if (((elements != null) || bounds.HasValue) && (players != null))
            {
				throw new System.ArgumentException("Can't specify both elements (or bounds) and players");
            }

			// Build elements list from bounds
			if (bounds.HasValue)
            {
				var intersectElems = FindElementsIntersect(bounds.Value);
				if (intersectElems.Length == 0)
                {
					Debug.LogWarning("AudioDevicesController: no elements found in bounds " + bounds.Value);
				}
				elements = elements == null ? intersectElems : elements.Intersect(intersectElems);
			}

			// Create sound
			var sound = _ctrl.CreateSound(name, ignoreMuteWhenUnoccupiedFlag);
			_sounds.Add(sound);

			// Sanity check for players
			if (_playersInfo.Count != _gameController.RuntimePlayers.Count)
			{
				Debug.LogErrorFormat("AudioDevicesController: haptic players count = {0} mismatch with session = {1} !", _playersInfo.Count, _gameController.RuntimePlayers.Count);
			}

			// Add players or elements to sound (can't have both)
			var str = new System.Text.StringBuilder();
			if (players != null)
            {
				foreach (var player in players)
                {
					var runtimePlayer = _playersInfo.FirstOrDefault(i => i.HapticPlayer == player).RuntimePlayer;
					if (runtimePlayer == null)
                    {
						Debug.LogErrorFormat("AudioDevicesController: invalid haptic player {0}", player.Name);
					}
					else
                    {
						sound.AddPlayer(player);
						if (str.Length > 0) str.Append(", ");
						str.Append(runtimePlayer.Player.ComponentId);
					}
				}
				str.Insert(0, " with players: ");
			}
			else if (elements != null)
            {
				foreach (var e in elements)
				{
					sound.AddElement(e);
					if (str.Length > 0) str.Append(", ");
					str.Append(e.Name);
				}
				str.Insert(0, " with elements: ");
			}

			// Logging
			if (ConfigService.VerboseSdkLog) Debug.LogFormat(
				"<color=magenta>AudioDevicesController: initializing sound {0}{1}</color>", name, str);

			// Initialize
			sound.Initialize();
			return sound;
		}

		/// <summary>
		/// Returns the haptic player for the given RuntimePlayer
		/// </summary>
		public Player GetHapticPlayer(RuntimePlayer player)
        {
			return _playersInfo.FirstOrDefault(i => i.RuntimePlayer == player).HapticPlayer;
        }

		/// <summary>
		/// Returns the element closest element for the given position
		/// </summary>
		public Element FindClosestElement(Vector2 pos)
		{
			var pos3d = new Vector3(pos.x, 0, pos.y);
			return _ctrl.Elements.FirstOrDefault(e => new Bounds(0.5f * (e.Min.ToUnity() + e.Max.ToUnity()), e.Max.ToUnity() - e.Min.ToUnity()).Contains(pos3d));
		}

		/// <summary>
		/// Returns the elements intersecting with the given bounds
		/// </summary>
		public Element[] FindElementsIntersect(Bounds bounds)
        {
			return _ctrl.Elements.Where(e => new Bounds(0.5f * (e.Min.ToUnity() + e.Max.ToUnity()), e.Max.ToUnity() - e.Min.ToUnity()).Intersects(bounds)).ToArray();
		}

		void Setup()
        {
			string error = null;
			_ctrl = AudioHapticsController.Instance;

			if (_ctrl == null)
			{
				error = "AudioHapticsController: Failed to retrieve singleton";
			}
			else
			{
				string hapticsConfigFile = "audio_haptics_config.xml";
				string pathname = Path.Combine(Application.streamingAssetsPath, Path.Combine("Haptics", hapticsConfigFile));
#if UNITY_EDITOR
				var devPathname = Path.Combine(Artanim.Utils.Paths.UserDevDir, hapticsConfigFile);
				if (File.Exists(devPathname))
                {
					pathname = devPathname;
					if (ConfigService.VerboseSdkLog) Debug.LogFormat(
						"<color=magenta>AudioDevicesController: loading dev config {0}</color>", pathname);
				}
#endif
				bool mute = CommandLineUtils.GetValue("MuteHaptics", true);
				if (_ctrl.LoadConfig(pathname))
				{
					if (mute)
					{
						_ctrl.IsMuted = mute;
					}
				}
				else
				{
					error = "error loading config";
				}

#if UNITY_EDITOR
				// Check if audio devices were found
				if ((_ctrl.AudioDevices.Count() == 0) && (!mute))
                {
					error = "no matching audio device";
				}
#endif
			}

			_gameController = GameController.Instance;
			if ((error == null) && (_gameController == null))
			{
				error = "can't find GameController instance";
			}

			if (error != null)
			{
				Debug.LogError("AudioHapticsController: " + error + ", disabling AudioHapticsController");
				enabled = false;
			}
			else
            {
                _gameController.OnJoinedSession += GameController_OnJoinedSession;
                _gameController.OnSessionPlayerJoined += GameController_OnSessionPlayerJoined;
                _gameController.OnLeftSession += GameController_OnLeftSession;
			}
		}

        void CleanUp()
		{
			if (_gameController != null)
            {
				_gameController.OnJoinedSession -= GameController_OnJoinedSession;
				_gameController.OnSessionPlayerJoined -= GameController_OnSessionPlayerJoined;
				_gameController.OnLeftSession -= GameController_OnLeftSession;
			}

			CleanUpSoundsAndPlayers();
			_ctrl = null;
			_gameController = null;
		}

		void CleanUpSoundsAndPlayers()
        {
			if (ConfigService.VerboseSdkLog) Debug.Log("<color=magenta>AudioDevicesController: removing all sounds and players</color>");

			foreach (var sound in _sounds)
			{
				sound.Dispose();
			}
			_sounds.Clear();

			foreach (var player in _playersInfo)
			{
				player.HapticPlayer.Dispose();
			}
			_playersInfo.Clear();
		}

		void UpdatePlayerList()
        {
			// Remove players that have gone away
			for (int i = _playersInfo.Count - 1; i >= 0; --i)
            {
				var player = _playersInfo[i].RuntimePlayer;
				if (!_gameController.RuntimePlayers.Contains(player))
                {
					if (ConfigService.VerboseSdkLog)
						Debug.LogFormat("<color=magenta>AudioDevicesController: removing player {0}</color>", player.Player.ComponentId);

					_playersInfo[i].HapticPlayer.Dispose();
					_playersInfo.RemoveAt(i);
                }
            }

			// Add new players
			foreach (var player in _gameController.RuntimePlayers)
            {
				if (_playersInfo.All(p => p.RuntimePlayer != player))
                {
					var playerId = player.Player.ComponentId;
					if (ConfigService.VerboseSdkLog)
						Debug.LogFormat("<color=magenta>AudioDevicesController: adding player {0}</color>", playerId);

					_playersInfo.Add(new PlayerInfo { HapticPlayer = _ctrl.CreatePlayer(playerId.ToString()), RuntimePlayer = player });
				}
			}
        }

		void GameController_OnSessionPlayerJoined(Location.Data.Session session, System.Guid playerId)
		{
			UpdatePlayerList();
		}

		void GameController_OnJoinedSession(Location.Data.Session session, System.Guid playerId)
		{
			UpdatePlayerList();
		}

		void GameController_OnLeftSession()
		{
			CleanUpSoundsAndPlayers();
		}

		#region Unity messages

		void Awake()
        {
			// Disable self if required
			enabled = ConfigService.Instance.ExperienceConfig.EnableNativeHaptics;
        }

        void OnEnable()
		{
			Setup();
		}

		void OnDisable()
		{
			CleanUp();
		}

		void LateUpdate()
		{
			if (_ctrl != null)
            {
				foreach (var playerInfo in _playersInfo)
				{
					Vector3 left, right;
					if (playerInfo.RuntimePlayer.IsWheelchair)
                    {
						// Get left and right wheel position
						var root = playerInfo.RuntimePlayer.AvatarController.GetAvatarRoot();
						left = root.localPosition + root.localRotation * (0.4f * Vector3.left);
						right = root.localPosition + root.localRotation * (0.4f * Vector3.right);
					}
					else
                    {
						// Get left and right feet
						var avatarController = playerInfo.RuntimePlayer.AvatarController;
						left = avatarController.GetBodyPartTrackedPosition(Location.Messages.EAvatarBodyPart.LeftFoot);
						right = avatarController.GetBodyPartTrackedPosition(Location.Messages.EAvatarBodyPart.RightFoot);
					}
					playerInfo.HapticPlayer.PushPosition(left.ToVect3f());
					playerInfo.HapticPlayer.PushPosition(right.ToVect3f());
				}

				_ctrl.Update();
			}
		}

        #endregion
    }
}
