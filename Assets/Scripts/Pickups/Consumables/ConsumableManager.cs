
using System.Collections.Generic;
using UnityEngine;

public class ConsumableManager : MonoBehaviour {

    public static ConsumableManager Instance;

    [SerializeField] private SerializableDictionary<GameObject, ConsumableData> consumableToSceneMap = new SerializableDictionary<GameObject, ConsumableData>();
    [SerializeField] private SerializableDictionary<GameObject, BasicData> basicToSceneMap = new SerializableDictionary<GameObject, BasicData>();
    
    [SerializeField] private SerializableDictionary<GameObject, DestructableData> destructableToSceneMap = new SerializableDictionary<GameObject, DestructableData>();

    [SerializeField] private List<GameObject> consumablePickups = new List<GameObject>();


    private void Awake() {
        if(Instance != null) {
            Debug.LogError("More than one Consumable Manager in the scene");
        }
        Instance = this;
    }

    public void Register(GameObject obj) {
        consumablePickups.Add(obj);
        if(obj.layer == LayerMask.NameToLayer("Consumable"))
            consumableToSceneMap[obj] = new ConsumableData();
        else if(obj.layer == LayerMask.NameToLayer("Basic"))
            basicToSceneMap[obj] = new BasicData();
        else if(obj.layer == LayerMask.NameToLayer("Destructable"))
            destructableToSceneMap[obj] = new DestructableData();
    
    }

    
    public void SaveData(ref SceneConsumableData data) {
        // List<ConsumableSaveData> consumableSaveDataList = new List<ConsumableSaveData>();

        for(int i = consumablePickups.Count - 1; i >= 0; i--)  {

            if(!consumablePickups[i].activeInHierarchy) {
                consumableToSceneMap.Remove(consumablePickups[i]);
                basicToSceneMap.Remove(consumablePickups[i]);
                destructableToSceneMap.Remove(consumablePickups[i]);
                // Debug.Log("Removing consumable from scene data: " + consumablePickups[i].name);
                consumablePickups.RemoveAt(i);

                
            
               
            }
            if(consumablePickups[i].layer == LayerMask.NameToLayer("Consumable"))
            {
                var rb = consumablePickups[i].GetComponent<Rigidbody>();
            
                consumableToSceneMap[consumablePickups[i]] = new ConsumableData {
                    position = rb.position,
                    rotation = rb.rotation.normalized,
                    velocity = rb.linearVelocity
                };
            }

            else if(consumablePickups[i].layer == LayerMask.NameToLayer("Basic")) {
                basicToSceneMap[consumablePickups[i]] = new BasicData {
                    position = consumablePickups[i].transform.position,
                    rotation = consumablePickups[i].transform.rotation
                };
            }

            else if(consumablePickups[i].layer == LayerMask.NameToLayer("Destructable")) {
                var rb = consumablePickups[i].GetComponent<Rigidbody>();
                var destructable = consumablePickups[i].GetComponent<Crate>();
                destructableToSceneMap[consumablePickups[i]] = new DestructableData {
                    position = rb.position,
                    rotation = rb.rotation.normalized,
                    velocity = rb.linearVelocity,
                    currentHealth = destructable.currentHealth
                };
            }
            
            
            // else if(consumablePickups[i].layer == LayerMask.NameToLayer("Destructable")) {
            //     var destructable = consumablePickups[i].GetComponent<Crate>();
            //     consumableToSceneMap[consumablePickups[i]] = new DestructableData {
            //         position = rb.position,
            //         rotation = rb.rotation,
            //         velocity = rb.linearVelocity,
            //         currentHealth = destructable.CurrentHealth
            //     };
            // }
            
        }


        data.consumables = consumablePickups;

        data.consumableSceneData = consumableToSceneMap;
        data.basicSceneData = basicToSceneMap;
        data.destructableSceneData = destructableToSceneMap;
    }

    public void LoadData(SceneConsumableData data) {
        // consumablePickups = data.consumables;
        consumableToSceneMap = data.consumableSceneData;
        basicToSceneMap = data.basicSceneData;
        destructableToSceneMap = data.destructableSceneData;


        // Debug.Log("Loading Consumables. Total consumables in scene data: " + data.consumableSceneData.Count);
        foreach(var consumable in consumablePickups) {
            if(!data.consumables.Contains(consumable)) {
                consumable.SetActive(false);
                consumablePickups.Remove(consumable);
                // Debug.Log("Consumable removed from scene: " + consumable.name);
                continue;
            }
            consumable.SetActive(true);
            
            if(consumable.layer == LayerMask.NameToLayer("Consumable")) {
                var rb = consumable.GetComponent<Rigidbody>();
                rb.position = consumableToSceneMap[consumable].position;
                rb.rotation = consumableToSceneMap[consumable].rotation;
                rb.linearVelocity = consumableToSceneMap[consumable].velocity;
            }
            else if(consumable.layer == LayerMask.NameToLayer("Basic")) {
                consumable.transform.position = basicToSceneMap[consumable].position;
                consumable.transform.rotation = basicToSceneMap[consumable].rotation;
            }
            
            else if(consumable.layer == LayerMask.NameToLayer("Destructable")) {
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
public struct SceneConsumableData {
    public List<GameObject> consumables;
    public SerializableDictionary<GameObject, ConsumableData> consumableSceneData;
    public SerializableDictionary<GameObject, BasicData> basicSceneData;
    public SerializableDictionary<GameObject, DestructableData> destructableSceneData;

}


[System.Serializable]

public struct BasicData {
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]

public struct ConsumableData {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;    
    
}

[System.Serializable]

public struct DestructableData {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public float currentHealth;
}
