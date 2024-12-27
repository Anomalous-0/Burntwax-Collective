using UnityEngine;
[RequireComponent(typeof(Damageable))]

public class SpawnParticalSystemOnDeath : MonoBehaviour {

    [SerializeField] private ParticleSystem particles;
    public Damageable damageable;


    private void Awake() {
        damageable = GetComponent<Damageable>();

    }
    private void OnEnable() {
        damageable.OnDeath += Damageable_OnDeath;
    }

    private void Damageable_OnDeath(Vector3 Position) {
        Instantiate(particles, Position, Quaternion.identity);
    }
}
