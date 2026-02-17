using System;
using UnityEngine;

public static class CombatEvents 
{
    public static event Action<Entity, Component, float, ElementType> DamageDealt;

    public static void ReportDamage(Entity attacker, Component target, float damage)
    {
        ReportDamage(attacker, target, damage, ElementType.None);
    }

    public static void ReportDamage(Entity attacker, Component target, float damage, ElementType element)
    {
        DamageDealt?.Invoke(attacker, target, damage, element);
    }
}
