// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, August 2025.
//     Contributed to by Hainish Acharya for GAMES 4500, University of Utah, August 2025.
//          -Added independent character rotation functionality.
//          -Added knockback functionality.
//          -Added support for stat data modification.
//   </para>
// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction sprintActions;
    private InputAction jumpActions;
    private InputAction dashActions;

    [Header("Player References")]
    [SerializeField] private Transform _playerCenter;
    [SerializeField] private Transform _cameraTransform;
    private CharacterController _characterController;
    private Entity _playerEntity;
    private float _playerHalfHeight;
    private float _playerRadius;
    private float _groundedShpereCastRadius;
    private float _originalStepOffset;

    [Header("Movement Parameters")]
    [SerializeField] private float _maxSpeed;
    [SerializeField] [Range(1, 9)] private float _sprintMultiplier;
    [SerializeField] private float _accelerationSeconds;
    [SerializeField] private float _decelerationSeconds;
    [SerializeField] private float _characterRotationDamping;
    private float _currSprintMultiplierValue;
    private bool _isSprintEnabled;
    private float _acceleration;
    private float _deceleration;
    private Vector3 lateralVector;
    private Vector3 _moveInputUnitVector;

    [Header("KnockBack Parameters")]
    [SerializeField] private float _kbDamping;
    [SerializeField] private float _kbControlsLockTime;
    [SerializeField] private float _kbDashLockTime;
    private Vector3 _externalKnockbackVelocity;
    private float _kbControlsLockTimer;
    private float _kbDashLockTimer;

    [Header("Jump Parameters")]
    [SerializeField] private float _JumpHeight;
    [SerializeField] [Range(0, 1)] private float _hoverDescentReductionMultiplier;
    [SerializeField] private float _gravityMultiplier;
    private float _currHoverDescentReductionMultiplier;
    public bool IsGrounded { get; private set; }
    private bool _inputtedJumpThisFrame;
    private bool _isHovering;
    private Vector3 _verticalVector;

    [Header("Coyote Time Parameters")]
    [SerializeField] private float _coyoteTimeWindow;
    [SerializeField] private float _jumpBufferWindow;
    private float _currCoyoteTime;
    private float _currJumpBufferTime;
    private bool _jumpBufferPending;

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

    private int _groundCollisionLayerMasks;

    private bool _lockControls = false;

    // Set by AimController.
    private bool strafe = false;



    void Awake()
    {
        _playerEntity = GetComponent<Entity>();
        _characterController = GetComponent<CharacterController>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        moveActions = playerActions.Move;
        sprintActions = playerActions.Sprint;
        jumpActions = playerActions.Jump;
        dashActions = playerActions.Dash;
        
        moveActions.Enable();
        sprintActions.Enable();
        jumpActions.Enable();
        dashActions.Enable();
        
        jumpActions.started += JumpInputActionStarted;
        jumpActions.canceled += JumpInputActionCanceled;
        sprintActions.started += SprintInputActionStarted;
        dashActions.started += DashInputActionStarted;
    }

    private void OnDisable()
    {
        moveActions.Disable();
        sprintActions.Disable();
        jumpActions.Disable();
        dashActions.Disable();

        jumpActions.started -= JumpInputActionStarted;
        jumpActions.canceled -= JumpInputActionCanceled;
        sprintActions.started -= SprintInputActionStarted;
        dashActions.started -= DashInputActionStarted;
    }

    void Start()
    {
        _playerHalfHeight = GetComponent<CharacterController>().height / 2;
        _playerRadius = GetComponent<CharacterController>().radius;
        _groundedShpereCastRadius = _playerRadius - 0.1f;
        _originalStepOffset = _characterController.stepOffset;

        _currSprintMultiplierValue = 1;
        _isSprintEnabled = false;
        _acceleration = _maxSpeed / _accelerationSeconds;
        _deceleration = _maxSpeed / _decelerationSeconds;

        _currHoverDescentReductionMultiplier = 1;
        IsGrounded = CheckIsGrounded();
        _jumpBufferPending = false;
        _isHovering = false;

        _currCoyoteTime = _coyoteTimeWindow;
        _currJumpBufferTime = _jumpBufferWindow;

        _currBoostEnergy = _maxBoostEnergy;
        _currBoostDoubleTapTime = _boostDoubleTapWindow;
        _isBoosting = false;
        _isRegeneratingBoost = false;

        _isDashing = false;

        _groundCollisionLayerMasks = LayerMask.GetMask("Environment");
        _groundCollisionLayerMasks |= LayerMask.GetMask("Interactable");
        _groundCollisionLayerMasks |= LayerMask.GetMask("Obstacles");
        _groundCollisionLayerMasks |= LayerMask.GetMask("Enemy");
    }

    void Update()
    {
        if (_lockControls) return;

        if (_kbControlsLockTimer > 0) _kbControlsLockTimer -= Time.deltaTime;
        if (_kbDashLockTimer > 0) _kbDashLockTimer -= Time.deltaTime;

        ApplyGravity();

        if (_isDashing)
        {
            DashConditions();
        }
        else
        {
            MoveConditions();
            JumpConditions();
            IsGrounded = CheckIsGrounded();
            HoverConditions();
            BoostConditions();
        }
    }

    /// <summary>
    ///   <para>
    ///     
    ///   </para>
    /// </summary>
    /// <param name="on">  </param>
    public void SetStrafeMode(bool on) => strafe = on;


    /// <summary>
    ///   <para>
    ///     Applies a knockback force to the player character any time this method is called.
    ///   </para>
    /// </summary>
    /// <param name="impulse"> The total knockback force. </param>
    public void ApplyImpulse(Vector3 impulse)
    {
        _externalKnockbackVelocity += impulse;

        _kbControlsLockTimer = Mathf.Max(_kbControlsLockTimer, _kbControlsLockTime);
        _kbDashLockTimer = Mathf.Max(_kbDashLockTimer, _kbControlsLockTime + _kbDashLockTime);

        _isDashing = false; // Cancel dashing immediately.
    }

    /// <summary>
    ///   <para>
    ///     
    ///   </para>
    /// </summary>
    public event System.Action<float> DashCooldownStarted;

    /// <summary>
    ///   <para>
    ///     Checks if the player character is touching the ground on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the player character is on the ground, otherwise false. </returns>
    private bool CheckIsGrounded()
    {
        Vector3 SphereCastOrigin = _playerCenter.position + new Vector3(0, -_playerHalfHeight + _groundedShpereCastRadius, 0);

        if (Physics.SphereCast(SphereCastOrigin,
                               _groundedShpereCastRadius,
                               Vector2.down,
                               hitInfo: out RaycastHit hitInfo,
                               0.1f,
                               _groundCollisionLayerMasks,
                               QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return false;
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Vector3 SphereCastOrigin = _playerCenter.position + new Vector3(0, -_playerHalfHeight + _groundedShpereCastRadius - 0.1f, 0);
    //    Gizmos.DrawSphere(SphereCastOrigin, _groundedShpereCastRadius);
    //}

    /// <summary>
    ///   <para>
    ///     Applies calculated custom gravity to the player every frame.
    ///   </para>
    /// </summary>
    private void ApplyGravity()
    {
        IsGrounded = CheckIsGrounded();
        
        // Do not apply gravity when boosting.
        if (!IsGrounded && !_isBoosting)
        {
            float aggregateGravityModifier = _gravityMultiplier * _currHoverDescentReductionMultiplier;
            _verticalVector += Time.deltaTime * aggregateGravityModifier * Physics.gravity;

            // Limit descent speed to the strength of gravity.
            if (_verticalVector.y < aggregateGravityModifier * Physics.gravity.y)
            {
                _verticalVector.y = aggregateGravityModifier * Physics.gravity.y;
            }

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
    }

    /// <summary>
    ///   <para>
    ///     Moves the player in the input direction an amount of distance calculated for every frame.
    ///   </para>
    /// </summary>
    private void MoveConditions()
    {
        // Update _maxSpeed, _acceleration and _deceleration values whenever movement stats are changed.
        if (_playerEntity != null)
        {
            if (_maxSpeed != _playerEntity.Stats.MoveSpeed)
            {
                _maxSpeed = _playerEntity.Stats.MoveSpeed;
                RecalculateAccelDecel();
            }
        }

        // Because move speed right before moment of knockback must be preserved for correct calculations,
        // simply stop recording new movement values instead of completely skipping the MoveCase method.
        _moveInputUnitVector = (_kbControlsLockTimer > 0) ? Vector3.zero : GetMoveInputDirection();

        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();

        // Accelerate if movement is being input and sprint has not been canceled.
        if (_moveInputUnitVector != Vector3.zero && lateralVector.magnitude <= aggregateMaxSpeedValue)
        {
            Accelerate(aggregateMaxSpeedValue);
        }
        // Otherwise, disable sprint and decelerate.
        else
        {
            DisableSprint();
            Decelerate();
        }

        // Apply knockback until it has dissipated.
        if (_externalKnockbackVelocity.sqrMagnitude > 1e-6f)
        {
            _characterController.Move(Time.deltaTime * _externalKnockbackVelocity);
            _externalKnockbackVelocity = Vector3.Lerp(_externalKnockbackVelocity, Vector3.zero, Time.deltaTime * _kbDamping);
        }

        // Turn the player character toward the input direction.
        if (_kbControlsLockTimer <= 0 && !strafe && lateralVector.sqrMagnitude > 0.0001f)
        {
            Quaternion qa = transform.rotation;
            Quaternion qb = Quaternion.LookRotation(lateralVector, Vector3.up);
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, _characterRotationDamping));
            transform.rotation = Quaternion.Slerp(qa, qb, t);
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
        Vector2 moveInput = moveActions.ReadValue<Vector2>();

        Vector3 cameraPerspectiveForward = _cameraTransform ? _cameraTransform.forward : Vector3.forward;
        Vector3 cameraPerspectiveRight = _cameraTransform ? _cameraTransform.right : Vector3.right;
        cameraPerspectiveForward.y = 0;
        cameraPerspectiveRight.y = 0;

        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 moveDirection = (cameraPerspectiveRight.normalized * inputDirection.x)
                                 + (cameraPerspectiveForward.normalized * inputDirection.z);

        return moveDirection.normalized;
    }

    /// <summary>
    ///   Accelerates the player character on any frame this method is called up to the max movement speed.
    /// </summary>
    private void Accelerate(float aggregateMaxSpeedValue)
    {
        Vector3 aggregateAccelIncrement = Time.deltaTime * _acceleration * _currSprintMultiplierValue * _moveInputUnitVector;

        // Limit lateral move speed to aggregateMaxSpeedValue.
        if (lateralVector.magnitude < aggregateMaxSpeedValue)
        {
            // If acceleration increment for the current frame exceeds aggregateMaxSpeedValue,
            // then set current speed to exactly aggregateMaxSpeedValue.
            if (lateralVector.magnitude + aggregateAccelIncrement.magnitude > aggregateMaxSpeedValue)
            {
                aggregateAccelIncrement = (aggregateMaxSpeedValue - lateralVector.magnitude) * _moveInputUnitVector;
            }
        }
        else
        {
            aggregateAccelIncrement = Vector3.zero;
        }

        lateralVector = lateralVector.magnitude * _moveInputUnitVector;
        lateralVector += aggregateAccelIncrement;

        // Redundency check for limiting move speed to ensure sprint is not exited unexpectedly.
        if (lateralVector.magnitude > aggregateMaxSpeedValue)
        {
            lateralVector = aggregateMaxSpeedValue * lateralVector.normalized;
        }

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


    private float CalculateAggregateMaxSpeedValue()
    {
        return _maxSpeed * _currSprintMultiplierValue;
    }


    private void RecalculateAccelDecel()
    {
        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();
        _acceleration = aggregateMaxSpeedValue / _accelerationSeconds;
        _deceleration = aggregateMaxSpeedValue / _decelerationSeconds;
    }

    /// <summary>
    ///   <para>
    ///     Toggles sprinting for the player character on any frame that sprint is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The sprint input context. </param>
    private void SprintInputActionStarted(InputAction.CallbackContext context)
    {
        if (_isSprintEnabled)
        {
            DisableSprint();
        }
        else
        {
            EnableSprint();
        }
    }

    /// <summary>
    ///   <para>
    ///     Sets the current sprint multiplier value to _sprintMultiplier and recalculates acceleration
    ///     and deceleration on any frame this method is called.
    ///   </para>
    /// </summary>
    private void EnableSprint()
    {
        _isSprintEnabled = true;
        _currSprintMultiplierValue = _sprintMultiplier;
        RecalculateAccelDecel();
    }

    /// <summary>
    ///   <para>
    ///     Sets the current sprint multiplier value to 1 and recalculates acceleration and deceleration
    ///     on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DisableSprint()
    {
        _isSprintEnabled = false;
        _currSprintMultiplierValue = 1;
        RecalculateAccelDecel();
    }

    /// <summary>
    ///   <para>
    ///     Executes a sequence of jump conditions on any frame that jump is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The jump input context. </param>
    private void JumpInputActionStarted(InputAction.CallbackContext context)
    {
        if (_kbControlsLockTimer > 0) return;

        if (!_isDashing)
        {
            _inputtedJumpThisFrame = true;
            _jumpBufferPending = true;
        }
        if (!IsGrounded && !_isDashing)
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
    ///     Failsafe to ensure the player character stops hovering or boosting on any
    ///     frame that the jump input is released.
    ///   </para>
    /// </summary>
    /// <param name="context"> The jump input context. </param>
    private void JumpInputActionCanceled(InputAction.CallbackContext context)
    {
        _isHovering = false;
        _currHoverDescentReductionMultiplier = 1;

        _isBoosting = false;
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character jump if the conditions necessary are satisfied
    ///     on the frame this method is called.
    ///   </para>
    /// </summary>
    private void JumpConditions()
    {
        IsGrounded = CheckIsGrounded();

        // If on the ground and jump was inputted and jump buffer window is valid, or if walked off an edge and coyote time window is valid, then jump.
        if ( (_jumpBufferPending && IsGrounded && IsWithinJumpBufferWindow()) || (_inputtedJumpThisFrame && !IsGrounded && IsWithinCoyoteTimeWindow()) )
        {
            _jumpBufferPending = false;
            _currCoyoteTime = 0;
            _currJumpBufferTime = 0;
            _verticalVector.y = _JumpHeight;
            _characterController.stepOffset = 0; // Disable stepOffset in midair to prevent buggy movement behavior when near edges.

            _characterController.Move(Time.deltaTime * _verticalVector);
        }
        // If jump was inputted midair, reset jump buffer window and decrement coyote time and jump buffer timers.
        else if (_inputtedJumpThisFrame && !IsGrounded)
        {
            _currJumpBufferTime = _jumpBufferWindow;
            DecrementCoyoteAndBufferTimers();
        }
        // If in midair, decrement coyote time and jump buffer timers.
        else if (!IsGrounded)
        {
            DecrementCoyoteAndBufferTimers();
        }
        // Otherwise, reset coyote time, jump buffer time, boosting status, gravity force and stepOffset to
        // original states because player charater is on the ground.
        else
        {
            _jumpBufferPending = false;
            _currCoyoteTime = _coyoteTimeWindow;
            _currJumpBufferTime = _jumpBufferWindow;
            _isBoosting = false;
            _verticalVector.y = -0.5f;
            _characterController.stepOffset = _originalStepOffset;
        }

        _inputtedJumpThisFrame = false;
    }

    /// <summary>
    ///   <para>
    ///     Decrements the coyote time and jump buffer timers on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DecrementCoyoteAndBufferTimers()
    {
        if (IsWithinCoyoteTimeWindow()) _currCoyoteTime -= Time.deltaTime;
        if (IsWithinJumpBufferWindow()) _currJumpBufferTime -= Time.deltaTime;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the window of time in which coyote time is valid has closed on the
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns></returns>
    private bool IsWithinCoyoteTimeWindow()
    {
        return _currCoyoteTime > 0;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the window of time in which jump buffer is valid has closed on the
    ///     frame thise method is called.
    ///   </para>
    /// </summary>
    /// <returns></returns>
    private bool IsWithinJumpBufferWindow()
    {
        return _currJumpBufferTime > 0;
    }

    /// <summary>
    ///   <para>
    ///     Initiates hovering for the player character if the necessary conditions are satisfied
    ///     on the frame this method is called.
    ///   </para>
    /// </summary>
    private void HoverConditions()
    {
        // Cease hovering if jump input is no longer held or the player character landed.
        if ( (_isHovering && !jumpActions.IsPressed()) || (_isHovering && IsGrounded) )
        {
            _isHovering = false;
            _currHoverDescentReductionMultiplier = 1;
        }

        // Only modify gravity for hovering while falling and while the coyote time and jump buffer windows are invalid.
        if (_isHovering && !IsWithinCoyoteTimeWindow() && !IsWithinJumpBufferWindow() && _verticalVector.y <= 0)
        {
            _currHoverDescentReductionMultiplier = _hoverDescentReductionMultiplier;
        }
    }

    /// <summary>
    ///   <para>
    ///     Boosts the player character if the necessary conditions are satisfied
    ///     on the frame this method is called.
    ///   </para>
    /// </summary>
    private void BoostConditions()
    {
        // Cease hovering if jump input is no longer held or boost energy is depleted.
        if ( (_isBoosting && !jumpActions.IsPressed()) || _currBoostEnergy <= 0 )
        {
            _isBoosting = false;
        }

        // If in midair, jump input was double-tapped and is still held, and boost energy is not depleted, then boost.
        if (!IsGrounded && _isBoosting && jumpActions.IsPressed() && _currBoostEnergy > 0)
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
        if (IsGrounded && _currBoostEnergy < _maxBoostEnergy && !_isRegeneratingBoost)
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

        while (_currBoostEnergy < _maxBoostEnergy)
        {
            // Cancel boost energy regeneration if boost was inputted.
            if (_isBoosting)
            {
                break;
            }

            _currBoostEnergy += Time.deltaTime * _boostRegenerationRate;

            // If boost regeneration increment for the current frame exceeds _maxBoostEnergy,
            // then set boost energy to exactly _maxBoostEnergy.
            if (_currBoostEnergy >= _maxBoostEnergy)
            {
                _currBoostEnergy = _maxBoostEnergy;
                break;
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
    ///     closed on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the window of time has not closed, otherwise false. </returns>
    private bool IsWithinBoostWindow()
    {
        return _currBoostDoubleTapTime > 0;
    }

    /// <summary>
    ///   <para>
    ///     Executes a sequence of dash conditions on any frame that dash is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The dash input context. </param>
    private void DashInputActionStarted(InputAction.CallbackContext context)
    {
        // Check if the player character is being knocked back.
        if (_kbDashLockTimer > 0f) return;

        _dashVector = GetMoveInputDirection();

        if (_dashCharges != 0 && !_isDashing)
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
    ///     on the frame this method is called.
    ///   </para>
    /// </summary>
    private void DashConditions()
    {
        IsGrounded = CheckIsGrounded();
        
        if (IsGrounded)
        {
            _verticalVector.y = -0.5f;
        }
        else if (_isBoosting)
        {
            BoostConditions();
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
        DashCooldownStarted?.Invoke(seconds); // Tell listener to start the faded.

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
}
