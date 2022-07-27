using Artanim.Haptics.Internal;
using Artanim.Haptics.Utils;
using Artanim.Location.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics.Visuals
{
	public class HapticsVirtualControls : MonoBehaviour
	{
		[SerializeField]
		Transform _controls = null;

		[SerializeField]
		Transform _floorControls = null;

		[SerializeField]
		Transform _fanControls = null;

		[SerializeField]
		TextMesh _muteToggleText = null;

		[SerializeField]
		TextMesh _harmonicIndexText = null;

		[SerializeField]
		TextMesh _volumeText = null;

		[SerializeField]
		TextMesh _frequencyText = null;

		[SerializeField]
		TextMesh _screenText = null;

		[SerializeField]
		HighlightMesh[] _harmonicSelectionButtons = null;

		[SerializeField]
		TextMesh _fansListText = null;

		[SerializeField]
		TextMesh _fansSpeedText = null;

		[SerializeField]
		TextMesh _fansCountText = null;

		[SerializeField]
		HighlightMesh[] _fansSideSelectionButtons = null;

		HapticAudioPlayer _audioPlayer;
		WaveGenerator _waveGen;
		int _waveConfigIndex;
		HarmonicConfig _currentHarmonic;

		DmxDeviceTarget _fanTarget;

		System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();

		public bool EditingFloor { get { return _floorControls.gameObject.activeSelf; } }

		public static HapticsVirtualControls GetInstanceForPlayer(RuntimePlayer player)
		{
			return (player == null)  ? null : player.AvatarController.GetComponentInChildren<HapticsVirtualControls>();
		}

		public static void CreateInstanceForPlayer(GameObject prefab, RuntimePlayer player)
        {
			if (player != null)
			{
				var virtualControls = GetInstanceForPlayer(player);
				if (virtualControls == null)
                {
					virtualControls = UnityUtils.InstantiatePrefab<HapticsVirtualControls>(prefab);
				}
				virtualControls.AttachToPlayer(player);
			}
		}

		public static void DestroyInstanceForPlayer(RuntimePlayer player)
        {
			if (player != null)
            {
				var virtualControls = GetInstanceForPlayer(player);
				if (virtualControls)
                {
					GameObject.Destroy(virtualControls.gameObject);
				}
			}
        }

		public void ToggleConsole()
		{
			bool active = !_controls.gameObject.activeSelf;
			_controls.gameObject.SetActive(active);

			if (active)
			{
				var names = AudioWavesConfig.ReadWaveNames();
				if (names.Length > 0)
                {
					if (Location.Network.NetworkInterface.Instance.IsServer)
                    {
						_audioPlayer = HapticsController.Instance.CreateAudioPlayer(new HapticAudioPlayerSettings
						{
							WaveName = names[0],
							Volume = 1,
						});
						_waveGen = _audioPlayer.GetComponentInChildren<WaveGenerator>();
					}
					else
                    {
						_waveGen = transform.gameObject.AddComponent<WaveGenerator>();
						_waveGen.WaveName = names[0];
					}
					_waveGen.Muted = true;
					_waveGen.Harmonics = Enumerable.Range(1, _harmonicSelectionButtons.Length).Select(i => new HarmonicConfig { Frequency = 5 * i, Volume = 0.02f }).ToArray();

					// Setup wave
					_waveConfigIndex = 0;
					AssignWaveConfig();
				}
				else
                {
					Debug.LogError("Invalid or empty " + AudioWavesConfig.Filename);
                }

				// Create fan control
				_fanTarget = transform.gameObject.AddComponent<DmxDeviceTarget>();
				_fanTarget.Muted = true;
				_fanTarget.Count = 1;
				_fanTarget.Value = 0.5f;

				UpdateFansSideButtons();
			}
			else
			{
				// Clean up
				_audioPlayer = null; // Destroyed automatically
				if (!Location.Network.NetworkInterface.Instance.IsServer)
                {
					Object.Destroy(_waveGen);
				}
				_waveGen = null;
				Object.Destroy(_fanTarget);
				_fanTarget = null;
			}
		}

		public void ToggleControls()
		{
			bool val = EditingFloor;
			_floorControls.gameObject.SetActive(!val);
			_fanControls.gameObject.SetActive(val);
		}

		public void ToggleMute()
		{
			if (EditingFloor)
			{
				_waveGen.Muted = !_waveGen.Muted;
			}
			else
            {
				_fanTarget.Muted = !_fanTarget.Muted;
			}
		}

		public void DumpSettings()
		{
			if (EditingFloor)
			{
				string name = _waveGen.WaveName != null ? _waveGen.WaveName : string.Empty;
				Debug.Log("Wave settings for " + name + ": " + string.Join(" ; ", _waveGen.Harmonics.Where(w => w.Volume > 0).Select(w => "vol:" + w.Volume + ",freq:" + w.Frequency).ToArray()));
			}
		}

		public void AddVolume(float delta)
		{
			if (_currentHarmonic != null)
            {
				_currentHarmonic.Volume = Mathf.Clamp01(_currentHarmonic.Volume + delta);
				AudioWavesConfig.Save();
			}
		}

		public void MultiplyFrequency(float multiplier)
		{
			if (_currentHarmonic != null)
			{
				_currentHarmonic.Frequency *= multiplier;
				AudioWavesConfig.Save();
			}
		}

		public void SelectHarmonic(int index)
		{
			foreach (var btn in _harmonicSelectionButtons)
			{
				btn.Highlight(false);
			}

			if ((index >= 0) && (index < _waveGen.Harmonics.Length))
            {
				_currentHarmonic = _waveGen.Harmonics[index];
				_harmonicSelectionButtons[index].Highlight(true);
			}
		}

		public void PreviousPreset()
		{
			--_waveConfigIndex;
			AssignWaveConfig();
		}

		public void NextPreset()
		{
			++_waveConfigIndex;
			AssignWaveConfig();
		}

		public void AddSpeed(float delta)
		{
			_fanTarget.Value = Mathf.Clamp01(_fanTarget.Value + delta);
		}

		public void AddFan(int delta)
		{
			_fanTarget.Count = Mathf.Max(0, _fanTarget.Count + delta);
		}

		public void SelectFanSide(int side)
		{
			switch (side)
            {
				case 0: _fanTarget.Side = PodLayout.PodSide.All; break;
				case 1: _fanTarget.Side = PodLayout.PodSide.Left; break;
				case 2: _fanTarget.Side = PodLayout.PodSide.Right; break;
				case 3: _fanTarget.Side = PodLayout.PodSide.Back; break;
				case 4: _fanTarget.Side = PodLayout.PodSide.Front; break;
				default: throw new System.ArgumentException("side");
            }
			UpdateFansSideButtons();
		}

		void UpdateFansSideButtons()
        {
			foreach (var btn in _fansSideSelectionButtons)
			{
				btn.Highlight(false);
			}
			int btnIndex = 0;
			switch (_fanTarget.Side)
            {
				case PodLayout.PodSide.Left: btnIndex = 1; break;
				case PodLayout.PodSide.Right: btnIndex = 2; break;
				case PodLayout.PodSide.Back: btnIndex = 3; break;
				case PodLayout.PodSide.Front: btnIndex = 4; break;
            }
			_fansSideSelectionButtons[btnIndex].Highlight(true);
		}

		void AttachToPlayer(RuntimePlayer player)
		{
			// Attach to wrist
			var hand = player.AvatarController.GetAvatarBodyPart(EAvatarBodyPart.LeftHand);
			if (hand == null)
			{
				Debug.LogError("Couldn't find in avatar hand to instantiate haptics virtual controls");
			}
			else
			{
				transform.SetParent(hand.transform, worldPositionStays: false);

				// Assign unique object id to this instance
				foreach (var trigger in GetComponentsInChildren<AvatarTrigger>())
				{
					if (!trigger.ObjectId.Contains("player="))
					{
						trigger.ObjectId += "-player=" + player.Player.ComponentId;
					}
				}

				// Attach controls to player
				//var playerHips = player.AvatarController.GetComponent<Tracking.IKListener>().GetBoneTransform(Tracking.ERigBones.Hips);
				_controls.SetParent(player.AvatarController.GetAvatarRoot(), worldPositionStays: false);
			}
		}

		void AssignWaveConfig()
		{
			if (AudioWavesConfig.FileExists && AudioWavesConfig.Instance.HasGenerators)
			{
				if (_waveConfigIndex < 0) _waveConfigIndex = AudioWavesConfig.Instance.Generators.Length - 1;
				if (_waveConfigIndex >= AudioWavesConfig.Instance.Generators.Length) _waveConfigIndex = 0;
				if (_waveConfigIndex >= 0)
				{
					_waveGen.WaveName = AudioWavesConfig.Instance.Generators[_waveConfigIndex].Name;
					_waveGen.Harmonics = AudioWavesConfig.Instance.Generators[_waveConfigIndex].Harmonics;
				}
			}

			SelectHarmonic(0);
		}

		void UpdateDisplay()
        {
			_muteToggleText.text = (EditingFloor ? _waveGen.Muted : _fanTarget.Muted) ? "Muted" : "Live";

			if (EditingFloor)
			{
				_harmonicIndexText.text = string.Format("Harmonic #{0}", 1 + System.Array.IndexOf(_waveGen.Harmonics, _currentHarmonic));
				_volumeText.text = string.Format("Vol: {0:G3}%", 100 * _currentHarmonic.Volume);
				_frequencyText.text = string.Format("Freq: {0:G3}", _currentHarmonic.Frequency);

				_stringBuilder.Length = 0;
				_stringBuilder.Append(_waveGen.WaveName);
				_stringBuilder.Append("\n");
				foreach (var wave in _waveGen.Harmonics)
				{
					_stringBuilder.Append("Freq: ");
					_stringBuilder.Append(wave.Frequency.ToString("G3"));
					_stringBuilder.Append(" - Vol: ");
					_stringBuilder.Append((100 * wave.Volume).ToString("G3"));
					_stringBuilder.Append("%\n");
				}
				_screenText.text = _stringBuilder.ToString();
			}
			else
            {
				_fansListText.text = "Fans:";
				_fansSpeedText.text = string.Format("Speed: {0:G3}", 100 * _fanTarget.Value);
				_fansCountText.text = string.Format("Count: {0:G3}", _fanTarget.Count);
			}
		}

		#region Unity messages

		// Use this for initialization
		void Start()
		{
			// Show floor controls
			_floorControls.gameObject.SetActive(true);
			_fanControls.gameObject.SetActive(false);

			// Start hidden
			_controls.gameObject.SetActive(false);
		}

		void OnDestroy()
        {
			GameObject.Destroy(_controls.gameObject);
		}

		// Update is called once per frame
		void Update()
		{
			if (_controls.gameObject.activeSelf)
            {
				UpdateDisplay();
				if (_audioPlayer != null)
                {
					_audioPlayer.KeepAlive();
				}
			}
		}

		#endregion
	}
}
