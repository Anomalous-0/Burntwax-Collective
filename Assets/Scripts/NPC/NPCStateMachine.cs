using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class NPCStateMachine : MonoBehaviour {
    public PlayerStateMachine player;

    
    private NPCBaseState currentState;
    public NPCBaseState CurrentState {get { return currentState; } set { currentState = value; } }

    public NPCStateFactory states;


    private void Awake() {

        states = new NPCStateFactory(this);
        currentState = states.Idle();
        currentState.EnterState();
    }


    void Update() {
        currentState.UpdateStates();
    }
}
