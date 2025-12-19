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
        foreach(GameObject consumable in data.consumables) {
            consumable.SetActive(true);
            // dataConsumable = data.consumableSceneData[consumable];
            // consumableToSceneMap[consumable].position = dataConsumable.position;
            // consumableToSceneMap[consumable].rotation = dataConsumable.rotation;
            // consumableToSceneMap[consumable].rotation = dataConsumable.velocity;


        }
    
        
        
        // Try using the ConsumeabbleData struct (and fill those in with the gameobject and active state) and then do the gpt load thing
        
        
        // foreach(GameObject consumableData in data.consumables) {
        //     Debug.Log("Consumable count:: " + consumablePickups.Count);
 
                  
        //         GameObject consumable = Instantiate(consumableData.prefab, consumableData.position, Quaternion.Euler(consumableData.rotation));
        //         if(consumableData.rb != null) {
        //             consumable.GetComponent<Rigidbody>().linearVelocity = consumableData.rb.linearVelocity;
        //         }
        //         consumablePickups.Add(consumable);
        //         consumableToPrefabMap[consumable] = consumableData.prefab;
            
        // }
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
