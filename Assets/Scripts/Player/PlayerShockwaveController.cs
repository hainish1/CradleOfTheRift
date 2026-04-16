// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, November 2025.
//   </para>
// </summary>

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShockwaveController : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _shockwaveActions;

    // Player Paremeters

    [Header("Player Parameters")] [Space]
    [Tooltip("The camera impulse source to shake.")] public CinemachineImpulseSource _shockwaveCameraImpulseSource;
    [SerializeField]
    [Tooltip("A transform at the center of the player.")] private Transform _playerCenter;
    public bool IsCastingShockwave { get; private set; }
    private Entity _playerEntity;
    private Animator _playerAnim;

    // Shockwave Parameters

    private float ShockwaveDamage => _playerEntity.Stats.ShockwaveDamage;
    private float ShockwaveRadius => _playerEntity.Stats.ShockwaveRadius;
    private float ShockwaveKnockback => _playerEntity.Stats.ShockwaveKnockback;
    private float ShockwaveCooldown => _playerEntity.Stats.ShockwaveCooldown;
    [Header("Shockwave Parameters")] [Space]
    [SerializeField] private GameObject _shockwavePrefab;
    [SerializeField] private AnimationClip _shockwaveAnim;
    [SerializeField] private float _shockwaveAnimSpeedMultiplier;
    [SerializeField]
    [Tooltip("Layers that will be treated as damageable.")] private LayerMask _damageableLayerMasks;
    private float _shockwaveDuration;
    private float _shockwaveAnimTimer;
    private float _shockwaveCooldownTimer;
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
        _shockwaveDuration = _shockwaveAnim.length / _shockwaveAnimSpeedMultiplier;
        _playerAnim.SetFloat("ShockwaveAnimSpeedMultiplier", _shockwaveAnimSpeedMultiplier);
        _shockwaveCooldownTimer = ShockwaveCooldown;
    }

    void Update()
    {
        // Perform a player shockwave when inputted.
        if (_shockwaveActions.WasPressedThisFrame() && _shockwaveCooldownTimer >= ShockwaveCooldown)
        {
            IsCastingShockwave = true;
            _shockwaveAnimTimer = 0;
            _shockwaveCooldownTimer = 0;
            OnShockwaveUsed?.Invoke();
            _playerAnim.SetTrigger("Shockwave");
            GameObject shockwave = Instantiate(_shockwavePrefab, _playerCenter.position, Quaternion.identity);
            Shockwave shockwaveScript = shockwave.GetComponent<Shockwave>();
            shockwaveScript.Init(_playerCenter.position, _damageableLayerMasks, ShockwaveDamage, ShockwaveKnockback, ShockwaveRadius, _playerEntity);
        }

        if (_shockwaveAnimTimer < _shockwaveDuration)
        {
            _shockwaveAnimTimer += Time.deltaTime;
            if (_shockwaveAnimTimer >= _shockwaveDuration) IsCastingShockwave = false; // Set to false after shockwave duration is reached.
        }

        if (_shockwaveCooldownTimer < ShockwaveCooldown) _shockwaveCooldownTimer += Time.deltaTime;
    }

    /// <summary>
    ///   <para>
    ///     Shakes the player's cinemachine camera using a value derived from the impulse position, impulse intensity and dropoff distance.
    ///   </para>
    /// </summary>
    /// <param name="sourcePosition"> Position of the impulse. </param>
    /// <param name="cameraShakeIntensity"> Intensity of the impulse. </param>
    /// <param name="cameraShakeDropoff"> Dropoff distance of the impulse. </param>
    public void GenerateCameraImpulse(Vector3 sourcePosition, float cameraShakeIntensity, float cameraShakeDropoff)
    {
        float distance = Vector3.Distance(transform.position, sourcePosition);
        float positionalIntensity = cameraShakeIntensity * (1 - Mathf.Clamp01(distance / cameraShakeDropoff));
        _shockwaveCameraImpulseSource.GenerateImpulse(positionalIntensity);
    }
}
