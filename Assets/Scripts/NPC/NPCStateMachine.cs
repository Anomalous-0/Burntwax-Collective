using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class NPCStateMachine : MonoBehaviour {
    public InputManager input;
    public PlayerStateMachine player;
    public Transform npc;
    Rigidbody rb;


    private NPCBaseState currentState;
    public NPCBaseState CurrentState {get { return currentState; } set { currentState = value; } }

    public NPCStateFactory states;


    [Header("Triggers")]
    //[SerializeField] private UnityEvent allTriggers;
    public bool interactionHit;
    public bool dangerHitVisual; // Will be for visual danger cues
    // public something dangerHitAudio; // Will be for audio danger cues

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        states = new NPCStateFactory(this);
        currentState = states.Idle();
        currentState.EnterState();
    }


    void Update() {


        currentState.UpdateStates();
    }
}
