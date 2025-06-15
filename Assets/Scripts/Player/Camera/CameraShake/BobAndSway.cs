using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BobAndSway : MonoBehaviour
{
    [Header("External References")]
    [SerializeField] InputManager input;
    [SerializeField] PlayerStateMachine player;
    [SerializeField] GunStateMachine gun;
    [SerializeField] CamStateMachine cam;
    [SerializeField] Rigidbody rb;

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
    private float stepTimer;
    public float bobBaseFreq = 5f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    public AnimationCurve verticalAmplitudeCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.25f, 0.8f),
        new Keyframe(0.5f, 1.2f),
        new Keyframe(0.75f, 0.9f),
        new Keyframe(1f, 1f)
    );
    public AnimationCurve rollCurve = new AnimationCurve(
        new Keyframe(0f, 0.5f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(0.75f, 0f),
        new Keyframe(1f, 0.5f)
    );
    Vector3 bobPosition;

    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;

    [Header("Velocity Sway")]
    public float velocitySwayIntensity = 0.002f;
    public float velocitySwaySmooth = 6f;
    private Vector3 velocitySway;
    private Vector3 velocitySwaySpring;
    private Vector3 lastVelocity;

    [Header("Falling Inertia")]
    public float fallUpOffsetAmount = 0.04f;
    public float fallSpringReturnSpeed = 4f;
    public float landingJoltMultiplier = 0.2f;
    public float maxFallOffset = 0.08f;
    public float maxAirTimeForJoltScale = 1.5f;

    private Vector3 fallOffset;
    private Vector3 landingVelocityImpulse;
    private float landingJoltTimer;
    private float landingJoltDuration = 0.3f;

    private bool wasGroundedLastFrame = true;
    private float airTime = 0f;

    Vector2 walkInput;
    Vector2 lookInput;

    float smooth = 10f;
    float smoothRot = 12f;

    bool onSolid;

    void Update()
    {
        GetInput();

        onSolid = player.currentState == player.states.Grounded()
                  || player.currentState == player.states.Wallrun()
                  || player.currentState == player.states.Slope();

        if (input.MoveInput != Vector2.zero && onSolid && cam.currentState != cam.states.Aim() && gun.CurrentState != gun.states.Shoot())
        {
            Sway();
        }
        else
        {
            swayPos = Vector3.zero;
        }

        SwayRotation();

        bool shouldBob = cam.currentState != cam.states.Aim() && gun.CurrentState != gun.states.Shoot() && landingJoltTimer <= 0f;
        if (shouldBob)
        {
            BobOffset();
            BobRotation();
        }
        else
        {
            bobPosition = Vector3.Lerp(bobPosition, Vector3.zero, Time.deltaTime * smooth);
            bobEulerRotation = Vector3.Lerp(bobEulerRotation, Vector3.zero, Time.deltaTime * smooth);
        }

        Vector3 velocity = rb.velocity;
        Vector3 deltaVelocity = velocity - lastVelocity;
        velocitySway = Vector3.Lerp(velocitySway, deltaVelocity * velocitySwayIntensity, Time.deltaTime * velocitySwaySmooth);
        velocitySwaySpring = Vector3.Lerp(velocitySwaySpring, -velocitySway, Time.deltaTime * velocitySwaySmooth);
        lastVelocity = velocity;

        if (!onSolid)
        {
            airTime += Time.deltaTime;
            float upwardFloat = Mathf.Clamp(airTime * fallUpOffsetAmount, 0, maxFallOffset);
            fallOffset = Vector3.Lerp(fallOffset, new Vector3(0, upwardFloat, 0), Time.deltaTime * fallSpringReturnSpeed);
            landingVelocityImpulse = Vector3.Lerp(landingVelocityImpulse, Vector3.zero, Time.deltaTime * fallSpringReturnSpeed);
        }
        else
        {
            if (!wasGroundedLastFrame && airTime > 0.05f)
            {
                float clampedAirTime = Mathf.Clamp(airTime, 0, maxAirTimeForJoltScale);
                float impactStrength = clampedAirTime / maxAirTimeForJoltScale;
                landingVelocityImpulse = new Vector3(0, -impactStrength * landingJoltMultiplier, 0);
                landingJoltTimer = landingJoltDuration;
            }

            airTime = 0f;
            fallOffset = Vector3.Lerp(fallOffset, Vector3.zero, Time.deltaTime * fallSpringReturnSpeed);
            landingVelocityImpulse = Vector3.Lerp(landingVelocityImpulse, Vector3.zero, Time.deltaTime * fallSpringReturnSpeed);
        }

        if (landingJoltTimer > 0f)
        {
            landingJoltTimer -= Time.deltaTime;
        }

        wasGroundedLastFrame = onSolid;

        CompositePositionRotation();
    }

    void GetInput()
    {
        walkInput.x = input.MoveInput.x;
        walkInput.y = input.MoveInput.y;
        walkInput = walkInput.normalized;

        lookInput.x = input.LookInput.x;
        lookInput.y = input.LookInput.y;
    }

    void Sway()
    {
        if (!sway) return;
        Vector3 invertLook = lookInput * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);
        swayPos = invertLook;
    }

    void SwayRotation()
    {
        if (!swayRotation) return;
        Vector2 invertLook = lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void BobOffset()
    {
        if (!bobOffset || input.MoveInput == Vector2.zero || !onSolid)
        {
            bobPosition = Vector3.Lerp(bobPosition, Vector3.zero, Time.deltaTime * smooth);
            return;
        }

        stepTimer += Time.deltaTime * rb.velocity.magnitude * bobBaseFreq;
        if (stepTimer > 1f) stepTimer -= 1f;

        float horizontal = Mathf.Sin(stepTimer * 2 * Mathf.PI);
        float verticalBase = Mathf.Sin(stepTimer * 4 * Mathf.PI);
        float amplitude = verticalAmplitudeCurve.Evaluate(stepTimer);
        float vertical = verticalBase > 0 ? verticalBase * 0.5f : verticalBase * amplitude;

        bobPosition = new Vector3(horizontal * bobLimit.x, vertical * bobLimit.y, 0);
    }

    void BobRotation()
    {
        if (!bobSway || input.MoveInput == Vector2.zero || !onSolid)
        {
            bobEulerRotation = Vector3.Lerp(bobEulerRotation, Vector3.zero, Time.deltaTime * smooth);
            return;
        }

        float roll = (rollCurve.Evaluate(stepTimer) - 0.5f) * 2f;
        bobEulerRotation = new Vector3(0, 0, roll * multiplier.z);
    }

    void CompositePositionRotation()
    {
        Vector3 newPos = swayPos + bobPosition + velocitySwaySpring + fallOffset + landingVelocityImpulse;
        transform.localPosition = Vector3.Lerp(transform.localPosition, newPos, Time.deltaTime * smooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
    }
}
