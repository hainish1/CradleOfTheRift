using System;
using UnityEngine;

// higher you fall, harder you hit
public class FallDamageBonus : IDisposable
{
    private Entity owner;
    private GameObject ownerGameObject;
    private float damagePerMeter;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private CharacterController characterController;
    private PlayerGroundSlam groundSlam;
    private float slamStartHeight;
    private bool slamHeightRecorded;

    public static FallDamageBonus Instance;

    public FallDamageBonus(Entity owner, float damagePerMeter, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        this.ownerGameObject = owner.gameObject;
        this.damagePerMeter = damagePerMeter;
        this.stacks = initialStacks < 1 ? 1 : initialStacks;
        this.duration = durationSec;
        this.timer = durationSec;

        characterController = owner.GetComponent<CharacterController>();
        groundSlam = owner.GetComponent<PlayerGroundSlam>();
        Instance = this;

        slamStartHeight = 0f;
        slamHeightRecorded = false;

        Debug.Log($"[Fall Bonus] Activated! {damagePerMeter} damage per meter fallen, {stacks} stacks");
    }

    public void AddStack(int count = 1)
    {
        if (count < 1) count = 1;
        stacks += count;
        Debug.Log($"[Fall Bonus] Stacked! Now {stacks} stacks");
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
        if (disposed)
        {
            Debug.Log("[Fall Bonus] Disposed, returning 0");
            return 0f;
        }
        
        if (!slamHeightRecorded)
        {
            Debug.Log("[Fall Bonus] No slam height recorded! Did you record it before slamming?");
            return 0f;
        }

        float fallDistance = slamStartHeight - impactHeight;
        Debug.Log($"[Fall Bonus] Calculating: Start={slamStartHeight:F2}, Impact={impactHeight:F2}, Distance={fallDistance:F2}m");

        // did we actually fall?
        if (fallDistance <= 0f)
        {
            Debug.Log($"[Fall Bonus] No fall distance");
            slamHeightRecorded = false;
            return 0f;
        }

        float bonusDamage = fallDistance * damagePerMeter * stacks;
        Debug.Log($"[Fall Bonus] {fallDistance:F2}m fall = +{bonusDamage:F1} damage (x{stacks})");
        
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

