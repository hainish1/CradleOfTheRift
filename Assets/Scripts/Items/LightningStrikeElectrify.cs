using System;
using System.Collections.Generic;
using UnityEngine;

public class LightningStrikeElectrify : IDisposable
{
    private readonly Entity owner;
    private readonly float electrifyDamage;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private readonly HashSet<PoisonPool> poolBuffer = new HashSet<PoisonPool>();
    private bool disposed;

    private const float SearchRadius = 25f;

    public LightningStrikeElectrify(Entity owner, float electrifyDamage)
    {
        this.owner = owner;
        this.electrifyDamage = Mathf.Max(0f, electrifyDamage);
        LightningStrikeEvents.StrikeLanded += OnStrikeLanded;
    }

    public bool IsDisposed => disposed;

    private void OnStrikeLanded(Entity strikeOwner, Vector3 pos, float dmg)
    {
        if (disposed || strikeOwner != owner || electrifyDamage <= 0f) return;

        poolBuffer.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(pos, SearchRadius, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var pool = overlapBuffer[i].GetComponentInParent<PoisonPool>();
            if (pool == null || poolBuffer.Contains(pool)) continue;
            poolBuffer.Add(pool);
            pool.Electrify(electrifyDamage);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        LightningStrikeEvents.StrikeLanded -= OnStrikeLanded;
    }
}
