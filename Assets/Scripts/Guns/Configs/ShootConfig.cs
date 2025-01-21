using System;
using Burntwax.Guns;
using UnityEngine;

[CreateAssetMenu(fileName = "Shoot Config", menuName = "Guns/Shoot Config", order = 2)]
public class ShootConfig : ScriptableObject, ICloneable {
    public bool isHitscan = false; 
    public Bullet bulletPrefab;
    // public float bulletForce;
    public LayerMask HitMask;
    public Vector3 Spread = new Vector3(0.1f, 0.1f, 0.1f);
    // public float FireRate = 1f;

    public int bulletsPerShot = 1;
    public float bulletSpeed;

    public object Clone() {
        ShootConfig config = CreateInstance<ShootConfig>();
        Utilities.CopyValues(this, config);
        return config;
    }
}
