
using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{

    public static ObjectManager Instance;

    [SerializeField] private List<GameObject> prefabs;

    [SerializeField] private SerializableDictionary<string, GameObject> prefabToIDMap = new SerializableDictionary<string, GameObject>();


    [SerializeField] private SerializableDictionary<GameObject, ConsumableData> consumableToSceneMap = new SerializableDictionary<GameObject, ConsumableData>();
    [SerializeField] private SerializableDictionary<GameObject, BasicData> basicToSceneMap = new SerializableDictionary<GameObject, BasicData>();
    [SerializeField] private SerializableDictionary<GameObject, DestructableData> destructableToSceneMap = new SerializableDictionary<GameObject, DestructableData>();

    [SerializeField] private List<GameObject> objects = new List<GameObject>();


    private void Awake()
    {

        Instance = this;

        foreach (var prefab in prefabs)
        {
            var id = prefab.GetComponent<PrefabGuid>().guid;
            prefabToIDMap[id] = prefab;
        }
    }

    public void Register(GameObject obj)
    {
        objects.Add(obj);
        if (obj.layer == LayerMask.NameToLayer("Consumable"))
            consumableToSceneMap[obj] = new ConsumableData();
        else if(obj.layer == LayerMask.NameToLayer("Default") && obj.GetComponent<Rigidbody>())
            consumableToSceneMap[obj] = new ConsumableData();
        else if (obj.layer == LayerMask.NameToLayer("Basic"))
            basicToSceneMap[obj] = new BasicData();
        else if (obj.layer == LayerMask.NameToLayer("Destructable"))
            destructableToSceneMap[obj] = new DestructableData();


    }



    public void SaveData(ref ObjectConsumableData data)
    {

        for (int i = objects.Count - 1; i >= 0; i--)
        {

            if (objects[i] == null)
            {
                consumableToSceneMap.Remove(objects[i]);
                basicToSceneMap.Remove(objects[i]);
                destructableToSceneMap.Remove(objects[i]);
                // Debug.Log("Removing consumable from scene data: " + objects[i].name);
                objects.RemoveAt(i);
                continue;



            }

            var prefab = objects[i].GetComponent<PrefabGuid>();
            string id = prefab.guid;


            if (objects[i].layer == LayerMask.NameToLayer("Consumable") || objects[i].layer == LayerMask.NameToLayer("Default"))
            {
                var rb = objects[i].GetComponent<Rigidbody>();

                consumableToSceneMap[objects[i]] = new ConsumableData
                {
                    guid = id,
                    position = rb.position,
                    rotation = rb.rotation.normalized,
                    velocity = rb.linearVelocity
                };
            }

            else if (objects[i].layer == LayerMask.NameToLayer("Basic"))
            {
                basicToSceneMap[objects[i]] = new BasicData
                {
                    guid = id,
                    position = objects[i].transform.position,
                    rotation = objects[i].transform.rotation
                };
            }

            else if (objects[i].layer == LayerMask.NameToLayer("Destructable"))
            {
                var rb = objects[i].GetComponent<Rigidbody>();
                var destructable = objects[i].GetComponent<Crate>();
                destructableToSceneMap[objects[i]] = new DestructableData
                {
                    guid = id,
                    position = rb.position,
                    rotation = rb.rotation.normalized,
                    velocity = rb.linearVelocity,
                    currentHealth = destructable.currentHealth
                };
            }



        }


        data.objects = objects;

        data.consumableSceneData = consumableToSceneMap;
        data.basicSceneData = basicToSceneMap;
        data.destructableSceneData = destructableToSceneMap;
    }

    public void LoadData(ObjectConsumableData data)
    {
        consumableToSceneMap = data.consumableSceneData;
        basicToSceneMap = data.basicSceneData;
        destructableToSceneMap = data.destructableSceneData;

        foreach (var obj in objects)
        {
            if (obj != null)
                Destroy(obj);
        }

        foreach (var obj in consumableToSceneMap)
        {
            if (prefabToIDMap.TryGetValue(obj.Value.guid, out GameObject prefab))
            {
                GameObject newObj = Instantiate(prefab, obj.Value.position, obj.Value.rotation);
                newObj.GetComponent<Rigidbody>().linearVelocity = obj.Value.velocity;
                objects.Add(newObj);

            }
        }

        foreach (var obj in basicToSceneMap)
        {
            if (prefabToIDMap.TryGetValue(obj.Value.guid, out GameObject prefab))
            {
                GameObject newObj = Instantiate(prefab, obj.Value.position, obj.Value.rotation);
                objects.Add(newObj);

            }
        }

        foreach (var obj in destructableToSceneMap)
        {
            if(prefabToIDMap.TryGetValue(obj.Value.guid, out GameObject prefab)) 
            {
                GameObject newObj = Instantiate(prefab, obj.Value.position, obj.Value.rotation);
                newObj.GetComponent<Rigidbody>().linearVelocity = obj.Value.velocity;

                newObj.GetComponent<Destructable>().CurrentHealth = obj.Value.currentHealth;
                objects.Add(newObj);
                
            }
        }


    }
}

[System.Serializable]
public struct ObjectConsumableData
{
    public List<GameObject> objects;
    public SerializableDictionary<GameObject, ConsumableData> consumableSceneData;
    public SerializableDictionary<GameObject, BasicData> basicSceneData;
    public SerializableDictionary<GameObject, DestructableData> destructableSceneData;

}


[System.Serializable]

public struct BasicData
{
    public string guid;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]

public struct ConsumableData
{
    public string guid;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;

}

[System.Serializable]

public struct DestructableData
{
    public string guid;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float currentHealth;
}
