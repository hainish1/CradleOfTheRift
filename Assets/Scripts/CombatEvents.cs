using System;
using UnityEngine;

public static class CombatEvents 
{
    public static event Action<Entity, Component, float> DamageDealt;

    public static void ReportDamage(Entity attacker, Component target, float damage)
    {
        DamageDealt?.Invoke(attacker, target, damage);
    }
}
