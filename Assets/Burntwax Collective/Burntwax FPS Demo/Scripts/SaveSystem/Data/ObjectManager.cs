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
            }
            Instance = this;

            consumableList = new List<ConsumableData>();
            basicList = new List<BasicData>();
            destructableList = new List<DestructableData>();


            foreach (var prefab in prefabs)
            {
                var id = prefab.prefabAsset.GetComponent<PrefabGuid>().guid;
                prefabToIDMap[id] = prefab;
            }


        }


        public void Register(GameObject obj)
        {
            objects.Add(obj);
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

            consumableList.Clear();
            basicList.Clear();
            destructableList.Clear();

            foreach (var obj in objects)
            {
                if (obj != null)
                    Destroy(obj);

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


                }
            }

            foreach (var obj in basicList)
            {
                if (prefabToIDMap.TryGetValue(obj.guid, out PrefabData prefab))
                {
                    GameObject newObj = Instantiate(prefab.prefabAsset, obj.position, obj.rotation, prefab.parent.transform);
                    newObj.transform.localScale = obj.scale;


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
