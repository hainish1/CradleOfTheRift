using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public bool canAttack = true;
    public float timeBetweenAttacks;

    public abstract void Fire();

    public void Awake()
    {
        ResetAttack();
    }

    public void StartCoolDown()
    {
        canAttack = false;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }

    public void ResetAttack()
    {
        canAttack = true;
    }
}
