using System;
using UnityEngine;

// mario style stomp on enemies
public class StompDamage : IDisposable
{
    private Entity owner;
    private GameObject ownerGameObject;
    private float damagePerStack;
    private float bounceForce;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private PlayerMovement playerMovement;
    private PlayerMovement playerMovementV4;
    private CharacterController characterController;
    private SphereCollider stompDetector;

    // detection stuff
    private float detectionRadius = 1.5f;
    private Vector3 detectionOffset = new Vector3(0, -1f, 0);
    private float minFallSpeed = 2f;
    private LayerMask enemyLayer;

    public StompDamage(Entity owner, float damagePerStack, float bounceForce, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        this.ownerGameObject = owner.gameObject;
        this.damagePerStack = damagePerStack;
        this.bounceForce = bounceForce;
        this.stacks = initialStacks < 1 ? 1 : initialStacks;
        this.duration = durationSec;
        this.timer = durationSec;

        playerMovement = owner.GetComponent<PlayerMovement>();
        playerMovementV4 = owner.GetComponent<PlayerMovement>();
        characterController = owner.GetComponent<CharacterController>();

        enemyLayer = LayerMask.GetMask("Enemy");
        CreateStompDetector();
    }

    private void CreateStompDetector()
    {
        GameObject detectorObj = new GameObject("StompDetector");
        detectorObj.transform.SetParent(ownerGameObject.transform);
        detectorObj.transform.localPosition = detectionOffset;
        detectorObj.transform.localRotation = Quaternion.identity;
        
        stompDetector = detectorObj.AddComponent<SphereCollider>();
        stompDetector.isTrigger = true;
        stompDetector.radius = detectionRadius;
        stompDetector.center = Vector3.zero;
        
        Rigidbody rb = detectorObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var handler = detectorObj.AddComponent<StompCollisionHandler>();
        handler.Initialize(this);
        
        Debug.Log($"[Stomp] Detector created! Radius: {detectionRadius}, Offset: {detectionOffset}");
    }

    public void AddStack(int count = 1)
    {
        if (count < 1) count = 1;
        stacks += count;
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    public void OnEnemyDetected(Collider enemyCollider)
    {
        if (disposed || ownerGameObject == null) return;

        // need to be falling fast enough
        float verticalVelocity = 0f;
        if (characterController != null)
            verticalVelocity = characterController.velocity.y;

        if (verticalVelocity >= -minFallSpeed) return;
        
        Enemy enemy = enemyCollider.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        // need to be above enemy
        float playerY = ownerGameObject.transform.position.y;
        float enemyY = enemy.transform.position.y;
        if (playerY <= enemyY + 0.5f) return;
        
        float totalDamage = damagePerStack * stacks;
        var damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(totalDamage);
            CombatEvents.ReportDamage(owner, enemy, totalDamage);

            var flash = enemy.GetComponentInChildren<TargetFlash>();
            if (flash != null) flash.Flash();

            var kb = enemy.GetComponent<AgentKnockBack>();
            if (kb != null)
            {
                Vector3 direction = (enemy.transform.position - ownerGameObject.transform.position).normalized;
                direction.y = 0.3f;
                kb.ApplyImpulse(direction * 5f);
            }

            ApplyBounce();

            Debug.Log($"[Stomp] Dealt {totalDamage:F1} damage to {enemy.name} (x{stacks} stacks)");
        }
    }

    private void ApplyBounce()
    {
        if (playerMovementV4 != null)
        {
            playerMovementV4.SetVerticalVelocityFactor(bounceForce);
        }
        else if (playerMovement != null)
        {
            playerMovement.ApplyImpulse(Vector3.up * bounceForce);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (stompDetector != null)
        {
            UnityEngine.Object.Destroy(stompDetector.gameObject);
        }
    }
}

public class StompCollisionHandler : MonoBehaviour
{
    private StompDamage stompEffect;
    private float cooldown = 0.2f;
    private float lastStompTime = -999f;
    private SphereCollider detectorCollider;
    private int enemyLayer;

    public void Initialize(StompDamage effect)
    {
        stompEffect = effect;
        detectorCollider = GetComponent<SphereCollider>();
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }
    
    private void OnDrawGizmos()
    {
        if (detectorCollider != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 worldCenter = transform.position + detectorCollider.center;
            Gizmos.DrawWireSphere(worldCenter, detectorCollider.radius);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stompEffect == null) return;
        if (Time.time - lastStompTime < cooldown) return;
        
        // check if its an enemy
        if (other.gameObject.layer != enemyLayer) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            stompEffect.OnEnemyDetected(other);
            lastStompTime = Time.time;
        }
    }
    
}

