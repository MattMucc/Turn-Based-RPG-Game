using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float controllerSensitivity = 220f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float minPitch = -35f;

    [Header("Follow Settings")]
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeight = 1.5f;
    [SerializeField] private float followSmoothTime = 0.05f;
    private Vector3 followVelocity;
    private float distanceVelocity;
    private float currentDistance;

    [Header("Collision Settings")]
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionBuffer = 0.3f;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private bool displayCollisionDebug = false;

    [Header("References")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Transform cameraTransform;

    private float yaw;
    private float pitch;

    private void Start()
    {
        if (!input) input = GameManager.Instance.Input;
        if (!yawPivot) yawPivot = transform.GetChild(0);
        if (!pitchPivot) pitchPivot = yawPivot.GetChild(0);
        if (!cameraTransform) cameraTransform = Camera.main.transform;
        currentDistance = cameraDistance;

        // Initialize angles on start
        if (followTarget)
        {
            yaw = followTarget.eulerAngles.y;
            pitch = Mathf.Clamp(20f, minPitch, maxPitch); // Start with a slight downward angle looking at the player
            FollowTarget();
            ApplyRotation();
            ApplyCameraDistance();
        }
        else
        {
            if (yawPivot) yaw = yawPivot.eulerAngles.y;
            if (pitchPivot)
            {
                float x = pitchPivot.localEulerAngles.x;
                if (x > 180f) x -= 360f; // Convert to -180 to 180 range
                pitch = Mathf.Clamp(x, minPitch, maxPitch);
            }
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        UpdateNormalCamera();
    }

    private void UpdateNormalCamera()
    {
        RotateInput();
        FollowTarget();
        ApplyRotation();
        ApplyCameraDistance();
    }

    private void FollowTarget()
    {
        Vector3 desiredPos = followTarget.position + new Vector3(0, cameraHeight, 0);
        float smoothTime = followSmoothTime;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, smoothTime);
    }

    private void RotateInput()
    {
        Vector2 look = input.Look;
        bool usingController = input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad;
        if (!usingController)
        {
            yaw += look.x * mouseSensitivity;
            pitch -= look.y * mouseSensitivity;
        }
        else
        {
            yaw += look.x * controllerSensitivity * Time.deltaTime;
            pitch -= look.y * controllerSensitivity * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void ApplyRotation()
    {
        yawPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyCameraDistance()
    {
        // Calculating distance and offset being applied
        float targetDistance = cameraDistance;

        // Start collision check from the offset position
        Vector3 pivotPos = pitchPivot.position;
        Vector3 desiredCamPos = pivotPos - pitchPivot.forward * targetDistance;

        // Checking collisions
        Vector3 dir = (desiredCamPos - pivotPos).normalized;
        float castDistance = Vector3.Distance(pivotPos, desiredCamPos);
        if (Physics.SphereCast(pivotPos, collisionRadius, dir, out RaycastHit hit, castDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            targetDistance = Mathf.Max(0.1f, hit.distance - collisionBuffer);

        float activeTransitionSpeed = 1f / 18f;
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, activeTransitionSpeed);

        // Apply final camera position with offset
        Vector3 finalCamPos = pitchPivot.position - pitchPivot.forward * currentDistance;
        cameraTransform.position = finalCamPos;
        cameraTransform.rotation = pitchPivot.rotation;
    }

    public void SetFollowTarget(Transform character)
    {
        followTarget = character;
    }

    private void OnDrawGizmos()
    {
        if (displayCollisionDebug)
        {
            // Calculate the same values as ApplyCameraDistance
            float targetDistance = cameraDistance;
            Vector3 pivotPos = pitchPivot.position;
            Vector3 desiredCamPos = pivotPos - pitchPivot.forward * targetDistance;
            Vector3 dir = (desiredCamPos - pivotPos).normalized;

            // Draw the sphere cast visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivotPos, collisionRadius); // Starting sphere

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivotPos, desiredCamPos); // Cast direction line

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(desiredCamPos, collisionRadius); // End sphere

            // Draw the actual camera position
            if (cameraTransform)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(cameraTransform.position, 0.1f); // Actual camera position
                Gizmos.DrawLine(pivotPos, cameraTransform.position); // Line to camera
            }

            // If there's a hit, show where it hit
            float castDistance = Vector3.Distance(pivotPos, desiredCamPos);
            if (Physics.SphereCast(pivotPos, collisionRadius, dir, out RaycastHit hit, castDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, collisionRadius); // Hit sphere
                Gizmos.DrawLine(pivotPos, hit.point); // Line to hit point

                // Draw a cross at the exact hit point
                Gizmos.DrawLine(hit.point - Vector3.right * 0.2f, hit.point + Vector3.right * 0.2f);
                Gizmos.DrawLine(hit.point - Vector3.up * 0.2f, hit.point + Vector3.up * 0.2f);
                Gizmos.DrawLine(hit.point - Vector3.forward * 0.2f, hit.point + Vector3.forward * 0.2f);
            }
        }
    }
}