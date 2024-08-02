using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerShoot : MonoBehaviour {

   
    [SerializeField] private PlayerGunSelector GunSelector;
    public InputManager input;

    public PlayerStateMachine stateMachine;

    private bool autoReload = true;
    private bool isReloading = false;


    void Start() {
        stateMachine = GetComponent<PlayerStateMachine>();
    }



    private void Update() {
        

        if(GunSelector.ActiveGun != null) {
            CheckPos();

            
            if(ManualReload() || AutoReload()) {
                Debug.Log("Reload");
                isReloading = true;
                GunSelector.animator.SetTrigger(GunSelector.reloadHash);
                GunSelector.ActiveGun.AmmoConfig.Reload();
            }


            // LOOK AT THIS THIS SHIT IMPORTANT MAYBE FIX LATER 
            if (GunSelector.ActiveGun.AmmoConfig.currentClip > 0 && stateMachine.currentState.CurrentSubstate() != stateMachine.states.Sprint() &&  input.shootIsPressed && Time.time - GunSelector.ActiveGun.LastShootTime >= GunSelector.ActiveGun.ShootConfig.FireRate) {
                
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

    private void EndReload() {
        
        GunSelector.ActiveGun.EndReload();
        isReloading = false;
    }

    private bool ManualReload() {

        return input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
        
        // return  !isReloading && input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
    }

    private bool AutoReload() {
        return autoReload && GunSelector.ActiveGun.CanReload() && GunSelector.ActiveGun.AmmoConfig.currentClip == 0;
        // return !isReloading && autoReload && GunSelector.ActiveGun.CanReload() && GunSelector.ActiveGun.AmmoConfig.currentClip == 0;
    }

    private void CheckPos() {
        if (input.aimIsPressed) {
                GunSelector.ActiveGun.ADS();
        } 
        else if(stateMachine.currentState.CurrentSubstate() == stateMachine.states.Sprint()){
            GunSelector.ActiveGun.Stow();
                
        }
        else {
            GunSelector.ActiveGun.Rest();
        }
    }
}
