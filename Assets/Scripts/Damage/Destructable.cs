
using UnityEngine;

public interface Destructable  {
    public int CurrentHealth {get;}
    public int MaxHealth {get;}

    public delegate void TakeDamageEvent(int damage);
    public event TakeDamageEvent OnTakeDamage;

    public delegate void Destruct(Vector3 pos);
    public event Destruct OnDestruct;

    public void TakeDamage(int damage);
}
