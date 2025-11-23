using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : Projectile
{
    [Header("Homing Properties")]
    [SerializeField] private Transform targetLocation = null;
    [SerializeField] private float rotationForce = 30f;
    [SerializeField] private float homingForce = 20f;
    [SerializeField] private float initialLaunchForce = 20f;
    [SerializeField] private float delayBeforeTracking = 0.5f;
    [SerializeField] private float launchConeAngle = 1.0f;

    [Header("Targeting")]
    [SerializeField] private float targetingRange = 50f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float reTargetRate = 0.25f; // How often to scan for a target if we don't have one

    private bool following = false;
    private Entity target;
    private IDamageable targetDamageable; 

    public override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable(); 
        ResetForReuse();
        StartCoroutine(HomingCoroutine()); // Start the new coroutine
    }
    
    private void ResetForReuse()
    {
        StopAllCoroutines(); // Critical for object pooling
        following = false;
        targetLocation = null;
        targetDamageable = null;
    }

    public void Init(LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        base.Init(Vector3.zero, mask, damage, flyDistance, attacker);
    }

    public override void Update()
    {
        base.Update();

        // Update() is now only for movement logic
        if (targetLocation != null && following)
        {
            // We have a target, home in on it
            Vector3 direction = targetLocation.position - transform.position;
            direction.Normalize();

            Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
            if (rb != null)
            {
                rb.angularVelocity = rotateAmount * rotationForce;
                rb.linearVelocity = transform.forward * homingForce;
            }
        }
        else if (targetLocation == null && following)
        {
            // We don't have a target, just fly upwards
            if (rb != null && rb.linearVelocity.sqrMagnitude < homingForce * homingForce)
            {
                rb.linearVelocity = transform.up * Mathf.Max(homingForce * 0.25f, 5f);
            }
        }
    }

    private IEnumerator HomingCoroutine()
    {
        // Initial launch
        if (rb != null)
        {
            Vector3 randomOffset = Random.insideUnitSphere * launchConeAngle;
            Vector3 randomLaunchDirection = (Vector3.up + randomOffset).normalized;
            rb.AddForce(randomLaunchDirection * initialLaunchForce, ForceMode.Impulse);
        }

        // Wait for the initial delay
        yield return new WaitForSeconds(delayBeforeTracking);
        following = true;

        // Start the persistent targeting loop
        while (true)
        {
            // Check if we need a new target
            if (targetLocation == null || (targetDamageable != null && targetDamageable.IsDead))
            {
                FindTarget();
            }
            
            // Wait before checking again
            yield return new WaitForSeconds(reTargetRate);
        }
    }

    private void FindTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, targetMask);
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;
        IDamageable closestDamageable = null;

        foreach (Collider collider in colliders)
        {
            Enemy enemy = collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                // Check if target is valid and alive
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsDead)
                {
                    continue; // Skip, this one is already dead
                }
                
                float distance = (transform.position - enemy.transform.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = enemy.transform;
                    closestDamageable = damageable;
                }
            }
        }

        if (closestTarget != null)
        {
            targetLocation = closestTarget.transform;
            targetDamageable = closestDamageable;
        }
        else
        {
            targetLocation = null;
            targetDamageable = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (targetLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetLocation.position);
        }
        
        Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan, semi-transparent
        Gizmos.DrawSphere(transform.position, targetingRange);
    }
}