using System;
using UnityEngine;
public enum OperatorType { Add, Multiply, Percentage}

public class StatModifierPickup : Pickup
{

    [SerializeField] StatType type = StatType.ProjectileDamage;
    [SerializeField] OperatorType operatorType = OperatorType.Add;
    [SerializeField] float value = 2;
    [SerializeField] int duration = -1;

    protected override void ApplyPickupEffect(Entity entity)
    {
        float baseStatValue = entity.Stats.BaseValueForStat(type);
        StatModifier modifier = operatorType switch
        {
            OperatorType.Add => new BasicStatsModifier(type, duration, v => v + value),
            OperatorType.Multiply => new BasicStatsModifier(type, duration, v => v * value),
            OperatorType.Percentage => new BasicStatsModifier(type, duration, v => v + baseStatValue * value),
            _ => throw new ArgumentOutOfRangeException()
        };

        entity.Stats.Mediator.AddModifier(modifier);
    }
}
