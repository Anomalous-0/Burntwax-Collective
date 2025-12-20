using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ConsumableManager : MonoBehaviour {

    public static ConsumableManager Instance;

    [SerializeField] private SerializableDictionary<GameObject, ConsumableData> consumableToSceneMap = new SerializableDictionary<GameObject, ConsumableData>();
    [SerializeField] private List<GameObject> consumablePickups = new List<GameObject>();


    private void Awake() {
        if(Instance != null) {
            Debug.LogError("More than one Consumable Manager in the scene");
        }
        Instance = this;
    }

    public void Register(GameObject obj) {
        consumablePickups.Add(obj);
        consumableToSceneMap[obj] = new ConsumableData();
        // consumableToSceneMap[obj] = true;
    }

    
    public void SaveData(ref SceneConsumableData data) {
        // List<ConsumableSaveData> consumableSaveDataList = new List<ConsumableSaveData>();

        for(int i = consumableToSceneMap.Count - 1; i >= 0; i--)  {

            
            if(!consumablePickups[i].activeInHierarchy) {
                consumableToSceneMap.Remove(consumablePickups[i]);
                // Debug.Log("Removing consumable from scene data: " + consumablePickups[i].name);
                consumablePickups.RemoveAt(i);

                
            
               
            }

            var rb = consumablePickups[i].GetComponent<Rigidbody>();
            consumableToSceneMap[consumablePickups[i]] = new ConsumableData {
                position = rb.position,
                rotation = rb.rotation,
                velocity = rb.linearVelocity
            };
        }


        data.consumables = consumablePickups;
        data.consumableSceneData = consumableToSceneMap;
    }

    public void LoadData(SceneConsumableData data) {
        consumablePickups = data.consumables;
        consumableToSceneMap = data.consumableSceneData;

        foreach(var consumable in consumableToSceneMap) {
            if(!consumablePickups.Contains(consumable.Key)) {
                consumable.Key.SetActive(false);
                continue;
            }
            consumable.Key.SetActive(true);
            var rb = consumable.Key.GetComponent<Rigidbody>();
            rb.position = consumable.Value.position;
            rb.rotation = consumable.Value.rotation;
            rb.linearVelocity = consumable.Value.velocity;
            
        }  

    
      
    }
}

[System.Serializable]
public struct SceneConsumableData {
    public List<GameObject> consumables;
    public SerializableDictionary<GameObject, ConsumableData> consumableSceneData;

}



[System.Serializable]

public struct ConsumableData {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;    
    
}
