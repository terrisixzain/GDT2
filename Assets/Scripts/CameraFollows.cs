using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float minPitch = -30.0f;
    [SerializeField] private float maxPitch = 70.0f;
    [SerializeField] private float followLagSpeed = 15.0f;

    private PlayerActions inputActions;
    private float yaw = 0.0f;
    private float pitch = 20.0f;
    private Vector3 currentVelocity;

    void Awake()
    {
        inputActions = new PlayerActions();
    }

    void OnEnable()
    {
        inputActions.Movement.Enable();
    }

    void OnDisable()
    {
        inputActions.Movement.Disable();
    }

    void Start()
    {
        if (target == null && GameObject.FindWithTag("Player") != null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public bool CanControlCamera { get; set; } = true;
    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        Vector2 lookInput = inputActions.Movement.Look.ReadValue<Vector2>();
        yaw += lookInput.x * mouseSensitivity * 0.1f;
        pitch -= lookInput.y * mouseSensitivity * 0.1f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        transform.rotation = targetRotation;
        Vector3 targetPosition = target.position + Vector3.up * 1.5f;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, 1.0f / followLagSpeed);
    }
}