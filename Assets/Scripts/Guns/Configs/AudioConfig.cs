using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Ammo Config", menuName = "Guns/Ammo Config", order = 3)]
public class AmmoConfig : ScriptableObject {
    
    public int maxAmmo;
    public int clipSize;

    public int currentAmmo;
    public int currentClip;


    void OnEnable() {
        currentClip = clipSize;
        currentAmmo = maxAmmo;
    }

    public void Reload() {
        int maxReloadAmount = Mathf.Min(clipSize, currentAmmo);
        int availableBullets = clipSize - currentClip;
        int reloadAmount = Mathf.Min(maxReloadAmount, availableBullets);
        currentClip = currentClip + reloadAmount;
        currentAmmo -= reloadAmount;
    }


    public bool CanReload() {
        return currentClip < clipSize && currentAmmo > 0;
    }


}
