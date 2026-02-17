using System;
using UnityEngine;


public class FinisherStrike : IDisposable
{

    public static float CapsuleRadiusMultiplier { get; private set; } = 1f;

    public static float DamageMultiplier { get; private set; } = 1f;

    private readonly Entity owner;
    private readonly PlayerMeleeController meleeController;
    private readonly PlayerMeleeControllerV2 meleeControllerV2;
    private readonly int finisherComboIndex;
    private readonly float damageMultiplier;
    private readonly float rangeMultiplier;
    private readonly float effectLifetime;
    private float lifeTimer;
    private bool disposed;

    public FinisherStrike(Entity owner, float damageMultiplier, float rangeMultiplier, float durationSec = -1f)
    {
        this.owner = owner;
        this.damageMultiplier = Mathf.Max(1f, damageMultiplier);
        this.rangeMultiplier = Mathf.Max(1f, rangeMultiplier);
        this.effectLifetime = durationSec;
        this.lifeTimer = durationSec;

        meleeControllerV2 = owner != null
            ? (owner.GetComponentInChildren<PlayerMeleeControllerV2>()
               ?? (owner.transform.root != null ? owner.transform.root.GetComponentInChildren<PlayerMeleeControllerV2>() : null)
               ?? UnityEngine.Object.FindObjectOfType<PlayerMeleeControllerV2>())
            : UnityEngine.Object.FindObjectOfType<PlayerMeleeControllerV2>();
        meleeController = (meleeControllerV2 == null && owner != null) ? owner.GetComponentInChildren<PlayerMeleeController>() : null;
        finisherComboIndex = meleeControllerV2 != null ? 2 : (meleeController != null ? 3 : 0);
        if (meleeControllerV2 != null)
        {
            meleeControllerV2.OnMeleeComboAttack += OnComboAttack;
            meleeControllerV2.OnMeleeAttackEnd += OnAttackEnd;
            Debug.Log($"[FinisherStrike] Subscribed to PlayerMeleeControllerV2, finisherComboIndex={finisherComboIndex}");
        }
        else if (meleeController != null)
        {
            meleeController.OnMeleeComboAttack += OnComboAttack;
            meleeController.OnMeleeAttackEnd += OnAttackEnd;
        }
        else
        {
            Debug.LogWarning("[FinisherStrike] Could not find PlayerMeleeController or PlayerMeleeControllerV2");
        }
    }

    public bool IsDisposed => disposed;

    public void Update(float dt)
    {
        if (effectLifetime < 0f || disposed) return;
        lifeTimer -= dt;
        if (lifeTimer <= 0f) Dispose();
    }

    private void OnComboAttack(int comboIndex)
    {
        if (finisherComboIndex > 0 && comboIndex == finisherComboIndex)
        {
            DamageMultiplier = damageMultiplier;
            CapsuleRadiusMultiplier = rangeMultiplier;
            Debug.Log($"[FinisherStrike] Finisher active: damageMult={damageMultiplier}, rangeMult={rangeMultiplier}");
        }
    }

    private void OnAttackEnd()
    {
        DamageMultiplier = 1f;
        CapsuleRadiusMultiplier = 1f;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        DamageMultiplier = 1f;
        CapsuleRadiusMultiplier = 1f;

        if (meleeController != null)
        {
            meleeController.OnMeleeComboAttack -= OnComboAttack;
            meleeController.OnMeleeAttackEnd -= OnAttackEnd;
        }
        else if (meleeControllerV2 != null)
        {
            meleeControllerV2.OnMeleeComboAttack -= OnComboAttack;
            meleeControllerV2.OnMeleeAttackEnd -= OnAttackEnd;
        }
    }
}
