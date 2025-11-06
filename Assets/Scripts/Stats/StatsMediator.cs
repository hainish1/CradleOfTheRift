using System;
using System.Collections.Generic;
using UnityEngine;




// this will sit in between the stat class and all of the stat modifiers
public class StatsMediator
{
    readonly LinkedList<StatModifier> modifiers = new();

    public event EventHandler<Query> Queries;

    public void PerformQuery(object sender, Query query) => Queries?.Invoke(sender, query);

    public void AddModifier(StatModifier modifier)
    {
        modifiers.AddLast(modifier);
        Queries += modifier.Handle;

        modifier.OnDispose += _ =>
        {
            modifiers.Remove(modifier);
            Queries -= modifier.Handle;
        };
    }

    public void Update(float deltaTime)
    {
        // update all stat modifiers with delta time
        var node = modifiers.First;

        // update all the modifiers
        while (node != null)
        {
            var modifier = node.Value;
            modifier.Update(deltaTime);
            node = node.Next;
        }

        // Dispose modifiers that are finished, mark and sweep
        node = modifiers.First;
        while (node != null)
        {
            var nextNode = node.Next; // keep track of next node in case we remove this one

            if (node.Value.MarkedForRemoval)
            {
                node.Value.Dispose();
            }

            node = nextNode;
        }
    }
}

public class BasicStatsModifier : StatModifier
{
    readonly StatType type ;
    readonly Func<float, float> operation;

    public BasicStatsModifier(StatType type, float duration, Func<float, float> operation) : base(duration)
    {
        this.type = type;
        this.operation = operation;
    }

    public override void Handle(object sender, Query query)
    {
        if (query.StatType == type)
        {
            query.Value = operation(query.Value);
        }
    }
}


public class Query
{
    public readonly StatType StatType;
    public float Value;

    public Query(StatType statType, float value)
    {
        StatType = statType;
        Value = value;
    }
}

