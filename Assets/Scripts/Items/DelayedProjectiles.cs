using System;
using UnityEngine;

public class DelayedProjectiles : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float baseDelayTime;
    private float baseDamageMultiplier;

    public static float DelayTime { get; private set; }
    public static float DamageMultiplier { get; private set; }
    public static bool IsEnabled { get; private set; }
    public static GameObject MarkVFX { get; private set; }

    public DelayedProjectiles(Entity owner, float delayTime, float damageMultiplier, int initialStacks, float durationSec = -1f, GameObject markVFX = null)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        duration = durationSec;
        timer = durationSec;

        baseDelayTime = delayTime;
        baseDamageMultiplier = damageMultiplier;
        MarkVFX = markVFX;
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
        DelayTime = baseDelayTime;
        DamageMultiplier = baseDamageMultiplier * (1f + (stacks - 1) * 0.2f);
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
        DelayTime = 0f;
        DamageMultiplier = 1f;
        MarkVFX = null;
    }
}


