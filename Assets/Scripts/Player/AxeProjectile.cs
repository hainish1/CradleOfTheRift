// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah.
//     Projectile base class written by Hainish Acharya for GAMES 4500, University of Utah.
//   </para>
// </summary>

using UnityEngine;

public class AxeProjectile : Projectile
{
    [Header("Axe Projectile Parameters")]
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private float _maxArcDegrees;
    [SerializeField] private float _maxTravelDistance;
    private bool _isReturning = false;
    private float _baseTurnRate;
    private float originalReturnDist;
    private Vector3 _targetPos = Vector3.zero;
    private Transform _returnTarget;
    private Vector3 _returnTargetOriginOnReturn;

    void Start()
    {
        // Prevent Rigidbody from interfering with movement.
        Destroy(rb);
        rb = null;
    }

    public override void Update()
    {
        age += Time.deltaTime;
        if (age >= lifeTime) Destroy(gameObject); // Destroy axe if it did not return quickly enough.
        if (!_returnTarget) Destroy(gameObject); // Destroy axe if player died.

        FadeTrailVisuals();
        Move();
    }


    public override void OnCollisionEnter(Collision collision)
    {
        CreateImpactFX();

        var enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            ApplyEnemyHit(collision, enemy, passingThrough: true); // Damage enemy while passing through it.

            // Ignore future collisions with all colliders of the enemy.
            Collider myCollider = GetComponent<Collider>();
            if (myCollider != null)
            {
                Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
                foreach (Collider collider in enemyColliders)
                    Physics.IgnoreCollision(myCollider, collider);
            }
        }

        // Apply kockback force to the object collided with.
        if (collision.rigidbody != null)
        {
            Vector3 knockback = hitForce * rb.linearVelocity.normalized;
            collision.rigidbody.AddForceAtPosition(knockback, collision.contacts[0].point, ForceMode.Impulse);
        }
    }


    public void Init(RaycastHit raycastHit,
                     Vector3 direction,
                     Transform returnTarget,
                     LayerMask mask,
                     float damage,
                     float flyDistance = 100,
                     Entity attacker = null)
    {
        // Initialize target position.
        if (raycastHit.collider)
            _targetPos = raycastHit.point;
        else
            _targetPos = _maxTravelDistance * direction;

        // Initialize member variables.
        this._returnTarget = returnTarget;
        hitMask = mask;
        actualDamage = damage;
        this.attacker = attacker;
        age = 0;
        trail.Clear();
        trail.time = 0.25f;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;

        // Initialize arcing logic.
        float distance = Vector3.Distance(startPos, _targetPos);
        float initialAngle = _maxArcDegrees * Mathf.Clamp01(distance / 10); // Cap initial angle at maximum arc degrees.
        transform.LookAt(_targetPos); // Face axe toward its target.
        transform.Rotate(0, initialAngle, 0); // Rotate axe initially to its own right.
        _baseTurnRate = CalculateTurnRate(distance, initialAngle); // Calculate constant turn rate to intersect target position.
    }


    private void Move()
    {
        if (!_isReturning) // Curve directly towards target position on the attack path.
        {
            RotateTowards(_targetPos, turnModifier: 1); // Turn towards target position.
            if (Vector3.Distance(transform.position, _targetPos) < 0.5) // Switch to return routine when target position is reached.
                InitializeReturn();
        }
        else // Smart curve towards return target (player) on the return path.
        {
            // Adjust turn rate according to return target movement. Increase turn rate if return target
            // moves farther, ...........decrease turn rate if return target moves farther.
            //float originalDist = Vector3.Distance(transform.position, _returnTargetOriginOnReturn);
            //float originalDist = Vector3.Distance(_returnTarget.position, _returnTargetOriginOnReturn);
            //float currentDist = Vector3.Distance(transform.position, _returnTarget.transform.position);
            float returnTargetDiffDist = Vector3.Distance(_returnTarget.position, _returnTargetOriginOnReturn);
            float turnModifier = 1 + (returnTargetDiffDist / originalReturnDist);
            RotateTowards(_returnTarget.position, turnModifier);

            // Destroy axe when it reaches return target.
            if (Vector3.Distance(transform.position, _returnTarget.position) < 1)
                Destroy(gameObject);
        }

        transform.Translate(Vector3.forward * _projectileSpeed * Time.deltaTime);
    }


    private void InitializeReturn()
    {
        _isReturning = true;
        _returnTargetOriginOnReturn = _returnTarget.position;
        startPos = transform.position;
        originalReturnDist = Vector3.Distance(transform.position, _returnTarget.position);

        float distance = Vector3.Distance(transform.position, _returnTarget.position);
        float initialAngle = _maxArcDegrees * Mathf.Clamp01(distance / 10);

        transform.LookAt(_returnTarget.position);
        transform.Rotate(0, initialAngle, 0);

        _baseTurnRate = CalculateTurnRate(distance, initialAngle);
    }


    private void RotateTowards(Vector3 target, float turnModifier)
    {
        Vector3 targetDirection = target - transform.position;
        if (targetDirection == Vector3.zero) return;



        float returnTargetDiffDist = Vector3.Distance(_returnTarget.position, _returnTargetOriginOnReturn);
        float currTurnRate = _baseTurnRate * turnModifier;


        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                                                      targetRotation,
                                                      Time.deltaTime * currTurnRate);
    }


    private float CalculateTurnRate(float distance, float initialDegrees)
    {
        if (distance == 0) return 0;
        float initialRadians = initialDegrees * Mathf.Deg2Rad;
        float turnRateRadians = 2 * _projectileSpeed * Mathf.Sin(initialRadians) / distance;
        return turnRateRadians * Mathf.Rad2Deg;
    }
}
