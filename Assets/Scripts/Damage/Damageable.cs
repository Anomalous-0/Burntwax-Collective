
using UnityEngine;

public interface Damageable  {
    public int CurrentHealth {get;}
    public int MaxHealth {get;}

    public delegate void DamageEvent(int damage);
    public event DamageEvent OnTakeDamage;

    public delegate void DeathEvent(Vector3 pos);
    public event DeathEvent OnDeath;

    public void TakeDamage(int damage);
}
