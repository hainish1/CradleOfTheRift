using UnityEngine;
using UnityEngine.AI;


public abstract class Enemy : MonoBehaviour
{
    // [Header("Test Health Stuff")]
    // public int maxHealth = 3;
    // public int currentHealth;

    [Header("Target settings")]
    public Transform target;
    public NavMeshAgent agent { get; private set; }

    [Header("Common Enemy Settings")]
    public float aggressionRange;
    public float playerOutOfRange;
    public float turnSpeed;
    public float attackCooldown;
    public LayerMask playerMask = ~0;

    [Header("Debugging helpers")]
    public bool gizosOn = true;
    public Color aggressionColor = Color.red;


    public float nextAttackAllowed;
    protected EnemyStateMachine stateMachine;

    public virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = new EnemyStateMachine();

        // currentHealth = maxHealth;
    }

    public virtual void Start()
    {
        if (target == null)
        {
            // then find in scene something with the player TAG
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    // public void ApplyDamage(int amount)
    // {
    //     currentHealth -= amount;
    //     if (currentHealth <= 0) Die();
    // }

    public virtual void Die()
    {
        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        Destroy(gameObject);
    }

    public virtual void Update()
    {
        stateMachine.Tick();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!gizosOn) return;
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, aggressionRange);
    }
}   
