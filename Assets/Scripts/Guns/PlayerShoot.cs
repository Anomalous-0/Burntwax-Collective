using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerShoot : MonoBehaviour {

   
    [SerializeField] private PlayerGunSelector GunSelector;
    public InputManager input;

    public PlayerStateMachine stateMachine;


    void Start() {
        stateMachine = GetComponent<PlayerStateMachine>();
    }



    private void Update() {
        

        if(GunSelector.ActiveGun != null) {
            if (input.aimIsPressed) {
                GunSelector.ActiveGun.ADS();
            } 
            else if(stateMachine.currentState.CurrentSubstate() == stateMachine.states.Sprint()){
                GunSelector.ActiveGun.Stow();
                
            }
            else {
                GunSelector.ActiveGun.Rest();
            }

            // GunSelector.ActiveGun.ReturnToInitial();



            // LOOK AT THIS THIS SHIT IMPORTANT MAYBE FIX LATER 
            if (stateMachine.currentState.CurrentSubstate() != stateMachine.states.Sprint() &&  input.shootIsPressed && Time.time - GunSelector.ActiveGun.LastShootTime >= GunSelector.ActiveGun.ShootConfig.FireRate) {
                
                // GunSelector.animator.SetBool(GunSelector.restingHash, false);
                if (input.aimIsPressed) {
                    GunSelector.animator.SetTrigger(GunSelector.aimShootHash);

                }
                else {
                   
                    GunSelector.animator.SetTrigger(GunSelector.shootHash);
                }
                
                GunSelector.ActiveGun.Shoot();
            }
            else {
                // GunSelector.animator.SetBool(GunSelector.restingHash, true);
            }
        }
     

    }
}
