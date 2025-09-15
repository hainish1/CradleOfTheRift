using UnityEngine;


// almost a copy paste of my projectile script for player
public class EnemyProjectile : MonoBehaviour
{
    [Header("Bullet motion")]
    [SerializeField] float gravity = 0f;
    [SerializeField] float lifeTime = 5f; // just for now
    [SerializeField] float radius = 0.05f; // sphere ray cast check for collision n whatnot

    [Header("Hit")]
    [SerializeField] LayerMask hitMask = ~0; // what this projectile thing can hit
    [SerializeField] float hitForce = 3f; // SHOULD BE ENOUGH TO NUDGE OUI PLAYER
    // [SerializeField] int damage = 1; // LETTING THE PROJECTILES DO DAMAGE FOR NOW
    [SerializeField] GameObject impactFX;

    Vector3 velocity;
    float age;
    Collider[] ignoreColliders; // shooter colliders to ignore

    public void Init(Vector3 initialVelocity, LayerMask mask, Collider[] ignore = null)
    {
        velocity = initialVelocity;
        hitMask = mask;
        ignoreColliders = ignore;

        if (ignoreColliders != null)
        {
            var myCol = GetComponent<Collider>();
            if (myCol)
                foreach (var c in ignoreColliders)
                    if (c && c.enabled) Physics.IgnoreCollision(myCol, c, true);
        }
    }


    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (age >= lifeTime) { Destroy(gameObject); return; }

        // gravity
        if (gravity > 0f) velocity += Vector3.down * gravity * dt;

        // move with spherecast to avoid tunneling
        Vector3 displacement = velocity * dt;
        float distance = displacement.magnitude;
        if (distance > 0f)
        {
            if (Physics.SphereCast(transform.position, radius, displacement.normalized, out var hit, distance, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }
            transform.position += displacement;
            if (velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    // this is where the real magic will happen
    void OnHit(RaycastHit hit)
    {
        // place at the impact point
        transform.position = hit.point;

        // pure cosmetic stuff, for a thing that has rigidbody, NOT FOR PLAYER BUT STILL KEEPING IN 
        if (hit.rigidbody)
            hit.rigidbody.AddForceAtPosition(velocity.normalized * hitForce, hit.point, ForceMode.Impulse);

        // now do a tiny bit of knockback to the enemy just teeni
        PlayerMovement pm = hit.collider.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            Vector3 direction = (hit.point - transform.position).normalized;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = (pm.transform.position - transform.position).normalized;
            pm.ApplyImpulse(direction * hitForce);
        }

        // when we get impact fx we will apply here

        Destroy(gameObject);
    }
}
