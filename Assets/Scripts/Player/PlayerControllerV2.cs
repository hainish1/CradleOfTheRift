using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControllerV2 : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction lookActions;
    private InputAction moveActions;
    private InputAction jumpActions;
    private InputAction dashActions;
    private InputAction attackActions;
    private InputAction pauseActions;

    [Header("Player References")]
    [SerializeField] private Transform _playerCenter;
    [SerializeField] private Transform _playerCamera;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Transform _cameraMount;
    private CharacterController _characterController;
    private float _playerHalfHeight;
    private float _playerHalfWidth;
    private float _groundedRaycastFloorDistance;
    private float _groundedRaycastRadialDistance;
    private float _originalStepOffset;

    [Header("Look Parameters")]
    [SerializeField] private float _xSensitivity;
    [SerializeField] private float _ySensitivity;
    [SerializeField] private float _upwardClampAngle;
    [SerializeField] private float _downwardClampAngle;
    [SerializeField] private float _cameraLerpSpeed;
    [Range(0, 1)][SerializeField] private float _cameraCollisionOffset;
    private int _cameraCollisionMasks;
    private float _xRotation;
    private float _yRotation;
    private Vector2 _lookInput;

    [Header("Movement Parameters")]
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _accelerationSeconds;
    [SerializeField] private float _decelerationSeconds;
    private float _acceleration;
    private float _aeceleration;
    private Vector3 lateralVector;
    private Vector3 _moveInputUnitVector;

    [Header("Jump Parameters")]
    [SerializeField] private float _JumpHeight;
    [SerializeField] private float _strafeMultiplier;
    [SerializeField] [Range(0, 1)] private float _hoverDescentReductionMultiplier;
    [SerializeField] private float _gravityMultiplier;
    private bool _didPerformJump;
    private bool _isHovering;
    private float _aggregateGravityModifier;
    private Vector3 _verticalVector;

    //[Header("Coyote Time Parameters")]
    //[SerializeField] private float earlyCoyoteTime;
    //[SerializeField] private float lateCoyoteTime;
    //private float earlyCoyoteTimer;
    //private float lateCoyoteTimer;
    //private bool canEarlyJump;

    [Header("Boost Parameters")]
    [SerializeField] private int _maxBoostEnergy;
    [SerializeField] private int _maxBoostSpeed;
    [SerializeField] private int _boostAcceleration;
    [SerializeField] private int _boostRegenerationRate;
    [SerializeField] private int _boostDepletionRate;
    [SerializeField] private float _boostDoubleTapWindow;
    private float _currBoostEnergy;
    private float _currBoostDoubleTapTime;
    private bool _isBoosting;
    private bool _isRegeneratingBoost;

    [Header("Dash Parameters")]
    [SerializeField] private float _dashDistance;
    [SerializeField] private float _dashSpeed;
    [SerializeField] private float _dashCooldown;
    [SerializeField] private int _dashCharges;
    private bool _isDashing;
    private Vector3 _dashVector;

    //[Header("UI References")]
    //public Image crosshair;
    //public Image pauseMenu;
    //public bool gamePaused;

    [Header("Layer Parameters")]
    [SerializeField] private LayerMask _environmentLayer;
    private RaycastHit _hitInfo;

    private bool _lockControls = false;



    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        lookActions = playerActions.Look;
        lookActions.Enable();

        moveActions = playerActions.Move;
        moveActions.Enable();

        jumpActions = playerActions.Jump;
        jumpActions.Enable();
        jumpActions.started += JumpInputActionStarted;
        jumpActions.canceled += JumpInputActionCanceled;

        dashActions = playerActions.Dash;
        dashActions.Enable();
        dashActions.started += DashInputActionStarted;

        attackActions = playerActions.Attack;
        attackActions.Enable();
        attackActions.started += AttackInputActionStarted;

        pauseActions = playerInput.UI.Pause;
        pauseActions.Enable();
        pauseActions.started += PauseInputActionStarted;
    }

    private void OnDisable()
    {
        lookActions.Disable();
        moveActions.Disable();
        jumpActions.Disable();
        dashActions.Disable();
        attackActions.Enable();
        pauseActions.Disable();

        jumpActions.started -= JumpInputActionStarted;
        jumpActions.canceled -= JumpInputActionCanceled;
        dashActions.started -= DashInputActionStarted;
        attackActions.started -= AttackInputActionStarted;
        pauseActions.started -= PauseInputActionStarted;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerHalfHeight = GetComponent<CharacterController>().bounds.extents.y;
        _playerHalfWidth = GetComponent<CharacterController>().bounds.extents.x;
        _groundedRaycastFloorDistance = _playerHalfHeight + 0.1f;
        _groundedRaycastRadialDistance = _playerHalfWidth * 0.7f;
        _originalStepOffset = _characterController.stepOffset;

        _cameraCollisionMasks = _environmentLayer.value;

        _acceleration = _maxSpeed / _accelerationSeconds;
        _aeceleration = _maxSpeed / _decelerationSeconds;

        _didPerformJump = false;
        _isHovering = false;
        _aggregateGravityModifier = _gravityMultiplier;
        
        _currBoostEnergy = _maxBoostEnergy;
        _currBoostDoubleTapTime = _boostDoubleTapWindow;
        _isBoosting = false;
        _isRegeneratingBoost = false;

        _isDashing = false;

        //crosshair.enabled = true;
        //pauseMenu.enabled = false;

        //foreach (Transform child in pauseMenu.transform)
        //{
        //    child.gameObject.SetActive(false);
        //}
        //gamePaused = false;
    }

    void Update()
    {
        if (_lockControls) return;

        Look();

        ApplyGravity();

        if (_isDashing)
        {
            DashCase();
        }
        else
        {
            MoveCase();
            JumpCase();
            BoostCase();
            HoverCase();
        }
    }

    void LateUpdate()
    {
        if (_lockControls) return;

        CameraCollision();
    }


    private void Look()
    {
        if (_lockControls) return;

        _lookInput = lookActions.ReadValue<Vector2>();

        // NOTE: lookInput.x and lookInput.y are not mistakenly swapped.
        _xRotation -= Time.deltaTime * _xSensitivity * _lookInput.y;
        _yRotation += Time.deltaTime * _ySensitivity * _lookInput.x;
        _xRotation = Mathf.Clamp(_xRotation, _downwardClampAngle, _upwardClampAngle);

        transform.rotation = Quaternion.Euler(0, _yRotation, 0); // Rotate the player object horizontally around the y axis.
        _cameraPivot.rotation = Quaternion.Euler(_xRotation, _yRotation, 0); // Rotate the player's camera pivot around the x and y axes.
    }


    private void CameraCollision()
    {
        // Raycast only registers for Environment.
        if (Physics.Linecast(_cameraPivot.position, _cameraMount.position, out _hitInfo, _cameraCollisionMasks))
        {
            Vector3 newCameraPosition = new Vector3(_hitInfo.point.x, _hitInfo.point.y, _hitInfo.point.z);
            _playerCamera.position = newCameraPosition * _cameraCollisionOffset;
        }
        else
        {
            if (_playerCamera.position != _cameraMount.position)
            {
                _playerCamera.position = _cameraMount.position;
            }
        }
    }


    private Vector3 GetMoveInputDirection()
    {
        Vector3 moveInput = moveActions.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);

        return transform.TransformDirection(inputDirection).normalized;
    }


    private void MoveCase()
    {
        _moveInputUnitVector = GetMoveInputDirection();
        
        if (_moveInputUnitVector != Vector3.zero)
        {
            Accelerate();
        }
        else
        {
            Decelerate();
        }
    }


    private void Accelerate()
    {
        Vector3 accelIncrement = Time.deltaTime * _acceleration * _moveInputUnitVector;

        if (lateralVector.magnitude < _maxSpeed)
        {
            if (lateralVector.magnitude + accelIncrement.magnitude > _maxSpeed)
            {
                accelIncrement = (_maxSpeed - lateralVector.magnitude) * _moveInputUnitVector;
            }
        }
        else
        {
            accelIncrement = Vector3.zero;
        }

        lateralVector = lateralVector.magnitude * _moveInputUnitVector;
        lateralVector += accelIncrement;
        _characterController.Move(Time.deltaTime * lateralVector);
    }


    private void Decelerate()
    {
        if (lateralVector.magnitude > 0)
        {
            Vector3 decelIncrement = Time.deltaTime * _aeceleration * lateralVector.normalized;

            if (lateralVector.magnitude - decelIncrement.magnitude < 0)
            {
                lateralVector = Vector3.zero;
                decelIncrement = Vector3.zero;
            }

            lateralVector -= decelIncrement;
            _characterController.Move(Time.deltaTime * lateralVector);
        }
    }


    private bool IsGrounded()
    {
        // Exact center of the player's character.
        if (Physics.Raycast(_playerCenter.position, Vector2.down, _groundedRaycastFloorDistance))
        {
            return true;
        }

        // Four corners of the player's character.
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                if (Physics.Raycast(_playerCenter.position + new Vector3(_groundedRaycastRadialDistance * i, 0, _groundedRaycastRadialDistance * j),
                                    Vector2.down, _groundedRaycastFloorDistance))
                {
                    return true;
                }
            }
        }

        return false;
    }


    private void JumpInputActionStarted(InputAction.CallbackContext context)
    {
        if (IsGrounded() && !_isDashing)
        {
            _didPerformJump = true;
        }
        else if (!IsGrounded() && !_isDashing)
        {
            _isHovering = true;
        }

        if (_currBoostDoubleTapTime == _boostDoubleTapWindow)
        {
            StartCoroutine(BoostDoubleTapTimer());
        }
        else if (IsWithinBoostWindow())
        {
            _isHovering = false;
            _isBoosting = true;
        }
    }


    private void JumpInputActionCanceled(InputAction.CallbackContext context)
    {
        _isHovering = false;
        _aggregateGravityModifier = _gravityMultiplier;

        _isBoosting = false;

    }


    private void JumpCase()
    {
        if (_didPerformJump && IsGrounded())
        {
            _didPerformJump = false;
            _verticalVector.y = _JumpHeight;
            _characterController.stepOffset = 0;

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
        else if (!IsGrounded())
        {
            _characterController.stepOffset = 0;
        }
        else
        {
            _isBoosting = false;
            _verticalVector.y = -0.5f;
            _characterController.stepOffset = _originalStepOffset;
        }
    }


    private IEnumerator BoostDoubleTapTimer()
    {
        while (IsWithinBoostWindow())
        {
            _currBoostDoubleTapTime -= Time.deltaTime;
            
            yield return null;
        }

        _currBoostDoubleTapTime = _boostDoubleTapWindow;
    }


    private bool IsWithinBoostWindow()
    {
        return _currBoostDoubleTapTime > 0;
    }


    private void BoostCase()
    {
        // Player let go of spacebar or ran out of boost energy.
        if ((_isBoosting && !jumpActions.IsPressed()) || _currBoostEnergy <= 0)
        {
            _isBoosting = false;
        }
        
        // Player is boosting.
        if (!IsGrounded() && _isBoosting && jumpActions.IsPressed() && _currBoostEnergy > 0)
        {
            Vector3 boostIncrement = Time.deltaTime * _boostAcceleration * Vector3.up;
            float depletionDecrement = Time.deltaTime * _boostDepletionRate;

            // Decrease boost energy by the full decrement if not less than 0,
            // otherwise set to exactly 0 and begin hovering.
            if (_currBoostEnergy - depletionDecrement > 0)
            {
                _currBoostEnergy -= depletionDecrement;
            }
            else
            {
                _currBoostEnergy = 0;
                _isHovering = true;
            }

            // Limit vertical boost speed to _maxBoostSpeed.
            if (_verticalVector.magnitude < _maxBoostSpeed)
            {
                _verticalVector += boostIncrement;

                // Set vertical speed to exactly _maxBoostSpeed if the last increment went over.
                if (_verticalVector.magnitude > _maxBoostSpeed)
                {
                    _verticalVector = new Vector3(_verticalVector.x, _maxBoostSpeed, _verticalVector.z);
                }
            }

            _characterController.Move(Time.deltaTime * _verticalVector);
        }

        // Initialize regeneration coroutine if boosted, touched the ground and not already regenerating.
        if (IsGrounded() && _currBoostEnergy < _maxBoostEnergy && !_isRegeneratingBoost)
        {
            StartCoroutine(BoostRegeneration());
        }
    }


    private IEnumerator BoostRegeneration()
    {
        _isRegeneratingBoost = true;

        while (_currBoostEnergy <= _maxBoostEnergy)
        {
            if (_isBoosting)
            {
                break;
            }

            _currBoostEnergy += Time.deltaTime * _boostRegenerationRate;

            if (_currBoostEnergy > _maxBoostEnergy)
            {
                _currBoostEnergy = _maxBoostEnergy;
            }

            yield return null;
        }

        _isRegeneratingBoost = false;
    }


    private void ApplyGravity()
    {
        // Do not apply gravity when boosting.
        if (!IsGrounded() && !_isBoosting)
        {
            _verticalVector += Time.deltaTime * _aggregateGravityModifier * Physics.gravity;

            if (_verticalVector.y < _aggregateGravityModifier * Physics.gravity.y)
            {
                _verticalVector.y = _aggregateGravityModifier * Physics.gravity.y;
            }

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
    }


    private void HoverCase()
    {
        // Player let go of spacebar or landed.
        if ((_isHovering && !jumpActions.IsPressed()) || IsGrounded())
        {
            _isHovering = false;
            _aggregateGravityModifier = _gravityMultiplier;
        }

        // Player is hovering, only when falling.
        if (_isHovering && jumpActions.IsPressed() && _verticalVector.y < 0)
        {
            _aggregateGravityModifier = _gravityMultiplier * _hoverDescentReductionMultiplier;
        }
    }


    private void DashInputActionStarted(InputAction.CallbackContext context)
    {
        _dashVector = GetMoveInputDirection();

        if (_dashCharges != 0)
        {
            if (_dashVector.x == 0 && _dashVector.z == 0)
            {
                _dashVector = GetComponentInParent<Transform>().forward;
            }

            StartCoroutine(InitiateDashCooldown(_dashCooldown));
            StartCoroutine(InitiateDashDuration(_dashDistance / _dashSpeed));
        }
    }


    private void DashCase()
    {
        if (IsGrounded())
        {
            _verticalVector.y = -0.5f;
        }
        else
        {
            ApplyGravity();
        }
        
        _characterController.Move(Time.deltaTime * _dashSpeed * _dashVector);
    }


    private IEnumerator InitiateDashCooldown(float seconds)
    {
        _dashCharges--;

        yield return new WaitForSeconds(seconds);

        _dashCharges++;
    }


    private IEnumerator InitiateDashDuration(float seconds)
    {
        _isDashing = true;

        yield return new WaitForSeconds(seconds);

        _isDashing = false;
    }


    private void AttackInputActionStarted(InputAction.CallbackContext context)
    {

    }


    private void PauseInputActionStarted(InputAction.CallbackContext context)
    {
        //if (gamePaused)
        //{
        //    gamePaused = false;
        //    Time.timeScale = 1;

        //    lockControls = false;
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //    crosshair.enabled = true;
        //    pauseMenu.enabled = false;

        //    foreach (Transform child in pauseMenu.transform)
        //    {
        //        child.gameObject.SetActive(false);
        //    }
        //}
        //else
        //{
        //    gamePaused = true;
        //    Time.timeScale = 0;

        //    lockControls = true;
        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;
        //    crosshair.enabled = false;
        //    pauseMenu.enabled = true;

        //    foreach (Transform child in pauseMenu.transform)
        //    {
        //        child.gameObject.SetActive(true);
        //    }
        //}
    }


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;

    //    // Exact Center of the player's character.
    //    Gizmos.DrawRay(playerCenter.position, Vector2.down * groundedRaycastFloorDistance);

    //    // Four corners of the player's character.
    //    for (int i = -1; i <= 1; i += 2)
    //    {
    //        for (int j = -1; j <= 1; j += 2)
    //        {
    //            Gizmos.DrawRay(playerCenter.position + new Vector3(groundedRaycastRadialDistance * i, 0, groundedRaycastRadialDistance * j),
    //                           Vector2.down * groundedRaycastFloorDistance);
    //        }
    //    }
    //}
}
