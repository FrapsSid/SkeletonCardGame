using UnityEngine;

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

    private CharacterController _cc;
    private InputReader _input;
    private SkeletonBody _skeletonBody;
    private Vector3 _velocity;
    private Vector3 _smoothMoveVelocity;
    private float _coyoteTimer;

    private bool _isFirstPerson;
    public void SetFirstPersonLock(bool locked)
    {
        _isFirstPerson = locked;
    }

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = GetComponent<InputReader>();
        _skeletonBody = GetComponent<SkeletonBody>();
    }

    private void Update()
    {
        bool isGrounded = CheckGround();

        HandleGravity(isGrounded);
        if (_skeletonBody.IsIncapacitated || _isFirstPerson)
        {
            _input.ConsumeJump(); 
            
            _cc.Move(Vector3.SmoothDamp(_smoothMoveVelocity, Vector3.zero, ref _smoothMoveVelocity, accelerationTime) * Time.deltaTime);
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
            if (_velocity.y < 0) _velocity.y = -2f;
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
        Vector3 move = new Vector3(input.x, 0f, input.y);

        move = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * move;

        if (move.magnitude > 1f) move.Normalize();

        float currentMaxSpeed = _input.IsRunning ? runSpeed : walkSpeed;
        float finalSpeed = currentMaxSpeed * _skeletonBody.GetMovementMultiplier();

        Vector3 targetVelocity = move * finalSpeed;
        Vector3 smoothedVelocity = Vector3.SmoothDamp(_smoothMoveVelocity, targetVelocity, ref _smoothMoveVelocity, accelerationTime);

        _cc.Move(smoothedVelocity * Time.deltaTime);

        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckOffset, groundCheckRadius);
    }
}