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

public class PlayerMovementV4 : MonoBehaviour
{
    
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction sprintActions;
    private InputAction jumpActions;
    private InputAction dashActions;

    [Header("Player References")] [Space]
    [SerializeField]
    [Tooltip("An empty object positioned at the exact center of the player character object.")] private Transform _playerCenter;
    [SerializeField]
    [Tooltip("The player camera object.")] private Transform _cameraTransform;
    private Entity _playerEntity;
    private CharacterController _characterController;
    private float _playerHalfHeight;
    private float _playerRadius;

    [Header("Movement Parameters")] [Space]
    [SerializeField]
    [Tooltip("Max move speed in units per second.")] private float _maxSpeed;
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Speed.")] private float _accelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Speed.")] private float _decelerationSeconds;
    [SerializeField]
    [Tooltip("How quickly the player character aligns with the camera direction in units per second.")] private float _characterRotationDamping;
    private float _acceleration;
    private float _deceleration;
    private Vector3 lateralVelocityVector;
    private Vector3 _moveDirectionUnitVector;

    [Header("Hover Parameters")] [Space]
    [SerializeField]
    [Tooltip("Height above the ground the player character hovers in units.")] private float _hoverHeight;
    [SerializeField]
    [Tooltip("How strongly the player character is pulled to Hover Height in units per second.")] private float _hoverPullStrength;
    [SerializeField]
    [Tooltip("How strongly Hover Pull Strength dissipates in units per second.")] private float _hoverDampingStrength;
    [SerializeField]
    [Tooltip("Sphere casting distance below the player in units.")] private float _groundedCastLength;
    [SerializeField]
    [Tooltip("Seconds that sphere casting is paused after a jump is registered.")] private float _groundedCastJumpPauseDuration;
    private float _currHoverHeight;
    public bool IsGrounded { get; private set; }
    private float _groundedCastRadius;
    private float _groundedCastPauseTimer;
    private int _groundedLayerMasks;
    private RaycastHit _groundPoint;

    [Header("KnockBack Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds needed for a knockback impulse to dissipate.")] private float _kbDamping;
    [SerializeField]
    [Tooltip("Seconds that controls are locked after a knockback impulse.")] private float _kbControlsLockTime;
    [SerializeField]
    [Tooltip("Seconds that dashing is locked after a knockback impulse.")] private float _kbDashLockTime;
    private float _kbControlsLockTimer;
    private float _kbDashLockTimer;
    private Vector3 _externalKnockbackVelocity;

    [Header("Dash Parameters")] [Space]
    [SerializeField]
    [Tooltip("Distance that the player character dashes in units.")] private float _dashDistance;
    [SerializeField]
    [Tooltip("How quickly the player character travels Dash Distance in units per second.")] private float _dashSpeed;
    [SerializeField]
    [Tooltip("Seconds needed for dash charges to come off cooldown.")] private float _dashCooldown;
    [SerializeField]
    [Tooltip("The quantity of available dash charges.")] private int _dashCharges;
    private bool _isDashing;
    private int _currDashCharges;
    public event System.Action<float> DashCooldownStarted;
    private Vector3 _dashDirectionUnitVector;

    [Header("Jump Parameters")] [Space]
    [SerializeField]
    [Tooltip("Vertical jump strength in units per second.")] private float _JumpForce;
    private bool _inputtedJumpThisFrame;
    private Vector3 _verticalVelocityVector;

    [Header("Coyote Time Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered after walking off an edge.")] private float _coyoteTimeWindow;
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered before touching the ground.")] private float _jumpBufferWindow;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    
    [Header("Drift Parameters")] [Space]
    [SerializeField] [Range(0.1f, 1f)]
    [Tooltip("Gravity multiplier when gliding (lower = slower fall). 0.3 = gentle glide.")] private float _driftDescentDivisor = 0.3f;
    [SerializeField]
    [Tooltip("Seconds before Drift Descent Divisor gradually reaches full effect.")] private float _driftDelay;
    [SerializeField]
    [Tooltip("How much gravity is multiplied in units per second (base gravity value is -9.81).")] public float _gravityMultiplier;
    private bool _isDrifting;
    private float _currDriftDescentDivisor;
    private float _driftDelayTimer;

    [Header("Flight Energy Parameters")]
    [Space]
    [SerializeField]
    [Tooltip("Max flight energy.")] private int _maxFlightEnergy = 100;
    [SerializeField]
    [Tooltip("Energy used per second when flying.")] private float _flightDepletionRate = 20f;
    [SerializeField]
    [Tooltip("Energy refilled per second on ground.")] private float _flightRegenerationRate = 30f;
    [SerializeField]
    [Tooltip("Max upward speed when flying.")] private float _maxFlightSpeed = 15f;
    [SerializeField]
    [Tooltip("Time to reach max flight speed.")] private float _flightAccelerationSeconds = 0.5f;
    [SerializeField]
    [Range(1f, 3f)]
    [Tooltip("Move speed boost while flying.")] private float _flightSpeedMultiplier = 1.5f;
    private float _currFlightEnergy;
    private bool _isRegeneratingFlight;
    private float _flightAcceleration;
    private bool _isFlying;


    private bool strafe = false; // Set by AimController.

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
        dashActions.started -= DashInputActionStarted;
    }

    void Start()
    {
        // Player References
        _playerHalfHeight = GetComponent<CharacterController>().height / 2;
        _playerRadius = GetComponent<CharacterController>().radius;

        // Movement Parameters
        _acceleration = _maxSpeed / _accelerationSeconds;
        _deceleration = _maxSpeed / _decelerationSeconds;

        // Hover Parameters
        IsGrounded = CheckIsGrounded();
        _groundedCastRadius = _playerRadius - 0.1f;
        _groundedCastPauseTimer = 0;
        _groundedLayerMasks = LayerMask.GetMask("Environment");
        _groundedLayerMasks |= LayerMask.GetMask("Interactable");
        _groundedLayerMasks |= LayerMask.GetMask("Obstacles");
        _groundedLayerMasks |= LayerMask.GetMask("Enemy");

        // KnockBack Parameters
        _kbControlsLockTimer = 0;
        _kbDashLockTimer = 0;

        //Dash Parameters
        _isDashing = false;
        _currDashCharges = _dashCharges;

        // Jump Parameters
        _inputtedJumpThisFrame = false;

        // Coyote Time Parameters
        _coyoteTimer = _coyoteTimeWindow;
        _jumpBufferTimer = 0;

        // Drift Parameters
        _currDriftDescentDivisor = 1;
        _driftDelayTimer = 0;
        _isDrifting = false;

        // Flight Energy Parameters
        _currFlightEnergy = _maxFlightEnergy;
        _isRegeneratingFlight = false;
        _flightAcceleration = _maxFlightSpeed / _flightAccelerationSeconds;
        _isFlying = false;
        
    }

    void Update()
    {
        IsGrounded = CheckIsGrounded();
        DecrementAllTimers();

        if (_kbControlsLockTimer > 0) return;

        GravityConditions();

        if (_isDashing)
        {
            IsGrounded = CheckIsGrounded();
            DashConditions();
        }
        else
        {
            MoveConditions();
            IsGrounded = CheckIsGrounded();
            HoverConditions();
            JumpConditions();
            IsGrounded = CheckIsGrounded();
            DriftConditions();
            FlightConditions();
        }
    }

    /// <summary>
    ///   <para>
    ///     Sets the strafe mode status for the player character.
    ///   </para>
    /// </summary>
    /// <param name="on"> Strafe mode status. </param>
    public void SetStrafeMode(bool on) => strafe = on;

    /// <summary>
    ///   <para>
    ///     Applies a knockback force to the player character.
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
    ///     Decrements all active timers every frame.
    ///   </para>
    /// </summary>
    private void DecrementAllTimers()
    {
        if (_kbControlsLockTimer > 0) _kbControlsLockTimer -= Time.deltaTime;
        if (_kbDashLockTimer > 0) _kbDashLockTimer -= Time.deltaTime;
        if (_groundedCastPauseTimer > 0) _groundedCastPauseTimer -= Time.deltaTime;
        if (IsWithinCoyoteTimeWindow() && !IsGrounded) _coyoteTimer -= Time.deltaTime;
        if (IsWithinJumpBufferWindow() && !IsGrounded) _jumpBufferTimer -= Time.deltaTime;
        if (AreDriftRequirementsValid()) _driftDelayTimer -= Time.deltaTime;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the player character is touching the ground on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the player character is on the ground, otherwise false. </returns>
    private bool CheckIsGrounded()
    {
        if (_groundedCastPauseTimer > 0) return false;
        
        Vector3 SphereCastOrigin = GetPlayerCharacterBottom() + new Vector3(0, _groundedCastRadius, 0);

        if (Physics.SphereCast(SphereCastOrigin,
                               _groundedCastRadius,
                               Vector2.down,
                               hitInfo: out RaycastHit hitInfo,
                               _groundedCastLength + _groundedCastRadius, // Compensate for the SphereCast starting higher.
                               _groundedLayerMasks,
                               QueryTriggerInteraction.Ignore)
            && Vector3.Angle(Vector3.up, hitInfo.normal) <= 30)
        {
            _groundPoint = hitInfo;
            return true;
        }

        return false;
    }

    //private void OnDrawGizmos()
    //{
    //    if (_groundedCastPauseTimer > 0) return;

    //    Gizmos.color = Color.red;
    //    Vector3 SphereCastOrigin = GetPlayerCharacterBottom() - new Vector3(0, _groundedCastLength, 0);
    //    Gizmos.DrawSphere(SphereCastOrigin, _groundedCastRadius);
    //}

    /// <summary>
    ///   <para>
    ///     Applies calculated custom gravity to the player every frame.
    ///   </para>
    /// </summary>
    private void GravityConditions()
    {
        // Do not apply gravity when on the ground or flying
        if (IsGrounded || _isFlying) return;
        
        float aggregateGravityModifier = _gravityMultiplier * _currDriftDescentDivisor;
        _verticalVelocityVector.y += Time.deltaTime * aggregateGravityModifier * Physics.gravity.y;

        // Limit descent speed to the strength of gravity.
        if (_verticalVelocityVector.y < aggregateGravityModifier * Physics.gravity.y)
        {
            _verticalVelocityVector.y = aggregateGravityModifier * Physics.gravity.y;
        }

        _characterController.Move(Time.deltaTime * _verticalVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Moves the player in the input direction an amount of distance calculated on any frame
    ///     this method is called.
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
        _moveDirectionUnitVector = (_kbControlsLockTimer > 0) ? Vector3.zero : GetMoveInputDirection();

        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();

        // Accelerate if movement is being input and sprint has not been canceled.
        if (_moveDirectionUnitVector != Vector3.zero && lateralVelocityVector.magnitude <= aggregateMaxSpeedValue)
        {
            Accelerate(aggregateMaxSpeedValue);
        }
        // Otherwise, decelerate.
        else
        {
            Decelerate();
        }

        // Apply knockback until it has dissipated.
        if (_externalKnockbackVelocity.sqrMagnitude > 1e-6f)
        {
            _characterController.Move(Time.deltaTime * _externalKnockbackVelocity);
            _externalKnockbackVelocity = Vector3.Lerp(_externalKnockbackVelocity, Vector3.zero, Time.deltaTime / _kbDamping);
        }

        // Turn the player character toward the input direction.
        if (_kbControlsLockTimer <= 0 && !strafe && lateralVelocityVector.sqrMagnitude > 0.0001f)
        {
            Quaternion qa = transform.rotation;
            Quaternion qb = Quaternion.LookRotation(lateralVelocityVector, Vector3.up);
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, _characterRotationDamping));
            transform.rotation = Quaternion.Slerp(qa, qb, t);
        }
    }

    /// <summary>
    ///   <para>
    ///      Gets the current lateral input direction on any frame this method is called.
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
    ///   <para>
    ///     Accelerates the player character on any frame this method is called up to the max movement speed.
    ///   </para>
    /// </summary>
    private void Accelerate(float aggregateMaxSpeedValue)
    {
        Vector3 aggregateAccelIncrement = Time.deltaTime * _acceleration * _moveDirectionUnitVector;

        lateralVelocityVector = lateralVelocityVector.magnitude * _moveDirectionUnitVector;
        lateralVelocityVector += aggregateAccelIncrement;

        // Limit lateral move speed to aggregateMaxSpeedValue.
        if (lateralVelocityVector.magnitude > aggregateMaxSpeedValue)
        {
            lateralVelocityVector = aggregateMaxSpeedValue * lateralVelocityVector.normalized;
        }

        _characterController.Move(Time.deltaTime * lateralVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Decelerates the player character on any frame this method is called until fully stopped.
    ///   </para>
    /// </summary>
    private void Decelerate()
    {
        // Skip deceleration calculations if not moving.
        if (lateralVelocityVector.magnitude <= 0) return;

        Vector3 decelDecrement = Time.deltaTime * _deceleration * lateralVelocityVector.normalized;

        // If deceleration decrement for the current frame exceeds zero,
        // then set current speed to exactly zero.
        if (lateralVelocityVector.magnitude - decelDecrement.magnitude < 0)
        {
            lateralVelocityVector = Vector3.zero;
            decelDecrement = Vector3.zero;
        }

        lateralVelocityVector -= decelDecrement;
        _characterController.Move(Time.deltaTime * lateralVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Gets the combined max speed value considering all current speed parameters on any frame this
    ///     method is called.
    ///   </para>
    /// </summary>
    /// <returns> The aggregate max speed value. </returns>
    private float CalculateAggregateMaxSpeedValue()
    {
        // Apply speed multiplier when flying
        if (_isFlying && !IsGrounded)
        {
            return _maxSpeed * _flightSpeedMultiplier;
        }
        return _maxSpeed;
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the acceleration and deceleration rates for the aggregate max speed value on any
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateAccelDecel()
    {
        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();
        _acceleration = aggregateMaxSpeedValue / _accelerationSeconds;
        _deceleration = aggregateMaxSpeedValue / _decelerationSeconds;
    }


    /// <summary>
    ///   <para>
    ///     Makes the player character hover if the necessary conditions are satisfied on any frame this
    ///     method is called.
    ///   </para>
    /// </summary>
    private void HoverConditions()
    {
        // Skip hover calculations if not on the ground.
        if (!IsGrounded) return;

        _currHoverHeight = _groundPoint.point.y + _hoverHeight;
        float playerCharacterBottomHeight = GetPlayerCharacterBottom().y;
        float hoverHeightDisplacement = _currHoverHeight - playerCharacterBottomHeight;

        // Skip pull and damping calculations and set position of the bottom of the player character exactly to current
        // hover height if it is very close to it.
        if (Mathf.Abs(hoverHeightDisplacement) < 1e-2f && Mathf.Abs(_verticalVelocityVector.y) < 0.1f)
        {
            transform.position = new Vector3(_playerCenter.position.x, _currHoverHeight + _playerHalfHeight, _playerCenter.position.z);
            hoverHeightDisplacement = 0;
            _verticalVelocityVector.y = 0;
            return;
        }

        float hoverPullForce = _hoverPullStrength * hoverHeightDisplacement; // Pull player character into the direction of current hover height.
        float hoverDampingForce = _hoverDampingStrength * -_verticalVelocityVector.y; // Apply force in the opposite direction to dampen.
        float hoverTotalPullForce = hoverPullForce + hoverDampingForce; // Combine pull and dampen forces.

        _verticalVelocityVector.y += Time.deltaTime * hoverTotalPullForce;
        _characterController.Move(Time.deltaTime * _verticalVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Gets the location of the bottom of the player character.
    ///   </para>
    /// </summary>
    /// <returns> The location of the bottom of the player character. </returns>
    private Vector3 GetPlayerCharacterBottom()
    {
        return new Vector3(_playerCenter.position.x, _playerCenter.position.y - _playerHalfHeight, _playerCenter.position.z);
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
        if (_kbDashLockTimer > 0) return;

        _dashDirectionUnitVector = GetMoveInputDirection();

        if (_currDashCharges != 0 && !_isDashing)
        {
            // If not moving, default dash direction is forward.
            if (_dashDirectionUnitVector.x == 0 && _dashDirectionUnitVector.z == 0)
            {
                _dashDirectionUnitVector = GetComponentInParent<Transform>().forward;
            }

            _currDashCharges--;

            // Only initialize regeneration coroutine if it hasn't already.
            if (_currDashCharges == _dashCharges - 1)
            {
                StartCoroutine(DashChargesRegeneration());
            }

            float dashDuration = _dashDistance / _dashSpeed;
            StartCoroutine(InitiateDashDuration(dashDuration));
            DashCooldownStarted?.Invoke(dashDuration); // Notify listener to start the dash fade visual effect.
        }
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character dash on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DashConditions()
    {
        _characterController.Move(Time.deltaTime * _dashSpeed * _dashDirectionUnitVector);
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating dash charges over time.
    ///   </para>
    /// </summary>
    /// <param name="seconds"> The cooldown time for dash regeneration. </param>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator DashChargesRegeneration()
    {
        while (_currDashCharges != _dashCharges)
        {
            yield return new WaitForSeconds(_dashCooldown);
            _currDashCharges++;
        }
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
            _jumpBufferTimer = _jumpBufferWindow;
        }
    }

    /// <summary>
    ///   <para>
    ///     Stops flying when jump button is released.
    ///   </para>
    /// </summary>
    /// <param name="context"> The jump input context. </param>
    private void JumpInputActionCanceled(InputAction.CallbackContext context)
    {
        _isFlying = false;
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character jump if the necessary conditions are satisfied
    ///     on any frame this method is called.
    ///   </para>
    /// </summary>
    private void JumpConditions()
    {
        // If on the ground and jump was inputted and jump buffer window is valid, or if walked off an edge and coyote time window is valid, then jump.
        if ( (IsGrounded && IsWithinJumpBufferWindow()) || (_inputtedJumpThisFrame && !IsGrounded && IsWithinCoyoteTimeWindow()) )
        {
            _coyoteTimer = 0;
            _jumpBufferTimer = 0;
            _groundedCastPauseTimer = _groundedCastJumpPauseDuration;
            _verticalVelocityVector.y = _JumpForce;

            _characterController.Move(Time.deltaTime * _verticalVelocityVector);
        }
        // Otherwise, reset coyote time and jump buffer time to original states
        // because player charater is on the ground.
        else if (IsGrounded)
        {
            _coyoteTimer = _coyoteTimeWindow;
            _jumpBufferTimer = 0;
        }

        _inputtedJumpThisFrame = false;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the window of time in which coyote time is valid has closed on any
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the window of time has not closed, otherwise false. </returns>
    private bool IsWithinCoyoteTimeWindow()
    {
        return _coyoteTimer > 0;
    }

    /// <summary>
    ///   <para>
    ///     Checks if the window of time in which jump buffer is valid has closed on any
    ///     frame thise method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the window of time has not closed, otherwise false. </returns>
    private bool IsWithinJumpBufferWindow()
    {
        return _jumpBufferTimer > 0;
    }

    /// <summary>
    ///   <para>
    ///     Initiates drifting for the player character if the necessary conditions are satisfied
    ///     on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DriftConditions()
    {
        // Cease drifting if the player character landed.
        if (_isDrifting && IsGrounded)
        {
            DisableDrift();
        }

        // Only modify gravity for drifting while falling and while the coyote time and jump buffer windows are invalid.
        if (AreDriftRequirementsValid() && _verticalVelocityVector.y <= 0)
        {
            float timeRatio = _driftDelayTimer / _driftDelay;

            if (timeRatio > 0)
            {
                _currDriftDescentDivisor = _driftDescentDivisor + (timeRatio * (1 - _driftDescentDivisor));
            }
            else
            {
                _currDriftDescentDivisor = _driftDescentDivisor;
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Checks if the necessary conditions for drifting have all been met on any frame this
    ///     method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if the conditions are all met, otherwise false. </returns>
    private bool AreDriftRequirementsValid()
    {
        return _isDrifting && !IsWithinCoyoteTimeWindow() && !IsWithinJumpBufferWindow();
    }

    /// <summary>
    ///   <para>
    ///     Sets drifting status to true and sets the drift delay timer to 0 on any frame this
    ///     method is called.
    ///   </para>
    /// </summary>
    private void EnableDrift()
    {
        _isDrifting = true;
        _driftDelayTimer = _driftDelay;
    }

    /// <summary>
    ///   <para>
    ///     Sets drifting status to false and sets the drift descent reduction multiplier to 1 on any
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    private void DisableDrift()
    {
        _isDrifting = false;
        _currDriftDescentDivisor = 1;
    }






    /// <summary>
    ///   <para>
    ///     Makes player fly when holding jump in midair.
    ///   </para>
    /// </summary>
    private void FlightConditions()
    {
        // Debug: Check flight conditions
        if (!IsGrounded && jumpActions.IsPressed())
        {
            Debug.Log($"Flight Check - Energy: {_currFlightEnergy}, IsGrounded: {IsGrounded}, IsDashing: {_isDashing}");
        }
        
        // Hold jump in air to fly
        if (!IsGrounded && !_isDashing && jumpActions.IsPressed() && _currFlightEnergy > 0)
        {
            _isFlying = true;
            
            // Push player upward
            float flightSpeedIncrement = Time.deltaTime * _flightAcceleration;
            _currFlightEnergy -= Time.deltaTime * _flightDepletionRate;
            
            // Stop when out of energy
            if (_currFlightEnergy <= 0)
            {
                _currFlightEnergy = 0;
                _isFlying = false;
            }
            else
            {
                _verticalVelocityVector.y += flightSpeedIncrement;
                
                // Cap at max speed
                if (_verticalVelocityVector.y > _maxFlightSpeed)
                {
                    _verticalVelocityVector.y = _maxFlightSpeed;
                }
                
                _characterController.Move(Time.deltaTime * _verticalVelocityVector);
            }
        }
        else
        {
            _isFlying = false;
        }
        
        // Refill energy on ground
        if (IsGrounded && _currFlightEnergy < _maxFlightEnergy && !_isRegeneratingFlight)
        {
            StartCoroutine(FlightEnergyRegeneration());
        }
    }

    /// <summary>
    ///   <para>
    ///     Refills flight energy over time when on ground.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator FlightEnergyRegeneration()
    {
        _isRegeneratingFlight = true;

        while (_currFlightEnergy < _maxFlightEnergy)
        {
            // Stop refilling if player starts flying
            if (_isFlying)
            {
                break;
            }

            _currFlightEnergy += Time.deltaTime * _flightRegenerationRate;

            // Cap at max
            if (_currFlightEnergy >= _maxFlightEnergy)
            {
                _currFlightEnergy = _maxFlightEnergy;
                break;
            }

            yield return null;
        }

        _isRegeneratingFlight = false;
    }

    // SOME GETTERS FOR MY USE
    public float GetGroundPoint()
    {
        return _groundPoint.point.y;
    }

    public float GetHoverHeight()
    {
        return _hoverHeight;
    }

    public float GetHalfHeight()
    {
        return _playerHalfHeight;
    }

    public void SetVerticalVelocityFactor(float factor)
    {
        _verticalVelocityVector.y = factor;
    }

    public float GetCurrentFlightEnergy()
    {
        return _currFlightEnergy;
    }

    public float GetMaxFlightEnergy()
    {
        return _maxFlightEnergy;
    }

    public bool IsFlying()
    {
        return _isFlying;
    }

    // Debug helper method
    public void DebugFlightStatus()
    {
        Debug.Log("=== Flight System Debug ===");
        Debug.Log($"IsGrounded: {IsGrounded}");
        Debug.Log($"IsDashing: {_isDashing}");
        Debug.Log($"Jump Pressed: {jumpActions.IsPressed()}");
        Debug.Log($"Current Flight Energy: {_currFlightEnergy} / {_maxFlightEnergy}");
        Debug.Log($"IsFlying: {_isFlying}");
        Debug.Log($"Max Flight Speed: {_maxFlightSpeed}");
        Debug.Log($"Flight Acceleration: {_flightAcceleration}");
        Debug.Log($"Vertical Velocity: {_verticalVelocityVector.y}");
    }

    public void SnapToHoverAfterSlam()
    {
        float targetY = _groundPoint.point.y + _hoverHeight + _playerHalfHeight;

        _characterController.Move(Vector3.up * 0.02f); // tiny upward

        // move vertically to exact hover height
        float dy = targetY - transform.position.y;
        if (Mathf.Abs(dy) > 1e-5f)
            _characterController.Move(new Vector3(0f, dy, 0f));

        // kill vertical velocity 
        _verticalVelocityVector.y = 0f;


        _coyoteTimer = _coyoteTimeWindow;
        _jumpBufferTimer = 0f;
        _groundedCastPauseTimer = 0f;
    }
    
    public void SetPlayerIsGrounded(bool set)
    {
        IsGrounded = set;
    }
}
