using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float stopSpeedThreshold = 0.05f;
    [SerializeField] private float moveAccelRate = 40f;
    [SerializeField] private float moveDecelRate = 50f;
    [SerializeField] private float airControlMultiplier = 0.8f;
    [SerializeField] private float rotationSmoothTime = 12f;

    [Header("Jump Settings")]
    [SerializeField] private float baseJumpHeight = 2f;
    [SerializeField] private float holdJumpMultiplier = 2f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float terminalVelocity = -30f;
    [SerializeField] private float gravity = -30f;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckOrigin;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private bool displayGroundCheckDebug = false;

    [Header("References")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraYaw;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    public bool IsPossessed { get; private set; }
    public bool IsGrounded;
    public bool IsJumping;
    public bool IsCrouching;

    private bool wasGrounded = true;
    private bool sprintToggled;
    private bool crouchToggled;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    private void Start()
    {
        if (!input) input = GameManager.Instance.Input;
        if (!controller) controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!groundCheckOrigin) groundCheckOrigin = transform.Find("GroundCheck");
    }

    private void Update()
    {
        if (!IsPossessed) return;

        CheckGrounded();
        Move();
        Jump();
        //UpdateAnimator();

        // Apply both horizontal and vertical velocities
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void CheckGrounded()
    {
        bool controllerGrounded = controller.isGrounded;
        if (!controllerGrounded)
        {
            Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
            bool sphereHitGround = Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
            IsGrounded = sphereHitGround;
        }
        else
            IsGrounded = controllerGrounded;
    }

    private void Move()
    {
        Vector2 moveInput = input.Move;

        // Use the rig's yaw pivot
        Vector3 camForward = cameraYaw.forward;
        Vector3 camRight = cameraYaw.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;

        // Check sprint
        bool isGamepad = input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad;
        if (isGamepad && input.SprintPressed)
            sprintToggled = !sprintToggled;

        bool sprintActive = isGamepad ? sprintToggled : input.SprintHeld;
        bool noMoveInput = moveInput.sqrMagnitude < 0.01f;
        bool basicallyStopped = horizontalVelocity.sqrMagnitude < (stopSpeedThreshold * stopSpeedThreshold);
        if (noMoveInput & basicallyStopped)
            sprintToggled = false;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float speed = walkSpeed * inputMagnitude;
        if (sprintActive)
            speed *= sprintMultiplier;

        // Accel/Decel
        Vector3 desiredVel = desiredDir * speed;
        float rate = moveInput.sqrMagnitude > 0.1f ? moveAccelRate : moveDecelRate;
        float control = controller.isGrounded ? 1f : airControlMultiplier;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVel, rate * control * Time.deltaTime);
        if (desiredDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothTime * Time.deltaTime);
        }
    }

    private void Jump()
    {
        // Update coyote timer
        if (IsGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // Update jump buffer timer
        if (input.JumpPressed)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // Apply downward velocity when grounded
        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // Jump input
        bool canCoyoteJump = coyoteTimer > 0f;
        bool hasJumpBuffered = jumpBufferTimer > 0f;
        bool notAlreadyJumping = verticalVelocity <= 0f; // Prevents double jumping
        if (canCoyoteJump && hasJumpBuffered && notAlreadyJumping)
        {
            float targetHeight = baseJumpHeight * holdJumpMultiplier;
            verticalVelocity = Mathf.Sqrt(targetHeight * -2f * gravity);
            animator.SetTrigger("Jump");
            IsJumping = true;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        // Variable jump height: cut to base height if released early
        if (IsJumping && !input.JumpHeld && verticalVelocity > 0f)
        {
            float cutMultiplier = Mathf.Sqrt(1f / holdJumpMultiplier);
            verticalVelocity *= cutMultiplier;
            IsJumping = false;
        }

        // Reset jumping state when starting to fall
        if (verticalVelocity < 0f)
            IsJumping = false;

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity); // Clamps fall speed to terminal velocity (prevents becoming a meteor when falling)
    }

    private void UpdateAnimator()
    {
        if (!animator) return;

        bool justLanded = !wasGrounded && IsGrounded && verticalVelocity <= 0f;
        float currentSpeed = horizontalVelocity.magnitude;
        bool isGamepad = input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad;
        bool sprintActive = isGamepad ? sprintToggled : input.SprintHeld;
        float currentMaxSpeed = walkSpeed;
        if (sprintActive)
            currentMaxSpeed *= sprintMultiplier;

        float normalizedSpeed = currentSpeed / currentMaxSpeed;
        Vector2 moveInput = input.Move;
        //animator.SetBool("IsLockedOn", lockOn.IsLockedOn);
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
        if (justLanded)
            animator.SetTrigger("Landed");

        wasGrounded = IsGrounded;
    }

    public void Possess()
    {
        IsPossessed = true;
    }

    public void Unpossess()
    {
        IsPossessed = false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!displayGroundCheckDebug) return;

        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}