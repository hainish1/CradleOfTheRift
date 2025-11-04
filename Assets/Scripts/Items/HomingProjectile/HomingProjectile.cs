using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : Projectile
{
    [Header("Homing Properties")]
    [SerializeField] private Transform targetLocation;
    [SerializeField] private float rotationForce = 30f;
    [SerializeField] private float homingForce = 15f;
    [SerializeField] private float initialLaunchForce = 15f;
    [SerializeField] private float delayBeforeTracking = 0.5f;
    //private Vector3 startPos;
    private bool following = false;
    private Entity target;
    private Vector3 startPos;

    

    public override void Awake()
    {
        base.Awake();
        StartCoroutine(WaitBeforeTracking());

        // trail.Clear();
        // trail.time = 0.25f;
        // meshRenderer = GetComponent<MeshRenderer>();
    }

    // public override void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    // {
    //     base.Init(velocity, mask, damage, flyDistance, attacker);
    // }

    public override void Update()
    {
        // // Code ripped straight from Hainish. Thank you Hainish.
        // age += Time.fixedDeltaTime;
        // if (age >= lifeTime)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        base.Update();

        if (targetLocation == null) return;
        if (!following) return;     // Start homing after a delay

        Vector3 direction = targetLocation.position - transform.position;
        direction.Normalize();

        Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
        if (rb != null)
        {
            rb.angularVelocity = rotateAmount * rotationForce;
            rb.linearVelocity = transform.forward * homingForce;
        }
    }

    private IEnumerator WaitBeforeTracking()
    {
        //rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        if (rb != null)
        {
            rb.AddForce(Vector3.up * initialLaunchForce, ForceMode.Impulse);
        }
        yield return new WaitForSeconds(delayBeforeTracking);
        following = true;
    }

    private void OnDrawGizmos()
    {
        if (targetLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetLocation.position);
        }
    }
}
