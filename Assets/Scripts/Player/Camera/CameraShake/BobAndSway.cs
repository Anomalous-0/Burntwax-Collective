using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobAndSway : MonoBehaviour {
    [Header("External References")]

    [SerializeField] InputManager input;
    [SerializeField] PlayerStateMachine player;

    [SerializeField] GunStateMachine gun;
    [SerializeField] CamStateMachine cam;

    [SerializeField] Rigidbody rb;
    
    public bool sway = true;
    public bool swayRotation = true;
    public bool bobOffset = true;
    public bool bobSway = true;
    private bool shouldBob = true;

    private Vector3 originalBobPos;
    //private float timeAimed;

    // Update is called once per frame
    void Update() {
        GetInput();
        if (input.MoveInput != Vector2.zero && (player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun()) && shouldBob && cam.currentState != cam.states.Aim() && gun.CurrentState != gun.states.Shoot() ) {
            Sway();
        }
        else {
            speedCurve = 0;
        }
        SwayRotation();
        if (cam.currentState != cam.states.Aim()) {
            BobOffset();
            BobRotation();
            originalBobPos = bobPosition;
            //timeAimed = -5f;
        } else {
            // if (timeAimed < 0) {
            //     timeAimed = Time.time;
            // }
            //Debug.Log(bobPosition.x + ", " + bobPosition.y + ", " + bobPosition.z);
            transform.localPosition = Vector3.zero;
            bobPosition = Vector3.zero;
            speedCurve = 0;
            // if (bobPosition != Vector3.zero) {
                
                // if (Time.time - timeAimed <= 1f) {
                //     transform.localPosition = Vector3.zero;
                //     // new Vector3(
                //     //     Mathf.Lerp(bobPosition.x, 0, Time.time - timeAimed),
                //     //     transform.localPosition.y,
                //     //     transform.localPosition.z
                //     // );
                // } else {
                //     transform.localPosition = Vector3.zero;
                // }
            // }
        }

        CompositePositionRotation();
    }

    Vector2 walkInput;
    Vector2 lookInput;
    void GetInput() {
        walkInput.x = input.MoveInput.x;
        walkInput.y = input.MoveInput.y;
        walkInput = walkInput.normalized;

        lookInput.x = input.LookInput.x;
        lookInput.y = input.LookInput.y;
    }


    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;

    void Sway() {
        if (sway == false) {
            swayPos = Vector3.zero;
            return;
        }

        Vector3 invertLook = lookInput * -step;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        // if (lookInput.magnitude > 0.1f) {
        //     shouldBob = false;
        // } else {
        //     shouldBob = true;
        // }

        swayPos = invertLook;
    }

    
    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;

    void SwayRotation() {
        if (swayRotation == false) {
            swayEulerRot = Vector3.zero;
            return;
        }

        Vector2 invertLook = lookInput * -rotationStep;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);

        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }


    [Header("Bobbing")]
    public float speedCurve;
    float curveSin {get => Mathf.Sin(speedCurve);}
    float curveCos {get => Mathf.Cos(speedCurve);}

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;

    Vector3 bobPosition;
    private float lastTime = 0;

    void BobOffset() {

        if (player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun()) {
            lastTime = Time.deltaTime;
        }

        speedCurve += 1.5f * lastTime * (player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun() ? rb.velocity.magnitude : 1f);

        if (bobOffset == false) {
            bobPosition = Vector3.zero;
            return;
        }

        bobPosition.x = (curveCos * bobLimit.x * (player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun() ? 1 : 0)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (rb.velocity.y * travelLimit.y);
        bobPosition.z = (walkInput.y * travelLimit.z);
    }


    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;

    void BobRotation() {
        if (bobSway == false) {
            bobEulerRotation = Vector3.zero;
            return;
        }

        bobEulerRotation.x = (walkInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) :
                                                            multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (walkInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (walkInput != Vector2.zero ? multiplier.z * curveCos * walkInput.x : 0);
    }


    float smooth = 10f;
    float smoothRot = 12f;

    void CompositePositionRotation(){

        // Normalizing vertical chagnes
        // float currentZ = transform.localPosition.z;
        Vector3 newPos = swayPos + bobPosition;
        // float newZ = newPos.z;

        // float speedFactor = (newZ - currentZ) / 0.1f;

        transform.localPosition = new Vector3( 
                                    Mathf.Lerp(transform.localPosition.x, newPos.x, Time.deltaTime * smooth),   // x
                                    transform.localPosition.y,                                                                          // y
                                    Mathf.Lerp(transform.localPosition.z, newPos.z, Time.deltaTime * smooth));  // z

        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
    }

}
