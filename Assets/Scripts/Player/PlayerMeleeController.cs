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

    [SerializeField] private Transform _playerCamera;
    private Animator _weaponAnim;
    private Entity _playerEntity;
    private PlayerAudioController _audioController;

    // Hit Sweep Parameters

    [SerializeField] private Transform _hitSweepStartPoint;
    [SerializeField] private Transform _hitSweepEndPoint;
    [SerializeField] private int _hitSweepCasts;
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
    [SerializeField] private float knockbackForce;
    private float _attackCooldown;
    public bool CanAttack { get; set; }

    void Awake()
    {
        _playerEntity = GetComponentInParent<Entity>();
        _weaponAnim = GetComponentInChildren<Animator>();
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

        // Hit Sweep Parameters
        AlignHitSweep();
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _tempHitSweepArrayInitialized = false;
        _objectsHitThisAttack = new HashSet<Object>();
        _objectsHitThisSweep = new RaycastHit[32];

        // Attack Parameters
        _attackCooldown = _playerEntity.Stats.AttackSpeed; // TODO: Make melee attack speed property and change this to it.
        CanAttack = true;
    }

    void Update()
    {
        // Align weapon with camera direction.
        transform.rotation = Quaternion.Euler(_playerCamera.rotation.eulerAngles.x, _playerCamera.rotation.eulerAngles.y, 0);

        if (_attackActions.WasPressedThisFrame() && CanAttack)
        {
            PerformAttack();
        }

        // Only perform hit sweeps while attack is active.
        if (_weaponAnim.GetCurrentAnimatorStateInfo(0).IsName("Spear-Swing-Chamber")) // Problem with animator necessitates looking for the
                                                                                      // previous animation instead of the current one.
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
    ///     Makes the player character perform an attack on any frame this method is called.
    ///   </para>
    /// </summary>
    private void PerformAttack()
    {
        CanAttack = false;
        _weaponAnim.SetTrigger("Swing");
        _audioController.PlayMeleeSound();
        StartCoroutine(AttackCooldown());
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for putting melee attack on cooldown.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(_attackCooldown);
        CanAttack = true;
    }

    /// <summary>
    ///   <para>
    ///     Executes a hit registration sequence on any frame this method is called.
    ///   </para>
    /// </summary>
    private void ExecuteHitRegistration()
    {
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
            IncrementHitSweepTemp(i);
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
            _objectsHitThisSweep = Physics.RaycastAll(startPoint,
                                                      (endPoint - startPoint).normalized,
                                                      (endPoint - startPoint).magnitude,
                                                      _hitLayerMasks,
                                                      QueryTriggerInteraction.Ignore);
            //Debug.DrawRay(startPoint, endPoint - startPoint, Color.blue, 2);

            // For all valid objects that were hit, check if damage has already been applied to them.
            for (int j = 0; j < _objectsHitThisSweep.Length; j++)
            {
                var currObject = _objectsHitThisSweep[j].collider.gameObject;
                if (_objectsHitThisAttack.Contains(currObject)) continue; // Skip this object if damage was already applied.

                var enemyScript = currObject.GetComponent<Enemy>();
                if (enemyScript == null) continue; // <------------------------------ TODO: Make it so non-enemy objects can be damaged.

                // Apply knockback.
                var enemyKbScript = currObject.GetComponent<AgentKnockBack>();
                if (enemyKbScript != null)
                {
                    Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
                    enemyKbScript.ApplyImpulse(knockbackForce * impulseDirection);
                }
                enemyScript.GetComponentInParent<TargetFlash>().Flash();

                // Apply damage.
                var damageable = enemyScript.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(MeleeDamage);
                    CombatEvents.ReportDamage(_playerEntity, enemyScript, MeleeDamage);
                    Debug.Log($"Melee: {MeleeDamage} damage to {enemyScript.name}");
                }

                _objectsHitThisAttack.Add(currObject);
            }

            IncrementHitSweepTemp(i);
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
        _hitSweepStepVector = (_hitSweepLength / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position,
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
    private void IncrementHitSweepTemp(int index)
    {
        _prevHitSweepPointsTemp[index] = _hitSweepPointTemp;
        _hitSweepPointTemp += _hitSweepStepVector;
    }
}
