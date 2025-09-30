using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControllerV1 : MonoBehaviour
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
    private float _deceleration;
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
        _deceleration = _maxSpeed / _decelerationSeconds;

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
            HoverCase();
            BoostCase();
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

    /// <summary>
    ///   <para>
    ///      Gets the current lateral input direction for every frame.
    ///   </para>
    /// </summary>
    /// <returns> A normalized vector parallel with the xz-plane. </returns>
    private Vector3 GetMoveInputDirection()
    {
        Vector3 moveInput = moveActions.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);

        return transform.TransformDirection(inputDirection).normalized;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the player character is touching the ground during the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the player character is on the ground, otherwise false. </returns>
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

    /// <summary>
    ///   <para>
    ///     Applies calculated custom gravity to the player every frame.
    ///   </para>
    /// </summary>
    private void ApplyGravity()
    {
        // Do not apply gravity when boosting.
        if (!IsGrounded() && !_isBoosting)
        {
            _verticalVector += Time.deltaTime * _aggregateGravityModifier * Physics.gravity;

            // Limit descent speed to the strength of gravity.
            if (_verticalVector.y < _aggregateGravityModifier * Physics.gravity.y)
            {
                _verticalVector.y = _aggregateGravityModifier * Physics.gravity.y;
            }

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
    }

    /// <summary>
    ///   <para>
    ///     Moves the player in the input direction an amount of distance calculated for every frame.
    ///   </para>
    /// </summary>
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

    /// <summary>
    ///   Accelerates the player character during any frame this method is called up to the max movement speed.
    /// </summary>
    private void Accelerate()
    {
        Vector3 accelIncrement = Time.deltaTime * _acceleration * _moveInputUnitVector;

        // Limit lateral move speed to _maxSpeed.
        if (lateralVector.magnitude < _maxSpeed)
        {
            // If acceleration increment for the current frame exceeds _maxSpeed,
            // then set current speed to exactly _maxSpeed.
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

    /// <summary>
    ///   <para>
    ///     Decelerates the player character during any frame this method is called until fully stopped.
    ///   </para>
    /// </summary>
    private void Decelerate()
    {
        // Skip deceleration calculations if not moving.
        if (lateralVector.magnitude > 0)
        {
            Vector3 decelIncrement = Time.deltaTime * _deceleration * lateralVector.normalized;

            // If deceleration decrement for the current frame exceeds zero,
            // then set current speed to exactly zero.
            if (lateralVector.magnitude - decelIncrement.magnitude < 0)
            {
                lateralVector = Vector3.zero;
                decelIncrement = Vector3.zero;
            }

            lateralVector -= decelIncrement;
            _characterController.Move(Time.deltaTime * lateralVector);
        }
    }

    /// <summary>
    ///   <para>
    ///     Executes a sequence of conditions during any frame that jump is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The jump input context. </param>
    private void JumpInputActionStarted(InputAction.CallbackContext context)
    {
        // If on the ground and not dashing, then jump.
        if (IsGrounded() && !_isDashing)
        {
            _didPerformJump = true;
        }
        // Otherwise, begin hovering if not on the ground and not dashing.
        else if (!IsGrounded() && !_isDashing)
        {
            _isHovering = true;
        }

        // Begin the window of time for double-tapping the jump input if not already initiated.
        if (_currBoostDoubleTapTime == _boostDoubleTapWindow)
        {
            StartCoroutine(BoostDoubleTapTimer());
        }
        // Otherwise, if the window of time has not closed then begin boosting.
        else if (IsWithinBoostWindow())
        {
            _isHovering = false;
            _isBoosting = true;
        }
    }

    /// <summary>
    ///   <para>
    ///     Failsafe to ensure the player character stops hovering or boosting when
    ///     the jump input is no longer held.
    ///   </para>
    /// </summary>
    /// <param name="context"> The jump input context. </param>
    private void JumpInputActionCanceled(InputAction.CallbackContext context)
    {
        _isHovering = false;
        _aggregateGravityModifier = _gravityMultiplier;

        _isBoosting = false;

    }

    /// <summary>
    ///   <para>
    ///     Makes the player character jump if the conditions necessary are satisfied
    ///     during the frame this method is called.
    ///   </para>
    /// </summary>
    private void JumpCase()
    {
        // If on the ground and jump was inputted, then jump. 
        // Disable stepOffset to prevent buggy movement behavior when near edges.
        if (_didPerformJump && IsGrounded())
        {
            _didPerformJump = false;
            _verticalVector.y = _JumpHeight;
            _characterController.stepOffset = 0;

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
        // Otherwise, if still in midair then keep stepOffset disabled.
        else if (!IsGrounded())
        {
            _characterController.stepOffset = 0;
        }
        // Otherwise, reset boosting status, gravity force and stepOffset to original states
        // because player charater is on the ground.
        else
        {
            _isBoosting = false;
            _verticalVector.y = -0.5f;
            _characterController.stepOffset = _originalStepOffset;
        }
    }

    /// <summary>
    ///   <para>
    ///     Hovers the player character if the necessary conditions are satisfied
    ///     during the frame this method is called.
    ///   </para>
    /// </summary>
    private void HoverCase()
    {
        // Cease hovering if jump input is no longer held or the player character landed.
        if ((_isHovering && !jumpActions.IsPressed()) || IsGrounded())
        {
            _isHovering = false;
            _aggregateGravityModifier = _gravityMultiplier;
        }

        // Only modify gravity for hovering while falling.
        if (_isHovering && jumpActions.IsPressed() && _verticalVector.y < 0)
        {
            _aggregateGravityModifier = _gravityMultiplier * _hoverDescentReductionMultiplier;
        }
    }

    /// <summary>
    ///   <para>
    ///     Boosts the player character if the necessary conditions are satisfied
    ///     during the frame this method is called.
    ///   </para>
    /// </summary>
    private void BoostCase()
    {
        // Cease hovering if jump input is no longer held or boost energy is depleted.
        if ((_isBoosting && !jumpActions.IsPressed()) || _currBoostEnergy <= 0)
        {
            _isBoosting = false;
        }

        // If in midair, jump input was double-tapped and is still held, and boost energy is not depleted, then boost.
        if (!IsGrounded() && _isBoosting && jumpActions.IsPressed() && _currBoostEnergy > 0)
        {
            Vector3 boostSpeedIncrement = Time.deltaTime * _boostAcceleration * Vector3.up;
            float boostDepletionDecrement = Time.deltaTime * _boostDepletionRate;

            // If boost energy decrement for the current frame does not exceed zero,
            // then decrease current boost energy by the full decrement.
            if (_currBoostEnergy - boostDepletionDecrement > 0)
            {
                _currBoostEnergy -= boostDepletionDecrement;
            }
            // Otherwise set current boost energy to exactly zero and immediately begin hovering.
            else
            {
                _currBoostEnergy = 0;
                _isHovering = true;
            }

            // Limit vertical move speed to _maxBoostSpeed.
            if (_verticalVector.magnitude < _maxBoostSpeed)
            {
                _verticalVector += boostSpeedIncrement;

                // If boost increment for the current frame exceeds _maxBoostSpeed,
                // then set vertical move speed to exactly _maxBoostSpeed.
                if (_verticalVector.magnitude > _maxBoostSpeed)
                {
                    _verticalVector = new Vector3(_verticalVector.x, _maxBoostSpeed, _verticalVector.z);
                }
            }

            _characterController.Move(Time.deltaTime * _verticalVector);
        }

        // Initialize regeneration coroutine if boosted, on the ground and not already regenerating.
        if (IsGrounded() && _currBoostEnergy < _maxBoostEnergy && !_isRegeneratingBoost)
        {
            StartCoroutine(BoostRegeneration());
        }
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating boost energy to full capacity over time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator BoostRegeneration()
    {
        _isRegeneratingBoost = true;

        while (_currBoostEnergy <= _maxBoostEnergy)
        {
            // Stop regenerating boost energy if boost was inputted.
            if (_isBoosting)
            {
                break;
            }

            _currBoostEnergy += Time.deltaTime * _boostRegenerationRate;

            // If boost regeneration increment for the current frame exceeds _maxBoostEnergy,
            // then set boost energy to exactly _maxBoostEnergy.
            if (_currBoostEnergy > _maxBoostEnergy)
            {
                _currBoostEnergy = _maxBoostEnergy;
            }

            yield return null;
        }

        _isRegeneratingBoost = false;
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for tracking the window of time in which the jump input can be double-tapped.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator BoostDoubleTapTimer()
    {
        while (IsWithinBoostWindow())
        {
            _currBoostDoubleTapTime -= Time.deltaTime;

            yield return null;
        }

        _currBoostDoubleTapTime = _boostDoubleTapWindow;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the window of time in which the jump input can be double-tapped has
    ///     closed during the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the window of time has not closed, otherwise false. </returns>
    private bool IsWithinBoostWindow()
    {
        return _currBoostDoubleTapTime > 0;
    }

    /// <summary>
    ///   <para>
    ///     Executes a sequence of conditions during any frame that dash is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The dash input context. </param>
    private void DashInputActionStarted(InputAction.CallbackContext context)
    {
        _dashVector = GetMoveInputDirection();

        if (_dashCharges != 0)
        {
            // If not moving, default dash direction is forward.
            if (_dashVector.x == 0 && _dashVector.z == 0)
            {
                _dashVector = GetComponentInParent<Transform>().forward;
            }

            StartCoroutine(InitiateDashCooldown(_dashCooldown));
            StartCoroutine(InitiateDashDuration(_dashDistance / _dashSpeed));
        }
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character dash if the necessary conditions are satisfied
    ///     during the frame this method is called.
    ///   </para>
    /// </summary>
    private void DashCase()
    {
        if (IsGrounded())
        {
            _verticalVector.y = -0.5f;
        }
        else if (_isBoosting)
        {
            BoostCase();
        }
        else
        {
            ApplyGravity();
        }
        
        _characterController.Move(Time.deltaTime * _dashSpeed * _dashVector);
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating individual dash charges over time.
    ///   </para>
    /// </summary>
    /// <param name="seconds"> The cooldown time for dash regeneration. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator InitiateDashCooldown(float seconds)
    {
        _dashCharges--;

        yield return new WaitForSeconds(seconds);

        _dashCharges++;
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for tracking the length of time in which a dash takes place.
    ///   </para>
    /// </summary>
    /// <param name="seconds"> The duration of the dash. </param>
    /// <returns> IEnumerator object. </returns>
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
