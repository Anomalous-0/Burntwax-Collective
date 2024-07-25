using UnityEngine;

[CreateAssetMenu(fileName = "Recoil Config", menuName = "Guns/Recoil Config", order = 2)]
public class RecoilConfig : ScriptableObject
{
    
    public float RecoilTime = 0.05f;
    public float RecoilAngle = 0.2f;
    public float RecoilDistance = 0.2f;
    public float RecoveryTime = 0.25f;
    

}
