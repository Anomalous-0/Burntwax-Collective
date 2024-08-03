
using System.Collections;
using UnityEngine;

public class GloveStateMachine : MonoBehaviour {

    

    public CamStateMachine cam;
    public InputManager input;
    public CapsuleCollider capsule;


    [Header("Animation")]
    public Animator animator;
    public int restingHash, attractingHash, holdingHash, droppingHash, shootingHash;


    [Header("Attracting")]
    [SerializeField] Transform glovePos;
    [SerializeField] bool inRange;
    [SerializeField] float force;
    [SerializeField] float attractForce;
    [SerializeField] float attractTime;
    [SerializeField] float attractTimeCounter;
    [SerializeField] float acceleration;
    private Rigidbody target;


    [Header("Holding")]
    [SerializeField] float sphereCastRadius;
    [SerializeField] float holdDistance;
    [SerializeField] float lerpTime;
    [SerializeField] float tempStateTime;
    [SerializeField] private Transform bodyHierarchy;


    [Header("Shoot")]
    [SerializeField] float shootForce;
    [SerializeField] float colliderCooldown;
    [SerializeField] float shootGapTime;


    [Header("Icon")]
    public bool looking;



    // [Header("States")]
    // public bool glovePressed = false;
    // public bool attracting = false;
    // public bool holding = false;
    public GloveBaseState currentState;
    public GloveStateFactory states;

    //Attracting
    public GloveBaseState CurrentState {get { return currentState; } set { currentState = value; } }
    
    public Rigidbody Target {get {return target;} set {target = value;}}
    
    public Transform GlovePos {get {return glovePos; }}
    public bool InRange {get {return inRange;} set {inRange = value;}}

    public float Force {get {return force;} set {force = value;}}
    public float AttractForce {get {return attractForce;}}
    public float AttractTime {get{return attractTime;}}
    public float AttractTimeCounter {get{return attractTimeCounter;} set {attractTimeCounter = value;}}
    public float Acceleration {get {return acceleration;}}


    //Holding
    public float SphereCastRadius {get {return sphereCastRadius;}}
    public float HoldDistance {get {return holdDistance;}}
    public float LerpTime {get {return lerpTime;}}
    public Transform BodyHierarchy {get{return bodyHierarchy;} set {bodyHierarchy = value;}}
    public float TempStateTime {get {return tempStateTime;}}


    //Shooting
    public float ShootForce {get {return shootForce;}}

    
   


    private void Awake() {
        force = attractForce;
        attractTimeCounter = 0;
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        states = new GloveStateFactory(this);
        currentState = states.Rest();
        currentState.EnterState();

        restingHash = Animator.StringToHash("resting");
        attractingHash = Animator.StringToHash("attracting");
        holdingHash = Animator.StringToHash("holding");
        droppingHash = Animator.StringToHash("dropped");
        shootingHash = Animator.StringToHash("shot");

        animator.SetBool(restingHash, true);
        
    }


    private void Update() {
        currentState.UpdateStates();
        
        }


    private void OnTriggerStay(Collider other) {
        if((currentState == states.Search() || currentState == states.Attract())  && other.GetComponent<Rigidbody>() != false && other.GetComponent<Pickupable>() != false) {
            inRange = true;
            looking = false;
            target = other.GetComponent<Rigidbody>();
        }
        else if(currentState == states.Rest() && other.GetComponent<Rigidbody>() != false && other.GetComponent<Pickupable>() != false) {
            looking = true;
        }
          
    }
    private void OnTriggerExit(Collider other) {
        inRange = false;
        looking = false;
        
    }
       
}
