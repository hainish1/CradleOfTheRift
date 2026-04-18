// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah, January 2026.
//     Contributed to by Hainish Acharya.
//          -Added Enemy script implementation for damage, knockback and flash effect
//           in the ApplyDamageEffects method.
//   </para>
// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeControllerV2 : MonoBehaviour
{
    // Input Parameters

    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _attackActions;

    // Player Parameters

    [Header("Player Parameters")] [Space]
    [SerializeField]
    [Tooltip("The player camera.")] private Transform _playerCamera;
    [SerializeField]
    [Tooltip("Pivot of the player model.")] private Transform _playerModelPivot;
    [SerializeField]
    [Tooltip("Controller for player aim.")] private PlayerAimController _playerAimController;
    private PlayerMovement _playerMovement;
    private PlayerShooter _playerShooter;
    private PlayerShockwaveController _shockwaveController;
    private PlayerHeldWeaponController _heldWeaponController;
    private Entity _playerEntity;

    // Animation Parameters

    [Header("Animation Parameters")] [Space]
    [SerializeField]
    [Tooltip("How quickly attacks pitch up and down in degrees per second.")] private float _attackPitchSpeed;
    [SerializeField]
    [Tooltip("The upward pitch limit of attacks in degrees.")] private float _upwardDegreesLimit;
    [SerializeField]
    [Tooltip("The downward pitch limit of attacks in degrees.")] private float _downwardDegreesLimit;
    [SerializeField] private List<AttackInfo> _attacks = new();
    private Animator _playerAnim;
    private float _degreesPerSecond;
    private bool _isModelHorizontal = true;

    // Hit Registration Parameters

    [Header("Hit Registration Parameters")] [Space]
    [SerializeField]
    [Tooltip("The start point for hit sweeps.")] private Transform _hitCapsuleStartPoint;
    [SerializeField]
    [Tooltip("The end point for hit sweeps.")] private Transform _hitCapsuleEndPoint;
    [SerializeField]
    [Tooltip("The radius of sphere casts.")] private float _hitCapsuleCastRadius;
    [SerializeField]
    [Tooltip("Layers that will be treated as damageables.")] private LayerMask _damageableLayerMasks;
    [SerializeField]
    [Tooltip("Toggle visual debugging for attack registration.")] private bool _debug;
    private Vector3 _prevHitCapsuleStartPointTemp;
    private Vector3 _prevHitCapsuleEndPointTemp;
    private Vector3 _prevHitCapsuleCenterPointTemp;
    private RaycastHit[] _objectsHitThisCast = new RaycastHit[32];
    private HashSet<GameObject> _objectsHitThisAttack = new();
    private bool _prevHitCapsuleTempPointsInitialized;

    // Attack Parameters

    [Header("Attack Parameters")] [Space]
    [SerializeField]
    [Tooltip("Knockback force of attacks.")] private float _knockbackForce;
    [SerializeField]
    [Tooltip("The buffer time for inputting attack combos in seconds.")] private float _comboInputBuffer;
    public bool IsAttacking { get; private set; }
    private HeldWeaponType CurrWeapon => _heldWeaponController != null ? _heldWeaponController.HeldWeapon : HeldWeaponType.None;
    private float MeleeDamage => _playerEntity.Stats.MeleeDamageForWeapon(CurrWeapon);
    private float AttackCooldown => GetAttackCooldown();
    private bool _isRegistering;
    public bool CanAttack { get; set; } = true;
    private bool _comboInputted;
    private int _maxComboCount;
    private int _currComboCount = 0;
    public event Action<int> OnMeleeComboAttack; /// <summary> Fired when a combo attack starts. Argument: combo index (1=first, 2=second/finisher). </summary>
    public event Action OnMeleeAttackEnd; /// <summary> Fired when the current melee attack animation ends. </summary>

    // Audio Parameters
    [Header("Sound Effects")]
    [SerializeField] public AK.Wwise.Event swingSound;
    private PlayerAudioController _audioController;

    void Awake()
    {
        // Input Parameters
        _playerInput = new InputSystem_Actions();
        _playerActions = _playerInput.Player;

        // Player Parameters
        _playerEntity = GetComponent<Entity>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerShooter = GetComponent<PlayerShooter>();
        _shockwaveController = GetComponent<PlayerShockwaveController>();
        _heldWeaponController = GetComponent<PlayerHeldWeaponController>();

        // Animation Parameters
        _playerAnim = GetComponentInChildren<Animator>();
        _upwardDegreesLimit = Mathf.Abs(_upwardDegreesLimit);
        _downwardDegreesLimit = -Mathf.Abs(_downwardDegreesLimit);
        _degreesPerSecond = Mathf.Deg2Rad * _attackPitchSpeed;

        // Attack Parameters
        _maxComboCount = _attacks.Count;

        // Audio Parameters
        _audioController = GetComponent<PlayerAudioController>();

    }

    void OnEnable()
    {
        _attackActions = _playerActions.Attack;
        _attackActions.Enable();
}

    void OnDisable()
    {
        _attackActions.Disable();
    }

    void Update()
    {
        // Do not allow attacks while dashing, throwing or casting shockwave.
        if (_playerMovement.IsDashing || _playerShooter.IsThrowing || _shockwaveController.IsCastingShockwave) return;

        RecalculateAnimationSpeed();

        // Trigger an attack when inputted.
        if ((_attackActions.IsPressed() || _comboInputted) && CanAttack) TriggerAttack();

        // Gradually align player model with the player's crosshair while attacking.
        AlignPlayerCharacter();

        // Continually register targets while an attack is active.
        if (_isRegistering) ExecuteHitRegistrationCast();
    }

    /// <summary>
    ///   <para>
    ///     Activates melee registration and plays a swing sound.
    ///   </para>
    /// </summary>
    public void StartRegistering()
    {
        _isRegistering = true;
        swingSound.Post(gameObject); // For the weapon sound.
    }

    /// <summary>
    ///   <para>
    ///     Deactivates and clears melee registration.
    ///   </para>
    /// </summary>
    public void StopRegistering()
    {
        _isRegistering = false;
        _prevHitCapsuleTempPointsInitialized = false;
        _objectsHitThisAttack.Clear();
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the attack animation durations and speed multiplier on any frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateAnimationSpeed()
    {
        float currAnimationSpeed = _playerEntity.Stats.MeleeAnimationSpeed;
        foreach (AttackInfo info in _attacks)
        {
            float duration = info.PreTransitionAnim.length + info.AttackAnim.length;
            info.AttackDuration = Mathf.Clamp(duration / currAnimationSpeed, 1e-3f, float.MaxValue);
            info.BufferedAttackDuration = Mathf.Clamp(info.AttackDuration - _comboInputBuffer, 0, float.MaxValue);
        }

        _playerAnim.SetFloat("AttackAnimSpeedMultiplier", currAnimationSpeed);
    }

    /// <summary>
    ///   <para>
    ///     Gets the current attack cooldown on any frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> The caluclated attack cooldown. </returns>
    private float GetAttackCooldown() => _attacks[_currComboCount - 1].PostTransitionAnim.length
                                         + _playerEntity.Stats.MeleeAttackRateForWeapon(CurrWeapon);

    /// <summary>
    ///   <para>
    ///     Makes the player character perform an attack on any frame this method is called.
    ///   </para>
    /// </summary>
    private void TriggerAttack()
    {
        IsAttacking = true;
        CanAttack = false;
        _comboInputted = false;
        _currComboCount++;
        OnMeleeComboAttack?.Invoke(_currComboCount);
        _playerAnim.SetTrigger("Attack" + _currComboCount);
        StartCoroutine(DelayAttack());
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for putting melee attack on cooldown.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator DelayAttack()
    {
        // Get the current attack duration and buffered duration.
        float currAttackDuration = _attacks[_currComboCount - 1].AttackDuration;
        float currBufferedAttackDuration = _attacks[_currComboCount - 1].BufferedAttackDuration;

        // Wait for a combo input if max combo count is not reached.
        if (_currComboCount < _maxComboCount)
        {
            yield return new WaitForSeconds(currBufferedAttackDuration);

            // Wait for another attack if a combo input is still possible.
            float timer = currBufferedAttackDuration;
            while (timer < currAttackDuration)
            {
                // Register a pending combo input and exit coroutine.
                if (_attackActions.IsPressed())
                {
                    // Wait remaining time until current attack ends before allowing the next.
                    yield return new WaitForSeconds(currAttackDuration - timer);
                    _comboInputted = true;
                    CanAttack = true;
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            // Leave attack animation sequence if the combo input time window was missed.
            _playerAnim.SetTrigger("ComboMiss");
        }
        // Wait for full attack duration if max combo count is reached.
        else
            yield return new WaitForSeconds(currAttackDuration);

        IsAttacking = false;
        OnMeleeAttackEnd?.Invoke();

        // Wait for attack cooldown if max combo count was reached or a combo input was missed.
        yield return new WaitForSeconds(AttackCooldown);
        _currComboCount = 0;
        CanAttack = true;
        yield break;
    }

    /// <summary>
    ///   <para>
    ///     Gradually rotates the player character to be vertically aligned with the closest point the crosshair
    ///     is intersecting while attacking, or perfectly level with the world horizontal while not attacking.
    ///   </para>
    /// </summary>
    private void AlignPlayerCharacter()
    {
        if (IsAttacking)
        {
            Vector3 crosshairIntersect = _playerAimController.GetAimDirection(_playerModelPivot.position, _playerModelPivot.forward, out RaycastHit hit);
            Vector3 rotationIncrement = Vector3.RotateTowards(_playerModelPivot.forward, crosshairIntersect, Time.deltaTime * _degreesPerSecond, 0);
            rotationIncrement = CopyVectorAngles(rotationIncrement, _playerCamera.forward); // Ensure player character is always horizontally aligned with camera.
            float pitch = GetPitchDegrees(rotationIncrement);
            if (pitch >= _downwardDegreesLimit && pitch <= _upwardDegreesLimit) // Constrain vertical rotation of player character to the pitch limits.
                _playerModelPivot.forward = rotationIncrement;

            _isModelHorizontal = false;
        }
        else if (!IsAttacking && !_isModelHorizontal) // Gradually reset player model alignment while not attacking or if no damageable target is being aimed at.
        {
            Vector3 worldHorizontal = new Vector3(_playerCamera.forward.x, 0, _playerCamera.forward.z).normalized;
            _playerModelPivot.forward = Vector3.RotateTowards(_playerModelPivot.forward, worldHorizontal, Time.deltaTime * _degreesPerSecond, 0);
            if (Vector3.Angle(_playerModelPivot.forward, worldHorizontal) < 1e-3f)
            {
                _playerModelPivot.localRotation = Quaternion.Euler(0, 0, 0); // Zero out pivot rotation for exactness.
                _isModelHorizontal = true;
            }
        }
        else
        {
            _playerModelPivot.localRotation = Quaternion.Euler(0, 0, 0); // Safety measure on every frame to ensure default alignment.
        }
    }

    /// <summary>
    ///   <para>
    ///     Gets a Vector3 that is composed of the pitch and yaw from two other given vectors.
    ///   </para>
    /// </summary>
    /// <param name="copyVectorPitch"> The vector pitch to copy. </param>
    /// <param name="copyVectorYaw"> The vector yaw to copy. </param>
    /// <returns> Composite vector from the pitch and yaw. </returns>
    private Vector3 CopyVectorAngles(Vector3 copyVectorPitch, Vector3 copyVectorYaw)
    {
        // Get copied pitch and yaw in radians.
        float copiedPitch = Mathf.Atan2(copyVectorPitch.y, Mathf.Sqrt(copyVectorPitch.x * copyVectorPitch.x
                                                                      + copyVectorPitch.z * copyVectorPitch.z));
        float copiedYaw = Mathf.Atan2(copyVectorYaw.x, copyVectorYaw.z);

        // Calculate the composite vector using the copied pitch and yaw.
        float cosPitch = Mathf.Cos(copiedPitch);
        float vectorX = cosPitch * Mathf.Sin(copiedYaw);
        float vectorY = Mathf.Sin(copiedPitch);
        float vectorZ = cosPitch * Mathf.Cos(copiedYaw);

        return new Vector3(vectorX, vectorY, vectorZ);
    }

    /// <summary>
    ///   <para>
    ///     Gets the pitch of a given vector.
    ///   </para>
    /// </summary>
    /// <param name="vector"> The vector. </param>
    /// <returns> Pitch of the vector. </returns>
    private float GetPitchDegrees(Vector3 vector)
    {
        return Mathf.Rad2Deg * Mathf.Atan2(vector.y, Mathf.Sqrt((vector.x * vector.x) + (vector.z * vector.z)));
    }

    /// <summary>
    ///   <para>
    ///     Executes a hit registration cast on any frame this method is called.
    ///   </para>
    /// </summary>
    private void ExecuteHitRegistrationCast()
    {
        // Initialize the hit capsule temp points at the beginning of every attack.
        if (!_prevHitCapsuleTempPointsInitialized)
        {
            _prevHitCapsuleTempPointsInitialized = true;
            InitializePrevHitSweepPointTemp();
        }

        // Cast a capsule from the previous attack point to the current one.
        Vector3 prevStartPoint = _prevHitCapsuleStartPointTemp;
        Vector3 prevEndPoint = _prevHitCapsuleEndPointTemp;
        Vector3 currCenterPoint = _hitCapsuleStartPoint.position + (_hitCapsuleEndPoint.position - _hitCapsuleStartPoint.position) / 2;
        Vector3 castDirection = currCenterPoint - _prevHitCapsuleCenterPointTemp;

        // Record all valid objects that were hit.
        float castRadius = _hitCapsuleCastRadius * FinisherStrike.CapsuleRadiusMultiplier;
        int hitCountThisCast = Physics.CapsuleCastNonAlloc(prevStartPoint,
                                                           prevEndPoint,
                                                           castRadius,
                                                           castDirection.normalized,
                                                           _objectsHitThisCast,
                                                           castDirection.magnitude,
                                                           _damageableLayerMasks,
                                                           QueryTriggerInteraction.Ignore);

        if (_debug) Debug.DrawRay(_prevHitCapsuleCenterPointTemp, castDirection, Color.blue, 2);

        if (hitCountThisCast > 0)
        {
            // For all valid objects that were hit, apply damage to them only if they haven't already received it.
            for (int i = 0; i < hitCountThisCast; i++)
            {
                RaycastHit hit = _objectsHitThisCast[i];
                Enemy enemyScript = hit.collider.gameObject.GetComponent<Enemy>();

                // Skip this object if damage was already applied or if it is not an enemy.
                if (_objectsHitThisAttack.Contains(hit.collider.gameObject) || enemyScript == null) continue;

                _objectsHitThisAttack.Add(hit.collider.gameObject);
                ApplyDamageEffects(enemyScript);
            }
        }

        InitializePrevHitSweepPointTemp();
    }

    /// <summary>
    ///   <para>
    ///     Prepares the temp points array for a hit sweep on any frame this method is called.
    ///   </para>
    /// </summary>
    private void InitializePrevHitSweepPointTemp()
    {
        _prevHitCapsuleStartPointTemp = _hitCapsuleStartPoint.position;
        _prevHitCapsuleEndPointTemp = _hitCapsuleEndPoint.position;
        _prevHitCapsuleCenterPointTemp = _hitCapsuleStartPoint.position + (_hitCapsuleEndPoint.position - _hitCapsuleStartPoint.position) / 2;
    }

    /// <summary>
    ///   Draws a wire capsule using two wire spheres and four lines connecting them.
    /// </summary>
    void OnDrawGizmos()
    {
        if (!_debug) return;

        // Melee aim debugging.
        if (IsAttacking)
        {
            Vector3 crosshairIntersect = _playerAimController.GetAimDirection(_playerModelPivot.position, _playerModelPivot.forward, out RaycastHit hit);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_playerModelPivot.position, 0.5f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(hit.point, 0.5f);
        }

        // Attack registration debugging.
        if (_isRegistering)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_hitCapsuleStartPoint.position, _hitCapsuleCastRadius);
            Gizmos.DrawWireSphere(_hitCapsuleEndPoint.position, _hitCapsuleCastRadius);

            Vector3[] lineStartPoints = new Vector3[4];
            lineStartPoints[0] = _hitCapsuleStartPoint.position + _hitCapsuleCastRadius * _hitCapsuleStartPoint.forward;
            lineStartPoints[1] = _hitCapsuleStartPoint.position - _hitCapsuleCastRadius * _hitCapsuleStartPoint.forward;
            lineStartPoints[2] = _hitCapsuleStartPoint.position + _hitCapsuleCastRadius * _hitCapsuleStartPoint.right;
            lineStartPoints[3] = _hitCapsuleStartPoint.position - _hitCapsuleCastRadius * _hitCapsuleStartPoint.right;

            Vector3[] lineEndPoints = new Vector3[4];
            lineEndPoints[0] = _hitCapsuleEndPoint.position + _hitCapsuleCastRadius * _hitCapsuleEndPoint.forward;
            lineEndPoints[1] = _hitCapsuleEndPoint.position - _hitCapsuleCastRadius * _hitCapsuleEndPoint.forward;
            lineEndPoints[2] = _hitCapsuleEndPoint.position + _hitCapsuleCastRadius * _hitCapsuleEndPoint.right;
            lineEndPoints[3] = _hitCapsuleEndPoint.position - _hitCapsuleCastRadius * _hitCapsuleEndPoint.right;

            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(lineStartPoints[i], lineEndPoints[i]);
        }
    }

    /// <summary>
    ///   <para>
    ///     Applies damage and knockback to a given Enemy on any frame this method is called.
    ///     The Enemy script should be checked for nullness before calling this method.
    ///   </para>
    /// </summary>
    /// <param name="enemyScript"> The Enemy script. </param>
    private void ApplyDamageEffects(Enemy enemyScript)
    {
        float damage = MeleeDamage * FinisherStrike.DamageMultiplier;
        IDamageable damageable = enemyScript.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(damage);
            CombatEvents.ReportDamage(_playerEntity, enemyScript, damage);
            Debug.Log($"Melee: {damage} damage to {enemyScript.name}");
        }

        // Apply flash effect.
        TargetFlash targetFlash = enemyScript.GetComponent<TargetFlash>();
        if (targetFlash != null) targetFlash.Flash();

        // Apply knockback.
        AgentKnockBack enemyKbScript = enemyScript.GetComponent<AgentKnockBack>();
        if (enemyKbScript != null)
        {
            Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
            enemyKbScript.ApplyImpulse(_knockbackForce * impulseDirection);
        }
    }
}

/// <summary>
///   <para>
///     A class to represent information for attack animations. Each attack animation
///     requires a transition clip from the previous animation to attack, an attack
///     clip, and a transition clip from attack to idle.
///   </para>
/// </summary>
[Serializable] public class AttackInfo
{
    public AnimationClip PreTransitionAnim;
    public AnimationClip AttackAnim;
    public AnimationClip PostTransitionAnim;
    [HideInInspector] public float AttackDuration;
    [HideInInspector] public float BufferedAttackDuration;
}
