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

        }

        void Start()
        {
            foreach (var prefab in prefabs)
            {
                var id = prefab.prefabAsset.GetComponent<PrefabGuid>().guid;
                prefabToIDMap[id] = prefab;
            }

            if (GameManager.Instance.IsLoadingFromSave)
                return;

            objects.Clear();

            PrefabGuid[] sceneObjects = FindObjectsByType<PrefabGuid>();
            foreach (var obj in sceneObjects)
            {
                objects.Add(obj.gameObject);
            }
        }


        public void Register(GameObject obj)
        {
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


                if (objects[i].layer == LayerMask.NameToLayer("Consumable") || (objects[i].layer == LayerMask.NameToLayer("Default") && objects[i].GetComponent<Rigidbody>()))
                {
                    var rb = objects[i].GetComponent<Rigidbody>();

                    consumableList.Add(new ConsumableData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = rb.position,
                        rotation = rb.rotation.normalized,
                        velocity = rb.linearVelocity
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

                else if (objects[i].layer == LayerMask.NameToLayer("Destructable"))
                {
                    var rb = objects[i].GetComponent<Rigidbody>();
                    var destructable = objects[i].GetComponent<Destructable>();

                    destructableList.Add(new DestructableData
                    {
                        guid = id,
                        scale = objects[i].transform.localScale,
                        position = rb.position,
                        rotation = rb.rotation.normalized,
                        velocity = rb.linearVelocity,
                        currentHealth = destructable.CurrentHealth
                    });
                }
            }

            data.consumableList = consumableList;
            data.basicList = basicList;
            data.destructableList = destructableList;
        }

        public void LoadData(ObjectConsumableData data)
        {

            PrefabGuid[] existingObjects = FindObjectsByType<PrefabGuid>();
            foreach (var existing in existingObjects)
            {
                if (existing != null && existing.gameObject != null)
                    Destroy(existing.gameObject);
            }
            objects.Clear();

            consumableList = data.consumableList;
            basicList = data.basicList;
            destructableList = data.destructableList; ;

            foreach (var obj in consumableList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, prefab.parent.transform);
                    newObj.transform.localScale = obj.scale;
                    newObj.GetComponent<Rigidbody>().linearVelocity = obj.velocity;
                    objects.Add(newObj);
                }
            }

            foreach (var obj in basicList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, prefab.parent.transform);
                    newObj.transform.localScale = obj.scale;
                    objects.Add(newObj);
                }
            }

            foreach (var obj in destructableList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, prefab.parent.transform);
                    newObj.transform.localScale = obj.scale;
                    newObj.GetComponent<Rigidbody>().linearVelocity = obj.velocity;
                    newObj.GetComponent<Destructable>().CurrentHealth = obj.currentHealth;
                    objects.Add(newObj);
                }
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
