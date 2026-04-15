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

    private float? overrideDamagePerTick;
    private float? overrideDamagePerStack;
    private float? overrideTickInterval;
    private float? overrideDuration;
    private bool? overrideCanStack;
    private int? overrideMaxStacks;
    private bool? overrideApplyImmediately;

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
        // Allow Fire-element hits (e.g. Staff fireball) to trigger poison on-hit effects.
        ElementSystem.AddTempRule(ElementType.Fire, ElementType.Poison);
    }

    public float BaseDamagePerTick => baseDamagePerTick;
    public float DamagePerStack => damagePerStack;
    public float TickInterval => tickInterval;
    public float Duration => duration;
    public int MaxStacks => GetMaxStacks();
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

    public void SetDotParams(float damagePerTick, float damagePerStack, float tickInterval, float duration, bool canStack, int maxStacks, bool applyImmediately)
    {
        overrideDamagePerTick = damagePerTick;
        overrideDamagePerStack = damagePerStack;
        overrideTickInterval = tickInterval;
        overrideDuration = duration;
        overrideCanStack = canStack;
        overrideMaxStacks = maxStacks;
        overrideApplyImmediately = applyImmediately;
        RecalculateTotals();
    }

    public void ClearDotParams()
    {
        overrideDamagePerTick = null;
        overrideDamagePerStack = null;
        overrideTickInterval = null;
        overrideDuration = null;
        overrideCanStack = null;
        overrideMaxStacks = null;
        overrideApplyImmediately = null;
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
            tickInterval = overrideTickInterval ?? TickIntervalValue;
            duration = overrideDuration ?? DurationValue;
            return;
        }

        float baseVal = overrideDamagePerTick ?? BaseDamagePerTickValue;
        float stackVal = overrideDamagePerStack ?? DamagePerStackValue;
        baseDamagePerTick = baseVal * totalStacks;
        damagePerStack = stackVal * totalStacks;
        tickInterval = overrideTickInterval ?? TickIntervalValue;
        duration = overrideDuration ?? DurationValue;
    }

    private int GetMaxStacks() => overrideMaxStacks ?? MaxStacksOnTarget;
    private bool GetCanStack() => overrideCanStack ?? true;

    public void ApplyTo(Enemy enemy, Entity source, bool applyImmediately)
    {
        if (disposed || enemy == null || source == null) return;
        if (!HasData) return;

        var dotDebuff = enemy.GetComponent<DotDebuff>();
        if (!dotDebuff)
            dotDebuff = enemy.gameObject.AddComponent<DotDebuff>();

        bool useApplyImmediately = overrideApplyImmediately ?? applyImmediately;
        dotDebuff.AddDot(
            baseDamagePerTick: baseDamagePerTick,
            damagePerStack: damagePerStack,
            tickInterval: tickInterval,
            duration: duration,
            source: source,
            canStack: GetCanStack(),
            id: "poison",
            maxStacks: GetMaxStacks(),
            applyImmediately: useApplyImmediately
        );
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        contributions.Clear();
        ElementSystem.RemoveTempRule(ElementType.Fire, ElementType.Poison);
        if (Active == this)
            Active = null;
    }
}

