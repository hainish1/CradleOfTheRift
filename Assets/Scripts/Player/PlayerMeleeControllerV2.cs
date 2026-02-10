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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeControllerV2 : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _attackActions;

    // Weapon Parameters

    [Header("Weapon Parameters")] [Space]
    [SerializeField] private Transform _playerModel;
    [SerializeField] private Transform _playerCamera;
    private Animator _weaponAnim;
    private Entity _playerEntity;
    private PlayerAudioController _audioController;

    // Animation Parameters

    [Header("Animation Parameters")] [Space]
    [SerializeField] private AnimationClip _attack1;
    [SerializeField] private AnimationClip _attack2;
    [SerializeField] private AnimationClip _preTransition1;
    [SerializeField] private AnimationClip _preTransition2;
    [SerializeField] private AnimationClip _postTransition1;
    [SerializeField] private AnimationClip _postTransition2;
    private float[] _attackDurations;

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
    private RaycastHit[] _objectsHitThisCast;
    private HashSet<GameObject> _objectsHitThisAttack;
    private bool _prevHitCapsuleTempPointsInitialized;

    // Attack Parameters

    private float MeleeDamage => _playerEntity.Stats.MeleeDamage;
    private float AttackCooldown => FindLargestTimeCooldown();
    [Header("Attack Parameters")] [Space]
    [SerializeField] private float _comboInputSecondsBuffer;
    [SerializeField] private float knockbackForce;
    private float _currAttackDuration;
    private bool _attackInputPending;
    private bool _isAttacking;
    private bool _isRegistering;
    public bool CanAttack { get; set; }
    private int _maxComboCount;
    private int _currComboCount;
    private float _comboTimer;

    void Awake()
    {
        _playerEntity = GetComponentInParent<Entity>();
        _weaponAnim = GetComponent<Animator>();
        _audioController = GetComponentInParent<PlayerAudioController>();
        _playerInput = new InputSystem_Actions();
        _playerActions = _playerInput.Player;
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

    void Start()
    {
        if (_playerEntity == null) return;

        // Animation Parameters
        _attackDurations = new float[2];
        RecalculateAttackSpeed();

        // Hit Registration Parameters
        _objectsHitThisCast = new RaycastHit[32];
        _objectsHitThisAttack = new HashSet<GameObject>();
        _prevHitCapsuleTempPointsInitialized = false;

        // Attack Parameters
        _currAttackDuration = _attackDurations[0];
        _attackInputPending = false;
        _isAttacking = false;
        _isRegistering = false;
        CanAttack = true;
        _maxComboCount = _attackDurations.Length;
        _currComboCount = 0;
        _comboTimer = _currAttackDuration;
    }

    void Update()
    {
        RecalculateAttackSpeed();

        if (_comboTimer < _currAttackDuration) _comboTimer += Time.deltaTime;

        // Check if attack was inputted slightly before the latest attack ends.
        if (_attackActions.IsPressed() && _comboTimer > GetCooldownMinusBuffer() && _currComboCount < _maxComboCount)
        {
            _attackInputPending = true;
        }

        // Activate an attack when inputted.
        if (_attackInputPending && CanAttack) PerformAttack();

        // Gradually align player model with camera direction.
        if (_isAttacking)
        {
            //float degreesPerSecond = Time.deltaTime * Mathf.Deg2Rad * 120;
            //Vector3 rotateIncrement = Vector3.RotateTowards(_playerModel.forward, _playerCamera.forward, degreesPerSecond, 0);
            //_playerModel.forward = rotateIncrement;

            //_playerModel.transform.rotation = Quaternion.Euler(_playerCamera.rotation.eulerAngles.x, _playerCamera.rotation.eulerAngles.y, 0);
        }
        else
        {
            //Vector3 cameraForward = _playerCamera.forward;
            //cameraForward.y = 0;
            //cameraForward.Normalize();

            //if (_playerModel.forward != cameraForward)
            //{
            //    _playerModel.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
            //}
        }

        // Continually register targets while an attack is active.
        if (_isRegistering) ExecuteHitRegistrationCast();
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the attack durations and animation speed multiplier on any frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateAttackSpeed()
    {
        float currAttackSpeed = _playerEntity.Stats.MeleeAttackSpeed;
        float duration1 = Mathf.Clamp((_preTransition1.length + _attack1.length) / currAttackSpeed, 1e-3f, float.MaxValue);
        float duration2 = Mathf.Clamp((_preTransition2.length + _attack2.length) / currAttackSpeed, 1e-3f, float.MaxValue);

        _attackDurations[0] = duration1;
        _attackDurations[1] = duration2;
        _weaponAnim.SetFloat("AttackSpeedMultiplier", currAttackSpeed);
    }

    /// <summary>
    ///   <para>
    ///     Finds the largest time value out of the attack cooldown and the combo attack durations and returns it.
    ///     To avoid unexpected issues, the attack cooldown should always be set to a larger value than the longest
    ///     attack duration.
    ///   </para>
    /// </summary>
    /// <returns> The largest time value. </returns>
    private float FindLargestTimeCooldown()
    {
        float duration1 = _postTransition1.length;
        float duration2 = _postTransition2.length;
        return Mathf.Max(Mathf.Max(duration1, duration2), _playerEntity.Stats.MeleeAttackRate);
    }

    /// <summary>
    ///   <para>
    ///     The duration of the current attack cooldown in seconds with the input buffer time subtracted from it.
    ///   </para>
    /// </summary>
    /// <returns> The cooldown minus the buffer margin. </returns>
    private float GetCooldownMinusBuffer()
    {
        //print($"CooldownMinusBuffer: {_currAttackDuration - _comboInputSecondsBuffer}");
        return Mathf.Clamp(_currAttackDuration - _comboInputSecondsBuffer, 0, float.MaxValue);
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character perform an attack on any frame this method is called.
    ///   </para>
    /// </summary>
    private void PerformAttack()
    {
        _attackInputPending = false;
        CanAttack = false;
        
        _currComboCount++;
        _weaponAnim.SetTrigger("Attack" + _currComboCount);
        _currAttackDuration = _attackDurations[_currComboCount - 1];
        print($"_currAttackDuration: {_currAttackDuration}");

        // Wait for cooldown minus buffer if combo attack is still possible.
        float delaySeconds;
        if (_currComboCount < _maxComboCount)
        {
            _comboTimer = 0;
            delaySeconds = GetCooldownMinusBuffer();
        }
        else
        {
            _comboTimer = _currAttackDuration;
            delaySeconds = AttackCooldown;
        }

        StartCoroutine(DelayAttack(delaySeconds));
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for putting melee attack on cooldown.
    ///   </para>
    /// </summary>
    /// <param name="delaySeconds"> The seconds to delay the next attack. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator DelayAttack(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        // Return immediately if a full delay was executed.
        if (delaySeconds == AttackCooldown)
        {
            CanAttack = true;
            _currComboCount = 0;
            print($"AttackCooldown ended: {delaySeconds}");
            yield break;
        }

        //print($"Was Attacking: {_isAttacking}");
        // Wait for another attack if a combo is still possible.
        while (_isAttacking)
        {
            // Break out of the coroutine if an attack input is pending during the buffer.
            if (_attackInputPending && _currComboCount < _maxComboCount)
            {
                // Wait until the current attack has ended before allowing the next one.
                while (_isAttacking) yield return null;
                CanAttack = true;
                yield break;
            }

            yield return null;
        }

        // Force a longer delay if the combo time window was missed.
        _currComboCount = 0;
        _weaponAnim.SetTrigger("ComboMiss");
        yield return new WaitForSeconds(AttackCooldown);
        CanAttack = true;
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
        int hitCountThisCast = Physics.CapsuleCastNonAlloc(prevStartPoint,
                                                           prevEndPoint,
                                                           _hitCapsuleCastRadius,
                                                           castDirection.normalized,
                                                           _objectsHitThisCast,
                                                           castDirection.magnitude,
                                                           _damageableLayerMasks,
                                                           QueryTriggerInteraction.Ignore);

        if (_debug)
        {
            Debug.DrawRay(_prevHitCapsuleCenterPointTemp, castDirection, Color.blue, 2);
        }

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
        if (!_isRegistering || !_debug) return;

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
        {
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
        // Apply damage.
        IDamageable damageable = enemyScript.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(MeleeDamage);
            CombatEvents.ReportDamage(_playerEntity, enemyScript, MeleeDamage);
            Debug.Log($"Melee: {MeleeDamage} damage to {enemyScript.name}");
        }

        // Apply flash effect.
        TargetFlash targetFlash = enemyScript.GetComponent<TargetFlash>();
        if (targetFlash != null)
        {
            targetFlash.Flash();
        }

        // Apply knockback.
        AgentKnockBack enemyKbScript = enemyScript.GetComponent<AgentKnockBack>();
        if (enemyKbScript != null)
        {
            Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
            enemyKbScript.ApplyImpulse(knockbackForce * impulseDirection);
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to activate attacking condition.
    ///   </para>
    /// </summary>
    private void OnAttackStart()
    {
        _isAttacking = true;
    }

    /// <summary>
    ///   <para>
    ///     Animation event to activate registration condition.
    ///   </para>
    /// </summary>
    private void StartRegistering()
    {
        _isRegistering = true;
    }

    /// <summary>
    ///   <para>
    ///     Animation event to deactivate attacking and registration conditions.
    ///   </para>
    /// </summary>
    private void OnAttackEnd()
    {
        _isAttacking = false;
        _isRegistering = false;

        //print($"Attack end: {_currComboCount}");
        if (_currComboCount == _maxComboCount) _currComboCount = 0;
        
        _prevHitCapsuleTempPointsInitialized = false;
        _objectsHitThisAttack.Clear();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to play an attack sound.
    ///   </para>
    /// </summary>
    private void PlaySound()
    {
        _audioController.PlayMeleeSound();
    }
}
