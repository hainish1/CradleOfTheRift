// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah.
//     Projectile base class written by Hainish Acharya for GAMES 4500, University of Utah.
//   </para>
// </summary>

using System.Collections;
using UnityEngine;

public class AxeProjectile : Projectile
{
    [Header("Axe Model Parameters")]
    [SerializeField] private Transform _modelTransform;
    [SerializeField] private float _whirlSpeed;

    [Header("Axe Projectile Parameters")]
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private float _maxArcDegrees;
    [SerializeField] private float _arcFlatteningCapDistance;
    [SerializeField] private float _timeoutReturnSpeed;
    public float MaxTravelDistance;
    private bool _isInitialized = false;
    private bool _isExpired = false;
    private bool _isReturning = false;
    private float _baseTurnRate;
    private Vector3 _attackTargetPos = Vector3.zero;
    private Vector3 _returnTargetOriginOnReturn;
    private GameObject _returnTargetObj;
    private Transform _returnTargetCenter;
    private Coroutine _whirlCoroutine;

    void Start()
    {
        _whirlCoroutine = StartCoroutine(WhirlModel()); // Start axe model whirling.
    }

    protected override void FixedUpdate()
    {
        AxeProjectile script = GetComponent<AxeProjectile>();
        print($"Component ID: {script.GetInstanceID()}");

        if (!_isInitialized) return; // Wait until initialization has occured.
        if (_isExpired) return; // Do nothing if projectile is expired.
        if (!_returnTargetCenter) DestroyAxe(); // Destroy axe if player died.

        age += Time.deltaTime;
        if (age >= lifeTime) // Destroy axe if it did not return quickly enough.
        {
            _isExpired = true;
            StartCoroutine(TimeoutReturn());
            return;
        }

        // Start returning when target position is reached.
        if (!_isReturning && Vector3.Distance(transform.position, _attackTargetPos) < 2) InitializeReturn();

        // Destroy axe when it reaches return target.
        if (_isReturning && Vector3.Distance(transform.position, _returnTargetCenter.position) < 2) DestroyAxe();

        FadeTrailVisuals();
        RotateAndMove();
    }

    public override void Update() {}

    public override void OnCollisionEnter(Collision collision)
    {
        if (_isExpired) return; // Do nothing if projectile is expired.

        int collisionLayerResult = (1 << collision.gameObject.layer) & hitMask;
        if (collisionLayerResult == 0) return; // Do nothing if collision layer is not of a valid type.
        
        CreateImpactFX();
        
        var enemyScript = collision.collider.GetComponentInParent<Enemy>();
        if (enemyScript)
        {
            ApplyEnemyHit(collision, enemyScript, passingThrough: true); // Damage enemy while passing through it.
            if (selfCollider) // Temporarily ignore future collisions with all colliders of the enemy that was hit.
            {
                Collider[] enemyColliders = enemyScript.GetComponentsInChildren<Collider>();
                foreach (Collider collider in enemyColliders)
                {
                    Physics.IgnoreCollision(selfCollider, collider, true);
                    StartCoroutine(ReactivateEnemyColliders(enemyColliders, delaySeconds: 1));
                }
            }
        }

        if (collision.rigidbody) // Apply kockback force to the object collided with.
        {
            Vector3 knockback = hitForce * transform.forward;
            collision.rigidbody.AddForceAtPosition(knockback, collision.contacts[0].point, ForceMode.Impulse);
        }
    }


    public void Init(Vector3 attackTargetPos,
                     GameObject returnTargetObj,
                     Transform returnTargetCenter,
                     LayerMask mask,
                     float damage,
                     float flyDistance = 100,
                     Entity attacker = null)
    {
        // Initialize member variables.
        _attackTargetPos = attackTargetPos;
        _returnTargetObj = returnTargetObj;
        _returnTargetCenter = returnTargetCenter;
        hitMask = mask;
        actualDamage = damage;
        this.attacker = attacker;
        age = 0;
        rb.freezeRotation = true;
        trail.Clear();
        trail.time = 0.25f;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;

        // Initialize arcing logic.
        float distance = Vector3.Distance(startPos, _attackTargetPos);
        InitializeArcPath(distance, _attackTargetPos);
        rb.linearVelocity = Vector3.zero;
        _isInitialized = true;

        AxeProjectile script = GetComponent<AxeProjectile>();
        print($"Component ID: {script.GetInstanceID()}");
    }


    private void RotateAndMove()
    {
        Vector3 currTargetPos = _isReturning ? _returnTargetCenter.position : _attackTargetPos;
        float currTurnRate = _isReturning
            // Increase turn rate according to how far the return target moved from its original position on the return path.
            ? _baseTurnRate * (1 + Vector3.Distance(_returnTargetCenter.position, _returnTargetOriginOnReturn))
            : _baseTurnRate;

        // Rotate towards target position.
        Quaternion targetRotation = CalculateRotationToTarget(currTargetPos);
        Quaternion rotateIncrement = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * currTurnRate);
        rb.MoveRotation(rotateIncrement);
        
        // Move towards target position.
        float moveIncrement = Time.deltaTime * _projectileSpeed;
        rb.MovePosition(rb.position + (moveIncrement * transform.forward));
    }


    private void InitializeReturn()
    {
        float distance = Vector3.Distance(transform.position, _returnTargetCenter.position);
        InitializeArcPath(distance, _returnTargetCenter.position);
        _returnTargetOriginOnReturn = _returnTargetCenter.position;
        _isReturning = true;
    }


    private void InitializeArcPath(float distance, Vector3 targetPos)
    {
        if (distance < 1e-3) return; // Do nothing if distance is very small.

        float initialDegrees = _maxArcDegrees * Mathf.Clamp01(distance / _arcFlatteningCapDistance); // Cap initial angle at maximum arc degrees.
        float initialRadians = initialDegrees * Mathf.Deg2Rad;
        float turnRateRadians = 2 * _projectileSpeed * Mathf.Sin(initialRadians) / distance; // Calculate constant turn rate to intersect target position.
        _baseTurnRate = turnRateRadians * Mathf.Rad2Deg;

        // Set the transform rotation and the rigidbody rotation to the same angle at the same time to avoid a race condition.
        Quaternion targetRotation = CalculateRotationToTarget(targetPos);
        Quaternion initialRotation = targetRotation * Quaternion.Euler(0, initialDegrees, 0);
        rb.rotation = initialRotation;
        transform.rotation = initialRotation;
    }


    private Quaternion CalculateRotationToTarget(Vector3 targetPos)
    {
        Vector3 targetDirectionUnitVector = (targetPos - transform.position).normalized;
        return Quaternion.LookRotation(targetDirectionUnitVector);
    }


    private IEnumerator WhirlModel()
    {
        while (true)
        {
            _modelTransform.Rotate(xAngle: 0, yAngle: 0, Time.deltaTime * _whirlSpeed);
            yield return null;
        }
    }


    private IEnumerator ReactivateEnemyColliders(Collider[] enemyColliders, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        foreach (Collider collider in enemyColliders)
            Physics.IgnoreCollision(selfCollider, collider, false);
    }


    private IEnumerator TimeoutReturn()
    {
        float timer = 2;
        while (timer > 0) // Rapidly move straight towards return target for two seconds.
        {
            float moveIncrement = Time.deltaTime * _timeoutReturnSpeed;
            float currDistance = Vector3.Distance(transform.position, _returnTargetCenter.position);
            if (moveIncrement > currDistance) break; // Exit loop if the return target is reached.

            // Rotate and move towards return target position.
            Vector3 targetDirectionUnitVector = (_returnTargetCenter.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirectionUnitVector);
            rb.MoveRotation(targetRotation);
            rb.MovePosition(rb.position + (moveIncrement * transform.forward));

            timer -= Time.deltaTime;
            yield return null;
        }

        DestroyAxe(); // Destroy axe if the return target is reached or return travel times out.
    }


    private void DestroyAxe()
    {
        // Return axe to thrower before it is destroyed.
        PlayerShooter shooterScript = _returnTargetObj.GetComponent<PlayerShooter>();
        if (shooterScript) shooterScript.ReturnAxe();

        StopCoroutine(_whirlCoroutine);
        Destroy(gameObject);
    }
}
