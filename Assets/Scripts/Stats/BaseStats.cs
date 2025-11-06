using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    [Header("Attack Parameters")]
    public float projectileDamage = 1;
    public float meleeDamage = 1;
    public float slamDamage = 2;
    public float slamRadius = 10;
    public float attackSpeed = 5.0f; // attacks per second
    public float projectileSpread = 0.1f; // in radians
    public int enableHomingProjectiles = 0; // use int instead of bool

    [Space]

    [Header("Health Parameters")]
    public float health = 10;

    [Space]

    [Header("Gravity Parameters")]
    [Tooltip("How much gravity is multiplied in units per second (base gravity value is -9.81).")] public float gravityMultiplier;
    [Tooltip("How quickly the player character decelerates to the aggregate gravity descent speed if it is exceeded in units per second.")] public float gravityAirDrag;

    [Space]

    [Header("Movement Parameters")]
    [Tooltip("Max move speed in units per second.")] public float moveSpeed = 10;

    [Space]

    [Header("KnockBack Parameters")]
    [Tooltip("Seconds needed for a knockback impulse to dissipate.")] public float kbDamping;
    [Tooltip("Seconds that controls are locked after a knockback impulse.")] public float kbControlsLockTime;
    [Tooltip("Seconds that dashing is locked after a knockback impulse.")] public float kbDashLockTime;

    [Space]

    [Header("Dash Parameters")]
    [Tooltip("Distance that the player character dashes in units.")] public float dashDistance = 14;
    [Tooltip("How quickly the player character travels Dash Distance in units per second.")] public float dashSpeed = 100;
    [Tooltip("Seconds needed for dash charges to come off cooldown.")] public float dashCooldown = 2;
    [Tooltip("The quantity of available dash charges.")] public int dashCharges = 2;

    [Space]

    [Header("Jump Parameters")]
    [Tooltip("Vertical jump strength in units per second.")] public float jumpForce = 10;

    [Space]

    [Header("Drift Parameters")]
    [Range(0, 1)]
    [Tooltip("How much gravity is divided when drifting.")] public float driftDescentDivisor;

    [Space]

    [Header("Flight Parameters")]
    [Tooltip("Max vertical flight speed in units per second.")] public float flightMaxSpeed;
    [Tooltip("Capacity value of flight energy")] public int flightMaxEnergy;
    [Tooltip("Amount of flight energy regeneration per second.")] public float flightRegenerationRate;
    [Tooltip("Amount of flight energy depleted per second.")] public float flightDepletionRate;
}
