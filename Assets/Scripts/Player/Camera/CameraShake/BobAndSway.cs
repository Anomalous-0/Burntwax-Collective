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


    // Remove all these bools in final release
    [Header("Toggles")]
    public bool sway = true;
    public bool swayRotation = true;
    public bool bobOffset = true;
    public bool bobSway = true;


    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;


    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;


    [Header("Bobbing")]
    public float speedCurve;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }
    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;
    private float lastTime = 0;
    private Vector3 originalBobPos;


    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;


    // Specialized Input Vectors
    Vector2 walkInput;
    Vector2 lookInput;


    // Private composite motion fields
    float smooth = 10f;
    float smoothRot = 12f;


    // Booleans that are often used
    bool onSolid;


    void Update() {
        GetInput();

        onSolid = player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun() || player.currentState == player.states.Slope();
        // SHOULD I USE PRIORITY QUEUE HERE INSTEAD OF BASIC SHOOTISPRESSED
        if (input.MoveInput != Vector2.zero && onSolid && cam.currentState != cam.states.Aim() && !input.shootIsPressed) {
            Sway();
        }
        else {
            speedCurve = 0;
        }

        SwayRotation();

        if (cam.currentState != cam.states.Aim() && !input.shootIsPressed) {
            BobOffset();
            BobRotation();
            originalBobPos = bobPosition;
        } else {
            transform.localPosition = Vector3.zero;
            bobPosition = Vector3.zero;
            speedCurve = 0;
        }

        CompositePositionRotation();
    }


    void GetInput() {
        walkInput.x = input.MoveInput.x;
        walkInput.y = input.MoveInput.y;
        walkInput = walkInput.normalized;

        lookInput.x = input.LookInput.x;
        lookInput.y = input.LookInput.y;
    }


    void Sway()
    {
        if (sway == false)
        {
            swayPos = Vector3.zero;
            return;
        }

        Vector3 invertLook = lookInput * -step;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }
    

    void SwayRotation() {
        if (swayRotation == false) 
        {
            swayEulerRot = Vector3.zero;
            return;
        }

        Vector2 invertLook = lookInput * -rotationStep;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);

        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }


    void BobOffset() {

        if (bobOffset == false)
        {
            bobPosition = Vector3.zero;
            return;
        }

        if (onSolid && !input.shootIsPressed) {
            lastTime = Time.deltaTime;
        }

        speedCurve += 1.5f * lastTime * (onSolid ? rb.velocity.magnitude : 1f);

        int shouldBobX = (player.currentState == player.states.Grounded() || player.currentState == player.states.Wallrun() ? 1 : 0);

        bobPosition.x = (curveCos * bobLimit.x * shouldBobX) - (walkInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (rb.velocity.y * travelLimit.y);
        bobPosition.z = (walkInput.y * travelLimit.z);
    }


    void BobRotation() {
        if (bobSway == false) {
            bobEulerRotation = Vector3.zero;
            return;
        }

        bobEulerRotation.x = (walkInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (walkInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (walkInput != Vector2.zero ? multiplier.z * curveCos * walkInput.x : 0);
    }


    void CompositePositionRotation(){

        Vector3 newPos = swayPos + bobPosition;

        transform.localPosition = new Vector3(  Mathf.Lerp(transform.localPosition.x, newPos.x, Time.deltaTime * smooth),
                                                transform.localPosition.y,
                                                Mathf.Lerp(transform.localPosition.z, newPos.z, Time.deltaTime * smooth));

        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
    }

}
