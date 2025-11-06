using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;

public class PlayerGroundSlam : MonoBehaviour
{
    [Header("Settings")]
    public float slamStartDelay = .1f;
    public float slamDownSpeed = 40f; // vertical down speed
    public float minSlamHeight = 2.0f; // need this height

    // public float slamDamage = 2; // for now
    public float slamKnockbackForce = 30f;
    public LayerMask enemyMask;

    [Header("Cinemachine shake")]
    public CinemachineImpulseSource slamImpulseSource;
    public float shakeForce = 1.0f;
    [SerializeField] private GameObject groundImpactFX;
    [SerializeField] private Transform groundSlamPoint;



    [Header("debug")]
    public bool debug = true;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private bool isSlamming = false;
    // private bool canDoSlam => !controller.isGrounded && (controller.transform.position.y > minSlamHeight);
    private bool canDoSlam => !controller.isGrounded && HeightAboveGroundSufficient();

    private Entity _playerEntity;
    private float SlamDamage => _playerEntity.Stats.SlamDamage;
    // private float SlamRadius => _playerEntity.Stats.SlamRadius;
    // public float slamRadius = 5f;

    // Sounds
    private PlayerAudioController audioController;
    private PlayerGroundCheck groundCheck;

    [SerializeField] private float previewSlamRadius = 10f;

    private float CurrentSlamRadius
    {
        get
        {
            if (_playerEntity != null)
            {
                return Mathf.Max(0.01f, _playerEntity.Stats.SlamRadius);
            }
            return Mathf.Max(0.01f, previewSlamRadius);
        }
    }


    void Awake()
    {
        _playerEntity = GetComponent<Entity>();
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        audioController = GetComponent<PlayerAudioController>();
        groundCheck = GetComponent<PlayerGroundCheck>();
    }

    void OnEnable()
    {
        var action = new InputAction("GroundSlam", binding: "<Keyboard>/x");
        action.Enable();
        action.performed += ctx =>
        {
            TryToStartSlam();
            Debug.Log("X pressed, trying to slam");
        };
    }

    void TryToStartSlam()
    {
        if (!isSlamming && canDoSlam)
        {
            StartCoroutine(SlamRoutine());
        }
    }

    private IEnumerator SlamRoutine()
    {
        if (FallDamageBonus.Instance != null)
        {
            FallDamageBonus.Instance.RecordSlamStartHeight(transform.position.y);
        }
        
        yield return new WaitForSeconds(slamStartDelay);

        isSlamming = true;

        float originalGravity = playerMovement != null ? playerMovement._gravityMultiplier : 1f;
        if (playerMovement != null) playerMovement._gravityMultiplier = 0;

        Vector3 fallVelocity = Vector3.down * slamDownSpeed;

        // go down when not grounded
        // while (!controller.isGrounded)
        // {
        //     controller.Move(fallVelocity * Time.deltaTime);
        //     yield return null;
        // }

        // now using this instead of fall velocity thing
        CollisionFlags flags;
        do
        {
            flags = controller.Move(Vector3.down * slamDownSpeed * Time.deltaTime);
            yield return null;
        }
        while ((flags & CollisionFlags.Below) == 0);


        // restore normal gravity again
        // if (playerMovement != null) playerMovement._gravityMultiplier = originalGravity;
        // restore original hover now 
        if(playerMovement != null)
        {
            playerMovement._gravityMultiplier = originalGravity;
            playerMovement.SetPlayerIsGrounded(true);
            playerMovement.SnapToHoverAfterSlam();
        }

        if (debug)
        {
            Debug.Log("Ground Slam!!");
        }

        // any effects we have
        DoImpactEffect(); // i wanna do a camera shake here

        // Play slam sound
        audioController?.PlaySlamSound();

        HashSet<Enemy> uniqueEnemies = new HashSet<Enemy>();
        // now do attacks to enemy in sphere overlap
        Collider[] hits = Physics.OverlapSphere(transform.position, CurrentSlamRadius, enemyMask);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !uniqueEnemies.Contains(enemy))
            {
                uniqueEnemies.Add(enemy);
                // first do knockback if enenmy has it
                var kb = enemy?.GetComponent<AgentKnockBack>();
                if (kb != null)
                {
                    Vector3 dir = (enemy.transform.position - transform.position).normalized + Vector3.up * 0.5f;
                    kb.ApplyImpulse(dir * slamKnockbackForce);
                }
                var flash = col.GetComponentInParent<TargetFlash>();
                if (flash != null) flash.Flash();

                // then do damage
                var dmg = enemy?.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    float totalDamage = SlamDamage;
                    
                    if (FallDamageBonus.Instance != null)
                    {
                        float bonusDamage = FallDamageBonus.Instance.GetBonusSlamDamage(transform.position.y);
                        totalDamage += bonusDamage;
                    }
                    
                    dmg.TakeDamage(totalDamage);
                }
            }
        }

        isSlamming = false;
    }

    void DoImpactEffect()
    {
        // camera shake here
        if (slamImpulseSource != null)
        {
            slamImpulseSource.GenerateImpulse(shakeForce);
        }
        CreateImpactFX();
    }

    void OnDrawGizmosSelected()
    {
        if (!debug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, CurrentSlamRadius);
    }

    protected void CreateImpactFX()
    {
        if (groundImpactFX == null) return;
        GameObject newFX = Instantiate(groundImpactFX);
        if (groundSlamPoint != null)
        {
            newFX.transform.position = groundSlamPoint.position;
        }
        else
        {
            newFX.transform.position = transform.position;
        }

        Destroy(newFX, 2);

        // GameObject newImpacFX = ObjectPool.instance.GetObject(bulletImpactFX, transform);
        // ObjectPool.instance.ReturnObject(newImpacFX, 1f); // return the effect back to the pool after 1 second of delay

    }
    
    private bool HeightAboveGroundSufficient()
    {
        Vector3 playerBottom = groundSlamPoint.transform.position;

        int layerMask = LayerMask.GetMask("Default", "Environment", "Interactable", "Obstacles", "Enemy");

        float height = PlayerGroundCheck.GetHeightAboveGround(playerBottom, layerMask, 99);

        return height >= minSlamHeight;
    }
}
