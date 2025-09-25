using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    public int health = 10;
    public int projectileDamage = 1;
}
