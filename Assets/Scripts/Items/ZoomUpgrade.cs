using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomUpgrade : IDisposable
{
    private Entity owner;
    private PlayerMovement playerMovement;
    private Collider playerCollider;
    private bool disposed;
    public bool IsDisposed => disposed;

    private float damage;
    private float damageRange;
    private GameObject vfxPrefab;

    private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
    private HashSet<Enemy> hitEnemiesCache = new HashSet<Enemy>();
    private LayerMask enemyLayerMask;
    private Coroutine activeDashCoroutine;

    private GameObject activeVFXInstance;
    private StatModifier dashDistanceModifier;

    public ZoomUpgrade(Entity owner, float damage, float damageRange, float dashDistanceBonus = 0f, float durationSec = -1f, GameObject vfxPrefab = null)
    {
        this.owner = owner;
        this.damage = damage;
        this.damageRange = damageRange;
        this.vfxPrefab = vfxPrefab;

        playerMovement = owner.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("[ZoomUpgrade] requires PlayerMovement component");
            return;
        }

        playerCollider = owner.GetComponent<Collider>();
        if (playerCollider == null)
            playerCollider = owner.GetComponentInChildren<Collider>();

        if (dashDistanceBonus != 0f && owner.Stats != null)
        {
            dashDistanceModifier = new BasicStatsModifier(
                StatType.DashDistance, -1f, v => v + dashDistanceBonus);
            owner.Stats.Mediator.AddModifier(dashDistanceModifier);
        }

        enemyLayerMask = LayerMask.GetMask("Enemy");

        playerMovement.DashCooldownStarted += OnDashStarted;
        Debug.Log($"[ZoomUpgrade] Activated! {damage} lightning damage, {damageRange}m range, +{dashDistanceBonus} dash distance");
    }

    public void Update(float dt) { }

    private void OnDashStarted(float dashDuration)
    {
        if (disposed) return;

        if (activeDashCoroutine != null && playerMovement != null)
            playerMovement.StopCoroutine(activeDashCoroutine);

        if (playerCollider != null)
            IgnoreAllEnemyCollisions();

        activeDashCoroutine = playerMovement.StartCoroutine(ZoomDashCoroutine(dashDuration));
    }

    private IEnumerator ZoomDashCoroutine(float dashDuration)
    {
        SpawnVFX();

        float elapsed = 0f;
        float checkInterval = 0.1f;
        float collisionCheckInterval = 0.05f;
        float lastCollisionCheck = 0f;

        hitEnemiesCache.Clear();

        while (elapsed < dashDuration)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;
            lastCollisionCheck += checkInterval;

            if (playerMovement == null || owner == null) break;

            if (playerCollider != null && lastCollisionCheck >= collisionCheckInterval)
            {
                IgnoreAllEnemyCollisions();
                lastCollisionCheck = 0f;
            }

            Vector3 playerPos = playerMovement.transform.position;
            Vector3 behindDirection = -playerMovement.transform.forward;

            Collider[] nearbyColliders = Physics.OverlapSphere(playerPos, damageRange, enemyLayerMask);

            foreach (Collider col in nearbyColliders)
            {
                Enemy enemy = col.GetComponentInParent<Enemy>();
                if (enemy == null || hitEnemiesCache.Contains(enemy)) continue;

                Vector3 toEnemy = (enemy.transform.position - playerPos).normalized;
                toEnemy.y = 0f;
                toEnemy.Normalize();

                float dot = Vector3.Dot(behindDirection, toEnemy);
                if (dot > 0.3f)
                {
                    hitEnemiesCache.Add(enemy);

                    var damageable = enemy.GetComponent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(damage);
                        CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Lightning);

                        var flash = enemy.GetComponentInChildren<TargetFlash>();
                        if (flash != null) flash.Flash();
                    }
                }
            }
        }

        RestoreAllEnemyCollisions();
        DestroyVFX();
        activeDashCoroutine = null;
    }

    private void SpawnVFX()
    {
        if (vfxPrefab == null || playerMovement == null) return;
        activeVFXInstance = UnityEngine.Object.Instantiate(vfxPrefab, playerMovement.transform);
        activeVFXInstance.transform.localPosition = Vector3.zero;
    }

    private void DestroyVFX()
    {
        if (activeVFXInstance != null)
        {
            UnityEngine.Object.Destroy(activeVFXInstance);
            activeVFXInstance = null;
        }
    }

    private void IgnoreAllEnemyCollisions()
    {
        if (playerMovement == null || playerCollider == null) return;

        Collider[] enemyColliders = Physics.OverlapSphere(
            playerMovement.transform.position, 20f, enemyLayerMask);

        foreach (Collider enemyCol in enemyColliders)
        {
            if (enemyCol == null || enemyCol == playerCollider) continue;
            if (ignoredColliders.Contains(enemyCol)) continue;

            Physics.IgnoreCollision(playerCollider, enemyCol, true);
            ignoredColliders.Add(enemyCol);
        }
    }

    private void RestoreAllEnemyCollisions()
    {
        if (playerCollider == null) return;

        foreach (Collider enemyCol in ignoredColliders)
        {
            if (enemyCol != null)
                Physics.IgnoreCollision(playerCollider, enemyCol, false);
        }
        ignoredColliders.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (activeDashCoroutine != null && playerMovement != null)
        {
            playerMovement.StopCoroutine(activeDashCoroutine);
            activeDashCoroutine = null;
        }

        RestoreAllEnemyCollisions();
        DestroyVFX();

        if (dashDistanceModifier != null)
        {
            dashDistanceModifier.Dispose();
            dashDistanceModifier = null;
        }

        if (playerMovement != null)
            playerMovement.DashCooldownStarted -= OnDashStarted;

        hitEnemiesCache.Clear();
        ignoredColliders.Clear();
    }
}
