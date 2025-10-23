using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationForce = 30f;
    [SerializeField] private float homingForce = 15f;
    [SerializeField] private float initialLaunchForce = 15f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float delayBeforeTracking = 0.5f;
    private float age = 0f;
    private bool following = false;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(WaitBeforeTracking());
    }

    private void FixedUpdate()
    {
        // Code ripped straight from Hainish. Thank you Hainish.
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null) return;
        if (!following) return;     // Start homing after a delay

        Vector3 direction = target.position - transform.position;
        direction.Normalize();

        Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
        rb.angularVelocity = rotateAmount * rotationForce;
        rb.linearVelocity = transform.forward * homingForce;
    }

    // private void onTriggerEnter(Collider other)
    // {
    //     if (other.transform == target)
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    private void onCollisionEnter(Collision collision)
    {
        // doesnt work lol kms
        Debug.Log("Collision Detected");
        Destroy(gameObject);
        // if (collision.transform == target)
        // {
        //     Destroy(gameObject);
        // }
    }

    private IEnumerator WaitBeforeTracking()
    {
        //rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * initialLaunchForce, ForceMode.Impulse);
        yield return new WaitForSeconds(delayBeforeTracking);
        following = true;
    }

    private void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
