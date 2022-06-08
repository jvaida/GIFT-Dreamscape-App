using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Artanim.Haptics.Internal
{
	public class WaveGenerator : MonoBehaviour
	{
		public string WaveName;

		public HarmonicConfig[] Harmonics;

		public bool Muted = false;

		const int _sampleRate = 44100;
		const float _sampleFreq = 1f / _sampleRate;
		const int _dataPushFreq = 10;
		const int _bufferSize = _sampleRate / _dataPushFreq;

		HapticAudioPlayer.SendSoundBufferHandler _pushSoundData;

		public static WaveGenerator AddToGameObject(GameObject gameObject, string waveName, HapticAudioPlayer.SendSoundBufferHandler pushSoundData)
		{
			if (gameObject == null)
			{
				throw new System.ArgumentNullException("gameObject");
			}
			if (waveName == null)
			{
				throw new System.ArgumentNullException("waveName");
			}
			if (string.IsNullOrEmpty(waveName))
			{
				throw new System.ArgumentException("waveName");
			}
			if (gameObject.GetComponent<WaveGenerator>() != null)
			{
				throw new System.InvalidOperationException("WaveGenerator already present on " + gameObject.name);
			}

			if (ConfigService.VerboseSdkLog) Debug.LogFormat("<color=magenta>WaveGenerator: initializing for wave {0}</color>", waveName);

			var waveGen = gameObject.AddComponent<WaveGenerator>();
			waveGen.WaveName = waveName;
			waveGen._pushSoundData = pushSoundData;
			return waveGen;
		}

		[ContextMenu("Load XML config")]
		void LoadConfig()
		{
			if (!string.IsNullOrEmpty(WaveName))
            {
				if ((AudioWavesConfig.HasInstance || AudioWavesConfig.FileExists) && AudioWavesConfig.Instance.HasGenerators)
                {
					var waveGenConfig = AudioWavesConfig.Instance.Generators.FirstOrDefault(w => w.Name == WaveName);
					if (waveGenConfig != null)
					{
						Harmonics = waveGenConfig.Harmonics; // Keep a duplicate of the original array
						Debug.LogFormat("<color=magenta>WaveGenerator: loaded wave {0}</color>", WaveName);
					}
					else
					{
						Debug.LogErrorFormat("WaveGenerator: Couldn't find wave `{0}` in config file", WaveName);
					}
				}
				else
				{
					Debug.LogError("WaveGenerator: Empty or missing config file");
				}
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Save XML config")]
		void SaveConfig()
		{
			if ((!string.IsNullOrEmpty(WaveName)) && (Harmonics != null) && (Harmonics.Length > 0))
			{
				AudioWavesConfig config = AudioWavesConfig.FileExists ? AudioWavesConfig.Instance : new AudioWavesConfig();
				WaveGeneratorConfig waveGenConfig = null;
				if (config.HasGenerators)
                {
					waveGenConfig = config.Generators.FirstOrDefault(w => w.Name == WaveName);
				}
				if (waveGenConfig == null)
                {
					waveGenConfig = new WaveGeneratorConfig { Name = WaveName };
					if (config.Generators == null)
                    {
						config.Generators = new[] { waveGenConfig };
					}
					else
                    {
						config.Generators = config.Generators.Concat(new[] { waveGenConfig }).ToArray();
					}
				}
				waveGenConfig.Harmonics = Harmonics/*.Where(w => (w.Frequency > 0) && (w.Volume > 0))*/.OrderBy(w => w.Frequency).ToArray();
				AudioWavesConfig.Save();
			}
		}
#endif

		IEnumerator GenerateSound()
		{
			float[] data = new float[_bufferSize];
			float sampleTime = 0;

			while (true)
			{
				if ((!Muted) && (_pushSoundData != null) && (Harmonics != null))
				{
					for (int i = 0; i < data.Length; ++i)
					{
						float sample = 0;
						foreach (var harmonic in Harmonics)
						{
							if ((harmonic != null) && (harmonic.Volume > 0))
							{
								sample += harmonic.Volume * (float)Mathf.Sin(2 * Mathf.PI * harmonic.Frequency * sampleTime);
							}
						}
						data[i] = sample;
						sampleTime = (sampleTime + _sampleFreq) % 1f;
					}
					_pushSoundData(data, _sampleRate);
				}

				yield return new WaitForSecondsRealtime(1f / _dataPushFreq);
			}
		}

		void Awake()
        {
			enabled = HapticsController.IsAudioEnabled;
		}

		IEnumerator Start()
		{
			LoadConfig();
			return GenerateSound();
		}
	}
}