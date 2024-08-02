using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
 
public class PlayerAmmo : MonoBehaviour {
  

    [SerializeField ] private PlayerGunSelector gunSelector;
    [SerializeField] private TextMeshProUGUI ammoText;

    

    void Update() {
        if(gunSelector.ActiveGun != null) {
            ammoText.SetText($"{gunSelector.ActiveGun.AmmoConfig.currentClip} / {gunSelector.ActiveGun.AmmoConfig.currentAmmo}");
        }
    }
}
