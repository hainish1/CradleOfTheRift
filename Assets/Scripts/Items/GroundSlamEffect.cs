using System;
using UnityEngine;

public class GroundSlamEffect : IDisposable
{
    private PlayerShockwave shockwave;
    private PlayerGroundSlam groundSlam;
    private bool disposed;
    public bool IsDisposed => disposed;

    public GroundSlamEffect(Entity owner, float slamDamage)
    {
        shockwave = owner.GetComponent<PlayerShockwave>();
        groundSlam = owner.GetComponent<PlayerGroundSlam>();

        if (shockwave != null) shockwave.enabled = false;
        if (groundSlam != null)
        {
            groundSlam.slamDamage = slamDamage;
            groundSlam.enabled = true;
        }

        Debug.Log("[Effect] GroundSlamEffect created: Shockwave disabled, GroundSlam enabled");
    }

    public void Update(float dt) { }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (shockwave != null) shockwave.enabled = true;
        if (groundSlam != null) groundSlam.enabled = false;

        Debug.Log("[Effect] GroundSlamEffect disposed: Shockwave restored, GroundSlam disabled");
    }
}
