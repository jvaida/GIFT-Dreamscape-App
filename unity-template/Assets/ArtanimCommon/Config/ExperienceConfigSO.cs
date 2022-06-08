using Artanim.Algebra;
using Artanim.Location.Config;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artanim
{
	public class ExperienceConfigSO : ScriptableObject
	{
		#region Factory

		public static ExperienceConfigSO GetOrCreateConfig()
		{
			var experienceConfigSO = CreateInstance<ExperienceConfigSO>();
			experienceConfigSO.ExperienceConfigPath = Path.Combine(Application.streamingAssetsPath, ConfigService.EXPERIENCE_CONFIG_NAME);
			if (File.Exists(experienceConfigSO.ExperienceConfigPath))
			{
				//Load from XML
				using (var reader = XmlReader.Create(experienceConfigSO.ExperienceConfigPath))
				{
					var serializer = new XmlSerializer(typeof(ExperienceConfig));
					var config = serializer.Deserialize(reader) as ExperienceConfig;

					experienceConfigSO.LoadConfig(config);
				}
			}
			else
			{
#if UNITY_EDITOR
				//Init a new one with default values
				experienceConfigSO.ExperienceName = "";
				experienceConfigSO.ServerFPS = 90;
				experienceConfigSO.TSMuteMic = false;

				experienceConfigSO.StartScenes = new List<SerializableScene>();
				experienceConfigSO.Avatars = new List<SerializableAvatar>();
                experienceConfigSO.TrackedProps = new List<SerializableTrackedProp>();
				experienceConfigSO.ExperienceProperties = new List<SerializableExperienceProperty>();

				experienceConfigSO.UserMessage = string.Format("Created new experience config.");

                experienceConfigSO.SaveConfig();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

#else
				Debug.LogWarningFormat("No experience config found. Create a new ExperienceConfig XML in the StreamingAssets folder of your project called '%0'", ConfigService.EXPERIENCE_CONFIG_NAME);
#endif

			}

			return experienceConfigSO;
		}

		#endregion

		[Tooltip("Name of the experience.")]
		public string ExperienceName;

		[Tooltip("Defines if players can join the experience once a session has been started.")]
		public bool AllowAddPlayerWhileRunning;

		[Tooltip("Target frames per second for the server component.")]
		public int ServerFPS;

        [Tooltip("Limit client FPS to 30 when HMD is not on the players head.")]
        public bool LimitFPSOnHMDOffHead;

        [Tooltip("Enabled seated experience. This ")]
        public bool SeatedExperience;


        [Header("Teamspeak / Voice Communication")]

        [Tooltip("Select Voice Chat technology to use.")]
        public ExperienceConfig.EVoiceChat VoiceChat;

        [Tooltip("Mutes the Teamspeak communication of the experience.")]
		public bool TSMuteMic;

        [Tooltip("Disable the Teamspeak audio output of the experience but leaves the player audio streams for lipsync.")]
        public bool TSMuteAudio;

        [Tooltip("Disable the Teamspeak hostess audio.")]
        public bool TSMuteHostessAudio;


        [Header("Lipsync")]
        
		[Tooltip("Lipsync mode used: MicOnly=Local mic is used and streamed to other, Teamspeak=Teamspeak based lipsync")]
        public ExperienceConfig.ELipsyncMode LipsyncMode;

		[Tooltip("Audio source level applied before processing lipsync. 0: mute, 1: Value received by the audio source (TS and mic)")]
		public float LipsyncAudioGain;

		[Tooltip("Time in seconds between two lipsync updates synced to the network. 0=All updates")]
        public float LipsyncSyncUpdatePeriod;


		[Header("Haptics")]

		[Tooltip("Enable Native Haptics")]
        public bool EnableNativeHaptics;


		public string ExperienceConfigPath;
		public string UserMessage;

		public List<SerializableScene> StartScenes;
		public List<SerializableAvatar> Avatars;
		public List<SerializableExperienceProperty> ExperienceProperties;
		public List<SerializableTrackedProp> TrackedProps;

		private ExperienceConfig ExperienceConfig;

		public void ReloadConfig()
		{
			LoadConfig(ExperienceConfig);
		}

		public void SaveConfig()
		{
			SaveConfig(ExperienceConfigPath);
		}

		public void LoadConfig(ExperienceConfig config)
		{
			ExperienceConfig = config;

			ExperienceName = config.ExperienceName;
			AllowAddPlayerWhileRunning = config.AllowAddPlayerWhileRunning;
			ServerFPS = config.ServerFPS;
            LimitFPSOnHMDOffHead = config.LimitFPSOnHMDOffHead;
            VoiceChat = config.VoiceChat;
            TSMuteMic = config.TSMuteMic;
            TSMuteAudio = config.TSMuteAudio;
            TSMuteHostessAudio = config.TSMuteHostessAudio;
            LipsyncMode = config.LipsyncMode;
			LipsyncAudioGain = config.LipsyncAudioGain;
            LipsyncSyncUpdatePeriod = config.LipsyncSyncUpdatePeriod;
			SeatedExperience = config.SeatedExperience;
			EnableNativeHaptics = config.EnableNativeHaptics;

			//Read scenes
			StartScenes = new List<SerializableScene>();
			foreach (var scene in config.StartScenes)
				StartScenes.Add(new SerializableScene(scene));

			//Avatars
			Avatars = new List<SerializableAvatar>();
			foreach (var avatar in config.Avatars)
				Avatars.Add(new SerializableAvatar(avatar));

            //Tracked Props
            TrackedProps = new List<SerializableTrackedProp>();
            foreach (var trackedProp in config.TrackedProps)
                TrackedProps.Add(new SerializableTrackedProp(trackedProp));

			//Read experience properties
			ExperienceProperties = new List<SerializableExperienceProperty>();
			foreach (var property in config.ExperienceProperties)
				ExperienceProperties.Add(new SerializableExperienceProperty(property));

			UserMessage = string.Format("Loaded experience config from: {0}", ExperienceConfigPath);
		}


		private void SaveConfig(string path)
		{
#if UNITY_EDITOR
			XmlUtils.SaveXmlConfig<ExperienceConfig>(path, CreateConfig());

			UserMessage = string.Format("Saved experience config to: {0}", path);
#endif
		}

#if UNITY_EDITOR

		private ExperienceConfig CreateConfig()
		{
			var config = new ExperienceConfig();

			config.ExperienceName = ExperienceName;
			config.AllowAddPlayerWhileRunning = AllowAddPlayerWhileRunning;
			config.ServerFPS = ServerFPS;
            config.LimitFPSOnHMDOffHead = LimitFPSOnHMDOffHead;
            config.VoiceChat = VoiceChat;
			config.TSMuteMic = TSMuteMic;
            config.TSMuteAudio = TSMuteAudio;
            config.TSMuteHostessAudio = TSMuteHostessAudio;
            config.LipsyncMode = LipsyncMode;
			config.LipsyncAudioGain = LipsyncAudioGain;
            config.LipsyncSyncUpdatePeriod = LipsyncSyncUpdatePeriod;
			config.SeatedExperience = SeatedExperience;
			config.EnableNativeHaptics = EnableNativeHaptics;

			//Scenes
			config.StartScenes = new List<Scene>();
			foreach (var scene in StartScenes)
				config.StartScenes.Add(scene.ToScene());

			//Avatars
			config.Avatars = new List<Location.Config.Avatar>();
			foreach (var avatar in Avatars)
				config.Avatars.Add(avatar.ToAvatar());

            config.TrackedProps = new List<TrackedProp>();
            foreach (var trackedProp in TrackedProps)
                config.TrackedProps.Add(trackedProp.ToTrackedProp());

			//Experience properties
			config.ExperienceProperties = new List<ExperienceProperty>();
			foreach (var property in ExperienceProperties)
				config.ExperienceProperties.Add(property.ToExperienceProperty());

			return config;
		}

#endif

		#region Helper classes

		[Serializable]
		public class SerializableScene
		{
			public string SceneName;

			public SerializableScene(Scene scene)
			{
				SceneName = scene.SceneName;
			}

#if UNITY_EDITOR

			public Scene ToScene()
			{
				return new Scene
				{
					SceneName = SceneName,
				};
			}

#endif
		}

		[Serializable]
		public class SerializableExperienceProperty
		{
			public string Key;
			public string Value;

			public SerializableExperienceProperty(ExperienceProperty experienceProperty)
			{
				Key = experienceProperty.Key;
				Value = experienceProperty.Value;
			}


#if UNITY_EDITOR
			public ExperienceProperty ToExperienceProperty()
			{
				var property = new ExperienceProperty()
				{
					Key = Key,
					Value = Value,
				};

				return property;
			}
#endif
		}

		[Serializable]
		public class SerializableAvatar
		{
			public string Name;
			public string AvatarResource;
			public string RigName;

			public SerializableAvatar(Artanim.Location.Config.Avatar avatar)
			{
				Name = avatar.Name;
				AvatarResource = avatar.AvatarResource;
				RigName = avatar.RigName;
			}

#if UNITY_EDITOR
			public Artanim.Location.Config.Avatar ToAvatar()
			{
				var avatar = new Artanim.Location.Config.Avatar()
				{
					Name = Name,
					AvatarResource = AvatarResource,
					RigName = RigName,
				};
				return avatar;
			}
#endif
		}

        [Serializable]
        public class SerializableTrackedProp
        {
            public string Name;
            public string Group;
            public bool Transient;

            public SerializableStartPosition StartPosition = new SerializableStartPosition();
            public SerializableStartDirection StartDirection1 = new SerializableStartDirection();
            public SerializableStartDirection StartDirection2 = new SerializableStartDirection();

            public SerializableTrackedProp(TrackedProp trackedProp)
            {
                Name = trackedProp.Name;
                Group = trackedProp.Group;
                Transient = trackedProp.Transient;

                if(trackedProp.StartPosition != null)
                    StartPosition = new SerializableStartPosition(trackedProp.StartPosition);

                if (trackedProp.StartDirection1 != null)
                    StartDirection1 = new SerializableStartDirection(trackedProp.StartDirection1);

                if (trackedProp.StartDirection2 != null)
                    StartDirection2 = new SerializableStartDirection(trackedProp.StartDirection2);
            }

#if UNITY_EDITOR
            public TrackedProp ToTrackedProp()
            {
                var trackedProp = new TrackedProp
                {
                    Name = Name,
                    Group = Group,
                    Transient = Transient,
                    StartPosition = StartPosition.ToStartPosition(),
                    StartDirection1 = StartDirection1.ToStartDirection(),
                    StartDirection2 = StartDirection2.ToStartDirection(),
                };

                return trackedProp;
            }
#endif
        }

        [Serializable]
        public class SerializableStartPosition
        {
            public float Tolerance = 0f;
            public Vector3 Vector = Vector3.zero;

            public SerializableStartPosition() { }

            public SerializableStartPosition(StartPosition startPosition)
            {
                if(startPosition != null)
                {
                    Tolerance = startPosition.Tolerance;
                    Vector = startPosition.Vector.ToUnity();
                }
            }

#if UNITY_EDITOR
            public StartPosition ToStartPosition()
            {
                var startPosition = new StartPosition
                {
                    Tolerance = Tolerance,
                    Vector = Vector.ToVect3f(),
                };

                return startPosition;
            }
#endif
        }

        [Serializable]
        public class SerializableStartDirection : SerializableStartPosition
        {
            public StartDirection.AxisName Axis;

            public SerializableStartDirection() { }

            public SerializableStartDirection(StartDirection startDirection) : base(startDirection)
            {
                Axis = startDirection.Axis;
            }

#if UNITY_EDITOR
            public StartDirection ToStartDirection()
            {
                var startDirection = new StartDirection
                {
                    Axis = Axis,
                    Tolerance = Tolerance,
                    Vector = Vector.ToVect3f(),
                };

                return startDirection;
            }
#endif
        }


        #endregion
    }

}
