using UnityEngine;

[DisallowMultipleComponent]

public class DestructableObject : MonoBehaviour {

    public DestructableObjectHealth health;

    public DestructableObjectDamageResponse damageResponse;

    private void Start() {
        health.OnTakeDamage += damageResponse.HandlePain;
        health.OnDestruct += Destruct;
    }

    private void Destruct(Vector3 Position) {
        
        damageResponse.HandleDestruct();
    }
    
}
