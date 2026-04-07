using System;
using UnityEngine;

// higher you fall, harder you hit
public class FallDamageBonus : IDisposable
{
    private float damagePerMeter;
    private float duration;
    private float timer;
    private bool disposed;
    public bool IsDisposed => disposed;

    private float slamStartHeight;
    private bool slamHeightRecorded;

    public static FallDamageBonus Instance;

    public FallDamageBonus(Entity owner, float damagePerMeter, float durationSec = -1f)
    {
        this.damagePerMeter = damagePerMeter;
        this.duration = durationSec;
        this.timer = durationSec;
        Instance = this;

        slamStartHeight = 0f;
        slamHeightRecorded = false;

        Debug.Log($"[Fall Bonus] Activated! {damagePerMeter} damage per meter fallen");
    }

    public void Update(float dt)
    {
        if (disposed) return;

        if (duration >= 0f)
        {
            timer -= dt;
            if (timer <= 0f) Dispose();
        }
    }

    public void RecordSlamStartHeight(float height)
    {
        slamStartHeight = height;
        slamHeightRecorded = true;
        Debug.Log($"[Fall Bonus] Slam started from height: {height:F2}");
    }

    public float GetBonusSlamDamage(float impactHeight)
    {
        if (disposed) return 0f;

        if (!slamHeightRecorded) return 0f;

        float fallDistance = slamStartHeight - impactHeight;

        if (fallDistance <= 0f)
        {
            slamHeightRecorded = false;
            return 0f;
        }

        float bonusDamage = fallDistance * damagePerMeter;
        Debug.Log($"[Fall Bonus] {fallDistance:F2}m fall = +{bonusDamage:F1} damage");

        slamHeightRecorded = false;
        return bonusDamage;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (Instance == this) Instance = null;
    }
}

