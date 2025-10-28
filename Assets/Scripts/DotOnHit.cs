using System;
using UnityEngine;

/// <summary>
/// DOT item effect - applies damage over time on hit
/// </summary>
public class DotOnHit : IDisposable
{
    private readonly Entity owner;
    private readonly float dotDamagePerTick;
    private readonly float dotTickInterval;
    private readonly float dotDuration;
    private readonly float dotDamagePerStack;
    private int stacks;
    private readonly float duration; // -1 = permanent
    private float timer;
    private bool disposed;
    private readonly bool dotCanStack;
    private readonly string dotId;
    private readonly int dotMaxStacks;
    private readonly bool dotApplyImmediately;

    public DotOnHit(
        Entity owner, 
        float dotDamagePerTick, 
        float dotTickInterval, 
        float dotDuration,
        float dotDamagePerStack,
        int initialStacks = 1,
        float durationSec = -1f,
        bool dotCanStack = true,
        int dotMaxStacks = 5,
        bool dotApplyImmediately = false)
    {
        this.owner = owner;
        this.dotDamagePerTick = dotDamagePerTick;
        this.dotTickInterval = dotTickInterval;
        this.dotDuration = dotDuration;
        this.dotDamagePerStack = dotDamagePerStack;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;
        this.dotCanStack = dotCanStack;
        this.dotId = "poison";
        this.dotMaxStacks = dotMaxStacks;
        this.dotApplyImmediately = dotApplyImmediately;

        CombatEvents.DamageDealt += OnDamageDealt;

        string immediateText = dotApplyImmediately ? "instant" : $"delayed {dotTickInterval}s";
        Debug.Log($"[DotOnHit] Init: {dotDamagePerTick}dmg/tick, {dotTickInterval}s interval, {dotDuration}s, {stacks} stacks, max {dotMaxStacks} ({immediateText})");
    }

    public void AddStack(int count = 1)
    {
        stacks += Mathf.Max(1, count);
        Debug.Log($"[DotOnHit] Stacks: {stacks}");
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage)
    {
        if (disposed || attacker != owner) return;

        // Prevent DOT from triggering new DOT (anti-recursion)
        if (DotDebuff.IsProcessingDotDamage)
        {
            return;
        }

        var enemy = target as Enemy;
        if (enemy == null)
        {
            enemy = target.GetComponent<Enemy>();
        }

        if (enemy == null) return;

        var dotDebuff = enemy.GetComponent<DotDebuff>();
        if (dotDebuff == null)
        {
            dotDebuff = enemy.gameObject.AddComponent<DotDebuff>();
        }

        float totalDotDamage = dotDamagePerTick + (dotDamagePerStack * (stacks - 1));

        dotDebuff.AddDot(
            damagePerTick: totalDotDamage,
            tickInterval: dotTickInterval,
            duration: dotDuration,
            source: owner,
            canStack: dotCanStack,
            id: dotId,
            maxStacks: dotMaxStacks,
            applyImmediately: dotApplyImmediately
        );

        string immediateText = dotApplyImmediately ? "instant" : "delayed";
        Debug.Log($"[DotOnHit] Applied to {enemy.name}: {totalDotDamage}dmg/tick for {dotDuration}s ({immediateText}, item stacks: {stacks})");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
        Debug.Log("[DotOnHit] Disposed");
    }
}

