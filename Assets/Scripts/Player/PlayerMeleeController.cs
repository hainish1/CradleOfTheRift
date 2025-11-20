using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeController : MonoBehaviour
{
    private Animator _weaponAnim;
    private Entity _playerEntity;
    private PlayerAudioController _audioController;

    // Input Parameters

    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction attackActions;

    // Attack Parameters

    private float MeleeDamage => _playerEntity.Stats.MeleeDamage;
    [SerializeField] private float knockbackForce;
    private float _attackCooldown;
    private bool _inputtedAttackThisFrame;
    public bool CanAttack { get; set; }

    // Hit Sweep Parameters

    [SerializeField] private Transform _hitSweepStartPoint;
    [SerializeField] private Transform _hitSweepEndPoint;
    [SerializeField] private int _hitSweepCasts;
    [SerializeField] private LayerMask _hitLayerMasks;
    private HashSet<Object> _objectsHitThisAttack;
    private RaycastHit[] _objectsHitThisSweep;
    private float _hitSweepBreadth;
    private Vector3 _hitSweepStepVector;
    private Vector3 _hitSweepPointTemp;
    private Vector3[] _prevHitSweepPointsTemp;
    private bool _tempHitSweepArrayInitialized;

    void Awake()
    {
        _playerEntity = GetComponentInParent<Entity>();
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

        _weaponAnim = GetComponent<Animator>();
        _inputtedAttackThisFrame = false;
        _hitSweepBreadth = (_hitSweepEndPoint.position - _hitSweepStartPoint.position).magnitude;
        _hitSweepStepVector = (_hitSweepBreadth / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position, _hitSweepEndPoint.position);
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _tempHitSweepArrayInitialized = false;
        _attackCooldown = _playerEntity.Stats.AttackSpeed; // TODO: Make melee attack speed property and change this to it.
        CanAttack = true;
        _objectsHitThisAttack = new HashSet<Object>();
        _objectsHitThisSweep = new RaycastHit[32];
    }

    void Update()
    {
        if (_inputtedAttackThisFrame && CanAttack)
        {
            PerformAttack();
        }

        if (_weaponAnim.GetCurrentAnimatorStateInfo(0).IsName("Spear-Swing-Chamber"))
        {
            ExecuteHitRegistration();
        }
        else
        {
            _tempHitSweepArrayInitialized = false;
            _objectsHitThisSweep = new RaycastHit[32];
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
            Debug.Log("Uninitialized.");
            _tempHitSweepArrayInitialized = true;
            InitializeHitSweepPointsTempArray();
            return;
        }

        Debug.Log("Initialized.");
        ExecuteHitSweepCasts();
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


    private void ExecuteHitSweepCasts()
    {
        AlignHitSweepStepVector();

        Vector3 startPoint;
        Vector3 endPoint;
        for (int i = 0; i < _hitSweepCasts; i++)
        {
            startPoint = _prevHitSweepPointsTemp[i];
            endPoint = _hitSweepPointTemp;
            _objectsHitThisSweep = Physics.RaycastAll(startPoint,
                                                       endPoint - startPoint,
                                                       (endPoint - startPoint).magnitude,
                                                       _hitLayerMasks,
                                                       QueryTriggerInteraction.Ignore);
            Debug.DrawLine(startPoint, endPoint, Color.blue, 2);


            for (int j = 0; j < _objectsHitThisSweep.Length; j++)
            {
                var currObject = _objectsHitThisSweep[j].collider.gameObject;

                if (_objectsHitThisAttack.Contains(currObject))
                {
                    continue;
                }
                else
                {
                    _objectsHitThisAttack.Add(currObject);
                }

                try
                {
                    var enemyScript = currObject.GetComponent<Enemy>();
                    if (enemyScript == null) continue;

                    var enemyKbScript = currObject.GetComponent<AgentKnockBack>();
                    if (enemyKbScript != null)
                    {
                        Vector3 impulseDirection = (enemyScript.transform.position - transform.position).normalized;
                        enemyKbScript.ApplyImpulse(knockbackForce * impulseDirection);
                    }
                    enemyScript.GetComponentInParent<TargetFlash>().Flash();

                    var damageable = enemyScript.GetComponent<IDamageable>();
                    if (damageable != null)
                    {

                    }

                }
                catch (System.Exception)
                {

                    throw;
                }
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
