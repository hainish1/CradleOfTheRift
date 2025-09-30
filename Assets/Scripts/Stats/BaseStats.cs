using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    public float health = 10;
    public float projectileDamage = 1;
    public float moveSpeed = 10; // idk what this is
    public int dashCharges = 1;
}
