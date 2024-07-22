using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerShoot : MonoBehaviour {
    [SerializeField] private PlayerGunSelector GunSelector;
    public InputManager input;

    private void Update() {
        if(GunSelector.ActiveGun != null) {
            bool aiming = false;
            bool shooting = false;

            if(input.aimIsPressed) {
                aiming = true;
            }
            if (input.shootIsPressed) {
                shooting = true;
            }
            
            GunSelector.ActiveGun.Tick(aiming, shooting);
        }   
    }
}
