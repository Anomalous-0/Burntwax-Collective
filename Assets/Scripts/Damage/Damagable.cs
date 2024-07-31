
using UnityEngine;

public interface Damagable  {
    public int currentHealth {get;}
    public int maxHealth {get;}

    public delegate void TakeDamage(int damage);
    public event TakeDamage OnTakeDamage;
}
