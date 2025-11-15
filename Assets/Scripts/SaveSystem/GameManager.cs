using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

public class GameManager : MonoBehaviour {

    public static GameManager Instance;
    public InputManager input;

    public PlayerStateMachine player;
    public CamStateMachine cam;



    private void Awake() {
        if(Instance != null) {
            Debug.LogError("Found more than one Game Manager in the scene");
        }
        Instance = this;
    }

    private void Start(){
        SaveSystem.Load();
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
