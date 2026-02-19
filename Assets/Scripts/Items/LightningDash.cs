using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningDash : IDisposable
{
    private Entity owner;
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private PlayerInventory playerInventory;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float chainDamage;
    private int maxChains;
    private float chainRange;
    private LayerMask enemyLayer;

    private Renderer[] playerRenderers;
    private bool renderersHidden;
    private Enemy lastHitEnemy;
    
    private static Collider[] overlapBuffer = new Collider[64];
    private static HashSet<Enemy> hitEnemiesBuffer = new HashSet<Enemy>();
    private static HashSet<Enemy> visitedBuffer = new HashSet<Enemy>();
    private static AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Coroutine activeDashCoroutine;
    private bool isDashActive;
    
    private const float DashDuration = 0.15f;
    private const float DashInterval = 0.1f;
    
    private PlayerShooter shooter;
    private PlayerMeleeController melee;
    private PlayerGroundSlam slam;
    private PlayerShockwave shockwave;

    public LightningDash(Entity owner, float damage, int chainCount, float range, int initialStacks = 1, float durationSec = -1f)
    {
        this.owner = owner;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;
        this.chainDamage = damage;
        this.maxChains = chainCount;
        this.chainRange = range;

        playerMovement = owner.GetComponent<PlayerMovement>();
        characterController = owner.GetComponent<CharacterController>();
        playerInventory = owner.GetComponent<PlayerInventory>();
        
        if (playerMovement == null)
        {
            Debug.LogError("LightningDash requires PlayerMovement component");
            return;
        }

        enemyLayer = LayerMask.GetMask("Enemy");
        playerRenderers = owner.GetComponentsInChildren<Renderer>();
        
        shooter = owner.GetComponentInChildren<PlayerShooter>();
        melee = owner.GetComponentInChildren<PlayerMeleeController>();
        slam = owner.GetComponent<PlayerGroundSlam>();
        shockwave = owner.GetComponent<PlayerShockwave>();

        SetupLightningTrail();
        
        playerMovement.DashCooldownStarted += OnDashStarted;
    }
    
    private void SetupLightningTrail()
    {
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

    private void OnDashStarted(float dashDuration)
    {
        if (disposed || isDashActive) return;
        
        if (activeDashCoroutine != null)
        {
            playerMovement.StopCoroutine(activeDashCoroutine);
            CleanupDashState();
        }
        
        activeDashCoroutine = playerMovement.StartCoroutine(CheckAndExecuteLightningDash());
    }
    
    private void CleanupDashState()
    {
        isDashActive = false;
        DisableLightningForm();
        activeDashCoroutine = null;
    }
    
    private IEnumerator DashToTarget(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        Vector3 midPoint = (from + to) / 2f + Vector3.up * 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = dashCurve.Evaluate(elapsed / duration);
            
            Vector3 p1 = Vector3.Lerp(from, midPoint, t);
            Vector3 p2 = Vector3.Lerp(midPoint, to, t);
            Vector3 currentPos = Vector3.Lerp(p1, p2, t);
            
            if (characterController != null)
            {
                characterController.enabled = false;
                owner.transform.position = currentPos;
                characterController.enabled = true;
            }
            else
            {
                owner.transform.position = currentPos;
            }
            
            yield return null;
        }
        
        if (characterController != null)
        {
            characterController.enabled = false;
            owner.transform.position = to;
            characterController.enabled = true;
        }
        else
        {
            owner.transform.position = to;
        }
    }

    private void EnableLightningForm()
    {
        if (renderersHidden) return;
        
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        renderersHidden = true;
        
        if (shooter != null) shooter.enabled = false;
        if (melee != null) melee.enabled = false;
        if (slam != null) slam.enabled = false;
        if (shockwave != null) shockwave.enabled = false;
    }

    private void DisableLightningForm()
    {
        if (!renderersHidden) return;
        
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        
        renderersHidden = false;
        
        if (shooter != null) shooter.enabled = true;
        if (melee != null) melee.enabled = true;
        if (slam != null) slam.enabled = true;
        if (shockwave != null) shockwave.enabled = true;
        
        playerInventory?.ResumeOrbitingFireballs();
    }

    private IEnumerator CheckAndExecuteLightningDash()
    {
        isDashActive = true;
        hitEnemiesBuffer.Clear();
        
        Enemy firstTarget = FindNearestEnemy(owner.transform.position, null);
        
        if (firstTarget == null)
        {
            CleanupDashState();
            yield break;
        }

        EnableLightningForm();
        playerInventory?.PauseOrbitingFireballs();
        
        int chainCount = CalculateActualChainCount(firstTarget);
        float lockDuration = chainCount * (DashDuration + DashInterval);
        playerMovement.LockMovement(lockDuration);
        
        lastHitEnemy = null;

        yield return playerMovement.StartCoroutine(ChainToEnemyCoroutine(firstTarget, owner.transform.position, chainDamage * stacks, 0));
        
        CleanupDashState();
    }

    private Enemy FindNearestEnemy(Vector3 fromPos, HashSet<Enemy> excludeSet)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(fromPos, chainRange, overlapBuffer, enemyLayer);
        Enemy nearestEnemy = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Enemy enemy = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            if (excludeSet != null && excludeSet.Contains(enemy)) continue;

            var dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            float dist = Vector3.Distance(fromPos, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }
    
    private Enemy FindNearestEnemyExcludeLast(Vector3 fromPos, Enemy excludeEnemy)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(fromPos, chainRange, overlapBuffer, enemyLayer);
        Enemy nearestEnemy = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Enemy enemy = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (enemy == null || enemy == excludeEnemy) continue;

            var dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            float dist = Vector3.Distance(fromPos, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }
    
    private int CalculateActualChainCount(Enemy firstTarget)
    {
        visitedBuffer.Clear();
        visitedBuffer.Add(firstTarget);
        int count = 1;
        Vector3 currentPos = firstTarget.transform.position;

        for (int i = 1; i < maxChains; i++)
        {
            Enemy nextTarget = FindNearestEnemy(currentPos, visitedBuffer);
            if (nextTarget == null) break;

            visitedBuffer.Add(nextTarget);
            currentPos = nextTarget.transform.position;
            count++;
        }

        return count;
    }

    private IEnumerator ChainToEnemyCoroutine(Enemy target, Vector3 fromPos, float damage, int chainNum)
    {
        if (chainNum >= maxChains || target == null) yield break;
        
        if (hitEnemiesBuffer.Contains(target)) yield break;

        Vector3 targetPos = target.transform.position;
        
        GameObject lightningStart = new GameObject("DashLightningStart");
        GameObject lightningEnd = new GameObject("DashLightningEnd");
        lightningStart.transform.position = fromPos;
        lightningEnd.transform.position = targetPos;
        
        float vfxLifetime = DashDuration + DashInterval;
        LightningCore.CreateLightningVFX(lightningStart.transform, lightningEnd.transform, chainRange, vfxLifetime, null, 0.5f, 0.5f, 0.2f);
        
        UnityEngine.Object.Destroy(lightningStart, vfxLifetime);
        UnityEngine.Object.Destroy(lightningEnd, vfxLifetime);
        
        yield return DashToTarget(fromPos, targetPos, DashDuration);

        if (target == null)
        {
            yield break;
        }

        lastHitEnemy = target;

        LightningCore.ApplyLightningDamage(owner, target, damage);

        var flash = target.GetComponentInChildren<TargetFlash>();
        if (flash != null) flash.Flash();
        
        hitEnemiesBuffer.Add(target);
        
        yield return new WaitForSeconds(DashInterval);

        if (target == null)
        {
            yield break;
        }

        Enemy nextTarget = FindNearestEnemy(target.transform.position, hitEnemiesBuffer);

        if (nextTarget != null)
        {
            yield return playerMovement.StartCoroutine(ChainToEnemyCoroutine(nextTarget, target.transform.position, damage, chainNum + 1));
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (activeDashCoroutine != null && playerMovement != null)
        {
            playerMovement.StopCoroutine(activeDashCoroutine);
        }

        if (playerMovement != null)
            playerMovement.DashCooldownStarted -= OnDashStarted;

        CleanupDashState();
    }
}
