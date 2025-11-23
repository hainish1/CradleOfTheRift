using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeController : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction attackActions;

    // Weapon Parameters

    private Animator _weaponAnim;
    private Entity _playerEntity;
    private PlayerAudioController _audioController;

    // Hit Sweep Parameters

    [SerializeField] private Transform _hitSweepStartPoint;
    [SerializeField] private Transform _hitSweepEndPoint;
    [SerializeField] private int _hitSweepCasts;
    [SerializeField] private LayerMask _hitLayerMasks;
    private float _hitSweepBreadth;
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
    private bool _inputtedAttackThisFrame;
    public bool CanAttack { get; set; }

    void Awake()
    {
        _playerEntity = GetComponentInParent<Entity>();
        _weaponAnim = GetComponent<Animator>();
        _audioController = GetComponentInParent<PlayerAudioController>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        attackActions = playerActions.Attack;
        attackActions.Enable();
        attackActions.started += AttackInputActionStarted;
    }

    private void OnDisable()
    {
        attackActions.Disable();
        attackActions.started -= AttackInputActionStarted;
    }

    void Start()
    {
        if (_playerEntity == null) return;

        // Hit Sweep Parameters
        _hitSweepBreadth = (_hitSweepEndPoint.position - _hitSweepStartPoint.position).magnitude;
        _hitSweepStepVector = (_hitSweepBreadth / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position, _hitSweepEndPoint.position);
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _tempHitSweepArrayInitialized = false;
        _objectsHitThisAttack = new HashSet<Object>();
        _objectsHitThisSweep = new RaycastHit[32];

        // Attack Parameters
        _attackCooldown = _playerEntity.Stats.AttackSpeed; // TODO: Make melee attack speed property and change this to it.
        _inputtedAttackThisFrame = false;
        CanAttack = true;
    }

    void Update()
    {
        if (_inputtedAttackThisFrame && CanAttack)
        {
            PerformAttack();
        }
        else
        {
            _inputtedAttackThisFrame = false;
        }

        if (_weaponAnim.GetCurrentAnimatorStateInfo(0).IsName("Spear-Swing-Chamber"))
        {
            ExecuteHitRegistration();
        }
        else
        {
            _tempHitSweepArrayInitialized = false;
            _objectsHitThisAttack.Clear();
        }
    }


    private void PerformAttack()
    {
        _inputtedAttackThisFrame = false;
        CanAttack = false;
        _weaponAnim.SetTrigger("Swing");
        _audioController.PlayMeleeSound();
        StartCoroutine(AttackCooldown());
    }


    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(_attackCooldown);
        CanAttack = true;
    }


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


    private void InitializeHitSweepPointsTempArray()
    {
        AlignHitSweepStepVector();
        for (int i = 0; i < _hitSweepCasts; i++)
        {
            _hitSweepPointTemp += _hitSweepStepVector;
            _prevHitSweepPointsTemp[i] = _hitSweepPointTemp;
        }
    }


    private void ExecuteHitSweep()
    {
        AlignHitSweepStepVector();

        for (int i = 0; i < _hitSweepCasts; i++)
        {
            Vector3 startPoint = _prevHitSweepPointsTemp[i];
            Vector3 endPoint = _hitSweepPointTemp;
            _objectsHitThisSweep = Physics.RaycastAll(startPoint,
                                                      (endPoint - startPoint).normalized,
                                                      (endPoint - startPoint).magnitude,
                                                      _hitLayerMasks,
                                                      QueryTriggerInteraction.Ignore);
            //Debug.DrawRay(startPoint, endPoint - startPoint, Color.blue, 2);

            for (int j = 0; j < _objectsHitThisSweep.Length; j++)
            {
                var currObject = _objectsHitThisSweep[j].collider.gameObject;
                if (_objectsHitThisAttack.Contains(currObject)) continue;

                var enemyScript = currObject.GetComponent<Enemy>();
                if (enemyScript == null) continue; // <------------------------------ TODO: Make it so non-enemy objects can be damaged.

                var enemyKbScript = currObject.GetComponent<AgentKnockBack>();
                if (enemyKbScript != null)
                {
                    Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
                    enemyKbScript.ApplyImpulse(knockbackForce * impulseDirection);
                }
                
                var targetFlash = enemyScript.GetComponentInParent<TargetFlash>();
                if (targetFlash != null)
                {
                    targetFlash.Flash();
                }

                var damageable = enemyScript.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(MeleeDamage);
                    CombatEvents.ReportDamage(_playerEntity, enemyScript, MeleeDamage);
                    Debug.Log($"Melee: {MeleeDamage} damage to {enemyScript.name}");
                }

                _objectsHitThisAttack.Add(currObject);
            }

            _hitSweepPointTemp += _hitSweepStepVector;
            _prevHitSweepPointsTemp[i] = _hitSweepPointTemp;
        }
    }


    private Vector3 PointVectorTo(Vector3 fromVector, Vector3 toVector)
    {
        return (toVector - fromVector).normalized;
    }


    private void AlignHitSweepStepVector()
    {
        _hitSweepStepVector = _hitSweepStepVector.magnitude * PointVectorTo(_hitSweepStartPoint.position,
                                                                            _hitSweepEndPoint.position);
        _hitSweepPointTemp = _hitSweepStartPoint.position - _hitSweepStepVector;
    }


    private void AttackInputActionStarted(InputAction.CallbackContext context)
    {
        _inputtedAttackThisFrame = true;
    }
}
