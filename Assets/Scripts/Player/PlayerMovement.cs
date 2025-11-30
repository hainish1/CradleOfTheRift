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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions _playerInput;
    private InputSystem_Actions.PlayerActions _playerActions;

    private InputAction _moveActions;
    private InputAction _dashActions;
    private InputAction _jumpActions;
    private InputAction _flightActions;
    private InputAction _sprintActions;

    // Player Parameters

    [Header("Player Parameters")] [Space]
    [SerializeField]
    [Tooltip("An empty object positioned at the exact center of the player character object.")] private Transform _playerCenter;
    [SerializeField]
    [Tooltip("The player camera object.")] private Transform _cameraTransform;
    private Entity _playerEntity;
    private CharacterController _characterController;
    private float _playerHalfHeight;
    private float _playerRadius;

    // Gravity Parameters

    [Header("Gravity Parameters")] [Space]
    [SerializeField]
    [Tooltip("How much gravity is multiplied in units per second (base gravity value is -9.81).")] public float _gravityMultiplier;
    [SerializeField]
    [Tooltip("How quickly the player character decelerates to the aggregate gravity descent speed if it is exceeded in units per second.")] private float _gravityAirDrag;

    // Movement Parameters

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
    private Vector3 _groundPlaneMoveVectorTemp;
    private Vector2 _moveInputTemp;

    // Hover Parameters

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
    [SerializeField]
    [Tooltip("Layers that will be treated as ground.")] private LayerMask _groundedLayerMasks;
    private float _currHoverHeight;
    public bool IsGrounded { get; private set; }
    private float _groundedCastRadius;
    private float _groundedCastPauseTimer;
    private RaycastHit _groundPoint;

    // Knockback Parameters
    
    private float _kbDamping;
    private float _kbControlsLockTime;
    private float _kbDashLockTime;
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

    // Jump Parameters

    private float _jumpForce;
    [Header("Jump Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered after walking off an edge.")] private float _coyoteTimeWindow;
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered before reaching the ground.")] private float _jumpBufferWindow;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private Vector3 _verticalVelocityVector;

    // Drift Parameters

    private float _driftDescentDivisor;
    [Header("Drift Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds before Drift Descent Divisor gradually reaches full effect.")] private float _driftDelay;
    private bool _isDrifting;
    private float _currDriftDescentDivisor;
    private float _driftDelayTimer;

    // Flight Parameters

    private float _flightMaxSpeed;
    private int _flightMaxEnergy;
    private float _flightRegenerationRate;
    private float _flightDepletionRate;
    [Header("Flight Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Flight Speed.")] private float _flightAccelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Flight Speed.")] private float _flightDecelerationSeconds;
    [SerializeField]
    [Range(1, 3)]
    [Tooltip("The multiplier strength of flight counter-acceleration in units per second.")] private float _flightCounterAccelerationMultiplier;
    [SerializeField]
    [Tooltip("Vertical strength of the jump right before flight in units per second.")] private float _flightJumpForce;
    private bool _isFlying;
    private bool _isRegeneratingFlight;
    public float FlightCooldownRatio { get; private set; }
    private float _currFlightEnergy;
    private float _flightAcceleration;
    private float _flightDeceleration;

    private bool strafe = false; // Set by AimController.

    void Awake()
    {
        _playerEntity = GetComponent<Entity>();
        _characterController = GetComponent<CharacterController>();
        _playerInput = new InputSystem_Actions();
        _playerActions = _playerInput.Player;
    }

    void OnEnable()
    {
        _moveActions = _playerActions.Move;
        _dashActions = _playerActions.Dash;
        _jumpActions = _playerActions.Jump;
        _flightActions = _playerActions.Flight;
        _sprintActions = _playerActions.Sprint;

        _moveActions.Enable();
        _dashActions.Enable();
        _jumpActions.Enable();
        _flightActions.Enable();
        _sprintActions.Enable();

        _dashActions.started += DashInputActionStarted;
        _jumpActions.started += JumpInputActionStarted;
        _flightActions.started += FlightInputActionStarted;
    }

    void OnDisable()
    {
        _moveActions.Disable();
        _dashActions.Disable();
        _jumpActions.Disable();
        _flightActions .Disable();
        _sprintActions.Disable();

        _dashActions.started -= DashInputActionStarted;
        _jumpActions.started -= JumpInputActionStarted;
        _flightActions.started -= FlightInputActionStarted;
    }

    void Start()
    {
        if (_playerEntity == null) return;

        // Player Parameters
        _playerHalfHeight = GetComponent<CharacterController>().height / 2;
        _playerRadius = GetComponent<CharacterController>().radius;

        // Gravity Parameters
        _gravityAirDrag += Mathf.Abs(Physics.gravity.y) * _gravityMultiplier;

        // Movement Parameters
        MoveMaxSpeed = _playerEntity.Stats.MoveSpeed;
        _moveAcceleration = MoveMaxSpeed / _moveAccelerationSeconds;
        _moveDeceleration = MoveMaxSpeed / _moveDecelerationSeconds;
        _moveInputTemp = Vector2.zero;

        // Hover Parameters
        _groundedCastRadius = _playerRadius - 0.1f;
        _groundedCastPauseTimer = 0;
        GetIsGrounded();

        // KnockBack Parameters
        _kbDamping = _playerEntity.Stats.KbDamping;
        _kbControlsLockTime = _playerEntity.Stats.KbControlsLockTime;
        _kbDashLockTime = _playerEntity.Stats.KbDashLockTime;
        _kbControlsLockTimer = 0;
        _kbDashLockTimer = 0;

        // Dash Parameters
        DashDistance = _playerEntity.Stats.DashDistance;
        DashSpeed = _playerEntity.Stats.DashSpeed;
        DashCooldown = _playerEntity.Stats.DashCooldown;
        DashMaxCharges = _playerEntity.Stats.DashCharges;
        _currDashCharges = DashMaxCharges;
        _isDashing = false;
        _isRegeneratingDash = false;

        // Jump Parameters
        _jumpForce = _playerEntity.Stats.JumpForce;

        // Coyote Time Parameters
        _coyoteTimer = _coyoteTimeWindow;
        _jumpBufferTimer = 0;

        // Drift Parameters
        _driftDescentDivisor = _playerEntity.Stats.DriftDescentDivisor;
        _currDriftDescentDivisor = 1;
        _driftDelayTimer = 0;
        _isDrifting = false;

        // Flight Parameters
        _flightMaxSpeed = _playerEntity.Stats.FlightMaxSpeed;
        _flightMaxEnergy = _playerEntity.Stats.FlightMaxEnergy;
        _flightRegenerationRate = _playerEntity.Stats.FlightRegenerationRate;
        _flightDepletionRate = _playerEntity.Stats.FlightDepletionRate;
        _isFlying = false;
        _isRegeneratingFlight = false;
        FlightCooldownRatio = 1;
        _flightAcceleration = _flightMaxSpeed / _flightAccelerationSeconds;
        _flightDeceleration = _flightMaxSpeed / _flightDecelerationSeconds;
        _currFlightEnergy = _flightMaxEnergy;
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
                    if (_currDashCharges >= DashMaxCharges)
                    {
                        _currDashCharges = DashMaxCharges;
                        _isRegeneratingDash = false;
                    }
                }
            }

            // Check flight speed.
            if (_flightMaxSpeed != _playerEntity.Stats.FlightMaxSpeed)
            {
                _flightMaxSpeed = _playerEntity.Stats.FlightMaxSpeed;
            }

            // Check flight energy.
            if (_flightMaxEnergy != _playerEntity.Stats.FlightMaxEnergy)
            {
                _flightMaxEnergy = _playerEntity.Stats.FlightMaxEnergy;
            }

            // Check flight regeneration rate.
            if (_flightRegenerationRate != _playerEntity.Stats.FlightRegenerationRate)
            {
                _flightRegenerationRate = _playerEntity.Stats.FlightRegenerationRate;
            }

            // Check flight depletion rate.
            if (_flightDepletionRate != _playerEntity.Stats.FlightDepletionRate)
            {
                _flightDepletionRate = _playerEntity.Stats.FlightDepletionRate;
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

        //Debug.DrawRay(GetPlayerCharacterBottom(), _groundedCastLength * Vector3.down, Color.red);
    }

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
        float accelIncrement = Time.deltaTime * aggregateGravityValue;
        _verticalVelocityVector.y = Mathf.Clamp(_verticalVelocityVector.y + accelIncrement, aggregateGravityValue, float.MaxValue);

        ApplyAirDrag(aggregateGravityValue); // Slow descent speed to the strength of gravity.

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
        if (_verticalVelocityVector.y < aggregateGravityValue)
        {
            float dragIncrement = Time.deltaTime * _gravityAirDrag;
            _verticalVelocityVector.y = Mathf.Clamp(_verticalVelocityVector.y + dragIncrement, 0, aggregateGravityValue);
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

        ParallelizeMoveDirectionToGround(moveDirectionUnitVector);

        // Accelerate if movement is being inputted and sprint has not been canceled.
        if (moveDirectionUnitVector != Vector3.zero && _lateralVelocityVector.magnitude <= MoveMaxSpeed)
        {
            MoveAccelerate(moveDirectionUnitVector);
        }
        // Otherwise, decelerate.
        else
        {
            MoveDecelerate();
        }

        _characterController.Move(Time.deltaTime * _lateralVelocityVector);

        // Apply knockback until it has dissipated.
        if (_externalKnockbackVelocity.sqrMagnitude > 1e-6f)
        {
            _characterController.Move(Time.deltaTime * _externalKnockbackVelocity);
            _externalKnockbackVelocity = Vector3.Lerp(_externalKnockbackVelocity, Vector3.zero, Time.deltaTime / _kbDamping);
        }

        // Turn the player character toward the input direction.
        // COUPLED : Aim decides where player faces
        // COUPLED WHEN MOVING : Aim + Input decide where player faces, but Aim has higher priority
        // DECOUPLED : Aim does not do shit
        if (_kbControlsLockTimer <= 0 && !strafe && _lateralVelocityVector.sqrMagnitude > 0.0001f)
        {
           Quaternion qa = transform.rotation;
           Quaternion qb = Quaternion.LookRotation(_lateralVelocityVector, Vector3.up);
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
        Vector2 moveInput = _moveActions.ReadValue<Vector2>();

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
    ///     Interpolates the move direction of the player character to be parallel with the
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> World direction the player is moving. </param>
    private void ParallelizeMoveDirectionToGround(Vector3 moveDirectionUnitVector)
    {
        // Move the player character parallel to the angle of the current ground plane.
        if (moveDirectionUnitVector != Vector3.zero)
        {
            // Get unit vector parallel to ground plane.
            Vector3 groundPlaneMoveUnitVector = Vector3.ProjectOnPlane(moveDirectionUnitVector, _groundPoint.normal).normalized;

            //Debug.Log($"{IsGrounded} | {_lateralVelocityVector.magnitude >= MoveMaxSpeed - 5f} | {Vector3.Dot(_lateralVelocityVector, _groundPlaneMoveVectorTemp) >= (_lateralVelocityVector.magnitude * _groundPlaneMoveVectorTemp.magnitude) - 1} | {moveActions.ReadValue<Vector2>() != _moveInputTemp}.");

            // Reset _lateralVelocityVector pitch if new move input is registered to ground clamp in a new direction faster.
            if (IsGrounded && Vector3.Dot(_lateralVelocityVector, _groundPlaneMoveVectorTemp) < (_lateralVelocityVector.magnitude * _groundPlaneMoveVectorTemp.magnitude) - 1 && _moveActions.ReadValue<Vector2>() == _moveInputTemp)
            {
                //Debug.Log("Reached normal case.");
                // Set pitch of moveDirectionUnitVector to that of _lateralVelocityVector.
                moveDirectionUnitVector = MatchPitchAngle(moveDirectionUnitVector, _lateralVelocityVector);

                // Rotate pitch of moveDirectionUnitVector towards that of groundPlaneMoveUnitVector by a deltaTime degree.
                float degreesPerSecond = Time.deltaTime * 360;
                moveDirectionUnitVector = GetRotationTowards(moveDirectionUnitVector, groundPlaneMoveUnitVector, degreesPerSecond);
            }
            else if (IsGrounded
                     && _lateralVelocityVector.magnitude >= MoveMaxSpeed - 5f
                     && Vector3.Dot(_lateralVelocityVector, _groundPlaneMoveVectorTemp) >= (_lateralVelocityVector.magnitude * _groundPlaneMoveVectorTemp.magnitude) - 1
                     && _moveActions.ReadValue<Vector2>() != _moveInputTemp)
            {
                //Debug.Log("Reached fullspeed case.");
                _lateralVelocityVector = _lateralVelocityVector.magnitude * groundPlaneMoveUnitVector;
            }
            else if (!IsGrounded)
            {
                _lateralVelocityVector = _lateralVelocityVector.magnitude * moveDirectionUnitVector;
            }

            _groundPlaneMoveVectorTemp = groundPlaneMoveUnitVector;

            Vector3 bottom = GetPlayerCharacterBottom();
            Debug.DrawRay(bottom, _lateralVelocityVector, Color.green);
            Debug.DrawRay(bottom, groundPlaneMoveUnitVector * 10, Color.red);
        }

        _moveInputTemp = _moveActions.ReadValue<Vector2>();
    }

    /// <summary>
    ///   <para>
    ///     Matches a vector's vertical pitch to that of a target vector while preserving its horizontal yaw. 
    ///   </para>
    /// </summary>
    /// <param name="currVector"> Vector with yaw to be preserved. </param>
    /// <param name="targetVector"> Vector with pitch to be matched. </param>
    /// <returns> A vector with modified yaw and preserved pitch. </returns>
    private Vector3 MatchPitchAngle(Vector3 currVector, Vector3 targetVector)
    {
        const float Epsilon = 1e-9f;

        Vector3 currHorizontal = new Vector3(currVector.x, 0, currVector.z);
        Vector3 targetHorizontal = new Vector3(targetVector.x, 0, targetVector.z);

        // return targetVector if it and currVector are both extremely close to being fully vertical.
        if (currHorizontal.sqrMagnitude < Epsilon && targetHorizontal.sqrMagnitude < Epsilon)
        {
            return targetVector.normalized;
        }

        // Use horizontal of the target vector for yaw if the current vector is too short.
        if (currHorizontal.sqrMagnitude < Epsilon)
        {
            currHorizontal = targetHorizontal;
        }

        float yaw = Mathf.Atan2(currHorizontal.x, currHorizontal.z);
        float targetHorizontalLength = new Vector2(targetVector.x, targetVector.z).magnitude;
        float pitch = Mathf.Atan2(targetVector.y, targetHorizontalLength);
        float cosPitch = Mathf.Cos(pitch);
        
        float resultX = Mathf.Sin(yaw) * cosPitch;
        float resultY = Mathf.Sin(pitch);
        float resultZ = Mathf.Cos(yaw) * cosPitch;
        Vector3 result = new Vector3(Mathf.Sin(yaw) * cosPitch, Mathf.Sin(pitch), Mathf.Cos(yaw) * cosPitch);

        return result.normalized;
    }

    /// <summary>
    ///   <para>
    ///     Rotates a vector's direction towards that of a target vector by a given amount of degrees per second.
    ///   </para>
    /// </summary>
    /// <returns> The vector rotation this frame. </returns>
    private Vector3 GetRotationTowards(Vector3 currVector, Vector3 targetVector, float maxDegreesDelta)
    {
        const float Epsilon = 1e-9f;
        
        // Do nothing if current vector or target vector is too short.
        if (currVector.magnitude < Epsilon || targetVector.magnitude < Epsilon)
        {
            return currVector;
        }

        Vector3 currUnitVector = currVector.normalized;
        Vector3 targetUnitVector = targetVector.normalized;
        float currMagnitude = currVector.magnitude;
        float targetMagnitude = targetVector.magnitude;
        float dot = Mathf.Clamp(Vector3.Dot(currUnitVector, targetUnitVector), -1, 1);
        float angleRadians = Mathf.Acos(dot);
        float maxRadiansDelta = Mathf.Max(0, maxDegreesDelta) * Mathf.Deg2Rad;

        // Return target vector's direction if the maximum rotation this frame meets or exceeds it,
        // or if current vector's alignment is extremely close.
        if (maxRadiansDelta >= angleRadians || angleRadians < Epsilon)
        {
            return currMagnitude * targetUnitVector;
        }

        Vector3 axis = Vector3.Cross(currUnitVector, targetUnitVector);
        float axisMagnitude = axis.magnitude;

        // Normalize the axis vector.
        if (axis.magnitude < Epsilon)
        {
            axis = Vector3.Cross(currUnitVector, Vector3.right);
            if (axis.sqrMagnitude < Epsilon)
            {
                axis = Vector3.Cross(currUnitVector, Vector3.up);
            }

            axis.Normalize();
        }
        else
        {
            axis.Normalize();
        }

        // Rodrigues' Vector Rotation Formula: v * cos(theta) + (k � v) * sin(theta) + k * (k � v) * (1 - cos(theta))
        Vector3 currV = currUnitVector;
        Vector3 axisK = axis;
        float maxTheta = maxRadiansDelta;
        float cos = Mathf.Cos(maxTheta);
        float sin = Mathf.Sin(maxTheta);

        Vector3 term1 = currV * cos;
        Vector3 term2 = Vector3.Cross(axisK, currV) * sin;
        Vector3 term3 = axisK * Vector3.Dot(axisK, currV) * (1 - cos);

        return term1 + term2 + term3;
    }

    /// <summary>
    ///   <para>
    ///     Accelerates the player character laterally on any frame this method is called up to the max movement speed.
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> The world direction of the most recent move input. </param>
    /// <param name="aggregateMaxSpeedValue"> The aggregate speed value. </param>
    private void MoveAccelerate(Vector3 moveDirectionUnitVector)
    {
        if (_lateralVelocityVector.magnitude < MoveMaxSpeed)
        {
            float aggregateAccelIncrement = Time.deltaTime * _moveAcceleration;
            float newVelocityMagnitude = Mathf.Clamp(_lateralVelocityVector.magnitude + aggregateAccelIncrement, 0, MoveMaxSpeed);
            _lateralVelocityVector = newVelocityMagnitude * moveDirectionUnitVector;
        }
        else
        {
            _lateralVelocityVector = MoveMaxSpeed * moveDirectionUnitVector;
        }
    }

    /// <summary>
    ///   <para>
    ///     Decelerates the player character laterally on any frame this method is called until fully stopped.
    ///   </para>
    /// </summary>
    private void MoveDecelerate()
    {
        // Skip deceleration calculations if not moving.
        if (_lateralVelocityVector.magnitude == 0) return;

        float decelDecrement = Time.deltaTime * _moveDeceleration;
        float newVelocityMagnitude = Mathf.Clamp(_lateralVelocityVector.magnitude - decelDecrement, 0, float.MaxValue);
        _lateralVelocityVector = newVelocityMagnitude * _lateralVelocityVector.normalized;
    }

    /// <summary>
    ///   <para>
    ///     Recalculates the acceleration and deceleration rates for the aggregate max speed value on any
    ///     frame this method is called.
    ///   </para>
    /// </summary>
    private void RecalculateMoveAccelDecel()
    {
        _moveAcceleration = MoveMaxSpeed / _moveAccelerationSeconds;
        _moveDeceleration = MoveMaxSpeed / _moveDecelerationSeconds;
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
        float heightDisplacement = _currHoverHeight - playerCharacterBottomHeight;

        // Skip pull and damping calculations and set position of the bottom of the player character exactly to current
        // hover height if it is very close to it.
        if (Mathf.Abs(heightDisplacement) < 1e-2f && Mathf.Abs(_verticalVelocityVector.y) < 0.1f)
        {
            transform.position = new Vector3(_playerCenter.position.x, _currHoverHeight + _playerHalfHeight, _playerCenter.position.z);
            heightDisplacement = 0;
            _verticalVelocityVector.y = 0;
            return;
        }

        float pullForce = _hoverPullStrength * heightDisplacement; // Pull player character into the direction of current hover height.
        float dampingForce = _hoverDampingStrength * -_verticalVelocityVector.y; // Apply force in the opposite direction to dampen.
        float totalPullForce = pullForce + dampingForce; // Combine pull and dampen forces.

        _verticalVelocityVector.y += Time.deltaTime * totalPullForce;
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
            if (_currDashCharges == DashMaxCharges - 1)
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
        if ((IsGrounded && IsWithinJumpBufferWindow()) || (_jumpActions.WasPressedThisFrame() && !IsGrounded && IsWithinCoyoteTimeWindow()))
        {
            PerformJump(_jumpForce);
        }
        // Otherwise, reset coyote time and jump buffer time to original states
        // because player charater is on the ground.
        else if (IsGrounded)
        {
            _coyoteTimer = _coyoteTimeWindow;
            _jumpBufferTimer = 0;
        }
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character perform a jump on any frame this method is called.
    ///   </para>
    /// </summary>
    /// <param name="jumpForce"> The vertical jump force exerted. </param>
    private void PerformJump(float jumpForce)
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
        if ((_isDrifting && (!_jumpActions.IsPressed() || IsGrounded)) || _isFlying)
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
                    PerformJump(_flightJumpForce);
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
        // Only initialize regeneration routine if flew, on the ground and not already regenerating.
        if (_currFlightEnergy < _flightMaxEnergy && IsGrounded && !_isRegeneratingFlight)
        {
            StartCoroutine(FlightRegeneration());
        }

        // Skip calculations if not flying or on cooldown.
        if (!_isFlying || FlightCooldownRatio < 1) return;

        float depletionDecrement = Time.deltaTime * _flightDepletionRate;

        _currFlightEnergy = Mathf.Clamp(_currFlightEnergy - depletionDecrement, 0, _flightMaxEnergy);

        // If flight energy decrement for the current frame reaches zero, then
        // stop flying and immediately begin drifting.
        if (_currFlightEnergy == 0)
        {
            DisableFlight();
            EnableDrift();
            return;
        }
        // Just disable flight if on the ground.
        if (IsGrounded)
        {
            DisableFlight();
            return;
        }

        int flightInputValue = 0;
        if (_jumpActions.IsPressed()) flightInputValue += 1;
        if (_sprintActions.IsPressed()) flightInputValue -= 1;

        // If in midair and flight energy is not depleted, then fly.
        if (!IsGrounded && _currFlightEnergy > 0)
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

            _characterController.Move(Time.deltaTime * _verticalVelocityVector);
        }
    }

    /// <summary>
    ///   <para>
    ///     Accelerates the player character vertically on any frame this method is called up to the max flight speed.
    ///   </para>
    /// </summary>
    /// <param name="flightInputValue"> Whether jump or descend was inputted. </param>
    private void FlightAccelerate(int flightInputValue)
    {
        float accelIncrement = Time.deltaTime * _flightAcceleration;
        
        // If flightAccelIncrement is pushing against the direction of _verticalVelocityVector,
        // then accelerate faster than normal for snappier counter-acceleration.
        if (_verticalVelocityVector.y * accelIncrement < 0)
        {
            accelIncrement *= _flightCounterAccelerationMultiplier;
        }

        float newVelocityMagnitude = Mathf.Clamp(_verticalVelocityVector.magnitude + accelIncrement, 0, _flightMaxSpeed);
        _verticalVelocityVector = newVelocityMagnitude * new Vector3(0, flightInputValue, 0);
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

        float decelDecrement = Time.deltaTime * _flightDeceleration;
        float newVelocityMagnitude = Mathf.Clamp(_verticalVelocityVector.magnitude - decelDecrement, 0, float.MaxValue);
        _verticalVelocityVector = newVelocityMagnitude * _verticalVelocityVector.normalized;
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
        GetFlightCooldownRatio();

        while (_currFlightEnergy < _flightMaxEnergy)
        {
            float regenIncrement = Time.deltaTime * _flightRegenerationRate;
            _currFlightEnergy = Mathf.Clamp(_currFlightEnergy + regenIncrement, 0, _flightMaxEnergy);
            GetFlightCooldownRatio();

            yield return null;
        }

        _isRegeneratingFlight = false;
    }

    /// <summary>
    ///   <para>
    ///     Gets the remaining seconds needed before flight energy fully regenerates to max.
    ///   </para>
    /// </summary>
    private void GetFlightCooldownRatio()
    {
        FlightCooldownRatio = _currFlightEnergy / _flightMaxEnergy;
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
    

    public void SetVerticalVelocityFactor(float factor)
    {
        _verticalVelocityVector.y = factor;
    }


    public float GetCurrentFlightEnergy()
    {
        return _currFlightEnergy;
    }
}
