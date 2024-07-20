
using System.Collections;
using UnityEngine;

public class Glove : MonoBehaviour {

    

    public FirstPersonCam cam;
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



    private IEnumerator Idk() {
        yield return new WaitForSeconds(1);
    }
    // private void FixedUpdate() {  
    //     if(glovePressed && !holding) {
    //         if(inRange) {
    //             Attract(target);
    //             if(CheckHold()) {
    //                 holding = true;
    //                 attracting = false;
    //             }
    //             if(!attracting) {
    //                 attractTimeCounter++;
    //                 if(attractTimeCounter >= attractTime) {
    //                     attractTimeCounter = 0;
    //                     attracting = true;
    //                 }
    //             }
    //         }
    //         else {
    //             Release(target);
    //         }
    //     }   
    //     else {
    //         if(attracting) {
    //             Attract(target);
    //             if(CheckHold()) {
    //                 holding = true;
    //                 attracting = false;
    //             }
    //         }
    //         else if(holding) {
    //             Hold(target);
    //             if(glovePressed) {
    //                 Drop(target);
    //             }
    //             else if(input.gloveShootIsPressed) {
    //                 Shoot(target);
    //             }
    //         }
    //     }       
    // }

    // public IEnumerator ShootAndCooldown() {
    //     //capsule.enabled = false;
    //     // Debug.Log("Capsule disabled");
    //     // Shoot(target);
    //     yield return new WaitForSeconds(shootGapTime);
    //     Shoot(target);
    //     yield return new WaitForSeconds(colliderCooldown);
    //     capsule.enabled = true;
    //     // Debug.Log("Capsule enabled again");
    // }    

    // public IEnumerator ReleaseCooldown() {
    //     // capsule.enabled = false;
    //     //capsule.radius = 0.2f;
    //     yield return new WaitForSeconds(shootGapTime);
    //     Release(target);
    //     yield return new WaitForSeconds(colliderCooldown);
    //     capsule.enabled = true;
    //     //capsule.radius = 2;
    // }

    // private bool FindTarget() {
    //     RaycastHit hit;
    //     if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 11f)) {
    //         if(hit.collider.GetComponent<Rigidbody>() != false && hit.collider.GetComponent<Pickupable>() != false) {
                
    //             target = hit.collider.GetComponent<Rigidbody>();
    //             force = attractForce;
    //             target.useGravity = false;
    //             return true;
    //         }
            
    //     }
       
    //     return false;
    // }


    // private void Attract(Rigidbody target) {
    //     //Debug.Log("Attracting");
    //     force += acceleration;
    //     Vector3 dir = (glovePos.position - target.position).normalized;
    //     target.AddForce(dir * force );
                
    // }


 
    // private void Release(Rigidbody target) {
    //     target.useGravity = true;
    //     attracting = false;
    //     attractTimeCounter = 0;
    //     force = attractForce;
    //     target = null;
    // }

    // private void Drop(Rigidbody target) {
    //     Debug.Log("Dropping");
    //     target.transform.SetParent(bodyHierarchy);
    //     target.interpolation = RigidbodyInterpolation.Interpolate;
    //     target.useGravity = true;
    //     holding = false;
            
    //     target = null;
        
    // }


    // private bool CheckHold() {
    //     RaycastHit hit;
    //     if(Physics.SphereCast(glovePos.position, sphereCastRadius, glovePos.forward, out hit, holdDistance)) {
    //         if(hit.collider.GetComponent<Rigidbody>() != false) {
    //             target.interpolation = RigidbodyInterpolation.None;

    //             bodyHierarchy = target.transform.parent.transform;
    //             target.transform.SetParent(glovePos.transform);

    //             target.velocity = Vector3.zero;
    //             input.useGloves = false;
    //             glovePressed = false;
                
    //             target.useGravity = false;
    //             //capsule.enabled = false;
    //             return true;
    //         }
    //     }
    //     return false;
       
    // }

    // private void Hold(Rigidbody target) {
    //     // target.isKinematic = true;
    //     target.transform.position = Vector3.Lerp(target.transform.position, glovePos.position + 0.4f * glovePos.forward, lerpTime * Time.deltaTime);
        
        
    // }


    // private void Shoot(Rigidbody target) {
    //     Vector3 aimDir = (cam.mouseWorldPosition - target.transform.position).normalized;
    //     target.transform.SetParent(bodyHierarchy);
    //     target.interpolation = RigidbodyInterpolation.Interpolate;
    //     target.useGravity = true; 
    //     target.AddForce(shootForce * aimDir, ForceMode.Impulse);    
        
    //     holding = false;
        
    // }



    // private void Jolt() {
        
    // }
    
    
}
