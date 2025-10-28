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
//          -Added slam attack movement assistance for hovering.
//   </para>
// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementV4 : MonoBehaviour
{
    public bool IsGrounded { get; private set; }

    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction dashActions;
    private InputAction jumpActions;
    private InputAction flightActions;
    private InputAction sprintActions;

    [Header("Player References")] [Space]
    [SerializeField]
    [Tooltip("An empty object positioned at the exact center of the player character object.")] private Transform _playerCenter;
    [SerializeField]
    [Tooltip("The player camera object.")] private Transform _cameraTransform;
    private Entity _playerEntity;
    private CharacterController _characterController;
    private float _playerHalfHeight;
    private float _playerRadius;

    [Header("Gravity Parameters")]
    [SerializeField]
    [Tooltip("How much gravity is multiplied in units per second (base gravity value is -9.81).")] public float _gravityMultiplier;
    [SerializeField]
    [Tooltip("How quickly the player character decelerates to the aggregate gravity descent speed if it is exceeded in units per second.")] private float _gravityAirDrag;

    private float MoveMaxSpeed { get; set; }
    [Header("Movement Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Speed.")] private float _moveAccelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Speed.")] private float _moveDecelerationSeconds;
    [SerializeField]
    [Tooltip("How quickly the player character aligns with the camera direction in units per second.")] private float _characterRotationDamping;
    private float _moveAcceleration;
    private float _moveDeceleration;
    private Vector3 _lateralVelocityVector;

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
    [Tooltip("The maximum degree angle of valid ground surfaces.")] private float _maxGroundAngle;
    [SerializeField]
    [Tooltip("Seconds that sphere casting is paused after a jump is registered.")] private float _groundedCastJumpPauseDuration;
    private float _currHoverHeight;
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

    // Dash Parameters
    private float DashDistance { get; set; }
    private float DashSpeed { get; set; }
    private float DashCooldown { get; set; }
    private int DashMaxCharges { get; set; }
    private int _currDashCharges;
    private bool _isDashing;
    private bool _isRegeneratingDash;
    public event System.Action<float> DashCooldownStarted;
    private Vector3 _dashDirectionUnitVector;

    [Header("Jump Parameters")] [Space]
    [SerializeField]
    [Tooltip("Vertical jump strength in units per second.")] private float _jumpForce;
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
    [SerializeField]
    [Range(0, 1)]
    [Tooltip("How much gravity is divided when drifting.")] private float _driftDescentDivisor;
    [SerializeField]
    [Tooltip("Seconds before Drift Descent Divisor gradually reaches full effect.")] private float _driftDelay;
    private bool _isDrifting;
    private float _currDriftDescentDivisor;
    private float _driftDelayTimer;

    [Header("Flight Parameters")] [Space]
    [SerializeField]
    [Tooltip("Max vertical flight speed in units per second.")] private float _flightMaxSpeed;
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Flight Speed.")] private float _flightAccelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Flight Speed.")] private float _flightDecelerationSeconds;
    [SerializeField]
    [Range(1, 3)]
    [Tooltip("The multiplier strength of flight counter-acceleration in units per second.")] private float _flightCounterAccelerationMultiplier;
    [SerializeField]
    [Tooltip("Vertical strength of the jump right before flight in units per second.")] private float _flightJumpForce;
    [SerializeField]
    [Tooltip("Amount of flight energy regeneration per second.")] private float _flightRegenerationRate;
    [SerializeField]
    [Tooltip("Capacity value of flight energy")] private int _flightMaxEnergy;
    [SerializeField]
    [Tooltip("Amount of flight energy depleted per second.")] private float _flightDepletionRate;
    private bool _isFlying;
    private bool _isRegeneratingFlight;
    private float _currFlightEnergy;
    private float _flightAcceleration;
    private float _flightDeceleration;

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
        dashActions = playerActions.Dash;
        jumpActions = playerActions.Jump;
        flightActions = playerActions.Flight;
        sprintActions = playerActions.Sprint;

        moveActions.Enable();
        dashActions.Enable();
        jumpActions.Enable();
        flightActions.Enable();
        sprintActions.Enable();

        dashActions.started += DashInputActionStarted;
        jumpActions.started += JumpInputActionStarted;
        flightActions.started += FlightInputActionStarted;
    }

    private void OnDisable()
    {
        moveActions.Disable();
        dashActions.Disable();
        jumpActions.Disable();
        flightActions .Disable();
        sprintActions.Disable();

        dashActions.started -= DashInputActionStarted;
        jumpActions.started -= JumpInputActionStarted;
        flightActions.started -= FlightInputActionStarted;
    }

    void Start()
    {
        if (_playerEntity != null)
        {
            // Player References
            _playerHalfHeight = GetComponent<CharacterController>().height / 2;
            _playerRadius = GetComponent<CharacterController>().radius;

            // Gravity Parameters
            _gravityAirDrag += Mathf.Abs(Physics.gravity.y) * _gravityMultiplier;

            // Movement Parameters
            MoveMaxSpeed = _playerEntity.Stats.MoveSpeed;
            _moveAcceleration = MoveMaxSpeed / _moveAccelerationSeconds;
            _moveDeceleration = MoveMaxSpeed / _moveDecelerationSeconds;

            // Hover Parameters
            _groundedCastRadius = _playerRadius - 0.1f;
            _groundedCastPauseTimer = 0;
            _groundedLayerMasks = LayerMask.GetMask("Environment");
            _groundedLayerMasks |= LayerMask.GetMask("Interactable");
            _groundedLayerMasks |= LayerMask.GetMask("Obstacles");
            _groundedLayerMasks |= LayerMask.GetMask("Enemy");
            GetIsGrounded();

            // KnockBack Parameters
            _kbControlsLockTimer = 0;
            _kbDashLockTimer = 0;

            // Dash Parameters
            if (_playerEntity != null)
            {
                DashDistance = _playerEntity.Stats.DashDistance;
                DashSpeed = _playerEntity.Stats.DashSpeed;
                DashCooldown = _playerEntity.Stats.DashCooldown;
                DashMaxCharges = _playerEntity.Stats.DashCharges;
            }
            _currDashCharges = DashMaxCharges;
            _isDashing = false;
            _isRegeneratingDash = false;

            // Jump Parameters
            _inputtedJumpThisFrame = false;

            // Coyote Time Parameters
            _coyoteTimer = _coyoteTimeWindow;
            _jumpBufferTimer = 0;

            // Drift Parameters
            _currDriftDescentDivisor = 1;
            _driftDelayTimer = 0;
            _isDrifting = false;

            // Flight Parameters
            _isFlying = false;
            _isRegeneratingFlight = false;
            _flightAcceleration = _flightMaxSpeed / _flightAccelerationSeconds;
            _flightDeceleration = _flightMaxSpeed / _flightDecelerationSeconds;
            _currFlightEnergy = _flightMaxEnergy;
        }
    }

    void Update()
    {
        TryGetStatChanges();

        GetIsGrounded();
        GravityConditions();
        DecrementAllTimers();

        if (_kbControlsLockTimer > 0) return;

        if (_isDashing)
        {
            GetIsGrounded();
            DashConditions();
        }
        else
        {
            MoveConditions();
            GetIsGrounded();
            HoverConditions();
            JumpConditions();
            GetIsGrounded();
            DriftConditions();
            FlightConditions();
        }
    }


    public void SetPlayerIsGrounded(bool set)
    {
        IsGrounded = set;
    }

    /// <summary>
    ///   <para>
    ///     Sets the strafe mode status for the player character.
    ///   </para>
    /// </summary>
    /// <param name="on"> Strafe mode status. </param>
    public void SetStrafeMode(bool on) => strafe = on;


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
    ///     Gets all stat changes from the stats file on any frame this method is called.
    ///   </para>
    /// </summary>
    private void TryGetStatChanges()
    {
        if (_playerEntity != null)
        {
            // Check MoveSpeed.
            if (MoveMaxSpeed != _playerEntity.Stats.MoveSpeed)
            {
                MoveMaxSpeed = _playerEntity.Stats.MoveSpeed;
                RecalculateMoveAccelDecel();
            }

            // Check DashDistance.
            if (DashDistance != _playerEntity.Stats.DashDistance)
            {
                DashDistance = _playerEntity.Stats.DashDistance;
            }

            // Check DashSpeed.
            if (DashSpeed != _playerEntity.Stats.DashSpeed)
            {
                DashSpeed = _playerEntity.Stats.DashSpeed;
            }

            // Check DashCooldown.
            if (DashCooldown != _playerEntity.Stats.DashCooldown)
            {
                DashCooldown = _playerEntity.Stats.DashCooldown;
            }

            // Check DashCharges.
            if (DashMaxCharges != _playerEntity.Stats.DashCharges)
            {
                int changeDifference = _playerEntity.Stats.DashCharges - DashMaxCharges;
                DashMaxCharges = _playerEntity.Stats.DashCharges;

                // Add positive difference to current charge count, even while regenerating.
                if (changeDifference > 0)
                {
                    _currDashCharges += changeDifference;
                }
                // Ensure negative difference is not affected by regeneration.
                else
                {
                    if (_currDashCharges >= _playerEntity.Stats.DashCharges)
                    {
                        _currDashCharges = _playerEntity.Stats.DashCharges;
                        _isRegeneratingDash = false;
                    }
                }
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Updates all grounded information on the frame this method is called.
    ///   </para>
    /// </summary>
    private void GetIsGrounded()
    {
        IsGrounded = PlayerGroundCheck.GetIsGrounded(GetPlayerCharacterBottom(),
                                                     _groundedCastLength,
                                                     _groundedCastRadius,
                                                     _maxGroundAngle,
                                                     _groundedLayerMasks,
                                                     out RaycastHit hitInfo,
                                                     _groundedCastPauseTimer);
        _groundPoint = hitInfo;
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
        // Do not apply gravity when on the ground or flying.
        if (IsGrounded || _isFlying) return;

        float aggregateGravityValue = Physics.gravity.y * _gravityMultiplier * _currDriftDescentDivisor;
        _verticalVelocityVector.y += Time.deltaTime * aggregateGravityValue;

        // Limit descent speed to the strength of gravity.
        if (_verticalVelocityVector.y < aggregateGravityValue)
        {
            ApplyAirDrag(aggregateGravityValue);
        }

        _characterController.Move(Time.deltaTime * _verticalVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Slows the player character to terminal velocity (aggregateGravityValue) if its
    ///     falling speed has exceeded it.
    ///   </para>
    /// </summary>
    /// <param name="aggregateGravityValue"> The aggregrate gravity value. </param>
    private void ApplyAirDrag(float aggregateGravityValue)
    {
        float dragIncrement = Time.deltaTime * _gravityAirDrag;

        _verticalVelocityVector.y += dragIncrement;

        // If drag increment for the current frame exceeds aggregateGravityValue,
        // then set current speed to exactly aggregateGravityValue.
        if (_verticalVelocityVector.y > aggregateGravityValue)
        {
            _verticalVelocityVector.y = aggregateGravityValue;
        }
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
        if (AreDriftRequirementsValid() && _driftDelayTimer > 0) _driftDelayTimer -= Time.deltaTime;
    }

    /// <summary>
    ///   <para>
    ///     Moves the player in the input direction an amount of distance calculated on any frame
    ///     this method is called.
    ///   </para>
    /// </summary>
    private void MoveConditions()
    {
        // Because move speed right before moment of knockback must be preserved for correct calculations,
        // simply stop recording new movement values instead of completely skipping the MoveCase method.
        Vector3 moveDirectionUnitVector = (_kbControlsLockTimer > 0) ? Vector3.zero : GetMoveInputDirection();

        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();

        // Accelerate if movement is being inputted and sprint has not been canceled.
        if (moveDirectionUnitVector != Vector3.zero && _lateralVelocityVector.magnitude <= aggregateMaxSpeedValue)
        {
            MoveAccelerate(moveDirectionUnitVector, aggregateMaxSpeedValue);
        }
        // Otherwise, decelerate.
        else
        {
            MoveDecelerate();
        }

        // Apply knockback until it has dissipated.
        if (_externalKnockbackVelocity.sqrMagnitude > 1e-6f)
        {
            _characterController.Move(Time.deltaTime * _externalKnockbackVelocity);
            _externalKnockbackVelocity = Vector3.Lerp(_externalKnockbackVelocity, Vector3.zero, Time.deltaTime / _kbDamping);
        }

        // Turn the player character toward the input direction.
        //if (_kbControlsLockTimer <= 0 && !strafe && _lateralVelocityVector.sqrMagnitude > 0.0001f)
        //{
        //    Quaternion qa = transform.rotation;
        //    Quaternion qb = Quaternion.LookRotation(_lateralVelocityVector, Vector3.up);
        //    float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, _characterRotationDamping));
        //    transform.rotation = Quaternion.Slerp(qa, qb, t);
        //}
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
    ///     Moves the player character using a current lateral or vertical speed vector
    ///     and an incremental speed vector on any frame this method is called.
    ///   </para>
    /// </summary>
    /// <param name="currSpeedVector"> The current speed vector. </param>
    /// <param name="incrementSpeedVector"> The incremental speed vector </param>
    private void MoveIncrementCharacter(ref Vector3 currSpeedVector, Vector3 incrementSpeedVector)
    {
        currSpeedVector += incrementSpeedVector;
        _characterController.Move(Time.deltaTime * currSpeedVector);
    }

    /// <summary>
    ///   <para>
    ///     Accelerates the player character laterally on any frame this method is called up to the max movement speed.
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> The world direction of the most recent move input. </param>
    /// <param name="aggregateMaxSpeedValue"> The aggregate speed value. </param>
    private void MoveAccelerate(Vector3 moveDirectionUnitVector, float aggregateMaxSpeedValue)
    {
        Vector3 aggregateAccelIncrement = Time.deltaTime * _moveAcceleration * moveDirectionUnitVector;

        _lateralVelocityVector = _lateralVelocityVector.magnitude * moveDirectionUnitVector;
        _lateralVelocityVector += aggregateAccelIncrement;

        // Limit lateral move speed to aggregateMaxSpeedValue.
        if (_lateralVelocityVector.magnitude > aggregateMaxSpeedValue)
        {
            _lateralVelocityVector = aggregateMaxSpeedValue * _lateralVelocityVector.normalized;
        }

        _characterController.Move(Time.deltaTime * _lateralVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Decelerates the player character laterally on any frame this method is called until fully stopped.
    ///   </para>
    /// </summary>
    private void MoveDecelerate()
    {
        // Skip deceleration calculations if not moving.
        if (_lateralVelocityVector.magnitude <= 0) return;

        Vector3 decelDecrement = Time.deltaTime * _moveDeceleration * _lateralVelocityVector.normalized;

        // If deceleration decrement for the current frame exceeds zero,
        // then set current speed to exactly zero.
        if (_lateralVelocityVector.magnitude - decelDecrement.magnitude < 0)
        {
            _lateralVelocityVector = Vector3.zero;
            return;
        }

        MoveIncrementCharacter(ref _lateralVelocityVector, -decelDecrement);
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
        return MoveMaxSpeed;
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the acceleration and deceleration rates for the aggregate max speed value on any
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateMoveAccelDecel()
    {
        float aggregateMaxSpeedValue = CalculateAggregateMaxSpeedValue();
        _moveAcceleration = aggregateMaxSpeedValue / _moveAccelerationSeconds;
        _moveDeceleration = aggregateMaxSpeedValue / _moveDecelerationSeconds;
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

        if (_currDashCharges != 0 && !_isDashing)
        {
            _dashDirectionUnitVector = GetMoveInputDirection();

            // If not moving, default dash direction is forward.
            if (_dashDirectionUnitVector.x == 0 && _dashDirectionUnitVector.z == 0)
            {
                _dashDirectionUnitVector = GetComponentInParent<Transform>().forward;
            }

            _currDashCharges--;

            // Only initialize regeneration routine if not already regenerating.
            if (_currDashCharges < DashMaxCharges)
            {
                StartCoroutine(DashChargesRegeneration());
            }

            float dashDuration = DashDistance / DashSpeed;
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
        if (_isFlying)
        {
            FlightConditions();
        }

        _characterController.Move(Time.deltaTime * DashSpeed * _dashDirectionUnitVector);
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating dash charges over time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator DashChargesRegeneration()
    {
        _isRegeneratingDash = true;

        float timer = 0;

        while (_currDashCharges < DashMaxCharges && _isRegeneratingDash)
        {
            timer += Time.deltaTime;

            if (timer >= DashCooldown)
            {
                timer = 0;
                _currDashCharges++;
            }

            if (_currDashCharges >= DashMaxCharges) break;

            yield return null;
        }

        _currDashCharges = Mathf.Min(_currDashCharges, DashMaxCharges); // In case DashMaxCharges is decreased during routine execution.

        _isRegeneratingDash = false;
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

        if (!_isFlying && !IsGrounded && !_isDashing)
        {
            EnableDrift();
        }
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
        if ((IsGrounded && IsWithinJumpBufferWindow()) || (_inputtedJumpThisFrame && !IsGrounded && IsWithinCoyoteTimeWindow()))
        {
            Jump(_jumpForce);
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
    ///     Makes the player character perform a jump on any frame this method is called.
    ///   </para>
    /// </summary>
    /// <param name="jumpForce"> The vertical jump force exerted. </param>
    private void Jump(float jumpForce)
    {
        _coyoteTimer = 0;
        _jumpBufferTimer = 0;
        _groundedCastPauseTimer = _groundedCastJumpPauseDuration;
        _verticalVelocityVector.y = jumpForce;

        _characterController.Move(Time.deltaTime * _verticalVelocityVector);
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
        if ((_isDrifting && (!jumpActions.IsPressed() || IsGrounded)) || _isFlying)
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
    ///     Executes a sequence of flight conditions on any frame that flight is inputted.
    ///   </para>
    /// </summary>
    /// <param name="context"> The flight input context. </param>
    private void FlightInputActionStarted(InputAction.CallbackContext context)
    {
        if (_currFlightEnergy > 0)
        {
            // Toggle flight.
            if (_isFlying)
            {
                DisableFlight();
            }
            else
            {
                EnableFlight();

                // Make the player character jump if on the
                // ground to prevent flight from exiting early.
                if (IsGrounded)
                {
                    Jump(_flightJumpForce);
                }
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Enters the player character into flight if the necessary conditions
    ///     are satisfied on any frame this method is called.
    ///   </para>
    /// </summary>
    private void FlightConditions()
    {
        // Cease drifting if jump input is no longer held or flight energy is depleted.
        //if ((_isflying && !jumpActions.IsPressed()) || _currFlightEnergy <= 0)
        //{
        //    _isFlying = false;
        //}

        // Only initialize regeneration routine if flew, touching the ground and not already regenerating.
        if (_currFlightEnergy < _flightMaxEnergy && !_isRegeneratingFlight && IsGrounded)
        {
            StartCoroutine(FlightRegeneration());
        }

        // Skip calculations if not flying.
        if (!_isFlying) return;

        float flightDepletionDecrement = Time.deltaTime * _flightDepletionRate;

        _currFlightEnergy -= flightDepletionDecrement;

        // If flight energy decrement for the current frame exceeds zero, then set current
        // flight energy to exactly zero, stop flying and immediately begin drifting.
        if (_currFlightEnergy <= 0)
        {
            _currFlightEnergy = 0;
            DisableFlight();
            EnableDrift();
            return;
        }
        // Just disable flight if touching the ground.
        if (IsGrounded)
        {
            DisableFlight();
            return;
        }

        float flightInputValue = 0;
        if (jumpActions.IsPressed()) flightInputValue += 1;
        if (sprintActions.IsPressed()) flightInputValue -= 1;

        // If in midair, flight was inputted and flight energy is not depleted, then fly.
        if (_isFlying && _currFlightEnergy > 0 && !IsGrounded)
        {
            // Accelerate if jump or descend are being inputted.
            if (flightInputValue != 0 && _verticalVelocityVector.magnitude <= _flightMaxSpeed)
            {
                FlightAccelerate(flightInputValue);
            }
            // Otherwise, decelerate.
            else
            {
                FlightDecelerate();
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Accelerates the player character vertically on any frame this method is called up to the max flight speed.
    ///   </para>
    /// </summary>
    /// <param name="flightInputValue"> Whether jump or descend was inputted. </param>
    private void FlightAccelerate(float flightInputValue)
    {
        Vector3 flightAccelIncrement = Time.deltaTime * _flightAcceleration * new Vector3(0, flightInputValue, 0);

        // If flightAccelIncrement is pushing against the direction of _verticalVelocityVector,
        // then increment twice as fast for snappier counter-acceleration.
        if (_verticalVelocityVector.y * flightAccelIncrement.y < 0)
        {
            flightAccelIncrement *= _flightCounterAccelerationMultiplier;
        }

        // Limit vertical move speed to _flightMaxSpeed.
        if (_verticalVelocityVector.magnitude + flightAccelIncrement.magnitude > _flightMaxSpeed)
        {
            flightAccelIncrement.y = Mathf.Sign(flightAccelIncrement.y) * (_flightMaxSpeed - _verticalVelocityVector.magnitude);
        }

        MoveIncrementCharacter(ref _verticalVelocityVector, flightAccelIncrement);
    }

    /// <summary>
    ///   <para>
    ///     Decelerates the player character vertically on any frame this method is called until fully stopped.
    ///   </para>
    /// </summary>
    private void FlightDecelerate()
    {
        // Skip deceleration calculations if not moving.
        if (_verticalVelocityVector.magnitude == 0) return;

        Vector3 decelDecrement = Time.deltaTime * _flightDeceleration * _verticalVelocityVector.normalized;

        // If deceleration decrement for the current frame exceeds zero,
        // then set current speed to exactly zero.
        if (_verticalVelocityVector.magnitude - decelDecrement.magnitude < 0)
        {
            _verticalVelocityVector = Vector3.zero;
            return;
        }

        MoveIncrementCharacter(ref _verticalVelocityVector, -decelDecrement);
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating flight energy to full capacity over time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator FlightRegeneration()
    {
        _isRegeneratingFlight = true;

        while (_currFlightEnergy < _flightMaxEnergy)
        {
            // Cancel flight energy regeneration if flight was inputted.
            if (_isFlying)
            {
                break;
            }

            _currFlightEnergy += Time.deltaTime * _flightRegenerationRate;

            // If the flight regeneration increment for the current frame exceeds
            // _maxFlightEnergy, then set flight energy to exactly _maxFlightEnergy.
            if (_currFlightEnergy >= _flightMaxEnergy)
            {
                _currFlightEnergy = _flightMaxEnergy;
                break;
            }

            yield return null;
        }

        _isRegeneratingFlight = false;
    }

    /// <summary>
    ///   <para>
    ///     Sets flying status to true, sets max movement speed to max flight speed, and sets movement acceleration
    ///     and deceleration to flight acceleration and deceleration on any frame this method is called.
    ///   </para>
    /// </summary>
    private void EnableFlight()
    {
        _isFlying = true;
        MoveMaxSpeed = _flightMaxSpeed;
        _moveAcceleration = _flightAcceleration;
        _moveDeceleration = _flightDeceleration;
    }

    /// <summary>
    ///   <para>
    ///     Sets flying status to false, resets max movement speed to its original value, and resets movement
    ///     acceleration and deceleration back to their original values on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DisableFlight()
    {
        _isFlying = false;
        
        if (_playerEntity != null)
        {
            MoveMaxSpeed = _playerEntity.Stats.MoveSpeed;
        }
        
        RecalculateMoveAccelDecel();
    }
}
