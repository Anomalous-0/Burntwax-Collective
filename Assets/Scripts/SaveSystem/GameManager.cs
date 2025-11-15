using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

public class GameManager : MonoBehaviour {

    [Header("File Storage Config")]
    [SerializeField] private string fileName;
    private FileDataHandler dataHandler;
    public static GameManager Instance {get; private set;}
    private GameData gameData;
   
    private List<IDataPersistance> gameObjects;

    private void Awake() {
        if(Instance != null) {
            Debug.LogError("Found more than one Game Manager in the scene");
        }
        Instance = this;
    }

    private void Start() {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        gameObjects = FindAllGameObjects();
        LoadGame();
    }

    public void NewGame() {
        this.gameData = new GameData();
    }

    public void LoadGame() {
        gameData = dataHandler.Load();
        if(gameData == null) {
            Debug.Log("No data was found. Initializing data to defaults");
            NewGame();
        }

        foreach(IDataPersistance obj in gameObjects)
        {
            obj.LoadData(gameData);
        }

        // Debug.Log("Loaded pos: " + gameData.playerPos);
    }

    public void SaveGame() {
        foreach(IDataPersistance obj in gameObjects)
        {
            obj.SaveData(ref gameData);
        }
        // Debug.Log("Saved pos: " + gameData.playerPos);
        dataHandler.Save(gameData);
    }


    private void OnApplicationQuit()
    {
        SaveGame();
    }


    private List<IDataPersistance> FindAllGameObjects() {
        IEnumerable<IDataPersistance> gameObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDataPersistance>();
        return new List<IDataPersistance>(gameObjects);
    }

}
