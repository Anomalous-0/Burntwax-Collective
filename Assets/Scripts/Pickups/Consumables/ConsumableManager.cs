using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ConsumableManager : MonoBehaviour {

    public static ConsumableManager Instance;

    [SerializeField] private SerializableDictionary<GameObject, bool> consumableToSceneMap = new SerializableDictionary<GameObject, bool>();
    [SerializeField] private List<GameObject> consumablePickups = new List<GameObject>();


    private void Awake() {
        if(Instance != null) {
            Debug.LogError("More than one Consumable Manager in the scene");
        }
        Instance = this;
    }

    public void Register(GameObject obj) {
        consumablePickups.Add(obj);
        //consumableToPrefabMap[obj] = PrefabUtility.GetCorrespondingObjectFromSource(obj);
    }
    
    public void SaveData(ref SceneConsumableData data) {
        // List<ConsumableSaveData> consumableSaveDataList = new List<ConsumableSaveData>();

        for(int i = consumablePickups.Count - 1; i >= 0; i--)  {
            // if(!consumablePickups[i].activeInHierarchy) {
                // consumablePickups.RemoveAt(i);    
            // }
            // if(consumablePickups[i]) {
            //     GameObject consumable = consumablePickups[i];
            //     ConsumableSaveData saveData = new ConsumableSaveData()
            //     {
            //         position = consumable.transform.position,
            //         rotation = consumable.transform.eulerAngles,
            //         prefab = consumableToPrefabMap[consumable],
            //         rb = consumable.TryGetComponent<Rigidbody>(out var rb) ? rb : null
            //     };
            //     consumableSaveDataList.Add(saveData);
            // }
            // else
            // {
            //     consumablePickups.RemoveAt(i);
            // }
            // data.consumables = consumableSaveDataList;
            
        }
        data.consumables = consumablePickups;
    }

    public void LoadData(SceneConsumableData data) {
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
}



