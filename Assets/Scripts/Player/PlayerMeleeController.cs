using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeController : MonoBehaviour
{
    [SerializeField] private GameObject weaponObject;
    private Animator weaponAnim;
    private Entity _playerEntity;

    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction attackActions;
    private bool _inputtedAttackThisFrame;

    [SerializeField] private Transform _hitSweepStartPoint;
    [SerializeField] private Transform _hitSweepEndPoint;
    [SerializeField] private int _hitSweepCasts;
    private float _hitSweepBreadth;
    private Vector3 _hitSweepStepVector;
    private Vector3 _hitSweepPointTemp;
    private Vector3[] _prevHitSweepPointsTemp;
    private bool _tempArrayInitialized;

    private float _attackCooldown;

    public bool CanAttack {get; set;}

    void Awake()
    {
        _playerEntity = GetComponentInParent<Entity>();
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

        weaponAnim = weaponObject.GetComponent<Animator>();
        _inputtedAttackThisFrame = false;
        _hitSweepBreadth = (_hitSweepEndPoint.position - _hitSweepStartPoint.position).magnitude;
        _hitSweepStepVector = (_hitSweepBreadth / _hitSweepCasts) * PointVectorTo(_hitSweepStartPoint.position, _hitSweepEndPoint.position);
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _tempArrayInitialized = false;
        _attackCooldown = _playerEntity.Stats.AttackSpeed; // TODO: Make melee attack speed property and change this to it.
        CanAttack = true;
    }

    void Update()
    {
        if (_inputtedAttackThisFrame && CanAttack)
        {
            PerformAttack();
        }
        
        if (weaponAnim.GetCurrentAnimatorStateInfo(0).IsName("Spear-Swing"))
        {
            ExecuteHitRegistration();
        }
        else
        {
            _tempArrayInitialized = false;
        }
    }


    private void PerformAttack()
    {
        _inputtedAttackThisFrame = false;
        CanAttack = false;
        weaponAnim.SetTrigger("Swing");
        StartCoroutine(AttackCooldown());
    }


    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(_attackCooldown);
        CanAttack = true;
    }


    private void ExecuteHitRegistration()
    {
        if (!_tempArrayInitialized)
        {
            Debug.Log("Uninitialized.");
            _tempArrayInitialized = true;
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
        for (int i = 0; i < _hitSweepCasts; i++)
        {
            Physics.Linecast(_prevHitSweepPointsTemp[i], _hitSweepPointTemp);
            Debug.DrawLine(_prevHitSweepPointsTemp[i], _hitSweepPointTemp, Color.blue, 2);
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
