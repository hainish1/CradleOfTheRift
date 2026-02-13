using System;
using UnityEngine;

public class PassThroughSpear : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private LayerMask projectileLayer;
    private LayerMask enemyLayer;

    private readonly bool[] previousIgnore = new bool[32];
    private bool collisionOverridesApplied;

    public static bool IsEnabled { get; private set; }

    public PassThroughSpear(Entity owner, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        timer = durationSec;

        projectileLayer = LayerMask.NameToLayer("Projectile");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        ApplyCollisionOverrides();
        IsEnabled = collisionOverridesApplied;
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }


    private void ApplyCollisionOverrides()
    {
        if (collisionOverridesApplied) return;

        if (projectileLayer < 0 || enemyLayer < 0)
        {
            Debug.LogWarning("[PassThroughSpear] Missing required layers: Projectile or Enemy.");
            return;
        }

        for (int i = 0; i < 32; i++)
            previousIgnore[i] = Physics.GetIgnoreLayerCollision(projectileLayer, i);

        for (int i = 0; i < 32; i++)
        {
            if (i == projectileLayer) continue;
            bool shouldIgnore = (i != enemyLayer);
            Physics.IgnoreLayerCollision(projectileLayer, i, shouldIgnore);
        }

        collisionOverridesApplied = true;
    }

    private void RestoreCollisionOverrides()
    {
        if (!collisionOverridesApplied) return;
        if (projectileLayer < 0) return;

        for (int i = 0; i < 32; i++)
        {
            Physics.IgnoreLayerCollision(projectileLayer, i, previousIgnore[i]);
        }

        collisionOverridesApplied = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        RestoreCollisionOverrides();
        IsEnabled = false;

        owner = null;
        stacks = 0;
    }
}
