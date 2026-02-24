using System;
using UnityEngine;

public class PoisonPoolProjectiles : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float baseRadius;
    private float baseLifetime;
    private GameObject bottleVFX;

    public static float Radius { get; private set; }
    public static float PoolLifetime { get; private set; }
    public static GameObject BottleVFX { get; private set; }
    public static Entity Owner { get; private set; }
    public static bool IsEnabled { get; private set; }

    public PoisonPoolProjectiles(Entity owner, float radius, float poolLifetime, int initialStacks, float durationSec = -1f, GameObject bottleVFX = null)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        duration = durationSec;
        timer = durationSec;

        baseRadius = radius;
        baseLifetime = poolLifetime;
        this.bottleVFX = bottleVFX;
        Owner = owner;
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
        Radius = baseRadius * (1f + 0.1f * (stacks - 1));
        PoolLifetime = baseLifetime;
        BottleVFX = bottleVFX;
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
        Radius = 0f;
        PoolLifetime = 0f;
        BottleVFX = null;
        Owner = null;
    }
}
