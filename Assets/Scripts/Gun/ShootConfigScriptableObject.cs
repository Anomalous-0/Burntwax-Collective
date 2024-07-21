using UnityEngine;

[CreateAssetMenu(fileName = "Shoot Config", menuName = "Guns/Shoot Config", order = 2)]
public class ShootConfigScriptableObject : ScriptableObject
{
    public LayerMask HitMask;
    public Vector3 Spread = new Vector3(0.1f, 0.1f, 0.1f);
    public float FireRate = 0.25f;
    public float RecoilRecoverySpeed = 1f;
    public float MaxSpreadTime = 1f;
    
    [Header("Simple Spread")]
    public BulletSpreadType SpreadType = BulletSpreadType.Simple;

    // [Header("Texture-Based Spread")]
    // [Range(0.001f, 5f)]
    // public float SpreadMultiplier = 0.1f;
    // public Texture2D SpreadTexture;


    public Vector3 GetSpread(float ShootTime = 0) {

        Vector3 spread = Vector3.zero;


        if (SpreadType == BulletSpreadType.Simple) {
            spread = Vector3.Lerp(
                Vector3.zero,
                new Vector3(
                    Random.Range(
                        -Spread.x,
                        Spread.x
                    ),
                    Random.Range(
                        -Spread.y,
                        Spread.y
                    ),
                    Random.Range(
                        -Spread.z,
                        Spread.z
                    )
                ),
                Mathf.Clamp01(ShootTime / MaxSpreadTime)
            );
        // } else if (SpreadType = BulletSpreadType.TextureBased) {
        //     spread = GetTextureDirection(ShootTime);
        //     spread *= SpreadMultiplier;
        }

        return spread;
    }

    // private Vector3 GetTextureDirection(float ShootTime) {

    // }

}
