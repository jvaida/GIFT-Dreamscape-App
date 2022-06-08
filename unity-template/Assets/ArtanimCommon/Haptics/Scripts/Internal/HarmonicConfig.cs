using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Artanim.Haptics.Internal
{
    [XmlRoot(ElementName = "AudioWaves")]
    public class AudioWavesConfig
    {
        public WaveGeneratorConfig[] Generators { get; set; }

        public bool HasGenerators { get { return (Generators != null) && (Generators.Length > 0); } }

        #region File load/save

        public const string Filename = "audio_waves_config.xml";

        static string Pathname { get { return Path.Combine(UnityEngine.Application.streamingAssetsPath, Path.Combine("Haptics", Filename)); } }

        public static bool FileExists { get { return File.Exists(Pathname); } }

        static AudioWavesConfig _instance;
        public static AudioWavesConfig Instance
        {
            get
            {
                Load();
                return _instance;
            }
        }

        public static bool HasInstance
        {
            get
            {
                return _instance != null;
            }
        }

        public static void Load()
        {
            if (_instance == null)
            {
                _instance = XmlUtils.LoadXmlConfig<AudioWavesConfig>(Pathname);
            }
        }

        public static void Save()
        {
            if (_instance != null)
            {
                UnityEngine.Debug.Log("Saving " + Pathname);
                XmlUtils.SaveXmlConfig(Pathname, _instance);
            }
        }

        public static void AddWave(string name, HarmonicConfig[] harmonics)
        {
            if (_instance == null)
            {
                try
                {
                    AudioWavesConfig.Load();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogErrorFormat("Failed to load {0}, got error: {1}", Pathname, e.Message);
                }
                if (_instance == null)
                {
                    _instance = new AudioWavesConfig();
                }
            }

            _instance.Generators = Artanim.Utils.ArrayUtils.Resize(_instance.Generators, _instance.Generators == null ? 1 : _instance.Generators.Length + 1);
            _instance.Generators[_instance.Generators.Length - 1] = new WaveGeneratorConfig
            {
                Name = name,
                Harmonics = harmonics,
            };
        }

        public static string[] ReadWaveNames()
        {
            if (AudioWavesConfig.FileExists)
            {
                var config = XmlUtils.LoadXmlConfig<AudioWavesConfig>(AudioWavesConfig.Pathname);
                if ((config != null) && (config.HasGenerators))
                {
                    return config.Generators.Select(g => g.Name).ToArray();
                }
            }
            else
            {
                UnityEngine.Debug.LogError("WaveGenerator: Empty or missing config file");
            }
            return new string[0];
        }

        #endregion
    }

    [XmlType(TypeName = "Generator")]
    public class WaveGeneratorConfig
    {
        [XmlAttribute]
        public string Name { get; set; }

        public HarmonicConfig[] Harmonics { get; set; }
    }

    [System.Serializable]
    [XmlType(TypeName = "Harmonic")]
    public class HarmonicConfig
    {
        [XmlAttribute]
        public float Frequency; // Field instead of property for Unity serialization
        [XmlAttribute]
        public float Volume; // Field instead of property for Unity serialization
    }
}
