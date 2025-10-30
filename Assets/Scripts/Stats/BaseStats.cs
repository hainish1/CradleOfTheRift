using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    [Header("Player Health")]
    public float health = 10;

    [Space]

    [Header("Player Movement")]
    // public float moveSpeed = 10;
    public float jumpHeight = 10;
    [Tooltip("Max move speed in units per second.")] public float moveSpeed = 10;
    
    [Header("Player Dash")]
    [Tooltip("Distance that the player character dashes in units.")] public float dashDistance = 14;
    [Tooltip("How quickly the player character travels Dash Distance in units per second.")] public float dashSpeed = 100;
    [Tooltip("Seconds needed for dash charges to come off cooldown.")] public float dashCooldown = 2;
    [Tooltip("The quantity of available dash charges.")] public int dashCharges = 2;

    [Space]

    [Header("Player Attack")]
    public float projectileDamage = 1;
    public float meleeDamage = 1;
    public float slamDamage = 2;
    public float slamAttackRadius = 10;

    public float attackSpeed = 5.0f; // attacks per second

    public float projectileSpread = 0.1f; // in radians
}

