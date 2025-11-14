using UnityEngine;

public class GameManager : MonoBehaviour {
   public static GameManager Instance {get; private set;}
   private GameData gameData;

   private void Awake() {
        if(Instance != null) {
            Debug.LogError("Found more than one Game Manager in the scene");
        }
        Instance = this;
    }

    public void NewGame() {
        this.gameData = new GameData();
    }

    public void LoadGame() {
        if(this.gameData == null) {
            Debug.Log("No data was found. Initializing data to defaults");
            NewGame();
        }
    }

    public void SaveGame() {
        
    }


    private void OnApplicationQuit()
    {
        SaveGame();
    }

}
