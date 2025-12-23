using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]

public class PlayerAmmo : MonoBehaviour
{


    [SerializeField] private GunStateMachine gunSelector;
    [SerializeField] private TextMeshProUGUI ammoText;



    void Update()
    {
        if (gunSelector.ActiveGunScriptable != null)
        {
            ammoText.SetText($"{gunSelector.ActiveGunScriptable.AmmoConfig.currentClip} / {gunSelector.ActiveGunScriptable.AmmoConfig.currentAmmo}");
        }
        else
        {
            ammoText.SetText("");
        }
    }
}
