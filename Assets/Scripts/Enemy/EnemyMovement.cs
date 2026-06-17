using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;
    [Tooltip("Make sure the enemy doesn't stop too far away from the player, otherwise they'll never trigger the battle.")]
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    [SerializeField] private float stuckCheckInterval = 1f;
    [SerializeField] private float stuckMoveThreshold = 0.2f;
    [SerializeField] private float gravity = -30f;

    [Header("Detection Settings")]
    [Tooltip("This value is only one half of the cone, meaning 60 gives a 120 degree cone of vision.")]
    [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float lostSightTimeout = 1.5f;
    [SerializeField] private LayerMask detectionLayer = ~0;
    [SerializeField] private bool displayDetectionDebug = false;

    [Header("References")]
    [SerializeField] private BattleGroup battleGroup;
    [SerializeField] private CharacterController controller;
    private Transform playerTransform;

    private enum EnemyState { Idle, Roaming, Chasing }
    private EnemyState currentState = EnemyState.Roaming;

    private enum DetectionState { Clear, InRange, Detected }
    private DetectionState detectionState = DetectionState.Clear;

    private Vector3 lastPos;
    private Vector3 targetPos;
    private float verticalVelocity;

    private float stuckTimer = 0f;
    private float idleTimer = 0f;
    private float lostSightTimer = 0f;

    private void Awake()
    {
        if (!controller) controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!playerTransform && GameManager.Instance.PossessedCharacter)
            playerTransform = GameManager.Instance.PossessedCharacter.transform;

        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Roaming:
                Roam();
                break;
            case EnemyState.Chasing:
                Chase();
                break;
        }

        ApplyGravity();
    }

    public void Initialize(BattleGroup group)
    {
        battleGroup = group;
        targetPos = battleGroup.GetRandomPositionInBounds();
        lastPos = transform.position;
    }

    private void Idle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            StartRoaming();
    }

    private void Roam()
    {
        // Check if the player is within distance and view
        if (playerTransform)
        {
            if (battleGroup.IsPlayerInBounds(playerTransform.position))
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distToPlayer <= detectionRadius)
                {
                    Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, dirToPlayer);
                    if (angle <= fieldOfView)
                    {
                        Vector3 origin = transform.position + Vector3.up * 0.5f;
                        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, detectionRadius, detectionLayer, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform.root.CompareTag("Player"))
                            {
                                // Clear line of sight
                                detectionState = DetectionState.Detected;
                                battleGroup.AlertGroup();
                                return;
                            }

                            // Obstacle is blocking line of sight
                            detectionState = DetectionState.InRange;
                        }
                    }
                    else
                        detectionState = DetectionState.InRange; // Outside the cone
                }
                else
                    detectionState = DetectionState.Clear; // Outside the radius
            }
            else
                detectionState = DetectionState.Clear; // Outside the bounds
        }

        // Stuck detection
        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckCheckInterval)
        {
            if (Vector3.Distance(transform.position, lastPos) < stuckMoveThreshold)
                targetPos = battleGroup.GetRandomPositionInBounds();

            lastPos = transform.position;
            stuckTimer = 0f;
        }

        // Move toward current target position
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;
        if (direction.magnitude <= waypointReachedDistance)
        {
            StartIdling();
            return;
        }

        controller.Move(direction.normalized * walkSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void Chase()
    {
        if (!playerTransform) return;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        bool hasLineOfSight = false;
        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, Mathf.Infinity, detectionLayer, QueryTriggerInteraction.Ignore))
            hasLineOfSight = hit.transform.root.CompareTag("Player");

        if (!hasLineOfSight)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightTimeout)
            {
                lostSightTimer = 0f;
                StartIdling();
                return;
            }
        }
        else
            lostSightTimer = 0f;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        if (direction.magnitude <= stoppingDistance)
            return;

        controller.Move(direction.normalized * chaseSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void StartIdling()
    {
        currentState = EnemyState.Idle;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    public void StartRoaming()
    {
        currentState = EnemyState.Roaming;
        targetPos = battleGroup.GetRandomPositionInBounds();
    }

    public void StartChasing()
    {
        currentState = EnemyState.Chasing;
        detectionState = DetectionState.Detected;
    }

    private void OnDrawGizmos()
    {
        if (!displayDetectionDebug) return;

        // Radius Sphere
        switch (detectionState)
        {
            case DetectionState.Clear:
                Gizmos.color = Color.green;
                break;
            case DetectionState.InRange:
                Gizmos.color = Color.yellow;
                break;
            case DetectionState.Detected:
                Gizmos.color = Color.red;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // FOV Cone
        Gizmos.color = Color.cyan;

        Vector3 leftEdge = Quaternion.Euler(0f, -fieldOfView, 0f) * transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0f, fieldOfView, 0f) * transform.forward;
        Gizmos.DrawRay(transform.position, leftEdge * detectionRadius);
        Gizmos.DrawRay(transform.position, rightEdge * detectionRadius);
    }
}