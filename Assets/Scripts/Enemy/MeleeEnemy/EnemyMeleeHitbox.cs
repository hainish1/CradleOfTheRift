using UnityEngine;

public class EnemyMeleeHitbox : MonoBehaviour
{
    public EnemyMelee owner;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            owner?.TryApplyHit(other);
        }      
    }
}
