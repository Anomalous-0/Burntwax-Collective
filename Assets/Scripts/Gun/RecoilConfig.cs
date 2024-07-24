using UnityEngine;

[CreateAssetMenu(fileName = "Recoil Config", menuName = "Guns/Recoil Config", order = 6)]
public class RecoilConfig : ScriptableObject
{
    public float RecoilTime = 0.05f;
    public float RecoveryTime = 0.25f;
    public float RecoilAngle = 0.1f;
    public float RecoilBack = 0.02f;
    

}
