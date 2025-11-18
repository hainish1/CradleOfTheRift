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
    [SerializeField] private Transform _hitSweepPointTemp;
    [SerializeField] private int _hitSweepCasts;
    private float _hitSweepBreadth;
    private Vector3[] _prevHitSweepPointsTemp;
    private Vector3 _hitSweepStepVector;

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
        _hitSweepStepVector = (_hitSweepBreadth / _hitSweepCasts) * (_hitSweepEndPoint.position - _hitSweepStartPoint.position).normalized;
        _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
        _attackCooldown = _playerEntity.Stats.AttackSpeed; // TODO: Make melee attack speed property and change this to it.
        CanAttack = true;
    }

    void Update()
    {
        if (_inputtedAttackThisFrame && CanAttack)
        {
            PerformAttack();
        }
        else if (_inputtedAttackThisFrame)
        {
            _inputtedAttackThisFrame = false;
        }

        if (weaponAnim.GetCurrentAnimatorStateInfo(0).IsName("Spear-Swing"))
        {
            Debug.Log("Reached.");
            ExecuteHitRegistration();
        }
    }


    private void PerformAttack()
    {
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
        if (_inputtedAttackThisFrame)
        {
            _inputtedAttackThisFrame = false;
            _prevHitSweepPointsTemp = new Vector3[_hitSweepCasts];
            InitializeHitSweepPointsTempArray();
            return;
        }

        ExecuteHitSweepCasts();
    }


    private void InitializeHitSweepPointsTempArray()
    {
        _hitSweepStepVector = _hitSweepStepVector.magnitude * (_hitSweepEndPoint.position - _hitSweepStartPoint.position).normalized;
        _hitSweepPointTemp.position = _hitSweepStartPoint.position - _hitSweepStepVector;

        for (int i = 0; i < _hitSweepCasts; i++)
        {
            _hitSweepPointTemp.position += _hitSweepStepVector;
            _prevHitSweepPointsTemp[i] = _hitSweepPointTemp.position;
        }
    }


    private void ExecuteHitSweepCasts()
    {
        _hitSweepStepVector = _hitSweepStepVector.magnitude * (_hitSweepEndPoint.position - _hitSweepStartPoint.position).normalized;
        _hitSweepPointTemp.position = _hitSweepStartPoint.position - _hitSweepStepVector;

        for (int i = 0; i < _hitSweepCasts; i++)
        {
            Physics.Linecast(_prevHitSweepPointsTemp[i], _hitSweepPointTemp.position);
            Debug.DrawLine(_prevHitSweepPointsTemp[i], _hitSweepPointTemp.position, Color.blue, 2);
            _hitSweepPointTemp.position += _hitSweepStepVector;
            _prevHitSweepPointsTemp[i] = _hitSweepPointTemp.position;
        }
    }


    private void AttackInputActionStarted(InputAction.CallbackContext context)
    {
        _inputtedAttackThisFrame = true;
    }
}
