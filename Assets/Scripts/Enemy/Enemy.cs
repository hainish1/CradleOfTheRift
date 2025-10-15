using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class : Base Enemy class that Melee and Range Inherit from. 
/// Declares common enemy properties
/// </summary>
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

    /// <summary>
    /// Called when any Enemy dies, Resets navmesh agent and destroys gameObject(self)
    /// 
    /// Not being used now, Die controlled by Enemy Health
    /// </summary>
    public virtual void Die()
    {
        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Enemy Update is used for checking the state an enemy is in
    /// </summary>
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
