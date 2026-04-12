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

        if (_shockwaveController) _shockwaveController.InstantiateShockwave(transform.position,
                                                                            _projectileShockwaveDamage,
                                                                            _projectileShockwaveRadius,
                                                                            _projectileShockwaveKnockback);

        ReturnToSource(); // destroy / return to pool
    }
}
