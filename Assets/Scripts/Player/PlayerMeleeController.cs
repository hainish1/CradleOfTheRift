// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, November 2025.
//     Contributed to by Hainish Acharya for GAMES 4500, University of Utah, November 2025.
//          -Added Enemy script implementation for damage, knockback and flash effect
//           in the ApplyDamageEffects method.
//   </para>
// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeController : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;
    private InputAction _attackActions;

    // Weapon Parameters

    [Header("Weapon Parameters")] [Space]
    [SerializeField] private GameObject _weaponModel;
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private Transform _playerCamera;
    private Animator _weaponAnim;
    private Entity _playerEntity;
    private PlayerAudioController _audioController;

    // Animation Parameters

    [Header("Animation Parameters")] [Space]
    [SerializeField] private AnimationClip _attack0;
    [SerializeField] private AnimationClip _attack1;
    [SerializeField] private AnimationClip _attack2;
    private float[] _attackDurations;

    // Hit Sweep Parameters

    [Header("Hit Sweep Parameters")] [Space]
    [SerializeField] private Transform _hitSweepStartPoint;
    [SerializeField] private Transform _hitSweepEndPoint;
    [SerializeField] private int _hitSweepCasts;
    [SerializeField] private float _castRadius;
    [SerializeField] private LayerMask _hitLayerMasks;
    private float _hitSweepLength;
    private Vector3 _hitSweepStepVector;
    private Vector3 _hitSweepPointTemp;
    private Vector3[] _prevHitSweepPointsTemp;
    private bool _tempHitSweepArrayInitialized;
    private HashSet<Object> _objectsHitThisAttack;
    private RaycastHit[] _objectsHitThisSweep;

    // Attack Parameters

    private float MeleeDamage => _playerEntity.Stats.MeleeDamage;
    private float AttackCooldown => FindLargestTimeCooldown();
    [Header("Attack Parameters")] [Space]
    [SerializeField] private float _comboInputSecondsMargin;
    [SerializeField] private float knockbackForce;
    private float _currAttackDuration;
    private bool _attackInputPending;
    private bool _isAttacking;
    public bool CanAttack { get; set; }
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

    private void OnEnable()
    {
        _attackActions = _playerActions.Attack;
        _attackActions.Enable();
    }

    private void OnDisable()
    {
        _attackActions.Disable();
    }

    void Start()
    {
        if (_playerEntity == null) return;

        // Weapon Parameters
        _weaponModel.SetActive(false);

        // Animation Parameters
        _attackDurations = new float[3];
        RecalculateAttackSpeed();

        // Hit Sweep Parameters
        AlignHitSweep();
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _tempHitSweepArrayInitialized = false;
        _objectsHitThisAttack = new HashSet<Object>();
        _objectsHitThisSweep = new RaycastHit[32];

        // Attack Parameters
        _currAttackDuration = _attackDurations[0];
        _attackInputPending = false;
        _isAttacking = false;
        CanAttack = true;
        _currComboCount = 0;
        _comboTimer = GetSecondsUpperMargin();
    }

    void Update()
    {
        RecalculateAttackSpeed();

        // Align weapon with camera direction.
        _weaponHolder.transform.rotation = Quaternion.Euler(_playerCamera.rotation.eulerAngles.x, _playerCamera.rotation.eulerAngles.y, 0);

        // Check if attack was inputted slightly before or after the latest attack ends.
        if (_comboTimer < GetSecondsUpperMargin())
        {
            _comboTimer += Time.deltaTime;

            if (_attackActions.WasPressedThisFrame() && _comboTimer > GetSecondsLowerMargin())
            {
                _attackInputPending = true;
            }
        }

        // Activate an attack when inputted.
        if ((_attackActions.WasPressedThisFrame() || _attackInputPending) && CanAttack)
        {
            PerformAttack();
        }

        // Continually register targets while an attack is active.
        if (_isAttacking)
        {
            ExecuteHitRegistration();
        }
        else
        {
            _tempHitSweepArrayInitialized = false;
            _objectsHitThisAttack.Clear();
        }
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the attack durations and animation speed multiplier on any frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateAttackSpeed()
    {
        float currAttackSpeed = _playerEntity.Stats.MeleeAttackSpeed;
        float attackSpeedMultiplier = Mathf.Clamp(1 / currAttackSpeed, 1e-3f, float.MaxValue);
        _attackDurations[0] = _attack0.length * attackSpeedMultiplier;
        _attackDurations[1] = _attack1.length * attackSpeedMultiplier;
        _attackDurations[2] = _attack2.length * attackSpeedMultiplier;
        _weaponAnim.SetFloat("AttackSpeedMultiplier", 1 / attackSpeedMultiplier);
    }

    /// <summary>
    ///   <para>
    ///     Finds the largest time value out of the attack cooldown and the combo attack durations and returns it.
    ///     To avoid unexpected issues, the attack cooldown should always be set to a larger value than the upper
    ///     time margin of the longest attack duration.
    ///   </para>
    /// </summary>
    /// <returns> The largest time value. </returns>
    private float FindLargestTimeCooldown()
    {
        return Mathf.Max(Mathf.Max(Mathf.Max(_attackDurations[0], _attackDurations[1]), _attackDurations[2]), _playerEntity.Stats.MeleeAttackRate);
    }

    /// <summary>
    ///   <para>
    ///     The upper margin of time around the current attack duration in which a combo can be inputted.
    ///   </para>
    /// </summary>
    /// <returns> The upper time margin. </returns>
    private float GetSecondsUpperMargin()
    {
        return _currAttackDuration + _comboInputSecondsMargin;
    }

    /// <summary>
    ///   <para>
    ///     The lower margin of time around the current attack duration in which a combo can be inputted.
    ///   </para>
    /// </summary>
    /// <returns> The lower time margin. </returns>
    private float GetSecondsLowerMargin()
    {
        return _currAttackDuration - _comboInputSecondsMargin;
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
        _weaponAnim.SetTrigger("Attack" + _currComboCount);
        _audioController.PlayMeleeSound();
        
        _currComboCount++;
        _currAttackDuration = _attackDurations[_currComboCount - 1];
        if (_currComboCount < 3)
        {
            _comboTimer = 0;
            StartCoroutine(DelayAttack(GetSecondsLowerMargin()));
        }
        else
        {
            _comboTimer = GetSecondsUpperMargin();
            _currComboCount = 0;
            _currAttackDuration = _attackDurations[_currComboCount];
            StartCoroutine(DelayAttack(AttackCooldown));
        }
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for putting melee attack on cooldown.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator DelayAttack(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        // Execute extra functionality if a combo attack is still possible.
        if (seconds == GetSecondsLowerMargin())
        {
            float timer = seconds;
            while (timer < GetSecondsUpperMargin())
            {
                timer += Time.deltaTime;
                
                // Force a longer delay if the combo time window was missed.
                if (timer > GetSecondsUpperMargin() && !_attackInputPending)
                {
                    _currComboCount = 0;
                    yield return new WaitForSeconds(AttackCooldown - GetSecondsUpperMargin());
                    CanAttack = true;
                    break;
                }
                else if (_attackInputPending) // Otherwise, break out of the coroutine if an attack input is pending.
                {
                    // Wait until the current attack has ended before allowing the next one.
                    while (_isAttacking)
                    {
                        yield return null;
                    }

                    CanAttack = true;
                    break;
                }

                yield return null;
            }
        }
        else // Otherwise, a full delay was executed and further functionality is not necessary.
        {
            CanAttack = true;
        }
    }

    /// <summary>
    ///   <para>
    ///     Executes a hit registration sequence on any frame this method is called.
    ///   </para>
    /// </summary>
    private void ExecuteHitRegistration()
    {
        if (_currComboCount == 1 && _objectsHitThisAttack.Count == 1) return; // Skip registration if thrust attack damaged something.
        
        // Initialize first hit sweep points of the attack.
        if (!_tempHitSweepArrayInitialized)
        {
            _tempHitSweepArrayInitialized = true;
            InitializeHitSweepPointsTempArray();
            return;
        }

        ExecuteHitSweep();
    }

    /// <summary>
    ///   <para>
    ///     Prepares the temp points array for a hit sweep on any frame this method is called.
    ///   </para>
    /// </summary>
    private void InitializeHitSweepPointsTempArray()
    {
        AlignHitSweep();

        for (int i = 0; i < _hitSweepCasts; i++)
        {
            IncrementHitSweepPointTemp(i);
        }
    }

    /// <summary>
    ///   <para>
    ///     Executes a full hit sweep sequence on any frame this method is called.
    ///   </para>
    /// </summary>
    private void ExecuteHitSweep()
    {
        AlignHitSweep();

        // Cast rays from previous hit sweep points to current ones all the way down the weapon length.
        for (int i = 0; i < _hitSweepCasts; i++)
        {
            Vector3 startPoint = _prevHitSweepPointsTemp[i];
            Vector3 endPoint = _hitSweepPointTemp;

            // Record all valid objects that were hit.
            _objectsHitThisSweep = Physics.SphereCastAll(startPoint,
                                                         _castRadius,
                                                         (endPoint - startPoint).normalized,
                                                         (endPoint - startPoint).magnitude,
                                                         _hitLayerMasks,
                                                         QueryTriggerInteraction.Ignore);
            Debug.DrawRay(startPoint, endPoint - startPoint, Color.blue, 2);

            // Ensure the initial thrust attack can only damage a single enemy.
            if (_currComboCount == 1 && _objectsHitThisSweep.Length > 1)
            {
                // Find the first object hit that has an Enemy script.
                for (int j = 0; j < _objectsHitThisSweep.Length; j++)
                {
                    GameObject currObject = _objectsHitThisSweep[j].collider.gameObject;
                    Enemy enemyScript = currObject.GetComponent<Enemy>();

                    if (enemyScript == null) continue;

                    ApplyDamageEffects(currObject, enemyScript);
                    _objectsHitThisAttack.Add(currObject);
                    return;
                }
            }
            else if (_currComboCount != 1 && _objectsHitThisSweep.Length > 0) // Other two attacks have area damage.
            {
                // For all valid objects that were hit, apply damage to them only if they haven't already received it.
                for (int j = 0; j < _objectsHitThisSweep.Length; j++)
                {
                    GameObject currObject = _objectsHitThisSweep[j].collider.gameObject;
                    if (_objectsHitThisAttack.Contains(currObject)) continue; // Skip this object if damage was already applied.

                    Enemy enemyScript = currObject.GetComponent<Enemy>();
                    if (enemyScript == null) continue; // <------------------------------ TODO: Make it so non-enemy objects can be damaged.

                    ApplyDamageEffects(currObject, enemyScript);

                    _objectsHitThisAttack.Add(currObject);
                }
            }

            IncrementHitSweepPointTemp(i);
        }
    }

    private void OnDrawGizmos()
    {
        float hitSweepLength = (_hitSweepEndPoint.position - _hitSweepStartPoint.position).magnitude;
        float segmentLength = _hitSweepLength / _hitSweepCasts;
        Vector3 hitSweepStepVector = (segmentLength + segmentLength / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position,
                                                                                                      _hitSweepEndPoint.position);
        Vector3 hitSweepPointTemp = _hitSweepStartPoint.position;

        for (int i = 0; i < _hitSweepCasts; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hitSweepPointTemp, _castRadius);
            hitSweepPointTemp += hitSweepStepVector;
        }
    }

    /// <summary>
    ///   <para>
    ///     Applies damage and knockback to a given GameObject on any frame this method is called.
    ///     The Enemy script should be checked for nullness before calling this method.
    ///   </para>
    /// </summary>
    /// <param name="currObject"> The provided GameObject. </param>
    /// <param name="enemyScript"> The object's Enemy script. </param>
    private void ApplyDamageEffects(GameObject currObject, Enemy enemyScript)
    {
        // Apply damage.
        IDamageable damageable = enemyScript.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(MeleeDamage);
            CombatEvents.ReportDamage(_playerEntity, enemyScript, MeleeDamage);
            Debug.Log($"Melee: {MeleeDamage} damage to {enemyScript.name}");
        }

        // Apply flash effect.
        TargetFlash targetFlash = enemyScript.GetComponentInParent<TargetFlash>();
        if (targetFlash != null)
        {
            targetFlash.Flash();
        }

        // Apply knockback.
        AgentKnockBack enemyKbScript = currObject.GetComponent<AgentKnockBack>();
        if (enemyKbScript != null)
        {
            Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
            enemyKbScript.ApplyImpulse(knockbackForce * impulseDirection);
        }
    }

    /// <summary>
    ///   <para>
    ///     Gets a normalized vector pointing from a "from" vector to a "to" vector.
    ///   </para>
    /// </summary>
    /// <param name="fromVector"> The "from" vector. </param>
    /// <param name="toVector"> The "to" vector. </param>
    /// <returns> A normalized vector pointing from one vector to another. </returns>
    private Vector3 PointVectorTo(Vector3 fromVector, Vector3 toVector)
    {
        return (toVector - fromVector).normalized;
    }
    
    /// <summary>
    ///   <para>
    ///     Gets the hit sweep length and step vector alignment on any frame this method is called.
    ///   </para>
    /// </summary>
    private void AlignHitSweep()
    {
        _hitSweepLength = (_hitSweepEndPoint.position - _hitSweepStartPoint.position).magnitude;
        float segmentLength = _hitSweepLength / _hitSweepCasts;
        _hitSweepStepVector = (segmentLength + segmentLength / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position,
                                                                                               _hitSweepEndPoint.position);
        _hitSweepPointTemp = _hitSweepStartPoint.position;
    }

    /// <summary>
    ///   <para>
    ///     Increments the hit sweep temp point to the next step down the weapon length on any frame
    ///     this method is called.
    ///   </para>
    /// </summary>
    /// <param name="index"> Index of the temp point. </param>
    private void IncrementHitSweepPointTemp(int index)
    {
        _prevHitSweepPointsTemp[index] = _hitSweepPointTemp;
        _hitSweepPointTemp += _hitSweepStepVector;
    }

    /// <summary>
    ///   <para>
    ///     Animation event to activate hit registration.
    ///   </para>
    /// </summary>
    private void OnAttackStart()
    {
        _isAttacking = true;
        _weaponModel.SetActive(true);
    }

    /// <summary>
    ///   <para>
    ///     Animation event to deactivate hit registration.
    ///   </para>
    /// </summary>
    private void OnAttackEnd()
    {
        _isAttacking = false;
        _weaponModel.SetActive(false);
    }
}
