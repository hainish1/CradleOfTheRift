// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using System.Collections.Generic;
using UnityEngine;

public class Shockwave : MonoBehaviour
{
    // Effect Parameters
    
    [Header("Effect Parameters")] [Space]
    [SerializeField] private float _cameraShakeIntensity;
    [SerializeField]
    [Tooltip("Distance in units from the player at which camera shake completely drops off.")] private float _cameraShakeDropoffDistance = 50;
    private bool _isInitialized;
    private Renderer _renderer;

    // Shockwave Parameters

    [Header("Shockwave Parameters")] [Space]
    [SerializeField]
    [Tooltip("Layers that will be treated as damageable.")] private LayerMask _damageableLayerMasks;
    [SerializeField]
    [Tooltip("How quickly the shockwave effect sphere expands to the shockwave radius in units per second.")] private float _shockEffectExpansionSpeed;
    private float _expansionTimer;
    private float _shockwaveEffectExpansionDuration;

    [SerializeField] private bool knockbackImmunity = false;

    void Start()
    {
        gameObject.transform.localScale = Vector3.zero;
        _renderer = gameObject.GetComponent<Renderer>();
    }

    void Update()
    {
        if (!_isInitialized) return; // Do nothing if initialization has not occured.
        if (_expansionTimer >= _shockwaveEffectExpansionDuration) // Destroy shockwave when duration is expired.
        {
            Destroy(gameObject);
            return;
        }

        // Expand shockwave effect every frame.
        gameObject.transform.localScale = _expansionTimer * _shockEffectExpansionSpeed * Vector3.one;

        // Linearly fade the effect sphere's alpha from opaque to fully transparent
        // in the exact time frame of its duration.
        Color currColor = _renderer.material.color;
        currColor.a = 1 - (_expansionTimer / _shockwaveEffectExpansionDuration);
        _renderer.material.color = currColor;

        _expansionTimer += Time.deltaTime;
    }

    /// <summary>
    ///   <para>
    ///     Initializes parameters that are necessary for expanding the shockwave effect and applies damage to all
    ///     valid targets within range of the shockwave.
    ///   </para>
    /// </summary>
    /// <param name="position"> Position of the shockwave. </param>
    /// <param name="damageableLayerMasks"> Layers that are valid for receiving damage. </param>
    /// <param name="damage"> Damage of the shockwave. </param>
    /// <param name="knockback"> Knockback of the shockwave. </param>
    /// <param name="radius"> Radius of the shockwave. </param>
    /// <param name="caster"> Entity of the caster. </param>
    public void Init(Vector3 position, LayerMask damageableLayerMasks, float damage, float knockback, float radius, Entity caster)
    {
        transform.position = position;
        _shockwaveEffectExpansionDuration = radius / _shockEffectExpansionSpeed;

        // Apply shockwave effects to all damageables in radius.
        HashSet<GameObject> objectsRegistered = new HashSet<GameObject>(); // Do not overcount objects with multiple colliders.
        Collider[] hitObjects = Physics.OverlapSphere(position, radius, _damageableLayerMasks);
        foreach (Collider col in hitObjects)
        {
            Enemy enemyScript = col.gameObject.GetComponent<Enemy>();
            if (objectsRegistered.Contains(col.gameObject) || !enemyScript) continue;

            objectsRegistered.Add(col.gameObject);

            // Apply damage.
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                CombatEvents.ReportDamage(caster, enemyScript, damage);
            }

            // Apply flash effect.
            TargetFlash targetFlash = col.GetComponentInParent<TargetFlash>();
            if (targetFlash)
                targetFlash.Flash();

            // Apply knockback.
            AgentKnockBack enemyKbScript = col.GetComponentInParent<AgentKnockBack>();
            if (enemyKbScript)
            {
                Vector3 impulseDirection = (col.transform.position - position).normalized;
                enemyKbScript.ApplyImpulse(knockback * impulseDirection);
            }
        }

        // Sends a camera shake impulse to the player's camera if they are the caster.
        PlayerShockwaveController shockwaveController = caster.GetComponent<PlayerShockwaveController>();
        if (shockwaveController) shockwaveController.GenerateCameraImpulse(position, _cameraShakeIntensity, _cameraShakeDropoffDistance);

        _isInitialized = true;
    }
}
