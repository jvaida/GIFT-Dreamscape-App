using Artanim.Algebra;
using Artanim.Haptics.PodLayout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Artanim.Haptics.Visuals
{
    [ExecuteInEditMode]
    public class Pod : SingletonBehaviour<Pod>
    {
        // State sync
        public class FullState { public DeviceState[] States; }
        public struct DeviceState { public string Name; public float Value; }

        class Pos2DWithAngle
        {
            public Vector2Int Position;
            public float Angle;
        }

        const string _instantiatePodLayout = "Instantiate Pod Layout";

        [SerializeField]
        ComponentsLibrary _componentsLibrary = null;

        [ReadOnlyProperty] [SerializeField]
        private string Contents = "";

        [ReadOnlyProperty] [SerializeField]
        private string ContentsContinued = "";

        const string _floorTransfName = "Floor";
        const string _railingTransfName = "Railing";

        PodLayoutConfig _config;
        Dictionary<string, HapticDevice> _devices = new Dictionary<string, HapticDevice>();

        public static bool IsInstantiated { get; private set; }

        #region Public methods

#if PLOP // Use AudioDevicesController instead

        public HapticDevice FindDevice(string name)
        {
            HapticDevice device;
            _devices.TryGetValue(name, out device);
            return device;
        }

        public void SetDeviceValue(string name, float value)
        {
            var device = FindDevice(name);
            if (device != null)
            {
                device.Value = value;
            }
            else
            {
                Debug.LogError("No visual for haptic device named " + name);
            }
        }

        public HapticDevice[] FindDevices(string type)
        {
            var root = transform.Find(type);
            return root == null ? null : root.OfType<Transform>().Select(t => t.GetComponent<HapticDevice>()).Where(d => d != null).ToArray();
        }

        public string FindTileAtPosition(Vector2 pos)
        {
            return FindTileAtPosition(pos.x, pos.y);
        }

        public string FindTileAtPosition(float x, float z)
        {
            var floor = transform.Find(_floorTransfName);
            if (floor == null) return null;

            if (_componentsLibrary == null) return null;
            if (_config == null || _config.Floor == null || _config.Floor.Dimensions == null) return null;

            var dim = _config.Floor.Dimensions;
            var position = _config.Floor.Position;
            Vector2 tileSize = _componentsLibrary.Floor.Size;

            var offset = 0.5f * new Vector3(dim.Width - 1, 0, dim.Depth - 1);

            // Follow ordering of floor tiles audio channels
            int ix = Mathf.RoundToInt(offset.x - (x - position.X) / tileSize.x);
            int iz = Mathf.RoundToInt(offset.z - (z - position.Z) / tileSize.y);
            if (ix != Mathf.Clamp(ix, 0, dim.Width - 1)) return null;
            if (iz != Mathf.Clamp(iz, 0, dim.Depth - 1)) return null;

            int index = ix + iz * _config.Floor.Dimensions.Depth;
            if (index < 0 || index >= floor.childCount) return null;

            return floor.GetChild(index).name;
        }

        public string[] FindTilesIntersect(Bounds bounds)
        {
            var floor = transform.Find(_floorTransfName);
            if (floor == null) return null;

            if (_componentsLibrary == null) return null;
            if (_config == null || _config.Floor == null || _config.Floor.Dimensions == null) return null;

            var dim = _config.Floor.Dimensions;
            var position = _config.Floor.Position;
            Vector2 tileSize = _componentsLibrary.Floor.Size;

            var size = new Vector3(tileSize.x, 0.4f, tileSize.y);
            return floor.Cast<Transform>().Where(t => new Bounds(t.position, size).Intersects(bounds)).Select(t => t.name).ToArray();
        }
#endif

        #endregion

        #region Unity messages

        void Start()
        {
            InstantiatePodLayout();
        }

        void OnEnable()
        {
            if (IsInstantiated)
            {
                Debug.LogError("Pod Visualization already instantiated");
            }
            IsInstantiated = true;

            // Syncing device states between client and server
            if (Application.isPlaying && GameController.Instance && !Location.Network.NetworkInterface.Instance.IsServer)
            {
                GameController.Instance.OnStreamingMessage += Instance_OnStreamingMessage;
            }
        }

        void OnDisable()
        {
            IsInstantiated = false;
            if (GameController.HasInstance)
            {
                GameController.Instance.OnStreamingMessage -= Instance_OnStreamingMessage;
            }
        }

        void LateUpdate()
        {
            if (Application.isPlaying && Location.Network.NetworkInterface.HasInstance && Location.Network.NetworkInterface.Instance.IsServer)
            {
                if (HapticsController.IsDmxEnabled)
                {
                    var devices = HapticsController.DmxDevicesController.AllDevices;
                    foreach (var dev in devices)
                    {
                        HapticDevice visualDev;
                        if (_devices.TryGetValue(dev.Name, out visualDev))
                        {
                            visualDev.Value = dev.GetChannelCurrentValue(0);
                        }
                    }
                }

                if (HapticsController.IsAudioEnabled)
                {
                    var elements = HapticsController.AudioDevicesController.AllElements;
                    var active = elements.Where(e => e.IsPlayingSound).ToArray();
                    foreach (var elem in elements)
                    {
                        HapticDevice visualDev;
                        if (_devices.TryGetValue(elem.Name, out visualDev))
                        {
                            visualDev.Value = System.Array.IndexOf(active, elem) >= 0 ? 1 : 0;
                        }
                    }
                }

                if (GameController.HasInstance && (GameController.Instance.CurrentSession != null))
                {
                    var states = _devices.Values.Select(d => new DeviceState { Name = d.name, Value = d.Value }).ToArray();
                    GameController.Instance.SendStreamingData(new FullState { States = states });
                }
            }
        }

        #endregion

        #region Script menu

        [ContextMenu("Load XML config")]
        void LoadConfig()
        {
            _config = null;
            try
            {
#if UNITY_EDITOR
                PodLayoutConfig.Reload();
#endif
                _config = PodLayoutConfig.Instance;
            }
            finally
            {
                Contents = ContentsContinued = "";
                if ((_config != null) && (_config.Railing != null))
                {
                    Contents = string.Format("Railing: {0}x{1}", _config.Railing.Dimensions.Width, _config.Railing.Dimensions.Depth);
                    if ((_config.Railing.Doors != null) && (_config.Railing.Doors.Length > 0))
                    {
                        Contents += ", Doors: " + _config.Railing.Doors.Length;
                    }
                    if (_config.Floor != null)
                    {
                        Contents += string.Format(", Floor: {0}x{1}", _config.Floor.Dimensions.Width, _config.Floor.Dimensions.Depth);
                    }

                    if (_config.Components != null)
                    {
                        foreach (var list in _config.Components.GroupBy(c => c.Type, c => c))
                        {
                            int c = list.Count();
                            if (c > 0)
                            {
                                if (ContentsContinued.Length > 0) ContentsContinued += ", ";
                                ContentsContinued += list.Key + ": " + c;
                            }
                        }
                    }

                    Debug.LogFormat("Loaded Pod Layout from file {0}: {1}", PodLayoutConfig.Filename, Contents + ", " + ContentsContinued);
                }
                if (string.IsNullOrEmpty(Contents))
                {
                    Contents = string.Format("Invalid or empty `{0}` file", PodLayoutConfig.Filename);
                    Debug.LogError(Contents);
                }
            }
        }

        [ContextMenu(_instantiatePodLayout)]
        public void InstantiatePodLayout()
        {
            Debug.Log("Instantiating pod layout");

#if UNITY_EDITOR
            Undo.RecordObject(transform, "Pod Layout");
#endif

            _devices.Clear();
            // Destroy all child transforms
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                UndoDestroyObjectImmediate(transform.GetChild(i).gameObject);
            }

            LoadConfig();

            if ((_componentsLibrary != null) && _componentsLibrary.IsValid && (_config != null))
            {
                if (_componentsLibrary.Floor.IsValid && (_config.Floor != null) && (_config.Floor.Dimensions != null))
                {
                    // Create floor
                    CreateTiling(_floorTransfName, _config.Floor.Position.ToUnity(), _config.Floor.Dimensions,
                        _componentsLibrary.Floor.Size, _componentsLibrary.Floor.TilePrefab,
                        (x, z) => PodLayoutConfig.CreateFloorTileName(_config.Floor.NameTemplate, new Vector2Int(x, z)));
                }

                if (_componentsLibrary.Railing.IsValid && (_config.Railing != null) && (_config.Railing.Dimensions != null))
                {
                    // Create railing
                    List<Pos2DWithAngle> otherPositions = null;
                    if (_config.Railing.Doors != null)
                    {
                        otherPositions = new List<Pos2DWithAngle>();
                        foreach (var door in _config.Railing.Doors)
                        {
                            var pos = Vector2Int.zero;
                            switch (door.Side)
                            {
                                case PodSide.Left:
                                case PodSide.Right:
                                    pos.y = door.Position;
                                    break;
                                case PodSide.Front:
                                case PodSide.Back:
                                    pos.x = door.Position;
                                    break;
                                default: throw new System.Exception("Unexpected door side value: " + door.Side);
                            }
                            if ((pos.x != Mathf.Clamp(pos.x, 0, _config.Railing.Dimensions.Width - 1))
                                || (pos.y != Mathf.Clamp(pos.y, 0, _config.Railing.Dimensions.Depth - 1)))
                            {
                                Debug.LogError("Invalid door position: " + door.Position);
                            }
                            switch (door.Side)
                            {
                                case PodSide.Left:
                                    pos.x = _config.Railing.Dimensions.Width;
                                    break;
                                case PodSide.Front:
                                    pos.y = _config.Railing.Dimensions.Depth;
                                    break;
                            }
                            otherPositions.Add(new Pos2DWithAngle { Position = pos, Angle = door.Angle });
                        }
                    }
                    CreateRailing(_railingTransfName, _config.Railing.Position.ToUnity(), _config.Railing.Dimensions, _componentsLibrary.Railing.Length, _componentsLibrary.Railing.StraightPrefab, _componentsLibrary.Railing.DoorPrefab, otherPositions);
                }

                if (_config.Components != null)
                {
                    foreach (var comp in _config.Components)
                    {
                        var compType = _componentsLibrary.Components.FirstOrDefault(e => e.Name == comp.Type);
                        if (!compType.IsValid)
                        {
                            Debug.LogError("Invalid component type: " + comp.Type);
                            continue;
                        }
                        var pos = comp.Position.ToUnity();
                        var rot = Quaternion.Euler(comp.Rotation.ToUnity());
                        CreateComponent(GetOrCreateRoot(comp.Type), compType.Prefab, pos, rot, comp.Name);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Empty pod layout or components library");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Save XML config")]
        void SaveConfig()
        {
            if (_config == null)
            {
                LoadConfig();
            }

            // Iterate through children to find devices
            var components = new List<ComponentConfig>();
            foreach (Transform child in transform)
            {
                if ((child.name != _floorTransfName) && (child.name != _railingTransfName))
                {
                    foreach (Transform t in child)
                    {
                        var hapticDevice = t.GetComponent<HapticDevice>();

                        ComponentConfig component = (hapticDevice != null)
                            ? new DmxComponentConfig { Name = hapticDevice.name } as ComponentConfig
                            : new PassiveComponentConfig();
                        component.Type = child.name;
                        component.Position = t.position.ToVect3f();
                        component.Rotation = t.rotation.eulerAngles.ToVect3f();
                        components.Add(component);
                    }
                }
            }

            _config.Components = components.ToArray();
            PodLayoutConfig.Save();

            Debug.LogFormat("Saved Pod Layout to file {0}", PodLayoutConfig.Filename);
        }
#endif

        Transform GetOrCreateRoot(string name, Vector3 position = default(Vector3))
        {
            var root = transform.Find(name);
            if (!root)
            {
                root = new GameObject(name).transform;
                root.SetParent(transform);
                root.localPosition = position;
                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one;

                UndoRegisterCreatedObjectUndo(root.gameObject, _instantiatePodLayout);
            }
            return root;
        }

        #endregion

        #region Internals

        void CreateComponent(Transform root, GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), string name = null)
        {
            var transf = InstantiatePrefab(prefab).transform;
            transf.parent = root;
            transf.localPosition = position;
            transf.localRotation = rotation;
            if (!string.IsNullOrEmpty(name))
            {
                transf.name = name;
            }

            UndoRegisterCreatedObjectUndo(transf.gameObject, _instantiatePodLayout);

            var device = transf.GetComponent<HapticDevice>();
            if ((device != null) && device.enabled)
            {
                _devices.Add(device.name, device);
            }
        }

        void CreateTiling(string rootName, Vector3 position, DimensionsConfig dim, Vector2 tileSize, GameObject tilePrefab, System.Func<int, int, string> getTileName = null)
        {
            var root = GetOrCreateRoot(rootName, position);
            var offset = 0.5f * new Vector3(dim.Width - 1, 0, dim.Depth - 1);

            // Follow ordering of floor tiles audio channels
            for (int z = 0; z < dim.Depth; ++z)
            {
                for (int x = 0; x < dim.Width; ++x)
                {
                    var pos = new Vector3((offset.x - x) * tileSize.x, 0, (offset.z - z) * tileSize.y);
                    string name = (getTileName == null) ? null : getTileName(x, z);
                    CreateComponent(root, tilePrefab, pos, name: name);
                }
            }
        }

        void CreateRailing(string rootName, Vector3 position, DimensionsConfig dim, float elemLength, GameObject prefab, GameObject otherPrefab = null, IEnumerable<Pos2DWithAngle> otherPositions = null)
        {
            System.Func<int, int, Pos2DWithAngle> FindSpecialPosition = (int x, int z)
                => otherPositions == null ? null : otherPositions.FirstOrDefault(sp => sp.Position == new Vector2Int(x, z));

            var root = GetOrCreateRoot(rootName, position);
            var offset = 0.5f * new Vector3(dim.Width - 1, 0, dim.Depth - 1);

            for (int i = 0; i < 2; ++i)
            {
                float sign = i == 0 ? 1 : -1;

                var rot = Quaternion.Euler(0, i == 0 ? 0 : 180, 0);
                for (int x = 0; x < dim.Width; ++x)
                {
                    var pos = elemLength * new Vector3(x - offset.x, 0, sign * (dim.Depth - 0.5f - offset.z));
                    var spos = FindSpecialPosition(x, i == 0 ? dim.Depth : 0);
                    CreateComponent(root, spos == null ? prefab : otherPrefab, pos, spos == null ? rot : Quaternion.Euler(0, spos.Angle, 0));
                }

                rot = Quaternion.Euler(0, i == 0 ? 90 : -90, 0);
                for (int z = 0; z < dim.Depth; ++z)
                {
                    var pos = elemLength * new Vector3(sign * (dim.Width - 0.5f - offset.x), 0, z - offset.z);
                    var spos = FindSpecialPosition(i == 0 ? dim.Width : 0, z);
                    CreateComponent(root, spos == null ? prefab : otherPrefab, pos, spos == null ? rot : Quaternion.Euler(0, spos.Angle, 0));
                }
            }
        }

        void UndoDestroyObjectImmediate(GameObject objectToUndo)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(objectToUndo);
#else
            if (Application.isPlaying)
            {
                GameObject.Destroy(objectToUndo);
            }
            else
            {
                GameObject.DestroyImmediate(objectToUndo);
            }
#endif
        }
        void UndoRegisterCreatedObjectUndo(GameObject objectToUndo, string name)
        {
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(objectToUndo, name);
#endif
        }

        GameObject InstantiatePrefab(GameObject prefab)
        {
#if UNITY_EDITOR
            return PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#else
            return UnityUtils.InstantiatePrefab(prefab);
#endif
        }

        void Instance_OnStreamingMessage(object data, bool sendBySelf)
        {
            if ((!sendBySelf) && (_devices.Count > 0))
            {
                var states = data as FullState;
                if (states != null)
                {
                    foreach (var state in states.States)
                    {
                        HapticDevice device;
                        if (_devices.TryGetValue(state.Name, out device))
                        {
                            device.Value = state.Value;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
