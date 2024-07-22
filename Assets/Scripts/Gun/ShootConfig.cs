using UnityEngine;

[CreateAssetMenu(fileName = "Shoot Config", menuName = "Guns/Shoot Config", order = 2)]
public class ShootConfig : ScriptableObject
{
    public LayerMask HitMask;
    public Vector3 Spread = new Vector3(0.1f, 0.1f, 0.1f);
    public float FireRate = 1f;
    public float RecoveryTime = 0.25f;
    public float RecoilAmount = 0.00001f;
    public float RecoilTime = 0.05f;

}
