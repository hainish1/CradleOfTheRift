// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah.
//     Projectile base class written by Hainish Acharya for GAMES 4500, University of Utah.
//   </para>
// </summary>

using UnityEngine;

public class MaceProjectile : Projectile
{
    [Header("Mace Projectile Parameters")]
    [SerializeField] private GameObject _shockwavePrefab;
    [SerializeField]
    [Tooltip("How much damage the projectile impact shockwave deals.")] private float _projectileShockwaveDamage;
    [SerializeField]
    [Tooltip("Radius of the projectile impact shockwave.")] private float _projectileShockwaveRadius;
    [SerializeField]
    [Tooltip("Knockback force of the projectile impact shockwave.")] private float _projectileShockwaveKnockback;
    private PlayerShockwaveController _shockwaveController;

    void Start()
    {
        _shockwaveController = attacker.gameObject.GetComponent<PlayerShockwaveController>();
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }
        // check layer mask
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        CreateImpactFX();

        GameObject shockwave = Instantiate(_shockwavePrefab, transform.position, Quaternion.identity);
        Shockwave shockwaveScript = shockwave.GetComponent<Shockwave>();
        shockwaveScript.Init(transform.position, hitMask, _projectileShockwaveDamage, _projectileShockwaveKnockback, _projectileShockwaveRadius, attacker);

        if (DelayedProjectiles.IsEnabled)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _projectileShockwaveRadius, hitMask);
            var marked = new System.Collections.Generic.HashSet<Enemy>();
            foreach (var col in hits)
            {
                var enemy = col.GetComponentInParent<Enemy>();
                if (enemy == null || marked.Contains(enemy)) continue;
                marked.Add(enemy);
                CreateDelayedDamageMark(enemy, col.ClosestPoint(transform.position));
            }
        }

        ReturnToSource(); // destroy / return to pool
    }
}
