using System;
using UnityEngine;

public class BounceProjectiles : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float baseBounceRange;
    private int baseMaxBounceCount;
    private float baseDamageMultiplierPerBounce;

    public static float BounceRange { get; private set; }
    public static int MaxBounceCount { get; private set; }
    public static float DamageMultiplierPerBounce { get; private set; }
    public static bool IsEnabled { get; private set; }
    public static GameObject BounceVFX { get; private set; }

    public BounceProjectiles(Entity owner, float bounceRange, int maxBounceCount, float damageMultiplierPerBounce, int initialStacks, float durationSec = -1f, GameObject bounceVFX = null)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        duration = durationSec;
        timer = durationSec;

        baseBounceRange = bounceRange;
        baseMaxBounceCount = maxBounceCount;
        baseDamageMultiplierPerBounce = damageMultiplierPerBounce;
        BounceVFX = bounceVFX;
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
        BounceRange = baseBounceRange * (1f + (stacks - 1) * 0.2f);
        MaxBounceCount = baseMaxBounceCount + (stacks - 1);
        DamageMultiplierPerBounce = baseDamageMultiplierPerBounce;
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
        BounceRange = 0f;
        MaxBounceCount = 0;
        DamageMultiplierPerBounce = 1f;
        BounceVFX = null;
    }
}

