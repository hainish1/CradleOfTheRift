using UnityEngine;


// any class implementing this interface will have to give definition for TakeDamage()
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(int damage);

}
