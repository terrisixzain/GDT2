using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    private struct PathNode
    {
        public Vector3 position;
        public float timestamp;

        public PathNode(Vector3 pos, float time)
        {
            position = pos;
            timestamp = time;
        }
    }

    private struct ContactInfo
    {
        public Vector3 point;
        public Vector3 normal;

        public ContactInfo(Vector3 pt, Vector3 norm)
        {
            point = pt;
            normal = norm;
        }
    }

    [Header("Horizontal Movement")]
    [SerializeField] private float maxMoveSpeed = 7.0f;
    [SerializeField] private float acceleration = 35.0f;
    [SerializeField] private float deceleration = 40.0f;

    [Header("Vertical & Gravity")]
    [SerializeField] private float gravity = 25.0f;
    [SerializeField] private float terminalVelocity = 20.0f;

    [Header("Jump Settings (Designer-Friendly Heights)")]
    [SerializeField] private float shortJumpHeight = 1.25f;
    [SerializeField] private float tallJumpHeight = 2.35f;
    [SerializeField] private float coyoteTimeDuration = 0.1f;
    [SerializeField] private float wallJumpSpeed = 10.5f;

    [Header("Surface Detection Thresholds")]
    [SerializeField] private float groundNormalThreshold = 0.7f;
    [SerializeField] private float ceilingNormalThreshold = -0.7f;
    [SerializeField] private float wallNormalThreshold = 0.3f;

    [Header("Debug Gizmos")]
    [SerializeField] private float pathHistoryDuration = 10.0f;
    [SerializeField] private float gizmoSphereRadius = 0.15f;
    [SerializeField] private float normalLineLength = 0.75f;

    private Rigidbody body;
    private PlayerActions inputActions;
    private Transform cameraTransform;

    private Vector2 moveInput;
    private bool jumpPressedEvent;
    private bool jumpReleasedEvent;
    private bool isJumpHeld;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool hitCeiling;
    private Vector3 lastWallNormal;
    private float coyoteTimer;
    private bool isRisingFromJump;

    private Vector3 lastJumpPressPosition;
    private Vector3 lastJumpReleasePosition;
    private bool hasJumpPressPoint;
    private bool hasJumpReleasePoint;

    private readonly Queue<PathNode> pathHistory = new Queue<PathNode>();
    private readonly List<ContactInfo> activeContacts = new List<ContactInfo>();

    public bool CanMove { get; set; } = true;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        inputActions = new PlayerActions();
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        inputActions.Movement.Enable();
    }

    void OnDisable()
    {
        inputActions.Movement.Disable();
    }

    void Update()
    {
        if (!CanMove)
        {
            moveInput = Vector2.zero;
            isJumpHeld = false;
            return;
        }

        moveInput = inputActions.Movement.Move.ReadValue<Vector2>();
        isJumpHeld = inputActions.Movement.Jump.IsPressed();

        if (inputActions.Movement.Jump.WasPressedThisFrame())
        {
            jumpPressedEvent = true;
            lastJumpPressPosition = transform.position;
            hasJumpPressPoint = true;
        }

        if (inputActions.Movement.Jump.WasReleasedThisFrame())
        {
            jumpReleasedEvent = true;
            lastJumpReleasePosition = transform.position;
            hasJumpReleasePoint = true;
        }

        RecordPathHistory();
    }

    void FixedUpdate()
    {
        UpdateCoyoteTimer();

        Vector3 velocity = body.linearVelocity;
        velocity = CalculateHorizontalVelocity(velocity);
        velocity = CalculateVerticalAndJumpVelocity(velocity);

        body.linearVelocity = velocity;

        ResetCollisionFlags();
        jumpPressedEvent = false;
        jumpReleasedEvent = false;
    }

    private void UpdateCoyoteTimer()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTimeDuration;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
    }

    private Vector3 CalculateHorizontalVelocity(Vector3 currentVelocity)
    {
        Vector3 moveDirection = GetCameraRelativeInputDirection();
        Vector3 targetHorizontalVelocity = moveDirection * maxMoveSpeed;
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0.0f, currentVelocity.z);

        float rate = (moveDirection.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        Vector3 newHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontalVelocity,
            rate * Time.fixedDeltaTime
        );

        return new Vector3(newHorizontal.x, currentVelocity.y, newHorizontal.z);
    }

    private Vector3 GetCameraRelativeInputDirection()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                return new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
            }
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0.0f;
        right.y = 0.0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveInput.y) + (right * moveInput.x);
        if (direction.sqrMagnitude > 1.0f)
        {
            direction.Normalize();
        }

        return direction;
    }

    private Vector3 CalculateVerticalAndJumpVelocity(Vector3 currentVelocity)
    {
        float verticalVelocity = currentVelocity.y;

        if (hitCeiling && verticalVelocity > 0.0f)
        {
            verticalVelocity = 0.0f;
            isRisingFromJump = false;
        }

        if (jumpPressedEvent)
        {
            if (coyoteTimer > 0.0f)
            {
                verticalVelocity = Mathf.Sqrt(2.0f * gravity * tallJumpHeight);
                coyoteTimer = 0.0f;
                isRisingFromJump = true;
            }
            else if (isTouchingWall && !isGrounded)
            {
                Vector3 wallLaunchDir = (lastWallNormal + Vector3.up).normalized;
                Vector3 launchVelocity = wallLaunchDir * wallJumpSpeed;
                currentVelocity.x = launchVelocity.x;
                currentVelocity.z = launchVelocity.z;
                verticalVelocity = launchVelocity.y;
                isRisingFromJump = true;
            }
        }

        if (jumpReleasedEvent && isRisingFromJump && verticalVelocity > 0.0f)
        {
            float shortJumpVelocity = Mathf.Sqrt(2.0f * gravity * shortJumpHeight);
            if (verticalVelocity > shortJumpVelocity)
            {
                verticalVelocity = shortJumpVelocity;
            }
            isRisingFromJump = false;
        }

        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            if (verticalVelocity < -terminalVelocity)
            {
                verticalVelocity = -terminalVelocity;
            }

            if (verticalVelocity <= 0.0f)
            {
                isRisingFromJump = false;
            }
        }
        else if (verticalVelocity < 0.0f)
        {
            verticalVelocity = 0.0f;
        }

        currentVelocity.y = verticalVelocity;
        return currentVelocity;
    }

    private void RecordPathHistory()
    {
        pathHistory.Enqueue(new PathNode(transform.position, Time.time));

        while (pathHistory.Count > 0 && (Time.time - pathHistory.Peek().timestamp) > pathHistoryDuration)
        {
            pathHistory.Dequeue();
        }
    }

    private void ResetCollisionFlags()
    {
        isGrounded = false;
        isTouchingWall = false;
        hitCeiling = false;
        activeContacts.Clear();
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollisionContacts(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollisionContacts(collision);
    }

    private void EvaluateCollisionContacts(Collision collision)
    {
        int contactCount = collision.contactCount;
        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Vector3 normal = contact.normal;

            activeContacts.Add(new ContactInfo(contact.point, normal));

            if (normal.y >= groundNormalThreshold)
            {
                isGrounded = true;
            }
            else if (normal.y <= ceilingNormalThreshold)
            {
                hitCeiling = true;
            }
            else if (Mathf.Abs(normal.y) <= wallNormalThreshold)
            {
                isTouchingWall = true;
                lastWallNormal = new Vector3(normal.x, 0.0f, normal.z).normalized;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        for (int i = 0; i < activeContacts.Count; i++)
        {
            ContactInfo info = activeContacts[i];
            Gizmos.DrawSphere(info.point, gizmoSphereRadius);
            Gizmos.DrawLine(info.point, info.point + (info.normal * normalLineLength));
        }

        if (hasJumpPressPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastJumpPressPosition, gizmoSphereRadius);
        }

        if (hasJumpReleasePoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lastJumpReleasePosition, gizmoSphereRadius);
        }

        if (pathHistory.Count > 1)
        {
            Gizmos.color = Color.cyan;
            Vector3 previousPoint = Vector3.zero;
            bool isFirst = true;

            foreach (PathNode node in pathHistory)
            {
                if (!isFirst)
                {
                    Gizmos.DrawLine(previousPoint, node.position);
                }
                previousPoint = node.position;
                isFirst = false;
            }
        }

#if UNITY_EDITOR
        if (body != null)
        {
            Vector3 labelPosition = transform.position + (Vector3.up * 2.5f);
            string velocityText = $"Velocity: {body.linearVelocity:F2} ({body.linearVelocity.magnitude:F2} m/s)";
            Handles.Label(labelPosition, velocityText);
        }
#endif
    }
}