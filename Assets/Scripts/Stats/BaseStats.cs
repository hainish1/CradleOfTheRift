using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    [Header("Player Health")]
    public float health = 10;

    [Space]

    [Header("Player Movement")]
    public float moveSpeed = 10; // idk what this is
    public float jumpHeight = 10;
    [Header("Player Dash")]
    public float dashSpeed = 100;
    public float dashDistance = 14;
    public float dashCooldown = 2;
    public int dashCharges = 2;

    [Space]

    [Header("Player Attack")]
    public float projectileDamage = 1;
    public float meleeDamage = 1;
    public float slamDamage = 2;
    public float slamAttackRadius = 10;
}

