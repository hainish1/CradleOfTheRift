using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

// Based off of video tutorial
// https://www.youtube.com/watch?v=UjkSFoLxesw
// And some of Hainish's GitHub!

public class EnemyAI : MonoBehaviour
{
    [Header("NavMesh References")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    
    // Patrolling mechanics
    [Header("Patrolling Mechanics")] 
    public float patrolPointRadius;
    public float patrollingSpeed;
    
    [Header("Chasing Mechanics")]
    public float chasingSpeed;
    public float sightRange;
    
    // Attacking Mechanics
    [Header("Attacking Mechanics")]
    public float attackRange;
    [Tooltip("How close the enemy gets before stopping and attacking.")]
    public float stopPursuingDistance;
    // public float timeBetweenAttacks;
    // private bool canAttack;
    public Weapon weapon;
    
    [Header("Debugging")]
    [ReadOnly]
    public Vector3 patrolPoint;
    public bool patrolPointSet;
    // States
    public bool playerIsInSightRange, playerIsInAttackRange;

    private void Awake()
    {
        // We are going to have tons of enemies, we don't want to set these variables individually!
        // THIS COULD TOTALLY BE CHANGED LATER!
        player = GameObject.FindGameObjectWithTag("Player").transform;
        // Possibly have this a RequiredComponent
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Check if the player is in sight.
        // Using a sphere collision here to detect that. Neat!
        playerIsInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerIsInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        
        // If the player is not in sight or attack range, start patrolling for player.
        if (!playerIsInSightRange && !playerIsInAttackRange) Patrolling();
        // If the player is in sight, but not close enough to hit, chase!
        if (playerIsInSightRange && !playerIsInAttackRange) ChasePlayer();
        // If we are able to attack, attack!
        if (playerIsInSightRange && playerIsInAttackRange) AttackPlayer();
    }

    /// <summary>
    /// Sets the enemy's AI state to roaming.
    /// Generates random points that the AI walks to.
    /// </summary>
    private void Patrolling()
    {
        print("Patrolling!!");
        agent.speed = patrollingSpeed;
        // If we don't know where we are going, find a place to go!
        if (!patrolPointSet) CreateNewPatrolPoint();
        // Now go to the place we generated.
        if(patrolPointSet) agent.SetDestination(patrolPoint);
        
        float distanceToWalkPoint = (transform.position - patrolPoint).magnitude;
        // If we have reached our walk point, restart the loop!
        if (distanceToWalkPoint < 1f)
            patrolPointSet = false;
    }

    /// <summary>
    /// Generates a new point for the AI to walk to.
    /// </summary>
    private void CreateNewPatrolPoint()
    {
        float goalX = transform.position.x + Random.Range(-patrolPointRadius, patrolPointRadius);
        float goalZ = transform.position.z + Random.Range(-patrolPointRadius, patrolPointRadius);

        patrolPoint = new Vector3(goalX, transform.position.y, goalZ);
        
        // Make sure that the point is actually on the map.
        // This is done by checking if there is a ground beneath the goal point.
        if (Physics.Raycast(patrolPoint, -transform.up, 2f, whatIsGround))
        {
            patrolPointSet = true;
        }
    }

    /// <summary>
    /// Sets the AI's state to chasing hte player.
    /// </summary>
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
        agent.speed = chasingSpeed;
    }

    /// <summary>
    /// Attacks the target player.
    /// </summary>
    private void AttackPlayer()
    {
        // If we are in attack range of the player
        // We don't need to get any closer!
        // WE CAN CHANGE THIS LATER FOR MELEE ENEMIES!
        if ((transform.position - player.position).magnitude < stopPursuingDistance)
            agent.SetDestination(transform.position);
        
        transform.LookAt(player);
        // This makes it so that the bots don't look up/down.
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        
        weapon.Fire();
    }

    // /// <summary>
    // /// Allows the weapon to be fired again.
    // /// </summary>
    // private void ResetAttack()
    // {
    //     canAttack = true;
    // }

    /// <summary>
    /// Draws the radii of the attack range and the sight range.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, stopPursuingDistance);
    }
}
