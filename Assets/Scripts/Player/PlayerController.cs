using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float rotationSpeed = 12f;
    public float accelerationTime = 0.1f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float coyoteTime = 0.15f;
    public float groundCheckRadius = 0.28f;
    public float groundCheckOffset = -0.1f;
    public LayerMask groundMask = ~0;

    [Header("References")]
    public Transform cameraTransform;
    [SerializeField] private UIStateController uiStateController;
    [Header("Animation")]
    public Animator animator;
    public float speedDampTime = 0.15f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private CharacterController _cc;
    private InputReader _input;
    private SkeletonBody _skeletonBody;
    private GhostController? _ghost;
    private Vector3 _velocity;
    private Vector3 _horizontalVelocity;
    private Vector3 _smoothMoveVelocity;
    private float _coyoteTimer;
    private bool _isFirstPerson;

    public void SetFirstPersonLock(bool locked)
    {
        _isFirstPerson = locked;
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (uiStateController == null)
            uiStateController = FindFirstObjectByType<UIStateController>();

        _cc = GetComponent<CharacterController>();
        _input = GetComponent<InputReader>();
        _skeletonBody = GetComponent<SkeletonBody>();
        _ghost = GetComponent<GhostController>();
    }

    private void Update()
    {
        // Ghost flight — ghosts skip all normal movement
        if (_ghost != null && _ghost.IsGhostActive)
        {
            bool up = Keyboard.current?.spaceKey.isPressed == true;
            bool down = Keyboard.current?.leftCtrlKey.isPressed == true;
            _ghost.HandleGhostMovement(_input.MoveInput, up, down);
            _input.ConsumeJump();
            UpdateAnimator(0f);
            return;
        }

        bool isGrounded = CheckGround();

        HandleGravity(isGrounded);
        if (_skeletonBody.IsIncapacitated)
        {
            _input.ConsumeJump();
            StopHorizontalMovement();
            return;
        }

        if (uiStateController != null && uiStateController.AnyUiOpen)
        {
            _input.ConsumeJump();
            StopHorizontalMovement();
            return;
        }

        HandleJump(isGrounded);
        HandleMovement();
    }

    private bool CheckGround()
    {
        Vector3 spherePos = transform.position + Vector3.up * groundCheckOffset;
        return Physics.CheckSphere(spherePos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleGravity(bool isGrounded)
    {
        if (isGrounded)
        {
            _coyoteTimer = coyoteTime;
            if (_velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    private void HandleJump(bool isGrounded)
    {
        if (_skeletonBody.GetLegCount() == 0)
        {
            _input.ConsumeJump();
            return;
        }

        if (_input.JumpPressed && _coyoteTimer > 0f)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _coyoteTimer = 0f;
        }

        _input.ConsumeJump();
    }

    private void HandleMovement()
    {
        Vector2 input = _input.MoveInput;
        Vector3 move = _isFirstPerson
            ? transform.right * input.x + transform.forward * input.y
            : new Vector3(input.x, 0f, input.y);

        if (!_isFirstPerson && cameraTransform != null)
        {
            move = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * move;
        }

        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        float currentMaxSpeed = _input.IsRunning ? runSpeed : walkSpeed;
        float finalSpeed = currentMaxSpeed * _skeletonBody.GetMovementMultiplier();

        Vector3 targetVelocity = move * finalSpeed;
        _horizontalVelocity = Vector3.SmoothDamp(_horizontalVelocity, targetVelocity, ref _smoothMoveVelocity, accelerationTime);

        _cc.Move(_horizontalVelocity * Time.deltaTime);

        if (!_isFirstPerson && move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        UpdateAnimator(finalSpeed);
    }
    private void UpdateAnimator(float currentMaxSpeed)
    {
        if (animator == null) return;

        float normalizedSpeed = currentMaxSpeed > 0f
            ? _horizontalVelocity.magnitude / runSpeed
            : 0f;

        animator.SetFloat(SpeedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
    }

    private void StopHorizontalMovement()
    {
        UpdateAnimator(0f);
        _horizontalVelocity = Vector3.SmoothDamp(_horizontalVelocity, Vector3.zero, ref _smoothMoveVelocity, accelerationTime);
        _cc.Move(_horizontalVelocity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckOffset, groundCheckRadius);
    }
}
