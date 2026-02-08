using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class - Represents a stationary explosion that damages anything in its area after a delay.
/// </summary>
public class EnemyDelayedAOE : MonoBehaviour
{
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("AOE Effect")]
    private float radius = 8f;
    private float damage = 5f;
    private float delay = 1f;
    [SerializeField] private GameObject explosionVFX;

    /// <summary>
    /// Initialize the AOE explosion with what it can hit, its damage, and its radius
    /// </summary>
    /// <param name="mask"> Collection of types of objects the AOE can affect. </param>
    /// <param name="aoeDamage"> Damage of the AOE. </param>
    /// <param name="aoeRadius"> Radius of the AOE effect. </param>
    public void Init(float newRadius, float newDamage, float newDelay)
    {
        radius = newRadius;
        damage = newDamage;
        delay = newDelay;

        StartCoroutine(SpawnAOEEffect());
    }

    /// <summary>
    /// Spawn the AOE effect at the current position and deal damage to players within the radius
    /// </summary>    
    public IEnumerator SpawnAOEEffect()
    {
        CreateExplosionVFX();

        yield return new WaitForSeconds(delay);

        // Check for players in radius and damage them once.
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitMask);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        foreach (var col in hits)
        {
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null && !dmg.IsDead && !damagedTargets.Contains(dmg))
            {
                var pm = col.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                {
                    dmg.TakeDamage(damage);

                    damagedTargets.Add(dmg);
                    Debug.Log(damage + " Delayed AOE Damage dealt to " + dmg.ToString() + " by " + this.ToString());
                }
            }
        }

        Destroy(gameObject);
    }

    public void CreateExplosionVFX()
    {
        if (explosionVFX == null)
        {
            Debug.LogError("No explosion VFX has been assigned!");
            return;
        }
        GameObject newFx = Instantiate(explosionVFX);
        newFx.transform.position = transform.position;
        newFx.transform.rotation = Quaternion.identity;
        newFx.transform.localScale = Vector3.one * radius * 0.25f;
        Destroy(newFx, 2f); // destroy after two second
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
