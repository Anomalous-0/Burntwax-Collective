using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerShoot : MonoBehaviour {
    [SerializeField] private PlayerGunSelector GunSelector;
    public InputManager input;

    private void Update() {
        if(GunSelector.ActiveGun != null) {
           if (input.aimIsPressed) {
                // GunSelector.ActiveGun.ADS();
            } else {
                // GunSelector.ActiveGun.DeADS();
            }

            if (input.shootIsPressed) {
                GunSelector.ActiveGun.Shoot();
            }
            
        }   
    }
}
