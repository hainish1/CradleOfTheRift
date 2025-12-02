using UnityEngine;

public class EnemyBoss_SS : Enemy
{
    [Header("Boss Attack Settings")]
    public GameObject expEnemyPrefab;  // explodies
    public Transform[] spawnPoints;
    public float bombSpawnInterval = 4f;
    public float idleTime = 1f;
    public Transform firePoint;
    public float slimeArcDistance;
    public float slimeArcDuration;

    [Header("Ring Attack Settings")]
    public float maxRadius;
    public float duration;
    public float explosionDamage;
    public float explosionCameraShakeForce = 10f;
    public GameObject poofVFX;

    public GameObject explosionVFXPrefab;
    public GameObject shockwaveVFXPrefab;

    [Space]

    [Header("Leap Attack")]
    [SerializeField] private EnemyMeleeHitbox hitbox;
    [HideInInspector] public bool hitAppliedThisAttack;
    public float slamDamage = 3f;
    public float minRequiredPlayerDistance = 90f; // to ensure our boss does not leap like very far
    public float playerTooClose = 10f; // to ensure our boss does not leap when its too close

    public float knockbackPower = 3f;
    public float windupTime = 0.25f;
    public float leapDuration = 0.6f;
    public float leapHeight = 3f; 
    public GameObject flashVFX;

    private IdleState_Boss idle;
    private SpawnBombState_Boss bombState;
    private RecoveryState_Boss recovery;
    private RingAttackState_Boss ringAttack;
    private LeapAttackState_Boss leapAttack;


    public override void Start()
    {
        base.Start();
        idle = new IdleState_Boss(this, stateMachine);
        bombState = new SpawnBombState_Boss(this, stateMachine);
        recovery = new RecoveryState_Boss(this, stateMachine);
        ringAttack = new RingAttackState_Boss(this, stateMachine, maxRadius, duration, explosionDamage, playerMask);
        leapAttack = new LeapAttackState_Boss(this, stateMachine);
        stateMachine.Initialize(idle);
    }

    public override void Die()
    {
        base.Die();
    }

    public EnemyState GetIdle() => idle;
    public EnemyState GetBombState() => bombState;
    public EnemyState GetRecoveryState() => recovery;
    public EnemyState GetExploisionState() => ringAttack;
    public EnemyState GetLeapAttackState() => leapAttack;

    public void CreatePoofVFX(Vector3 spawnPosition)
    {
        if (poofVFX == null) return;
        GameObject newFx = Instantiate(poofVFX);
        newFx.transform.position = spawnPosition;
        newFx.transform.rotation = Quaternion.identity;

        Destroy(newFx, 1); // destroy after one second
    }

    public void CreateVFX(GameObject vfxPrefab,  Vector3 spawnPosition, float destroyAfter)
    {
        if (vfxPrefab == null) return;
        GameObject newFx = Instantiate(vfxPrefab);
        newFx.transform.position = spawnPosition;
        newFx.transform.rotation = Quaternion.identity;
        Destroy(newFx, destroyAfter); // destroy after one second
    }


    public void EnableHitBox(bool enable)
    {
        if (hitbox != null && hitbox.gameObject.activeSelf != enable)
        {
            hitbox.gameObject.SetActive(enable);
        }
    }


        /// <summary>
    /// Try to apply damage and impulse to the player GameObject caught in colliders 
    /// </summary>
    /// <param name="playerCol"></param>
    public void TryApplyHit(Collider playerCol)
    {
        if (hitAppliedThisAttack) return;
        if (Time.time < nextAttackAllowed) return;

        Vector3 toPlayer = playerCol.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        toPlayer.Normalize();

        var pm = playerCol.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyImpulse(toPlayer * knockbackPower);

            var damageable = pm.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(slamDamage);
            }
        }
        hitAppliedThisAttack = true;
        nextAttackAllowed = Time.time + attackCooldown;
        EnableHitBox(false);
    }



    public bool IsPlayerTooFar()
    {
        return Vector3.Distance(transform.position, target.position) > minRequiredPlayerDistance;
    }

    public bool IsPlayerTooClose()
    {
        return Vector3.Distance(transform.position, target.position) <= playerTooClose;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerTooClose);

    }
}