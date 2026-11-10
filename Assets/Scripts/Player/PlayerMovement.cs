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

    [Header("Ground Dash (prototype tuning)")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 12f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.22f;
    [SerializeField, Min(0)] private float dashCooldown = 0.6f;
    private float dashRemaining, dashCooldownRemaining, groundedStepOffset;
    private Vector3 dashDirection;
    private bool motionInitialized, dashMovementApplied;
    public bool IsDashing => dashRemaining > 0;

    [Header("Overworld encounter strike (prototype)")]
    [SerializeField, Min(.1f)] private float encounterStrikeReach = 3.5f;
    [SerializeField, Range(1, 180)] private float encounterStrikeHalfAngle = 60f;
    [SerializeField, Min(0)] private float encounterStrikeCooldown = .4f;
    [Tooltip("Draw the strike's sphere range (cyan), facing limits (yellow), and current eligible target (green). Enable Gizmos in Scene/Game view. Also works outside Play Mode; walls can block targets inside the range.")]
    [SerializeField] private bool displayEncounterStrikeDebug;
    private float nextEncounterStrike;

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
    private PlayerInputReader input;
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
        if (!IsPossessed || !input || !Application.isFocused || (GameManager.Instance && GameManager.Instance.WorldInputBlocked)) return;
        if (input.WorldAttackPressed) TryEncounterStrike();
        StepMovement(Time.deltaTime, input.Move, input.JumpPressed, input.JumpHeld, input.DashPressed,
            input.SprintPressed, input.SprintHeld, input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad);
    }

    public bool TryEncounterStrike()
    {
        if (!EnemyTrigger.IsControlledActor(this) || Time.time < nextEncounterStrike || !BattleManager.Instance || BattleManager.Instance.InBattle) return false;
        nextEncounterStrike = Time.time + encounterStrikeCooldown;
        var target = WorldEncounterStrike.FindTarget(transform, encounterStrikeReach, encounterStrikeHalfAngle);
        if (!target || !target.BattleGroup) return false;
        var awareness = target.GetComponent<EnemyMovement>();
        // Missing awareness data must not silently grant an ambush.
        bool preemptive = awareness && !awareness.IsAwareOf(transform);
        target.BattleGroup.TriggerBattle(preemptive);
        return BattleManager.Instance.InBattle;
    }

    // Shared by runtime and non-PlayMode collision checks.
    public void StepMovement(float dt, Vector2 move, bool jumpPressed, bool jumpHeld, bool dashPressed,
        bool sprintPressed = false, bool sprintHeld = false, bool isGamepad = false)
    {
        if (!IsPossessed || dt <= 0) return;
        if (!controller) controller = GetComponent<CharacterController>();
        if (!controller || !controller.enabled || (BattleManager.Instance && BattleManager.Instance.InBattle)) return;
        if (!motionInitialized) { groundedStepOffset = controller.stepOffset; motionInitialized = true; }
        // Contact from the previous Move is reliable; a distant ground sphere is not a landing.
        IsGrounded = verticalVelocity <= 0 && controller.isGrounded;
        dashCooldownRemaining = Mathf.Max(0, dashCooldownRemaining - dt);
        if (!IsGrounded || jumpPressed) CancelDash();
        dashMovementApplied = false;
        Move(dt, move, dashPressed && !jumpPressed, sprintPressed, sprintHeld, isGamepad);
        Jump(dt, jumpPressed, jumpHeld);
        controller.stepOffset = IsGrounded && verticalVelocity <= 0 ? groundedStepOffset : 0;
        var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
        IsGrounded = verticalVelocity <= 0 && (flags & CollisionFlags.Below) != 0;
        if (IsGrounded) { verticalVelocity = -2; IsJumping = false; }
        if (dashMovementApplied && (!IsGrounded || dashRemaining <= 0 || (flags & CollisionFlags.Sides) != 0)) CancelDash();
    }
    private void CancelDash()
    {
        if (dashRemaining > 0 || dashMovementApplied) horizontalVelocity = Vector3.zero;
        dashRemaining = 0;
    }
    private void Move(float dt, Vector2 moveInput, bool dashPressed, bool sprintPressed, bool sprintHeld, bool isGamepad)

    {
        // Use the rig's yaw pivot
        var yaw = cameraYaw ? cameraYaw : Camera.main ? Camera.main.transform : transform;
        Vector3 camForward = yaw.forward;
        Vector3 camRight = yaw.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredDir = (camRight * moveInput.x + camForward * moveInput.y).normalized;

        if (dashPressed && IsGrounded && dashRemaining <= 0 && dashCooldownRemaining <= 0)
        {
            dashDirection = desiredDir.sqrMagnitude > 0.01f ? desiredDir : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            dashRemaining = dashDuration; dashCooldownRemaining = dashCooldown;
        }
        if (dashRemaining > 0)
        {
            dashMovementApplied = true;
            horizontalVelocity = dashDirection * dashSpeed * (Mathf.Min(dt, dashRemaining) / dt);
            dashRemaining = Mathf.Max(0, dashRemaining - dt);
            transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);
            return;
        }
        // Check sprint
        if (isGamepad && sprintPressed)
            sprintToggled = !sprintToggled;

        bool sprintActive = isGamepad ? sprintToggled : sprintHeld;
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
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVel, rate * control * dt);
        if (desiredDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothTime * dt);
        }
    }

    private void Jump(float dt, bool jumpPressed, bool jumpHeld)
    {
        // Update coyote timer
        if (IsGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= dt;

        // Update jump buffer timer
        if (jumpPressed)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

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
            if (animator) animator.SetTrigger("Jump");
            IsJumping = true;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        // Variable jump height: cut to base height if released early
        if (IsJumping && !jumpHeld && verticalVelocity > 0f)
        {
            float cutMultiplier = Mathf.Sqrt(1f / holdJumpMultiplier);
            verticalVelocity *= cutMultiplier;
            IsJumping = false;
        }

        // Reset jumping state when starting to fall
        if (verticalVelocity < 0f)
            IsJumping = false;

        // Apply gravity
        verticalVelocity += gravity * dt;
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

    public void ResetMotion()
    {
        horizontalVelocity = Vector3.zero; verticalVelocity = 0;
        coyoteTimer = 0; jumpBufferTimer = 0; IsJumping = false;
        sprintToggled = false; dashRemaining = dashCooldownRemaining = 0; dashMovementApplied = false;
        if (controller && motionInitialized) controller.stepOffset = groundedStepOffset;
    }

    public void Unpossess()
    {
        ResetMotion(); IsPossessed = false;
    }

    private void OnDrawGizmos()
    {
        if (displayEncounterStrikeDebug && (!Application.isPlaying || (IsPossessed && !(BattleManager.Instance && BattleManager.Instance.InBattle))))
            WorldEncounterStrike.DrawDebug(transform, encounterStrikeReach, encounterStrikeHalfAngle);
        if (!Application.isPlaying) return;
        if (!displayGroundCheckDebug) return;

        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}
