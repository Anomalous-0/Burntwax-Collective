using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {

    public InputManager input;
    public Animator animator;
    public 
    float velocityZ = 0.0f;
    float velocityX = 0.0f;
    float velocity = 0.0f;

    int isAimingHash;
    int isJumpingHash;

    public float acceleration = 3.0f;
    public float deceleration = 4.0f;

    void Start() {
        animator = GetComponent<Animator>();
        isAimingHash = Animator.StringToHash("isAiming");
        isJumpingHash = Animator.StringToHash("isJumping");
    }

    // Update is called once per frame
    void Update() {

        bool jumpPressed = input.jumpIsPressed;
        bool aimPressed = input.aimIsPressed;
        //bool isAiming = animator.GetBool(isAimingHash);
        bool forwardPressed = Input.GetKey("w");
            bool leftPressed = Input.GetKey("a");
            bool rightPressed = Input.GetKey("d");
            bool backPressed = Input.GetKey("s");
            bool isWalking = input.moveIsPressed;
            bool sprintPressed = input.sprintIsPressed;

        // if(jumpPressed) {
        //     animator.SetBool(isJumpingHash, true);
        // }
        // else {
        //     animator.SetBool(isJumpingHash, false);
        // }

        if(aimPressed) {
            // animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 1f, Time.deltaTime * 10f));

            animator.SetBool(isAimingHash, true);
            if(forwardPressed && velocityZ < 0.5f) {
                velocityZ += Time.deltaTime * 2 * acceleration;
            }

            if(backPressed && velocityZ > -0.5f) {
                velocityZ -= Time.deltaTime * 2 * acceleration;
            }

            if(!forwardPressed && velocityZ > 0.0f) {
                velocityZ -= Time.deltaTime * deceleration;
            }

            if(!backPressed && velocityZ < 0.0f) {
                velocityZ += Time.deltaTime * deceleration;
            }

            if(!forwardPressed && !backPressed && velocityZ != 0.0f && (velocityZ > -0.05f && velocityZ < 0.05f)) {
                velocityZ = 0.0f;
            }

            if(leftPressed && velocityX > -0.5f) {
                velocityX -= Time.deltaTime * acceleration;
            }

            if(rightPressed && velocityX < 0.5f) {
                velocityX += Time.deltaTime * acceleration;
            }

            if(!leftPressed && velocityX < 0.0f) {
                velocityX += Time.deltaTime * deceleration;
            }

            if(!rightPressed && velocityX > 0.0f) {
                velocityX -= Time.deltaTime * deceleration;
            }

            if(!leftPressed && !rightPressed && velocityX != 0.0f && (velocityX > -0.05f && velocityX < 0.05f)) {
                velocityX = 0.0f;
            }

            animator.SetFloat("VelocityZ", velocityZ);
            animator.SetFloat("VelocityX", velocityX);
        } else if(!aimPressed){
            // animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), 0f, Time.deltaTime * 10f));

            animator.SetBool(isAimingHash, false);
            velocityX = 0.0f;
            velocityZ = 0.0f;
                
            if(isWalking && velocity < 0.5f && !sprintPressed) {
                velocity += Time.deltaTime * acceleration;
            }

            if(!isWalking && velocity > 0.0f) {
                velocity -= Time.deltaTime * deceleration;
            }

            if(sprintPressed && isWalking && velocity < 1.0f) {
                velocity += Time.deltaTime * acceleration;
            }

            if(!sprintPressed && velocity > 0.5f) {
                velocity -= Time.deltaTime * deceleration;
            }

            if(velocity != 0.5f && velocity > 4.7f && velocity < 5.5f) {
                velocity = 0.5f;
            }

            animator.SetFloat("Velocity", velocity);
        }
        
    }
}
