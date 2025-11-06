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
    [SerializeField] private float delayBeforeTracking = 0.5f;  // Start homing after a delay
    [SerializeField] private float launchConeAngle = 1.0f;


    [Header("Targeting")]
    [SerializeField] private float targetingRange = 50f;
    [SerializeField] private LayerMask targetMask;

    private bool following = false;
    private Entity target;

    public override void Awake()
    {
        base.Awake();
        StartCoroutine(WaitBeforeTracking());
    }

    public void Init(LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        base.Init(new Vector3(0, 0, 0), mask, damage, flyDistance, attacker);
        
        // if (target != null)
        // {
        //     targetLocation = targetEntity.transform;
        // }
    }

    public override void Update()
    {
        base.Update();

        if (targetLocation != null && following)
        {
            Vector3 direction = targetLocation.position - transform.position;
            direction.Normalize();

            Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
            if (rb != null)
            {
                rb.angularVelocity = rotateAmount * rotationForce;
                rb.linearVelocity = transform.forward * homingForce;
            }
        }
        // Cannot find a target, so just slowly fly upwards
        else if (targetLocation == null && following)
        {
            //b.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }

    private IEnumerator WaitBeforeTracking()
    {
        //rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        if (rb != null)
        {
            Vector3 randomOffset = Random.insideUnitSphere * launchConeAngle;
            Vector3 randomLaunchDirection = (Vector3.up + randomOffset).normalized;
            rb.AddForce(randomLaunchDirection * initialLaunchForce, ForceMode.Impulse);
        }
        yield return new WaitForSeconds(delayBeforeTracking);
        
        if (targetLocation == null)
        {
            FindTarget();
        }

        
        following = true;
    }

    private void FindTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, targetMask);
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (Collider collider in colliders)
        {
            Enemy enemy = collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                float distance = (transform.position - enemy.transform.position).sqrMagnitude;   //Vector3.Distance(transform.position, entity.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = enemy.transform;
                }
            }
        }

        if (closestTarget != null)
        {
            targetLocation = closestTarget.transform;
        }
    }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     //Destroy(this.gameObject);
    //     //base.OnCollisionEnter(collision);
    //     // if (ObjectPool.instance != null)
    //     // {
    //     //     Destroy(this.gameObject);
    //     // }
    //     Destroy(this.gameObject);
    // }

    private void OnDrawGizmos()
    {
        // Draw targeting line
        if (targetLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetLocation.position);
        }

        // Draw the acquisition range
        if (!following) // Only draw acquisition range before tracking starts
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan, semi-transparent
            Gizmos.DrawSphere(transform.position, targetingRange);
        }
    }
}
