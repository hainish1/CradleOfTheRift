using System;
using UnityEngine;

// Handles DOT for items like poison, burn etc
// Listens to player damage events and applies DOT to enemies
public class DotOnHit : IDisposable
{
    private readonly Entity owner;
    private readonly float dotDamagePerTick;
    private readonly float dotTickInterval;
    private readonly float dotDuration;
    private readonly float dotDamagePerStack;  // extra dmg per item stack
    private int stacks; 
    private readonly float duration; // -1 = perm item
    private float timer;
    private bool disposed;
    private readonly bool dotCanStack;
    private readonly string dotId;  
    private readonly int dotMaxStacks;
    private readonly bool dotApplyImmediately;  // first tick instant

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
        this.stacks = Mathf.Max(1, initialStacks);  // at least 1 stack
        this.duration = durationSec;
        this.timer = durationSec;
        this.dotCanStack = dotCanStack;
        this.dotId = "poison";  
        this.dotMaxStacks = dotMaxStacks;
        this.dotApplyImmediately = dotApplyImmediately;

        // subscribe to damage events
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

        // prevent DOT damage from triggering more DOT 
        if (DotDebuff.IsProcessingDotDamage) return;

        var enemy = target as Enemy;
        if (!enemy) return;

        // add or get the DOT manager on the enemy
        var dotDebuff = enemy.GetComponent<DotDebuff>();
        if (!dotDebuff)
            dotDebuff = enemy.gameObject.AddComponent<DotDebuff>();

        dotDebuff.AddDot(
            baseDamagePerTick: dotDamagePerTick,
            damagePerStack: dotDamagePerStack,
            tickInterval: dotTickInterval,
            duration: dotDuration,
            source: owner,
            canStack: dotCanStack,
            id: dotId,
            maxStacks: dotMaxStacks,
            applyImmediately: dotApplyImmediately
        );

        string immediateText = dotApplyImmediately ? "instant" : "delayed";
        Debug.Log($"[DotOnHit] Applied to {enemy.name}: {dotDamagePerTick}dmg/tick + {dotDamagePerStack} per stack for {dotDuration}s ({immediateText}, item stacks: {stacks})");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
        Debug.Log("[DotOnHit] Disposed");
    }
}

