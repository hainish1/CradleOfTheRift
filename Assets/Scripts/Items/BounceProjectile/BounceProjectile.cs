using System.Collections.Generic;
using UnityEngine;

public class BounceProjectile : Projectile
{
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
    private int currentBounceCount = 0;
    private Enemy lastHitEnemy;
    private float initialSpeed = 0f;

    public override void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        base.Init(velocity, mask, damage, flyDistance, attacker);
        hitEnemies.Clear();
        ignoredColliders.Clear();
        currentBounceCount = 0;
        lastHitEnemy = null;
        initialSpeed = velocity.magnitude;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        hitEnemies.Clear();
        ignoredColliders.Clear();
        currentBounceCount = 0;
        lastHitEnemy = null;
        initialSpeed = 0f;
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;
        if (ignoredColliders.Contains(collision.collider)) return;

        CreateImpactFX();

        var enemy = collision.collider.GetComponentInParent<Enemy>();
        
        if (enemy != null && BounceProjectiles.IsEnabled)
        {
            if (!hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                lastHitEnemy = enemy;
                
                Collider projectileCollider = GetComponent<Collider>();
                if (projectileCollider != null)
                {
                    Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
                    foreach (var col in enemyColliders)
                    {
                        if (col != null && col != projectileCollider)
                        {
                            ignoredColliders.Add(col);
                            Physics.IgnoreCollision(projectileCollider, col, true);
                        }
                    }
                }

                var kb = enemy?.GetComponent<AgentKnockBack>();
                if (kb != null)
                {
                    var contact = collision.GetContact(0);
                    Vector3 dir = -contact.normal;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
                    kb.ApplyImpulse(dir * knockBackImpulse);
                }

                var flash = collision.collider.GetComponentInParent<TargetFlash>();
                if (flash != null) flash.Flash();

                var damageable = collision.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    float damageToDeal = actualDamage * Mathf.Pow(BounceProjectiles.DamageMultiplierPerBounce, currentBounceCount);
                    damageable.TakeDamage(damageToDeal);
                    CombatEvents.ReportDamage(attacker, enemy, damageToDeal);
                    Debug.Log($"Bounce {currentBounceCount}: Dealt {damageToDeal} damage to {collision.gameObject.name}");
                }

                currentBounceCount++;
                
                if (currentBounceCount <= BounceProjectiles.MaxBounceCount)
                {
                    TryBounceToNextEnemy(enemy.transform.position);
                }
                else
                {
                    ReturnToSource();
                }
            }
            else
            {
                ReturnToSource();
            }
        }
        else
        {
            if (enemy != null)
            {
                var kb = enemy?.GetComponent<AgentKnockBack>();
                if (kb != null)
                {
                    var contact = collision.GetContact(0);
                    Vector3 dir = -contact.normal;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
                    kb.ApplyImpulse(dir * knockBackImpulse);
                }
                var flash = collision.collider.GetComponentInParent<TargetFlash>();
                if (flash != null) flash.Flash();

                var damageable = collision.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(actualDamage);
                    CombatEvents.ReportDamage(attacker, enemy, actualDamage);
                }
            }

            if (collision.rigidbody != null)
            {
                Vector3 force = rb.linearVelocity.normalized * hitForce;
                collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
            }

            ReturnToSource();
        }
    }

    private void TryBounceToNextEnemy(Vector3 currentPos)
    {
        float searchRange = BounceProjectiles.BounceRange;
        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        Collider[] nearby = Physics.OverlapSphere(currentPos, searchRange, enemyLayer);
        Enemy closest = null;
        float minDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || hitEnemies.Contains(enemy)) continue;

            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            float dist = Vector3.Distance(currentPos, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        if (closest != null)
        {
            Vector3 direction = (closest.transform.position - currentPos).normalized;
            direction.y = 0f;
            
            float offsetDistance = 0.5f;
            Vector3 newPosition = currentPos + direction * offsetDistance;
            transform.position = newPosition;
            
            float speed = initialSpeed > 0f ? initialSpeed : rb.linearVelocity.magnitude;
            if (initialSpeed == 0f) initialSpeed = speed;
            rb.linearVelocity = direction * speed;
            
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            CreateBounceEffect(currentPos, closest.transform.position);
        }
        else
        {
            ReturnToSource();
        }
    }

    private void CreateBounceEffect(Vector3 from, Vector3 to)
    {
        if (BounceProjectiles.BounceVFX != null)
        {
            GameObject fx = Instantiate(BounceProjectiles.BounceVFX);
            fx.transform.position = from;
            fx.transform.LookAt(to);
            Destroy(fx, 0.5f);
        }
    }
}

