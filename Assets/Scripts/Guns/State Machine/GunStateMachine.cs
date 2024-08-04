using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GunStateMachine : MonoBehaviour {

   
    public PlayerGunSelector GunSelector;
    public InputManager input;


    public PlayerStateMachine stateMachine;

    public GunBaseState currentState;
    public GunStateFactory states;

  
    public GunBaseState CurrentState {get { return currentState; } set { currentState = value; } }


    public bool isReloading = false;
    public bool isShooting = false;
    



   


    void Awake() {
        states = new GunStateFactory(this);
        currentState = states.Rest();
        currentState.EnterState();
    }

    void Start() {
        stateMachine = GetComponent<PlayerStateMachine>();
    }



    private void Update() {
        if(GunSelector.ActiveGun != null) {
            currentState.UpdateStates();
        }
        //     CheckPos();

            
        //     if(ManualReload() || AutoReload()) {
        //         Debug.Log("Reload");
        //         //isReloading = true;
        //         GunSelector.animator.SetTrigger(GunSelector.reloadHash);
        //         GunSelector.ActiveGun.AmmoConfig.Reload();
        //     }


        //     // LOOK AT THIS THIS SHIT IMPORTANT MAYBE FIX LATER 
        //     if (GunSelector.ActiveGun.AmmoConfig.currentClip > 0 && stateMachine.currentState.CurrentSubstate() != stateMachine.states.Sprint() &&  input.shootIsPressed && Time.time - GunSelector.ActiveGun.LastShootTime >= GunSelector.ActiveGun.ShootConfig.FireRate) {
                
        //         // GunSelector.animator.SetBool(GunSelector.restingHash, false);
        //         if (input.aimIsPressed) {
        //             GunSelector.animator.SetTrigger(GunSelector.aimShootHash);

        //         }
        //         else {
                   
        //             GunSelector.animator.SetTrigger(GunSelector.shootHash);
        //         }
                
        //         GunSelector.ActiveGun.Shoot();
        //     }
        //     else {
        //         // GunSelector.animator.SetBool(GunSelector.restingHash, true);
        //     }
        // }
     

    }


    public bool CanShoot() {
        return GunSelector.ActiveGun.AmmoConfig.currentClip > 0;
    }

    public void EndReload() {
        
        GunSelector.ActiveGun.EndReload();
        // isReloading = false;
    }

    public bool ManualReload() {

        return input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
        
        // return  !isReloading && input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
    }

    public bool AutoReload() {
        return GunSelector.ActiveGun.CanReload() && GunSelector.ActiveGun.AmmoConfig.currentClip == 0;
        // return !isReloading && autoReload && GunSelector.ActiveGun.CanReload() && GunSelector.ActiveGun.AmmoConfig.currentClip == 0;
    }


}
