using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Data.Common;

public class GameManager : MonoBehaviour {

    public static GameManager Instance;
    public InputManager input;
    public PauseMenu pauseMenu;

    public PlayerStateMachine player;

    public GunStateMachine gun;

    public PickupManager pickupManager;

    // public Dictionary<string, bool> gunPickups;

    // public List<GunPickup> gunPickupObjects;



    private void Awake() {
        if(Instance != null) {
            Debug.LogError("Found more than one Game Manager in the scene");
        }
        Instance = this;
    }

    private void Start(){
        // SaveSystem.Load();
    }

    private void Update() { 
        if(input.saveIsPressed) {
            SaveSystem.Save();
        }
        if(input.loadIsPressed) {
            SaveSystem.Load();
        }
        
    }
}
