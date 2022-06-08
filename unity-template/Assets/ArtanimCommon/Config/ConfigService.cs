using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using Artanim.Location.Config;
using System.Linq;
using Artanim.Location.Network;
using Artanim.Location.HardwareConfig;
using System.Collections.Generic;

namespace Artanim
{
	public class RigConfig
	{
		public string Name { get; set; }
		public string RigResource { get; set; }
	}

	public class ConfigService
	{
		public const string EXPERIENCE_CONFIG_NAME = "experience_config.xml";
		public const string RIGIDBODIES_CONFIG_NAME = "rigidbodies.xml";

		#region Factory

		private static ConfigService _instance;

		public static ConfigService Instance
		{
			get
			{
				if(_instance == null)
				{
					_instance = new ConfigService();
				}
				return _instance;
			}
		}

		#endregion

		public Config Config
		{
			get
			{
				return Config.Instance;
			}
		}

		private ExperienceSettingsSO _ExperienceSettings;
		public ExperienceSettingsSO ExperienceSettings
		{
			get
			{
				if (!_ExperienceSettings)
					_ExperienceSettings = ExperienceSettingsSO.GetOrCreateSettings();
				return _ExperienceSettings;
			}
		}

        private ExperienceConfig _ExperienceConfig;
        public ExperienceConfig ExperienceConfig
        {
            get
            {
                if (_ExperienceConfig == null)
                {
                    ExperienceConfigPath = Path.Combine(Application.streamingAssetsPath, EXPERIENCE_CONFIG_NAME);
					_ExperienceConfig = XmlUtils.LoadXmlConfig<ExperienceConfig>(ExperienceConfigPath);
                }
                return _ExperienceConfig;
            }
        }

        private RigidBodiesConfig _RigidbodiesConfig;
        public RigidBodiesConfig RigidbodiesConfig
        {
            get
            {
                if (_RigidbodiesConfig == null)
                {
                    _RigidbodiesConfig = XmlUtils.LoadXmlConfig<RigidBodiesConfig>(Path.Combine(Utils.Paths.ConfigDir, RIGIDBODIES_CONFIG_NAME));
                }
                return _RigidbodiesConfig;
            }
        }

        private HardwareConfig _HardwareConfig;
        public HardwareConfig HardwareConfig
        {
            get
            {
                if (_HardwareConfig == null)
                    _HardwareConfig = HardwareConfig.Instance;
                return _HardwareConfig;
            }
        }

		private List<RigConfig> _rigs;
		public IEnumerable<RigConfig> Rigs
        {
			get
            {
				if (_rigs == null)
				{
					_rigs = new List<RigConfig>
					{
						new RigConfig { Name = "Mixamo", RigResource = "Rigs/PFB_Mixamo IK" },
						new RigConfig { Name = "Mixamo AR", RigResource = "Rigs/PFB_Mixamo Armroll IK" },
						new RigConfig { Name = "Simple_Human_IK", RigResource = "Rigs/PFB_Simple_HumanIK" },
						new RigConfig { Name = "Character Creator AR", RigResource = "Rigs/PFB_Character Creator AR" },
						new RigConfig { Name = "None", RigResource = "" },
					};
				}
				return _rigs;
			}
		}

		public RigConfig DesktopRig
        {
			get
            {
				return Rigs.FirstOrDefault(r => string.IsNullOrEmpty(r.RigResource));
			}
        }

		public bool HasExperienceSettings
		{
			get
			{
				//Ugly one... i wish Unity would have something like Resources.ResourceExists
				return _ExperienceSettings != null || ResourceUtils.LoadResources<ExperienceSettingsSO>(ExperienceSettingsSO.EXPERIENCE_SETTINGS_RESOURCE) != null;
			}
		}


		public static readonly bool VerboseSdkLog =
#if UNITY_EDITOR
			Instance.ExperienceSettings.VerboseSDKLogs;
#else
			true;
#endif

		public string ExperienceConfigPath { get; private set; }
    }
}