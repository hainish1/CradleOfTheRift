// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, November 2025.
//   </para>
// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShockwaveController : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _shockwaveActions;

    // Effect Paremeters

    [Header("Effect Parameters")] [Space]
    [SerializeField]
    [Tooltip("The object used to create the expanding shockwave effect.")] private GameObject _shockwaveEffectSphere;
    [Tooltip("The camera impulse source to shake.")] public CinemachineImpulseSource _shockwaveCameraImpulseSource;
    [SerializeField] private float _cameraShakeIntensity;
    [SerializeField]
    [Tooltip("Distance in units from the player at which camera shake completely drops off.")] private float _cameraShakeDropoffDistance = 50;
    [SerializeField]
    [Tooltip("A transform at the center of the player.")] private Transform _playerCenter;
    private Entity _playerEntity;
    private Animator _playerAnim;
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
    public static event System.Action OnShockwaveUsed;

    void Awake()
    {
        _playerEntity = GetComponent<Entity>();
        _playerAnim = GetComponentInChildren<Animator>();
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
        _shockwaveEffectSphere.SetActive(false);
        _shockwaveEffectSphere.transform.localScale = Vector3.zero;
        _originalColor = _shockwaveEffectSphere.GetComponent<Renderer>().material.color;
        _shockwaveTimer = ShockwaveCooldown;
    }

    void Update()
    {
        if (_shockwaveTimer < ShockwaveCooldown) _shockwaveTimer += Time.deltaTime;

        // Perform a player shockwave when inputted.
        if (_shockwaveActions.WasPressedThisFrame() && _shockwaveTimer >= ShockwaveCooldown)
        {
            _shockwaveTimer = 0;
            OnShockwaveUsed?.Invoke();
            _playerAnim.SetTrigger("Shockwave");
            InstantiateShockwave(_playerCenter.position, ShockwaveDamage, ShockwaveRadius, ShockwaveKnockback);
        }
    }

    /// <summary>
    ///   <para>
    ///     Instantiates a shockwave attack at a given position.
    ///   </para>
    /// </summary>
    /// <param name="position"> Position to instantiate the shockwave. </param>
    /// <param name="damage"> Damage of the shockwave. </param>
    /// <param name="radius"> Radius of the shockwave. </param>
    /// <param name="knockback"> Knockback of the shockwave. </param>
    public void InstantiateShockwave(Vector3 position, float damage, float radius, float knockback)
    {
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
                CombatEvents.ReportDamage(_playerEntity, enemyScript, damage);
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

        StartCoroutine(ExpandShockwaveEffect(position));
    }

    /// <summary>
    ///   <para>
    ///     Instantiates a visual shockwave effect at a given position and expands it for the dedicated number of seconds. 
    ///   </para>
    /// </summary>
    /// <param name="position"> Position to instantiate the shockwave effect. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator ExpandShockwaveEffect(Vector3 position)
    {
        // Instantiate shockwave visual effect.
        GameObject shockwaveSphere = Instantiate(_shockwaveEffectSphere, position, Quaternion.identity);
        shockwaveSphere.transform.localScale = Vector3.zero;
        shockwaveSphere.SetActive(true);
        Renderer renderer = shockwaveSphere.GetComponent<Renderer>();
        renderer.material.color = _originalColor;
        float distance = Vector3.Distance(position, transform.position);
        float shakeIntensity = _cameraShakeIntensity * (1 - Mathf.Clamp01(distance / _cameraShakeDropoffDistance));
        _shockwaveCameraImpulseSource.GenerateImpulse(shakeIntensity);

        float timer = 0;
        while (timer <= _shockwaveEffectExpansionDuration) // Rapidly expand the shockwave and make it disappear when expired.
        {
            shockwaveSphere.transform.localScale = timer * _shockEffectExpansionSpeed * Vector3.one;

            // Linearly fade the effect sphere's alpha from opaque to fully transparent
            // in the exact time frame of its duration.
            Color currColor = renderer.material.color;
            currColor.a = 1 - (timer / _shockwaveEffectExpansionDuration);
            renderer.material.color = currColor;

            timer += Time.deltaTime;
            yield return null;
        }

        shockwaveSphere.SetActive(false);
        Destroy(shockwaveSphere);
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
}
