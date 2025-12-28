using UnityEngine;

[CreateAssetMenu(fileName = "Aim Config", menuName = "Guns/Aim Config", order = 5)]
public class AimConfig : ScriptableObject
{
    public Vector3 aimPoint;
    public Vector3 aimRotation;

    public float speed;

}
