using System;
using UnityEngine;

public class StatModifierPickup : Pickup
{
    public enum OperatorType { Add, Multiply }

    [SerializeField] StatType type = StatType.ProjectileDamage;
    [SerializeField] OperatorType operatorType = OperatorType.Add;
    [SerializeField] int value = 2;
    [SerializeField] int duration = -1;

    protected override void ApplyPickupEffect(Entity entity)
    {
        StatModifier modifier = operatorType switch
        {
            OperatorType.Add => new BasicStatsModifier(type, duration, v => v + value),
            OperatorType.Multiply => new BasicStatsModifier(type, duration, v => v * value),
            _ => throw new ArgumentOutOfRangeException()
        };

        entity.Stats.Mediator.AddModifier(modifier);
    }
}
