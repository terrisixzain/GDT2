using UnityEngine;

public class MovingHazard : MonoBehaviour
{
    private Vector3 moveDirection = Vector3.right;
    private float moveSpeed = 4.0f;
    private float maxTravelDistance = 10.0f;
    private Vector3 startPosition;
    private bool isInitialized;

    public void Initialize(Vector3 direction, float speed, float travelDistance)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        maxTravelDistance = travelDistance;
        startPosition = transform.position;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }
}