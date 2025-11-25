using System;
using UnityEngine;

public class ExplosiveProjectiles : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float baseAoeRadius;
    private float baseAoeDamageMultiplier;
    private float baseMaxRange;

    public static float AoeRadius { get; private set; }
    public static float AoeDamageMultiplier { get; private set; }
    public static float MaxRange { get; private set; }
    public static bool IsEnabled { get; private set; }
    public static GameObject ExplosionVFX { get; private set; }

    public ExplosiveProjectiles(Entity owner, float aoeRadius, float aoeDamageMultiplier, float maxRange, int initialStacks, float durationSec = -1f, GameObject explosionVFX = null)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        duration = durationSec;
        timer = durationSec;

        baseAoeRadius = aoeRadius;
        baseAoeDamageMultiplier = aoeDamageMultiplier;
        baseMaxRange = maxRange;
        ExplosionVFX = explosionVFX;
        IsEnabled = true;
        UpdateValues();
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
        else UpdateValues();
    }

    private void UpdateValues()
    {
        AoeRadius = baseAoeRadius * (1f + (stacks - 1) * 0.2f);
        AoeDamageMultiplier = baseAoeDamageMultiplier * stacks;
        MaxRange = baseMaxRange > 0f ? baseMaxRange * (1f + (stacks - 1) * 0.2f) : 0f;
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        IsEnabled = false;
        AoeRadius = 0f;
        AoeDamageMultiplier = 0f;
        MaxRange = 0f;
        ExplosionVFX = null;
    }
}

