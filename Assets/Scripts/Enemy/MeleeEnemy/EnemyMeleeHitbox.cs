using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Class - Put on the Hitbox GameObject, checks if it collides with the Player Object, and applies a hit
/// </summary>
public class EnemyMeleeHitbox : MonoBehaviour
{
    public EnemyMelee owner;

    /// <summary>
    /// Checks if it collides with the Player Object, and applies a hit
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            owner?.TryApplyHit(other);
        }
    }

    
}
