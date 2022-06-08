using Artanim.Algebra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Artanim.Haptics.PodLayout
{
	[Flags]
	public enum PodSide
	{
		None,
		Left = 1, // PositiveX
		Right = 2, // NegativeX
		Front = 4, // PositiveZ
		Back = 8, // NegativeZ
		All = Left | Right | Front | Back,
	}

	[XmlRoot(ElementName = "PodLayout")]
	public class PodLayoutConfig
	{
		public RailingConfig Railing { get; set; }
		public FloorConfig Floor { get; set; }
		public ComponentConfig[] Components { get; set; }

        #region File load/save

        public static string Filename { get { return "Haptics\\pod_layout.xml"; } }

		public static string Pathname { get { return Path.Combine(Application.streamingAssetsPath, Filename); } }

		static PodLayoutConfig _instance;
		public static PodLayoutConfig Instance
        {
			get
            {
				if (_instance == null)
                {
					_instance = XmlUtils.LoadXmlConfig<PodLayoutConfig>(Pathname);
				}
				return _instance;
			}
        }

#if UNITY_EDITOR
		[UnityEditor.MenuItem("Artanim/Instantiate Pod Layout", priority = 100)]
		public static GameObject Instantiate()
		{
			var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(@"Assets\ArtanimCommon\Haptics\Prefabs\PFB_Pod Layout.prefab", typeof(GameObject)) as GameObject;
			return UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		}

		public static void Reload()
        {
			_instance = XmlUtils.LoadXmlConfig<PodLayoutConfig>(Pathname);
		}

		public static void Save()
        {
			if (_instance != null)
            {
				XmlUtils.SaveXmlConfig(Pathname, _instance);
			}
		}
#endif
        #endregion

        #region Static methods

        public static string CreateFloorTileName(string nameTemplate, Vector2Int pos)
		{
			if ((pos.x < 0) || (pos.x > 9) || (pos.y < 0) || (pos.y > 9))
            {
				throw new ArgumentException("pos");
            }
			return nameTemplate.Replace("{X}", (pos.x + 1).ToString()).Replace("{Z}", (pos.y + 1).ToString());
		}

		public static Vector2Int GetFloorTileIndex(string nameTemplate, string tileName)
		{
			int x = tileName[nameTemplate.IndexOf("{X}")] - '0';
			int z = tileName[nameTemplate.Replace("{X}", "0").IndexOf("{Z}")] - '0';
			return new Vector2Int(x - 1, z - 1);
		}

        #endregion
    }

    public class RailingConfig
	{
		public DimensionsConfig Dimensions { get; set; }
		public DoorConfig[] Doors { get; set; }
		public Vect3f Position { get; set; }
	}

	public class DimensionsConfig
	{
		[XmlAttribute]
		public int Width { get; set; }
		[XmlAttribute]
		public int Depth { get; set; }
	}

	[XmlType(TypeName = "Door")]
	public class DoorConfig
	{
		[XmlAttribute]
		public PodSide Side { get; set; }
		[XmlAttribute]
		public int Position { get; set; }
		[XmlAttribute]
		public float Angle { get; set; }
	}

	public class FloorConfig
	{
		[XmlAttribute]
		public string NameTemplate { get; set; }
		public DimensionsConfig Dimensions { get; set; }
		public Vect3f Position { get; set; }
	}

	[XmlType(TypeName = "Component")]
	[XmlInclude(typeof(DmxComponentConfig))]
	[XmlInclude(typeof(PassiveComponentConfig))]
	public class ComponentConfig
	{
		[XmlAttribute]
		public string Type { get; set; }
		[XmlAttribute]
		public string Name { get; set; }
		public Vect3f Position { get; set; }
		public Vect3f Rotation { get; set; }
	}

	[XmlType(TypeName = "DmxComponent")]
	public class DmxComponentConfig : ComponentConfig
	{
	}

	[XmlType(TypeName = "PassiveComponent")]
	public class PassiveComponentConfig : ComponentConfig
	{
	}
}
