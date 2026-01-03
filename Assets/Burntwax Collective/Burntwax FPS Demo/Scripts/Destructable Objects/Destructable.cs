
using UnityEngine;

public interface Destructable
{
    public float CurrentHealth{get; set;}
    public float MaxHealth { get; }
    public void TakeDamage(float damage);

    public void Destruct(Vector3 pos);
}
