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
    private InputAction jumpActions;
    private InputAction dashActions;

    [Header("Player References")]
    [SerializeField] private Transform _playerCenter;
    [SerializeField] private Transform _cameraTransform;
    private CharacterController _characterController;
    private Entity _playerEntity;
    private float _playerHalfHeight;
    private float _playerHalfWidth;
    private float _groundedRaycastFloorDistance;
    private float _groundedRaycastRadialDistance;
    private float _originalStepOffset;

    [Header("Movement Parameters")]
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _accelerationSeconds;
    [SerializeField] private float _decelerationSeconds;
    [SerializeField] private float _characterRotationDamping;
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
    [SerializeField][Range(0, 1)] private float _midairStrafeMultiplier;
    [SerializeField] [Range(0, 1)] private float _hoverDescentReductionMultiplier;
    [SerializeField] private float _gravityMultiplier;
    private bool _didPerformJump;
    private bool _isHovering;
    private float _aggregateGravityModifier;
    private Vector3 _verticalVector;

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

    [Header("Layer Parameter")]
    [SerializeField] private LayerMask _environmentLayer;

    private bool _lockControls = false;

    // Set by AimController.
    private bool strafe = false;
    


    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        moveActions = playerActions.Move;
        jumpActions = playerActions.Jump;
        dashActions = playerActions.Dash;
        
        moveActions.Enable();
        jumpActions.Enable();
        dashActions.Enable();
        
        jumpActions.started += JumpInputActionStarted;
        jumpActions.canceled += JumpInputActionCanceled;
        dashActions.started += DashInputActionStarted;
    }

    private void OnDisable()
    {
        moveActions.Disable();
        jumpActions.Disable();
        dashActions.Disable();

        jumpActions.started -= JumpInputActionStarted;
        jumpActions.canceled -= JumpInputActionCanceled;
        dashActions.started -= DashInputActionStarted;
    }

    void Start()
    {
        _playerEntity = GetComponent<Entity>();

        _playerHalfHeight = GetComponent<CharacterController>().bounds.extents.y;
        _playerHalfWidth = GetComponent<CharacterController>().bounds.extents.x;
        _groundedRaycastFloorDistance = _playerHalfHeight + 0.1f;
        _groundedRaycastRadialDistance = _playerHalfWidth * 0.7f;
        _originalStepOffset = _characterController.stepOffset;

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
    }

    void Update()
    {
        if (_lockControls) return;

        if (_kbControlsLockTimer > 0) _kbControlsLockTimer -= Time.deltaTime;
        if (_kbDashLockTimer > 0) _kbDashLockTimer -= Time.deltaTime;

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
        // Update _maxSpeed, _acceleration and _deceleration values whenever movement stats are changed.
        if (_playerEntity != null)
        {
            if (_maxSpeed != _playerEntity.Stats.MoveSpeed)
            {
                _maxSpeed = _playerEntity.Stats.MoveSpeed;
                _acceleration = _maxSpeed / _accelerationSeconds;
                _deceleration = _maxSpeed / _decelerationSeconds;
            }
        }

        // Because move speed right before moment of knockback must be preserved for correct calculations,
        // simply stop recording new movement values instead of completely skipping the MoveCase method.
        _moveInputUnitVector = (_kbControlsLockTimer > 0) ? Vector3.zero : GetMoveInputDirection();

        if (_moveInputUnitVector != Vector3.zero)
        {
            Accelerate();
        }
        else
        {
            Decelerate();
        }

        // Apply knockback until it has dissipated.
        if (_externalKnockbackVelocity.sqrMagnitude > 1e-6f)
        {
            // optional clamp to avoid crazy impulses
            // if (externalVelocity.magnitude > 100f)
            //     externalVelocity = externalVelocity.normalized * 100f;

            _characterController.Move(Time.deltaTime * _externalKnockbackVelocity);
            _externalKnockbackVelocity = Vector3.Lerp(_externalKnockbackVelocity, Vector3.zero, Time.deltaTime * _kbDamping);
        }

        // facing the movement stuff, turning player around
        if (_kbControlsLockTimer <= 0 && !strafe && lateralVector.sqrMagnitude > 0.0001f)
        {
            Quaternion qa = transform.rotation;
            Quaternion qb = Quaternion.LookRotation(lateralVector, Vector3.up);
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, _characterRotationDamping));
            transform.rotation = Quaternion.Slerp(qa, qb, t);
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
        if (_kbControlsLockTimer > 0) return;
        
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
            // Cancel boost energy regeneration if boost was inputted.
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
        // Check if the player character is being knocked back.
        if (_kbDashLockTimer > 0f) return;

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
