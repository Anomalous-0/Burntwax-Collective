using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    public class ObjectManager : MonoBehaviour
    {

        public static ObjectManager Instance;


        [Header("Prefab Configuration")]
        [SerializeField] private List<PrefabData> prefabs;
        [SerializeField] private SerializableDictionary<string, PrefabData> prefabToIDMap = new SerializableDictionary<string, PrefabData>();


        [Header("Runtime Tracking")]

        // [SerializeField] private SerializableDictionary<GameObject, ConsumableData> consumableRuntimeMap = new SerializableDictionary<GameObject, ConsumableData>();
        // [SerializeField] private SerializableDictionary<GameObject, BasicData> basicRuntimeMap = new SerializableDictionary<GameObject, BasicData>();
        // [SerializeField] private SerializableDictionary<GameObject, DestructableData> destructableRuntimeMap = new SerializableDictionary<GameObject, DestructableData>();
        [SerializeField] private List<GameObject> objects = new List<GameObject>();


        private List<ConsumableData> consumableList;
        private List<BasicData> basicList;
        private List<DestructableData> destructableList;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;


            consumableList = new List<ConsumableData>();
            basicList = new List<BasicData>();
            destructableList = new List<DestructableData>();

            // Loading can begin as soon as the sceneLoaded event fires, which is
            // before Start. Build this lookup in Awake so restored objects always
            // have access to their prefab definitions.
            BuildPrefabLookup();

        }

        void Start()
        {
            if (GameManager.Instance && GameManager.Instance.IsLoadingFromSave)
                return;
            else if(GameManager.Instance == null)
                Debug.LogWarning("GameManager not found. Save data will not be loaded.");

            objects.Clear();

            PrefabGuid[] sceneObjects = FindObjectsByType<PrefabGuid>();
            foreach (var obj in sceneObjects)
            {
                Register(obj.gameObject);
            }
        }

        private void BuildPrefabLookup()
        {
            prefabToIDMap.Clear();

            if (prefabs == null)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab == null || prefab.prefabAsset == null)
                {
                    Debug.LogWarning("ObjectManager has an empty prefab configuration entry.", this);
                    continue;
                }

                PrefabGuid prefabGuid = prefab.prefabAsset.GetComponent<PrefabGuid>();
                if (prefabGuid == null || string.IsNullOrWhiteSpace(prefabGuid.guid))
                {
                    Debug.LogWarning($"Saveable prefab '{prefab.prefabAsset.name}' is missing a valid PrefabGuid.", prefab.prefabAsset);
                    continue;
                }

                if (prefabToIDMap.ContainsKey(prefabGuid.guid))
                    Debug.LogWarning($"Duplicate save prefab GUID '{prefabGuid.guid}' found on '{prefab.prefabAsset.name}'.", prefab.prefabAsset);

                prefabToIDMap[prefabGuid.guid] = prefab;
            }
        }


        public void Register(GameObject obj)
        {
            if (obj == null || objects.Contains(obj))
                return;

            objects.Add(obj);
            // if (obj.layer == LayerMask.NameToLayer("Consumable") ||
            //    (obj.layer == LayerMask.NameToLayer("Default") && obj.GetComponent<Rigidbody>()))
            // {
            //     consumableRuntimeMap[obj] = new ConsumableData();
            // }
            // else if (obj.layer == LayerMask.NameToLayer("Basic"))
            // {
            //     basicRuntimeMap[obj] = new BasicData();
            // }
            // else if (obj.layer == LayerMask.NameToLayer("Destructable"))
            // {
            //     destructableRuntimeMap[obj] = new DestructableData();
            // }
        }



        public void SaveData(ref ObjectConsumableData data)
        {
            consumableList ??= new List<ConsumableData>();
            basicList ??= new List<BasicData>();
            destructableList ??= new List<DestructableData>();

            consumableList.Clear();
            basicList.Clear();
            destructableList.Clear();

            for (int i = objects.Count - 1; i >= 0; i--)
            {

                if (objects[i] == null)
                {
                    objects.RemoveAt(i);
                    continue;
                }

                var prefab = objects[i].GetComponent<PrefabGuid>();
                if (prefab == null) continue;
                string id = prefab.guid;

                Rigidbody rb = objects[i].GetComponent<Rigidbody>();
                Destructable destructable = objects[i].GetComponent<Destructable>();

                // Component capabilities determine the saved state. A layer alone
                // does not guarantee that an object has health (Target is one
                // example), so destructible objects are identified by interface.
                if (destructable != null)
                {
                    destructableList.Add(new DestructableData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = rb != null ? rb.position : objects[i].transform.position,
                        rotation = rb != null ? rb.rotation.normalized : objects[i].transform.rotation.normalized,
                        velocity = rb != null ? rb.linearVelocity : Vector3.zero,
                        currentHealth = destructable.CurrentHealth
                    });
                }
                else if (objects[i].layer == LayerMask.NameToLayer("Basic"))
                {
                    basicList.Add(new BasicData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = objects[i].transform.position,
                        rotation = objects[i].transform.rotation
                    });
                    // Debug.Log($"Saved Basic Object: {id} at Position: {objects[i].transform.position}");
                }
                else if (rb != null)
                {
                    consumableList.Add(new ConsumableData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = rb.position,
                        rotation = rb.rotation.normalized,
                        velocity = rb.linearVelocity
                    });
                }
                else
                {
                    basicList.Add(new BasicData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = objects[i].transform.position,
                        rotation = objects[i].transform.rotation
                    });
                }
            }

            data.consumableList = consumableList;
            data.basicList = basicList;
            data.destructableList = destructableList;
        }

        public void LoadData(ObjectConsumableData data)
        {

            // Awake normally builds this. Rebuilding here also makes direct calls
            // safe if Unity lifecycle order changes during development.
            if (prefabToIDMap.Count == 0)
                BuildPrefabLookup();

            PrefabGuid[] existingObjects = FindObjectsByType<PrefabGuid>();
            foreach (var existing in existingObjects)
            {
                if (existing != null && existing.gameObject != null)
                    Destroy(existing.gameObject);
            }
            objects.Clear();

            consumableList = data.consumableList ?? new List<ConsumableData>();
            basicList = data.basicList ?? new List<BasicData>();
            destructableList = data.destructableList ?? new List<DestructableData>();

            foreach (var obj in consumableList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    Transform parent = prefab.parent != null ? prefab.parent.transform : null;
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, parent);
                    newObj.transform.localScale = obj.scale;
                    Rigidbody rb = newObj.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                        rb.linearVelocity = obj.velocity;
                    Register(newObj);
                }
                else
                    Debug.LogWarning($"No configured prefab found for saved GUID '{obj.guid}'.", this);
            }

            foreach (var obj in basicList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    Transform parent = prefab.parent != null ? prefab.parent.transform : null;
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, parent);
                    newObj.transform.localScale = obj.scale;
                    Register(newObj);
                }
                else
                    Debug.LogWarning($"No configured prefab found for saved GUID '{obj.guid}'.", this);
            }

            foreach (var obj in destructableList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    Transform parent = prefab.parent != null ? prefab.parent.transform : null;
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, parent);
                    newObj.transform.localScale = obj.scale;
                    Rigidbody rb = newObj.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                        rb.linearVelocity = obj.velocity;

                    Destructable destructable = newObj.GetComponent<Destructable>();
                    if (destructable != null)
                        destructable.CurrentHealth = obj.currentHealth;
                    else
                        Debug.LogWarning($"Saved destructible prefab '{newObj.name}' no longer implements Destructable.", newObj);

                    Register(newObj);
                }
                else
                    Debug.LogWarning($"No configured prefab found for saved GUID '{obj.guid}'.", this);
            }




        }
    }


    [System.Serializable]
    public class PrefabData
    {
        public GameObject prefabAsset;
        public GameObject parent;

    }

    [System.Serializable]
    public struct ObjectConsumableData
    {
        public List<ConsumableData> consumableList;
        public List<BasicData> basicList;
        public List<DestructableData> destructableList;

    }


    [System.Serializable]

    public struct BasicData
    {
        public string guid;
        public Vector3 scale;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]

    public struct ConsumableData
    {
        public string guid;
        public Vector3 scale;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;

    }

    [System.Serializable]

    public struct DestructableData
    {
        public string guid;
        public Vector3 scale;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float currentHealth;
    }

}
