using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim.Haptics.PodLayout
{
    [CreateAssetMenu(fileName = "Components Library", menuName = "Artanim/Components Library", order = 1)]
    public class ComponentsLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct ComponentInfo
        {
            public string Name;
            public GameObject Prefab;

            public bool IsValid { get { return (!string.IsNullOrEmpty(Name)) && (Prefab != null); } }
        }

        [System.Serializable]
        public struct RailingInfo
        {
            public GameObject StraightPrefab;
            public GameObject DoorPrefab;
            public float Length;

            public bool IsValid { get { return (StraightPrefab != null) && (Length > 0); } }
        }

        [System.Serializable]
        public struct FloorInfo
        {
            public GameObject TilePrefab;
            public Vector2 Size;

            public bool IsValid { get { return (TilePrefab == null) || ((Size.x > 0) && (Size.y > 0)); } }
        }

        // Only the railing prefab is required"
        [SerializeField]
        RailingInfo _railing;

        [SerializeField]
        FloorInfo _floor;

        [SerializeField]
        ComponentInfo[] _components = null;

        public bool IsValid { get { return _railing.IsValid && _floor.IsValid; } }

        public RailingInfo Railing { get { return _railing; } }

        public FloorInfo Floor { get { return _floor; } }

        public int ComponentsCount { get { return _components != null ? _components.Length : 0; } }

        public IEnumerable<ComponentInfo> Components { get { return _components; } }
    }
}
