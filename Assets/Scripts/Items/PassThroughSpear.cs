using System;
using UnityEngine;

public class PassThroughSpear : IDisposable
{
    private Entity owner;
    private int stacks;
    private int passThroughEnemyCount;
    private float duration;
    private float timer;
    private bool disposed;

    public static bool IsEnabled { get; private set; }

    public static int MaxPassThroughCount { get; private set; }

    public PassThroughSpear(Entity owner, int passThroughEnemyCount, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        this.passThroughEnemyCount = Mathf.Max(1, passThroughEnemyCount);
        stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        timer = durationSec;

        IsEnabled = true;
        MaxPassThroughCount = this.passThroughEnemyCount;
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0)
        {
            Dispose();
            return;
        }
    }

    public void SetPassThroughEnemyCount(int count)
    {
        passThroughEnemyCount = Mathf.Max(1, count);
        MaxPassThroughCount = passThroughEnemyCount;
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
        MaxPassThroughCount = 0;

        owner = null;
        stacks = 0;
    }
}
