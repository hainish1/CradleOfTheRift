// <summary>
//   <authors>
//     Samuel Rigby, Hainish Acharya
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4500, University of Utah, August 2025.
//     Contributed to by Hainish Acharya.
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
    // Input Parameters
    
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
    [SerializeField]
    [Tooltip("Reference to the player jump animation to get its duration.")] private AnimationClip _jumpAnim;
    public Entity PlayerEntity { get; private set; }
    private Animator _playerAnim;
    private CharacterController _characterController;
    private PlayerMeleeControllerV2 _meleeController;
    private PlayerShooter _playerShooter;
    private PlayerShockwaveController _shockwaveController;
    private float _playerHalfHeight;
    private float _playerRadius;

    // Gravity Parameters

    [Header("Gravity Parameters")] [Space]
    [SerializeField]
    [Tooltip("How much gravity is multiplied in units per second (base gravity value is -9.81).")] public float _gravityMultiplier;
    [SerializeField]
    [Tooltip("How quickly the player character decelerates to the aggregate gravity descent speed if it is exceeded in units per second.")] private float _gravityAirDrag;

    // Movement Parameters

    [Header("Movement Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Speed.")] private float _moveAccelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Speed.")] private float _moveDecelerationSeconds;
    [SerializeField]
    [Tooltip("The lowest percent in units per second which player velocity can be reduced to when turning.")] private float _moveTurnDampingFloor;
    [SerializeField]
    [Tooltip("How quickly in seconds the player character move animations blend when turning.")] private float _moveBlendSpeed;
    [SerializeField]
    [Tooltip("How quickly the player character aligns with the camera direction in units per second.")] private float _characterRotationDamping;
    private float _moveMaxSpeed;
    private float _moveAcceleration;
    private float _moveDeceleration;
    public bool IsSlowed { get; set; }
    public Vector3 _lateralVelocityVector;
    private Vector2 _moveInputTemp = Vector2.zero;

    // Hover Parameters

    [Header("Hover Parameters")] [Space]
    [SerializeField]
    [Tooltip("Height above the ground the player character hovers in units.")] private float _hoverHeight;
    [SerializeField]
    [Tooltip("How strongly the player character is pulled to Hover Height in units per second.")] private float _hoverPullStrength;
    [SerializeField]
    [Tooltip("How strongly Hover Pull Strength dissipates in units per second.")] private float _hoverDampingStrength;
    [SerializeField]
    [Tooltip("Sphere casting distance below the player for hovering in units.")] private float _groundedCastLength;
    [SerializeField]
    [Tooltip("The maximum degree angle of valid ground surfaces when hovering.")] private float _maxGroundAngle;
    [SerializeField]
    [Tooltip("Seconds that sphere casting is paused after a jump is registered.")] private float _groundedCastJumpPauseDuration;
    [SerializeField]
    [Tooltip("Layers that will be treated as ground.")] private LayerMask _groundedLayerMasks;
    private float _currHoverHeight;
    public bool IsGrounded { get; private set; }
    private float _groundedCastRadius;
    private float _groundedCastPauseTimer;
    private RaycastHit _groundHoveringPoint;
    private ControllerColliderHit _groundCollidingPoint;

    // Knockback Parameters

    private float _kbDamping;
    private float _kbControlsLockTime;
    private float _kbDashLockTime;
    private float _kbControlsLockTimer;
    private float _kbDashLockTimer;
    private Vector3 _externalKnockbackVelocity;

    // Dash Parameters

    private float _dashDistance => PlayerEntity.Stats.DashDistance;
    private float _dashSpeed => PlayerEntity.Stats.DashSpeed;
    public bool IsDashing { get; private set; }
    private float _dashCooldown => PlayerEntity.Stats.DashCooldown;
    private int _dashMaxCharges;
    private int _currDashCharges;
    private bool _isRegeneratingDash;
    public int CurrentDashCharges => _currDashCharges;
    public event Action<float> DashCooldownStarted;
    public static event System.Action<int, int> OnDashChargeSpent;      // (current, max)
    public static event System.Action<int, int> OnDashChargeRestored;   // (current, max)
    private Vector3 _dashDirectionUnitVector;

    // Jump Parameters
    
    [Header("Jump Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered after walking off an edge.")] private float _coyoteTimeWindow;
    [SerializeField]
    [Tooltip("Seconds that jump can still be registered before reaching the ground.")] private float _jumpBufferWindow;
    [SerializeField, Range(0, 1)]
    [Tooltip("How quickly the player jump animation finishes. (0.0 is instant, 1.0 is the full default duration.)")] private float _jumpAnimationSpeedMultiplier;
    private float _jumpForce;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private Vector3 _verticalVelocityVector;
    public bool IsJumping { get; private set; }

    // Drift Parameters

    [Header("Drift Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds before Drift Descent Divisor gradually reaches full effect.")] private float _driftDelay;
    private bool _isDrifting;
    private float _driftDescentDivisor;
    private float _currDriftDescentDivisor = 1;
    private float _driftDelayTimer;

    // Flight Parameters
    
    [Header("Flight Parameters")] [Space]
    [SerializeField]
    [Tooltip("Seconds needed to reach Max Flight Speed.")] private float _flightAccelerationSeconds;
    [SerializeField]
    [Tooltip("Seconds needed to fully stop after moving at Max Flight Speed.")] private float _flightDecelerationSeconds;
    [SerializeField, Range(1, 3)]
    [Tooltip("The multiplier strength of flight counter-acceleration in units per second.")] private float _flightCounterAccelerationMultiplier;
    [SerializeField]
    [Tooltip("Vertical strength of the jump right before flight in units per second.")] private float _flightJumpForce;
    private float _flightMaxSpeed => PlayerEntity.Stats.FlightMaxSpeed;
    private float _flightAcceleration;
    private float _flightDeceleration;
    public bool IsFlying { get; private set; }
    private int _flightMaxEnergy => PlayerEntity.Stats.FlightMaxEnergy;
    private float _currFlightEnergy;
    private float _flightRegenerationRate => PlayerEntity.Stats.FlightRegenerationRate;
    private float _flightDepletionRate => PlayerEntity.Stats.FlightDepletionRate;
    public bool IsRegeneratingFlight { get; private set; }
    private Coroutine _flightRegenerationCoroutine;
    public float FlightEnergyRatio { get { return _currFlightEnergy / _flightMaxEnergy; } }

    private bool strafe = false; // Set by AimController.

    [Header("Audio")]
    [SerializeField]
    private AK.Wwise.Event dashSoundEvent;
    [SerializeField]
    private AK.Wwise.Event jumpSoundEvent;

    [Header("VFX")]
    [SerializeField] private GameObject dashVFXPrefab;

    void Awake()
    {
        // Input Parameters
        _playerInput = new InputSystem_Actions();
        _playerActions = _playerInput.Player;

        // Player Parameters
        PlayerEntity = GetComponent<Entity>();
        _characterController = GetComponent<CharacterController>();
        _meleeController = GetComponentInChildren<PlayerMeleeControllerV2>();
        _playerShooter = GetComponentInChildren<PlayerShooter>();
        _shockwaveController = GetComponent<PlayerShockwaveController>();
        _playerAnim = GetComponentInChildren<Animator>();
        _playerHalfHeight = _characterController.height / 2;
        _playerRadius = _characterController.radius;

        // Gravity Parameters
        _gravityAirDrag += Mathf.Abs(Physics.gravity.y) * _gravityMultiplier;

        // Hover Parameters
        _groundedCastRadius = _playerRadius - 0.1f;

        // Coyote Time Parameters
        _coyoteTimer = _coyoteTimeWindow;
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
        // Movement Parameters
        _moveMaxSpeed = PlayerEntity.Stats.MoveSpeed;
        _moveAcceleration = _moveMaxSpeed / _moveAccelerationSeconds;
        _moveDeceleration = _moveMaxSpeed / _moveDecelerationSeconds;

        // KnockBack Parameters
        _kbDamping = PlayerEntity.Stats.KbDamping;
        _kbControlsLockTime = PlayerEntity.Stats.KbControlsLockTime;
        _kbDashLockTime = PlayerEntity.Stats.KbDashLockTime;

        // Dash Parameters
        _dashMaxCharges = PlayerEntity.Stats.DashCharges;
        _currDashCharges = _dashMaxCharges;

        // Jump Parameters
        _jumpForce = PlayerEntity.Stats.JumpForce;
        _playerAnim.SetFloat("JumpAnimSpeedMultiplier", 1 / _jumpAnimationSpeedMultiplier);

        // Drift Parameters
        _driftDescentDivisor = PlayerEntity.Stats.DriftDescentDivisor;

        // Flight Parameters
        _flightAcceleration = _flightMaxSpeed / _flightAccelerationSeconds;
        _flightDeceleration = _flightMaxSpeed / _flightDecelerationSeconds;
        _currFlightEnergy = _flightMaxEnergy;

        // VFX
        dashVFXPrefab.SetActive(false);
    }

    void Update()
    {
        TryGetComplexStatChanges();

        IsGrounded = GetIsGrounded();
        GravityConditions();
        DecrementAllTimers();

        if (_kbControlsLockTimer > 0) return;

        if (IsDashing)
        {
            IsGrounded = GetIsGrounded();
            DashConditions();
        }
        else
        {
            MoveConditions();
            IsGrounded = GetIsGrounded();
            HoverConditions();
            JumpConditions();
            IsGrounded = GetIsGrounded();
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
        float targetY = _groundHoveringPoint.point.y + _hoverHeight + _playerHalfHeight;

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

        IsDashing = false; // Cancel dashing immediately.
    }

    /// <summary>
    ///   <para>
    ///     Locks player movement and actions for a set time.
    ///   </para>
    /// </summary>
    /// <param name="duration"> How long to lock controls in seconds. </param>
    public void LockMovement(float duration)
    {
        _kbControlsLockTimer = Mathf.Max(_kbControlsLockTimer, duration);
        _kbDashLockTimer = Mathf.Max(_kbDashLockTimer, duration);
    }

    /// <summary>
    ///   <para>
    ///     Gets all stat changes from the stats file on any frame this method is called.
    ///   </para>
    /// </summary>
    private void TryGetComplexStatChanges()
    {
        if (PlayerEntity == null) return;
        
        // Check MoveSpeed.
        if (_moveMaxSpeed != PlayerEntity.Stats.MoveSpeed)
        {
            _moveMaxSpeed = PlayerEntity.Stats.MoveSpeed;
            RecalculateMoveAccelDecel();
        }

        // Check DashCharges.
        if (_dashMaxCharges != PlayerEntity.Stats.DashCharges)
        {
            int changeDifference = PlayerEntity.Stats.DashCharges - _dashMaxCharges;
            _dashMaxCharges = PlayerEntity.Stats.DashCharges;

            // Add positive difference to current charge count, even while regenerating.
            if (changeDifference > 0)
                _currDashCharges += changeDifference;
            else // Ensure negative difference is not affected by regeneration.
            {
                if (_currDashCharges >= _dashMaxCharges)
                {
                    _currDashCharges = _dashMaxCharges;
                    _isRegeneratingDash = false;
                }
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Checks if the player character is grounded (hovering) and updates the
    ///     ground hovering point on the frame this method is called.
    ///   </para>
    /// </summary>
    /// <returns> True if near enough to the ground for hovering, otherwise false. </returns>
    private bool GetIsGrounded()
    {
        bool grounded = PlayerGroundCheck.GetIsGrounded(GetPlayerCharacterBottom(),
                                                        _groundedCastLength,
                                                        _groundedCastRadius,
                                                        _maxGroundAngle,
                                                        _groundedLayerMasks,
                                                        out RaycastHit hitInfo,
                                                        _groundedCastPauseTimer);
        _groundHoveringPoint = hitInfo;

        //Debug.DrawRay(GetPlayerCharacterBottom(), _groundedCastLength * Vector3.down, Color.red);
        return grounded;
    }

    /// <summary>
    ///   <para>
    ///     Get the collision point of the player character for when it needs to slide on a slope
    ///     that is too steep to be treated as ground.
    ///   </para>
    /// </summary>
    /// <param name="hit"> The collision point. </param>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsGrounded) _groundCollidingPoint = hit;

        // Handle collision with tutorial objects.
        if (hit.gameObject.layer == LayerMask.NameToLayer("TutorialEvent"))
        {
            TutorialObject tutorialScript = hit.gameObject.GetComponent<TutorialObject>();
            tutorialScript.OnTriggerOrCollide(hit.collider);
        }
    }

    /// <summary>
    ///   <para>
    ///     Applies calculated custom gravity to the player every frame.
    ///   </para>
    /// </summary>
    private void GravityConditions()
    {
        // Do not apply gravity while on the ground or flying.
        if (IsGrounded || IsFlying) return;

        float aggregateGravityStrength = Physics.gravity.y * _gravityMultiplier * _currDriftDescentDivisor;
        Vector3 gravityVelocityVector;

        // Do not accelerate if sliding on a steep slope.
        if (_groundCollidingPoint == null)
        {
            float accelIncrement = Time.deltaTime * aggregateGravityStrength;
            _verticalVelocityVector.y = Mathf.Clamp(_verticalVelocityVector.y + accelIncrement, aggregateGravityStrength, float.MaxValue);
            ApplyAirDrag(aggregateGravityStrength); // Slow descent speed to the strength of gravity.
            gravityVelocityVector = _verticalVelocityVector;
        }
        else
        {
            // If sliding on a steep slope, ensure gravity is always sliding the player character.
            if (_verticalVelocityVector.y > -1) _verticalVelocityVector.y = -1;

            gravityVelocityVector = _verticalVelocityVector.magnitude * new Vector3(_groundCollidingPoint.normal.x,
                                                                                    _verticalVelocityVector.y,
                                                                                    _groundCollidingPoint.normal.z).normalized;
            _groundCollidingPoint = null;
        }

        _characterController.Move(Time.deltaTime * gravityVelocityVector);

        //Vector3 bottom = GetPlayerCharacterBottom();
        //Debug.DrawRay(bottom, _verticalVelocityVector, Color.green);
        //Debug.DrawRay(bottom, gravityVelocityVector, Color.red);
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
        if (_shockwaveController.IsCastingShockwave) return; // Do not allow movement while casting shockwave.

        // Trigger corresponding move animations.
        Vector2 inputDirection = _moveActions.ReadValue<Vector2>().normalized;
        float moveBlendX = inputDirection.x * (_lateralVelocityVector.magnitude / _moveMaxSpeed);
        float moveBlendY = inputDirection.y * (_lateralVelocityVector.magnitude / _moveMaxSpeed);
        _playerAnim.SetFloat("MoveVector_X", moveBlendX, _moveBlendSpeed, Time.deltaTime);
        _playerAnim.SetFloat("MoveVector_Y", moveBlendY, _moveBlendSpeed, Time.deltaTime);

        // Because move speed right before moment of knockback must be preserved for correct calculations,
        // simply stop recording new movement values instead of completely skipping the MoveCase method.
        Vector3 moveDirectionUnitVector = (_kbControlsLockTimer > 0) ? Vector3.zero : GetMoveInputDirection();

        GroundClampInterpolate(ref moveDirectionUnitVector);

        // Lose velocity proportional to the severity of the angle of direction change.
        if (_lateralVelocityVector.magnitude > 1e-3f && moveDirectionUnitVector != Vector3.zero)
        {
            float dot = Vector3.Dot(_lateralVelocityVector.normalized, moveDirectionUnitVector);
            
            // Never reset velocity below the turn damping floor.
            float turnDamp = Mathf.Lerp(_moveTurnDampingFloor, 1, (dot + 1) / 2);
            _lateralVelocityVector *= turnDamp;
        }

        // Accelerate if movement is being inputted and sprint has not been canceled.
        if (_moveActions.ReadValue<Vector2>() != Vector2.zero && _lateralVelocityVector.magnitude <= _moveMaxSpeed)
            MoveAccelerate(moveDirectionUnitVector);
        else
            MoveDecelerate();

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
        // DECOUPLED : Aim does nothing
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
    ///     Gradually interpolates the move direction of the player character to be parallel with
    ///     the ground below it while moving.
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> World direction the player is moving. </param>
    private void GroundClampInterpolate(ref Vector3 moveDirectionUnitVector)
    {
        if (IsGrounded)
        {
            if (_moveActions.ReadValue<Vector2>() != Vector2.zero)
            {
                Vector3 groundPlaneMoveUnitVector = Vector3.ProjectOnPlane(moveDirectionUnitVector, _groundHoveringPoint.normal);

                if (_moveInputTemp == Vector2.zero)
                    moveDirectionUnitVector = CopyVectorAngles(groundPlaneMoveUnitVector, moveDirectionUnitVector);
                else
                {
                    moveDirectionUnitVector = CopyVectorAngles(_lateralVelocityVector, moveDirectionUnitVector);

                    if (_moveActions.ReadValue<Vector2>() == _moveInputTemp)
                    {
                        float degreesPerSecond = Time.deltaTime * Mathf.Deg2Rad * 120;
                        moveDirectionUnitVector = Vector3.RotateTowards(moveDirectionUnitVector, groundPlaneMoveUnitVector, degreesPerSecond, 0);
                    }
                }

                Vector3 bottom = GetPlayerCharacterBottom();
                Debug.DrawRay(bottom, _lateralVelocityVector, Color.green);
                Debug.DrawRay(bottom, groundPlaneMoveUnitVector * 10, Color.red);
            }
        }

        _moveInputTemp = _moveActions.ReadValue<Vector2>();
    }

    /// <summary>
    ///   <para>
    ///     Gets a Vector3 that is composed of the pitch and yaw from two other given vectors.
    ///   </para>
    /// </summary>
    /// <param name="copyVectorPitch"> The vector pitch to copy. </param>
    /// <param name="copyVectorYaw"> The vector yaw to copy. </param>
    /// <returns> Composite vector from the pitch and yaw. </returns>
    private Vector3 CopyVectorAngles(Vector3 copyVectorPitch, Vector3 copyVectorYaw)
    {
        // Get copied pitch and yaw in radians.
        float copiedPitch = Mathf.Atan2(copyVectorPitch.y, Mathf.Sqrt(copyVectorPitch.x * copyVectorPitch.x
                                                                      + copyVectorPitch.z * copyVectorPitch.z));
        float copiedYaw = Mathf.Atan2(copyVectorYaw.x, copyVectorYaw.z);

        // Calculate the composite vector using the copied pitch and yaw.
        float cosPitch = Mathf.Cos(copiedPitch);
        float vectorX = cosPitch * Mathf.Sin(copiedYaw);
        float vectorY = Mathf.Sin(copiedPitch);
        float vectorZ = cosPitch * Mathf.Cos(copiedYaw);
        
        return new Vector3(vectorX, vectorY, vectorZ);
    }

    /// <summary>
    ///   <para>
    ///     Accelerates the player character laterally on any frame this method is called up to the max movement speed.
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> The world direction of the most recent move input. </param>
    private void MoveAccelerate(Vector3 moveDirectionUnitVector)
    {
        if (_lateralVelocityVector.magnitude < _moveMaxSpeed)
        {
            float aggregateAccelIncrement = Time.deltaTime * _moveAcceleration;
            float newVelocityMagnitude = Mathf.Clamp(_lateralVelocityVector.magnitude + aggregateAccelIncrement, 0, _moveMaxSpeed);
            _lateralVelocityVector = newVelocityMagnitude * moveDirectionUnitVector;
        }
        else
            _lateralVelocityVector = _moveMaxSpeed * moveDirectionUnitVector;
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
        _moveAcceleration = _moveMaxSpeed / _moveAccelerationSeconds;
        _moveDeceleration = _moveMaxSpeed / _moveDecelerationSeconds;
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

        _currHoverHeight = _groundHoveringPoint.point.y + _hoverHeight;
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

        // Do not allow dashes while attacking, throwing or casting shockwave.
        if (_meleeController.IsAttacking || _playerShooter.IsThrowing || _shockwaveController.IsCastingShockwave) return;

        if (_currDashCharges != 0 && !IsDashing)
        {
            _dashDirectionUnitVector = GetMoveInputDirection();

            // If not moving, default dash direction is forward.
            if (_dashDirectionUnitVector.x == 0 && _dashDirectionUnitVector.z == 0)
                _dashDirectionUnitVector = GetComponentInParent<Transform>().forward;

            _currDashCharges--;
            OnDashChargeSpent?.Invoke(_currDashCharges, _dashMaxCharges);


            // Only initialize regeneration routine if not already regenerating.
            if (_currDashCharges == _dashMaxCharges - 1) StartCoroutine(DashChargesRegeneration());

            float dashDuration = _dashDistance / _dashSpeed;
            StartCoroutine(InitiateDashDuration(dashDuration));
            DashCooldownStarted?.Invoke(dashDuration); // Notify listener to start the dash fade visual effect.
            // Play the dash audio effect here?
            dashSoundEvent.Post(gameObject);

            // Play Dash VFX
            if(dashVFXPrefab == null)
                return;
            else
                StartCoroutine(EnableDashVFX());
            //PlayDashVFX(transform.position, Quaternion.LookRotation(_dashDirectionUnitVector));
        }
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character dash on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DashConditions()
    {
        Vector3 dashVelocityVector = _dashDirectionUnitVector;
        if (IsGrounded) GroundClampSnap(ref dashVelocityVector);
        if (IsFlying) FlightConditions();

        _characterController.Move(Time.deltaTime * _dashSpeed * dashVelocityVector);
    }

    /// <summary>
    ///   <para>
    ///     Snaps the move direction of the player character to be parallel with the ground below it.
    ///   </para>
    /// </summary>
    /// <param name="moveDirectionUnitVector"> World direction the player is moving. </param>
    private void GroundClampSnap(ref Vector3 moveDirectionUnitVector)
    {
        if (IsGrounded)
            moveDirectionUnitVector = Vector3.ProjectOnPlane(moveDirectionUnitVector, _groundHoveringPoint.normal);
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
        while (_currDashCharges < _dashMaxCharges && _isRegeneratingDash)
        {
            if (timer >= _dashCooldown)
            {
                timer = 0;
                _currDashCharges++;
                OnDashChargeRestored?.Invoke(_currDashCharges, _dashMaxCharges);
            }

            if (_currDashCharges >= _dashMaxCharges) break;

            timer += Time.deltaTime;
            yield return null;
        }

        _currDashCharges = Mathf.Min(_currDashCharges, _dashMaxCharges); // In case DashMaxCharges is decreased during routine execution.
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
        IsDashing = true;

        Vector2 inputDirection = _moveActions.ReadValue<Vector2>().normalized;
        if (inputDirection == new Vector2(0, 0)) inputDirection = new Vector2(0, 1);

        _playerAnim.SetFloat("DashVector_X", inputDirection.x);
        _playerAnim.SetFloat("DashVector_Y", inputDirection.y);
        _playerAnim.SetTrigger("Dash");

        yield return new WaitForSeconds(seconds);

        IsDashing = false;
        _playerAnim.SetFloat("DashVector_X", 0);
        _playerAnim.SetFloat("DashVector_Y", 0);
        _playerAnim.SetTrigger("DashExit");
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
        if (IsDashing) return;

        _jumpBufferTimer = _jumpBufferWindow;
        if (!IsFlying && !IsGrounded) EnableDrift();
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
            PerformJump(_jumpForce);
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

        jumpSoundEvent.Post(gameObject); // Play the jump sound effect.
    }

    /// <summary>
    ///   <para>
    ///     Sets IsJumping to true for the duration of the jump animation.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator JumpAnimationDuration()
    {
        IsJumping = true;
        yield return new WaitForSeconds((_jumpAnim.length * _jumpAnimationSpeedMultiplier) + 0.01f);
        IsJumping = false;
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
        if ((_isDrifting && (!_jumpActions.IsPressed() || IsGrounded)) || IsFlying)
            DisableDrift();

        // Only modify gravity for drifting while falling and while the coyote time and jump buffer windows are invalid.
        if (AreDriftRequirementsValid() && _verticalVelocityVector.y <= 0)
        {
            float timeRatio = _driftDelayTimer / _driftDelay;
            if (timeRatio > 0)
                _currDriftDescentDivisor = _driftDescentDivisor + (timeRatio * (1 - _driftDescentDivisor));
            else
                _currDriftDescentDivisor = _driftDescentDivisor;
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
        if (_currFlightEnergy > 0 && !IsRegeneratingFlight)
        {
            // Toggle flight.
            if (!IsFlying)
            {
                // Make the player character jump if on the ground to prevent flight from exiting early.
                if (IsGrounded)
                    PerformJump(_flightJumpForce);

                EnableFlight();
            }
            else
                DisableFlight();
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
        // Skip calculations if not flying or still regenerating.
        if (!IsFlying || IsRegeneratingFlight) return;

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
        
        // Disable flight if on the ground.
        if (IsGrounded)
        {
            DisableFlight();
            return;
        }

        int flightInputValue = 0;
        if (_jumpActions.IsPressed()) flightInputValue += 1;
        if (_sprintActions.IsPressed()) flightInputValue -= 1;

        // If flight energy is not depleted, then fly.
        if (_currFlightEnergy > 0)
        {
            // Accelerate if jump or descend are being inputted.
            if (flightInputValue != 0 && _verticalVelocityVector.magnitude <= _flightMaxSpeed)
                FlightAccelerate(flightInputValue);
            // Otherwise, decelerate.
            else
                FlightDecelerate();

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
            accelIncrement *= _flightCounterAccelerationMultiplier;

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
    ///     Sets flying status to true, sets max movement speed to max flight speed, and sets movement acceleration
    ///     and deceleration to flight acceleration and deceleration on any frame this method is called.
    ///   </para>
    /// </summary>
    private void EnableFlight()
    {
        IsFlying = true;
        _moveMaxSpeed = _flightMaxSpeed;
        _moveAcceleration = _flightAcceleration;
        _moveDeceleration = _flightDeceleration;
    }

    /// <summary>
    ///   <para>
    ///     Sets flying status to false, resets max movement speed to its original value, resets movement
    ///     acceleration and deceleration back to their original values and starts a flight regeneration
    ///     coroutine on any frame this method is called.
    ///   </para>
    /// </summary>
    private void DisableFlight()
    {
        IsFlying = false;
        if (PlayerEntity != null) _moveMaxSpeed = PlayerEntity.Stats.MoveSpeed;
        RecalculateMoveAccelDecel();

        // Regeneration only begins after reaching the ground.
        if (_flightRegenerationCoroutine == null)
            _flightRegenerationCoroutine = StartCoroutine(FlightRegeneration());
    }

    /// <summary>
    ///   <para>
    ///     Coroutine for regenerating flight energy to full capacity over time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator FlightRegeneration()
    {
        // Only begin regenerating when on the ground.
        while (!IsGrounded) yield return null;

        IsRegeneratingFlight = true;
        while (_currFlightEnergy < _flightMaxEnergy)
        {
            float regenIncrement = Time.deltaTime * _flightRegenerationRate;
            _currFlightEnergy = Mathf.Clamp(_currFlightEnergy + regenIncrement, 0, _flightMaxEnergy);
            yield return null;
        }

        IsRegeneratingFlight = false;
        _flightRegenerationCoroutine = null;
    }

    public float GetCurrentFlightEnergy()
    {
        return _currFlightEnergy;
    }

    private IEnumerator EnableDashVFX()
    {
        dashVFXPrefab.SetActive(true);
        yield return new WaitForSeconds(0.15f);
        dashVFXPrefab.SetActive(false);
    }

    public void SetVerticalVelocityFactor(float factor)
    {
        _verticalVelocityVector.y = factor;
    }
}
