// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//     Projectile base class written by Hainish Acharya for GAMES 4500, University of Utah.
//   </para>
// </summary>

using System.Collections;
using System.Linq;
using UnityEngine;

public class AxeProjectile : Projectile
{
    [Header("Axe Model Parameters")]
    [SerializeField]
    [Tooltip("Transform of the weapon model.")] private Transform _modelTransform;
    [SerializeField]
    [Tooltip("How quickly the weapon whirls in units per second.")] private float _whirlSpeed;

    [Header("Axe Projectile Parameters")]
    [SerializeField]
    [Tooltip("How quickly the weapon travels in units per second.")] private float _projectileSpeed;
    [SerializeField]
    [Tooltip("The maximum degrees which the weapon can arc away from the player upon throw.")] private float _maxArcDegrees;
    [SerializeField]
    [Tooltip("The distance from a target at which Max Arc Degrees is reached.")] private float _arcFlatteningCapDistance;
    [SerializeField]
    [Tooltip("How quickly the weapon forcibly returns to the thrower in units per second.")] private float _timeoutReturnSpeed;
    [Tooltip("The maximum distance in units from the thrower which the weapon can travel to.")] public float MaxTravelDistance;
    private bool _isInitialized;
    private bool _isExpired;
    private bool _isReturning;
    private Vector3 _currTargetPosition;
    private Vector3 _currTargetDirection;
    private Vector3 _attackPosition;
    private Vector3 _throwerOriginOnReturn;
    private Transform _throwerCenter;

    protected override void FixedUpdate()
    {
        if (!_isInitialized) return; // Do nothing if initialization has not occured.

        _modelTransform.Rotate(xAngle: 0, yAngle: 0, Time.fixedDeltaTime * _whirlSpeed); // Rotate the axe model for whirl effect.
        FadeTrailVisuals();

        if (_isExpired) return; // Return early if projectile is expired.
        if (!_throwerCenter) DestroyAxe(); // Destroy projectile if player died.

        if (age >= lifeTime) // Time out projectile if it did not return quickly enough.
        {
            _isExpired = true;
            StartCoroutine(TimeoutReturn(seconds: 5));
            return;
        }

        _currTargetPosition = _isReturning ? _throwerCenter.position : _attackPosition;
        _currTargetDirection = _currTargetPosition - transform.position;
        RotateAndMove();

        // Start returning when attack position is reached.
        if (!_isReturning && Vector3.Distance(transform.position, _currTargetPosition) < 0.1)
        {
            InitializeReturn();
            InitializeArcPath(_throwerCenter.position);
        }

        // Destroy projectile when thrower is reached.
        if (_isReturning && Vector3.Distance(transform.position, _currTargetPosition) < 0.1) DestroyAxe();

        age += Time.fixedDeltaTime;
    }

    public override void Update() {} // Override Update logic.

    public override void OnCollisionEnter(Collision collision)
    {
        if (_isExpired) return; // Do nothing if projectile is expired.

        int collisionLayerResult = (1 << collision.gameObject.layer) & hitMask;
        if (collisionLayerResult == 0) return; // Do nothing if collision layer is not of a valid type.

        CreateImpactFX();

        // Get contact point and surface normal.
        ContactPoint contact = collision.GetContact(0);
        Vector3 hitPoint = contact.point;
        Vector3 surfaceNormal = contact.normal;

        var enemyScript = collision.gameObject.GetComponentInParent<Enemy>();
        if (enemyScript)
        {
            ApplyEnemyHit(collision, enemyScript, passingThrough: true); // Damage enemy and pass through it.
            if (selfCollider) // Temporarily ignore future collisions with all colliders of the enemy that was hit.
            {
                Collider[] enemyColliders = enemyScript.GetComponentsInChildren<Collider>();
                foreach (Collider col in enemyColliders)
                {
                    Physics.IgnoreCollision(selfCollider, col, true);
                    StartCoroutine(ReactivateEnemyColliders(enemyColliders, collision.gameObject));
                }
            }
        }
        else // Bounce if other collider does not belong to an enemy.
        {
            Vector3 reflectedDirection = Vector3.Reflect(transform.forward, surfaceNormal);
            Vector3 offset = 0.1f * surfaceNormal; // Offset projectile away from surface to prevent a double-trigger.
            transform.forward = reflectedDirection;
            transform.position += offset;
            rb.rotation = Quaternion.LookRotation(reflectedDirection);
            rb.position += offset;
            InitializeReturn(); // Start returning immediately upon bounce.
        }

        if (collision.rigidbody) // Apply kockback force to the object collided with.
        {
            Vector3 knockback = hitForce * transform.forward;
            collision.rigidbody.AddForceAtPosition(knockback, hitPoint, ForceMode.Impulse);
        }

    }

    /// <summary>
    ///   <para>
    ///     Initializes parameters that are necessary for the attacking and arcing functionality of the axe projectile.
    ///   </para>
    /// </summary>
    /// <param name="attackPosition"> End position of the attack path. </param>
    /// <param name="throwerCenter"> A transform at the center of the thrower. </param>
    /// <param name="mask"> Layers that are valid for collision. </param>
    /// <param name="damage"> Damage of the projectile. </param>
    /// <param name="flyDistance"> Distance for the trail effect. </param>
    /// <param name="attacker"> Entity of the thrower. </param>
    public void Init(Vector3 attackPosition, Transform throwerCenter, LayerMask mask,
                     float damage, float flyDistance = 100, Entity attacker = null)
    {
        // Initialize member variables.
        _attackPosition = attackPosition;
        _throwerCenter = throwerCenter;
        hitMask = mask;
        actualDamage = damage;
        this.attacker = attacker;
        age = 0;
        rb.freezeRotation = true;
        trail.Clear();
        trail.time = lifeTime * 5;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;

        // Initialize model rotation.
        _modelTransform.rotation = Quaternion.Euler(90, 0, 0);

        // Initialize arcing logic.
        InitializeArcPath(_attackPosition);
        _isInitialized = true;
    }

    /// <summary>
    ///   <para>
    ///     Rotates and moves the projectile along its current arcing path on any frame this method is called.
    ///   </para>
    /// </summary>
    private void RotateAndMove()
    {
        float currDistance = _currTargetDirection.magnitude;

        // Get current turn rate.
        float targetDegrees = Vector3.Angle(transform.forward, _currTargetDirection);
        float targetRadians = Mathf.Deg2Rad * targetDegrees;
        float baseTurnRateRadians = 2 * _projectileSpeed * Mathf.Sin(targetRadians) / currDistance;
        float baseTurnRateDegrees = Mathf.Rad2Deg * baseTurnRateRadians;

        if (_isReturning) // Only perform turn rate corrections while returning.
        {
            float throwerDisplacement = Vector3.Distance(_throwerCenter.position, _throwerOriginOnReturn);
            float correctionMultiplier = 1 + (throwerDisplacement / currDistance);
            baseTurnRateDegrees *= correctionMultiplier;
        }

        // Rotate towards target position.
        Quaternion targetRotation = Quaternion.LookRotation(_currTargetDirection);
        Quaternion rotateIncrement = Quaternion.RotateTowards(rb.rotation, targetRotation, Time.fixedDeltaTime * baseTurnRateDegrees);
        rb.MoveRotation(rotateIncrement);

        // Move towards target position.
        float moveIncrement = Time.fixedDeltaTime * _projectileSpeed;
        if (moveIncrement < currDistance) // Ensure target is never passed.
            rb.MovePosition(rb.position + (moveIncrement * transform.forward));
        else
            rb.transform.position = _currTargetPosition;
    }

    /// <summary>
    ///   <para>
    ///     Initializes all necessary member variables for the return path.
    ///   </para>
    /// </summary>
    private void InitializeReturn()
    {
        _currTargetPosition = _throwerCenter.position;
        _currTargetDirection = _currTargetPosition - transform.position;
        _throwerOriginOnReturn = _throwerCenter.position;
        _isReturning = true;
    }

    /// <summary>
    ///   <para>
    ///     Initializes an arc path to a target position.
    ///   </para>
    /// </summary>
    /// <param name="targetPosition"> Position of the target. </param>
    private void InitializeArcPath(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < 1e-3) return; // Do nothing if distance is very small.

        // Set the transform rotation and the rigidbody rotation to the same angle at the same time to avoid a race condition.
        float initialDegrees = _maxArcDegrees * Mathf.Clamp01(distance / _arcFlatteningCapDistance); // Cap initial angle at maximum arc degrees.
        Vector3 targetDirection = targetPosition - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion initialRotation = targetRotation * Quaternion.Euler(0, initialDegrees, 0);
        transform.rotation = initialRotation;
        rb.rotation = initialRotation;
    }

    /// <summary>
    ///   <para>
    ///     Reactivates collision for an enemy's colliders after they are all no longer overlapping the axe projectile
    ///     in order to make getting damaged on the return path possible.
    ///   </para>
    /// </summary>
    /// <param name="enemyColliders"> All colliders of the enemy. </param>
    /// <param name="enemy"> Enemy script of the enemy. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator ReactivateEnemyColliders(Collider[] enemyColliders, GameObject enemy)
    {
        // Wait until either the enemy is dead or all of its colliders are no longer overlapping the projectile.
        yield return new WaitUntil(() => !enemy || !enemyColliders.Any(col => IsTouching(selfCollider, col)));
        if (!enemy) yield break; // Do nothing and end coroutine if enemy is dead.
        foreach (Collider col in enemyColliders)
            if (col != null)
                Physics.IgnoreCollision(selfCollider, col, false);
    }

    /// <summary>
    ///   <para>
    ///     Checks if two colliders are overlapping.
    ///   </para>
    /// </summary>
    /// <param name="a"> Collider a. </param>
    /// <param name="b"> Collider b. </param>
    /// <returns> True if colliders are overlapping, false otherwise. </returns>
    private bool IsTouching(Collider a, Collider b)
    {
        if (a == null || b == null) return false;
        return Physics.ComputePenetration(a, a.transform.position, a.transform.rotation,
                                          b, b.transform.position, b.transform.rotation,
                                          out _, out _);
    }

    /// <summary>
    ///   <para>
    ///     Returns the projectile straight to the player and destroys it. Will destroy the
    ///     projectile no matter what after a given number of seconds has elapsed.
    ///   </para>
    /// </summary>
    /// <param name="seconds"> Seconds before ensured destruction. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator TimeoutReturn(float seconds)
    {
        WaitForFixedUpdate waitForFixed = new();
        
        float timer = seconds;
        while (timer > 0)
        {
            float moveIncrement = Time.fixedDeltaTime * _timeoutReturnSpeed;
            float currDistance = Vector3.Distance(transform.position, _throwerCenter.position);
            if (moveIncrement > currDistance) break; // Exit loop if thrower is reached.

            // Rotate and move towards thrower's position.
            Vector3 targetDirectionUnitVector = (_throwerCenter.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirectionUnitVector);
            rb.MoveRotation(targetRotation);
            rb.MovePosition(rb.position + (moveIncrement * transform.forward));

            timer -= Time.fixedDeltaTime;
            yield return waitForFixed;
        }

        DestroyAxe(); // Destroy projectile if thrower was reached or return travel timed out.
    }

    /// <summary>
    ///   <para>
    ///     Destroys the projectile and returns its private member booleans to false in case
    ///     it is sent to the object pool.
    ///   </para>
    /// </summary>
    private void DestroyAxe()
    {
        _isInitialized = false;
        _isExpired = false;
        _isReturning = false;
        ReturnToSource(); // Destroy.
    }
}
