using UnityEngine;
[RequireComponent(typeof(Destructable))]

public class SpawnParticalSystemOnDeath : MonoBehaviour {

    [SerializeField] private ParticleSystem particles;
    public Destructable damageable;


    private void Awake() {
        damageable = GetComponent<Destructable>();

    }
    private void OnEnable() {
        damageable.OnDestruct += Destructable_OnDestruct;
    }

    private void Destructable_OnDestruct(Vector3 Position) {
        Instantiate(particles, Position, Quaternion.identity);
    }
}
