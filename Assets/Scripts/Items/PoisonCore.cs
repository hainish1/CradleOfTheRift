using System;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCore : IDisposable
{
    private class Contribution
    {
        public int stacks;
    }

    private const int MaxStacksOnTarget = 10;
    private const float BaseDamagePerTickValue = 2f;
    private const float DamagePerStackValue = 2f;
    private const float TickIntervalValue = 1f;
    private const float DurationValue = 4f;

    private readonly Dictionary<ItemData, Contribution> contributions = new();

    private float baseDamagePerTick;
    private float damagePerStack;
    private float tickInterval = TickIntervalValue;
    private float duration = DurationValue;
    private int totalStacks;
    private bool disposed;

    public static PoisonCore Active { get; private set; }

    public PoisonCore()
    {
        Active = this;
    }

    public float BaseDamagePerTick => baseDamagePerTick;
    public float DamagePerStack => damagePerStack;
    public float TickInterval => tickInterval;
    public float Duration => duration;
    public int MaxStacks => MaxStacksOnTarget;
    public bool HasData => totalStacks > 0;
    public bool IsEmpty => contributions.Count == 0;

    public void AddContribution(ItemData data, int stacksAdded)
    {
        if (disposed || data == null || stacksAdded == 0) return;

        if (!contributions.TryGetValue(data, out var entry))
        {
            entry = new Contribution
            {
                stacks = 0
            };
            contributions[data] = entry;
        }

        entry.stacks += stacksAdded;
        if (entry.stacks <= 0)
            contributions.Remove(data);

        RecalculateTotals();
    }

    public void RemoveContribution(ItemData data)
    {
        if (disposed || data == null) return;
        if (contributions.Remove(data))
            RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        totalStacks = 0;

        foreach (var kv in contributions)
        {
            var entry = kv.Value;
            totalStacks += entry.stacks;
        }

        if (totalStacks <= 0)
        {
            baseDamagePerTick = 0f;
            damagePerStack = 0f;
            tickInterval = TickIntervalValue;
            duration = DurationValue;
            return;
        }

        baseDamagePerTick = BaseDamagePerTickValue * totalStacks;
        damagePerStack = DamagePerStackValue * totalStacks;
        tickInterval = TickIntervalValue;
        duration = DurationValue;
    }

    public void ApplyTo(Enemy enemy, Entity source, bool applyImmediately)
    {
        if (disposed || enemy == null || source == null) return;
        if (!HasData) return;

        var dotDebuff = enemy.GetComponent<DotDebuff>();
        if (!dotDebuff)
            dotDebuff = enemy.gameObject.AddComponent<DotDebuff>();

        dotDebuff.AddDot(
            baseDamagePerTick: baseDamagePerTick,
            damagePerStack: damagePerStack,
            tickInterval: tickInterval,
            duration: duration,
            source: source,
            canStack: true,
            id: "poison",
            maxStacks: MaxStacksOnTarget,
            applyImmediately: applyImmediately
        );
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        contributions.Clear();
        if (Active == this)
            Active = null;
    }
}

