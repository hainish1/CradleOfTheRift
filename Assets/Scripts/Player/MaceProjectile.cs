// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//     Projectile base class written by Hainish Acharya for GAMES 4500, University of Utah.
//   </para>
// </summary>

using UnityEngine;

public class MaceProjectile : Projectile
{
    [Header("Axe Model Parameters")]
    [SerializeField]
    [Tooltip("Transform of the weapon model.")] private Transform _modelTransform;
    [SerializeField]
    [Tooltip("How quickly the weapon whirls in units per second.")] private float _spinSpeed;

    [Header("Mace Projectile Parameters")]
    [SerializeField] private GameObject _shockwavePrefab;
    [SerializeField]
    [Tooltip("Radius of the projectile impact shockwave.")] private float _projectileShockwaveRadius;
    [SerializeField]
    [Tooltip("Knockback force of the projectile impact shockwave.")] private float _projectileShockwaveKnockback;

    [Header("Sound Effects")]
    [SerializeField]
    private AK.Wwise.Event _hitSFX;

    public override void Update()
    {
        FadeTrailVisuals();
        
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            ReturnToSource();
            return;
        }

        if (gravity != 0f) rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        // Look along new direction if something was hit.
        if (!hasHit && rb.linearVelocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(rb.linearVelocity);

        _modelTransform.Rotate(xAngle: 0, Time.deltaTime * _spinSpeed, zAngle: 0); // Rotate the mace model for spinning effect.
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
        PlayDestorySound();
        
        GameObject shockwave = Instantiate(_shockwavePrefab, transform.position, Quaternion.identity);
        Shockwave shockwaveScript = shockwave.GetComponent<Shockwave>();
        shockwaveScript.Init(transform.position, hitMask, actualDamage, _projectileShockwaveKnockback, _projectileShockwaveRadius, attacker);

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
