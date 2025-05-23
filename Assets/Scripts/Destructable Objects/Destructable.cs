
using UnityEngine;

public interface Destructable  {
    public float CurrentHealth {get;}
    public float MaxHealth {get;}

    // public delegate void TakeDamageEvent(int damage);
    // public event TakeDamageEvent OnTakeDamage;

    // public delegate void Destruct(Vector3 pos);
    // public event Destruct OnDestruct;

    public void TakeDamage(float damage);

    public void Destruct(Vector3 pos);
}
