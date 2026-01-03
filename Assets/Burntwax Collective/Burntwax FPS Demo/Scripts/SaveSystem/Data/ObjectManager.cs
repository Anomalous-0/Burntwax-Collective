
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{

    public static ObjectManager Instance;

    [SerializeField] private SerializableDictionary<GameObject, ConsumableData> consumableToSceneMap = new SerializableDictionary<GameObject, ConsumableData>();
    [SerializeField] private SerializableDictionary<GameObject, BasicData> basicToSceneMap = new SerializableDictionary<GameObject, BasicData>();
    [SerializeField] private SerializableDictionary<GameObject, DestructableData> destructableToSceneMap = new SerializableDictionary<GameObject, DestructableData>();

    [SerializeField] private List<GameObject> objects = new List<GameObject>();


    private void Awake()
    {

        if (Instance != null)
        {
            Debug.LogError("More than one Object Manager in the scene");
            Destroy(gameObject);
        }
        Instance = this;
    }

    public void Register(GameObject obj)
    {
        objects.Add(obj);
        if (obj.layer == LayerMask.NameToLayer("Consumable"))
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

            if (!objects[i].activeInHierarchy)
            {
                consumableToSceneMap.Remove(objects[i]);
                basicToSceneMap.Remove(objects[i]);
                destructableToSceneMap.Remove(objects[i]);
                // Debug.Log("Removing consumable from scene data: " + objects[i].name);
                objects.RemoveAt(i);



            }
            if (objects[i].layer == LayerMask.NameToLayer("Consumable"))
            {
                var rb = objects[i].GetComponent<Rigidbody>();

                consumableToSceneMap[objects[i]] = new ConsumableData
                {
                    position = rb.position,
                    rotation = rb.rotation.normalized,
                    velocity = rb.linearVelocity
                };
            }

            else if (objects[i].layer == LayerMask.NameToLayer("Basic"))
            {
                basicToSceneMap[objects[i]] = new BasicData
                {
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


        // Debug.Log("Loading Consumables. Total consumables in scene data: " + data.consumableSceneData.Count);
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var consumable = objects[i];


            if (!data.objects.Contains(consumable))
            {
                // consumable.SetActive(false);
                Destroy(consumable);
                objects.Remove(consumable);
                // Debug.Log("Consumable removed from scene: " + consumable.name);
                continue;
            }
            consumable.SetActive(true);

            if (consumable.layer == LayerMask.NameToLayer("Consumable"))
            {
                var rb = consumable.GetComponent<Rigidbody>();
                rb.position = consumableToSceneMap[consumable].position;
                rb.rotation = consumableToSceneMap[consumable].rotation;
                rb.linearVelocity = consumableToSceneMap[consumable].velocity;
            }
            else if (consumable.layer == LayerMask.NameToLayer("Basic"))
            {
                consumable.transform.position = basicToSceneMap[consumable].position;
                consumable.transform.rotation = basicToSceneMap[consumable].rotation;
            }

            else if (consumable.layer == LayerMask.NameToLayer("Destructable"))
            {
                var rb = consumable.GetComponent<Rigidbody>();
                rb.position = destructableToSceneMap[consumable].position;
                rb.rotation = destructableToSceneMap[consumable].rotation;
                rb.linearVelocity = destructableToSceneMap[consumable].velocity;
                var destructable = consumable.GetComponent<Crate>();
                destructable.currentHealth = destructableToSceneMap[consumable].currentHealth;
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
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]

public struct ConsumableData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;

}

[System.Serializable]

public struct DestructableData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float currentHealth;
}
