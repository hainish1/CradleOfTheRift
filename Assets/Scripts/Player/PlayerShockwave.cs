// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, November 2025.
//   </para>
// </summary>

using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShockwave : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _shockwaveActions;

    // Effect Paremeters

    [Header("Effect Parameters")] [Space]
    [SerializeField] private GameObject _shockwaveEffectSphere;
    [SerializeField] public CinemachineImpulseSource _shockwaveCameraImpulseSource;
    [SerializeField] private float _cameraShakeIntensity;
    private Entity _playerEntity;
    private Renderer _renderer;
    private Color _originalColor;

    // Shockwave Parameters

    private float ShockwaveDamage => _playerEntity.Stats.ShockwaveDamage;
    private float ShockwaveRadius => _playerEntity.Stats.ShockwaveRadius;
    private float ShockwaveKnockback => _playerEntity.Stats.ShockwaveKnockback;
    private float ShockwaveCooldown => _playerEntity.Stats.ShockwaveCooldown;
    private float _shockwaveEffectExpansionDuration => GetShockwaveEffectExpansionDuration();
    [Header("Shockwave Parameters")] [Space]
    [SerializeField]
    [Tooltip("Layers that will be treated as damageables.")] private LayerMask _damageableLayerMasks;
    [SerializeField]
    [Tooltip("How quickly the shockwave effect sphere expands to the shockwave radius in units per second.")] private float _shockEffectExpansionSpeed;
    private float _shockwaveTimer;

    void Awake()
    {
        _playerEntity = GetComponent<Entity>();
        _renderer = _shockwaveEffectSphere.GetComponent<Renderer>();
        _playerInput = new InputSystem_Actions();
        _playerActions = _playerInput.Player;
    }

    void OnEnable()
    {
        _shockwaveActions = _playerActions.Shockwave;
        _shockwaveActions.Enable();
    }

    void OnDisable()
    {
       _shockwaveActions.Disable(); 
    }

    void Start()
    {
        _originalColor = _renderer.material.color;
        ResetShockwaveEffectSphere();
        _shockwaveTimer = ShockwaveCooldown;
    }

    void Update()
    {
        if (_shockwaveTimer < ShockwaveCooldown)
        {
            _shockwaveTimer += Time.deltaTime;

            // Rapidly expand the shockwave and make it disappear when expired.
            if (_shockwaveTimer <= _shockwaveEffectExpansionDuration)
            {
                _shockwaveEffectSphere.transform.localScale = _shockwaveTimer * _shockEffectExpansionSpeed * Vector3.one;
                
                // Linearly fade the effect sphere's alpha from opaque to fully transparent
                // in the exact time frame of its duration.
                Color currColor = _renderer.material.color;
                currColor.a = 1 - (_shockwaveTimer / _shockwaveEffectExpansionDuration);
                _renderer.material.color = currColor;
            }
            else
            {
                ResetShockwaveEffectSphere();
            }
        }
            
        // Create a shockwave when inputted.
        if (_shockwaveActions.WasPressedThisFrame() && _shockwaveTimer >= ShockwaveCooldown)
        {
            PerformShockwave();
        }
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character perform a shockwave attack on any frame this method is called.
    ///   </para>
    /// </summary>
    private void PerformShockwave()
    {
        _shockwaveTimer = 0;
        _shockwaveCameraImpulseSource.GenerateImpulse(_cameraShakeIntensity);
        _shockwaveEffectSphere.SetActive(true);

        HashSet<GameObject> objectsRegistered = new HashSet<GameObject>(); // Do not overcount objects with multiple colliders.
        Collider[] hitObjects = Physics.OverlapSphere(transform.position, ShockwaveRadius, _damageableLayerMasks);
        foreach (Collider col in hitObjects)
        {
            Enemy enemyScript = col.gameObject.GetComponent<Enemy>();
            if (objectsRegistered.Contains(col.gameObject) || enemyScript == null) continue;

            objectsRegistered.Add(col.gameObject);

            // Apply damage.
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(ShockwaveDamage);
                CombatEvents.ReportDamage(_playerEntity, enemyScript, ShockwaveDamage);
                Debug.Log($"Shockwave: {ShockwaveDamage} damage to {enemyScript.name}");
            }

            // Apply flash effect.
            TargetFlash targetFlash = col.GetComponentInParent<TargetFlash>();
            if (targetFlash != null)
            {
                targetFlash.Flash();
            }

            // Apply knockback.
            AgentKnockBack enemyKbScript = col.GetComponentInParent<AgentKnockBack>();
            if (enemyKbScript != null)
            {
                Vector3 impulseDirection = (col.transform.position - transform.position).normalized;
                enemyKbScript.ApplyImpulse(ShockwaveKnockback * impulseDirection);
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Gets the shockwave effect expansion duration on any frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> The expansion duration. </returns>
    private float GetShockwaveEffectExpansionDuration()
    {
        return ShockwaveRadius / _shockEffectExpansionSpeed;
    }

    /// <summary>
    ///   <para>
    ///     Resets the shockwave effect sphere to its inactive state on any frame this method is called.
    ///   </para>
    /// </summary>
    private void ResetShockwaveEffectSphere()
    {
        _shockwaveEffectSphere.SetActive(false);
        _shockwaveEffectSphere.transform.localScale = Vector3.zero;
        _renderer.material.color = _originalColor;
    }
}
