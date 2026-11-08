using System;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Serializable]
    public struct MovingLane
    {
        public float distanceAlongPlatform;
        public float spawnInterval;
        public float moveSpeed;
        public bool leftToRight;
        [HideInInspector] public float timer;
    }

    [Header("Prefabs & Platform Dimensions")]
    [SerializeField] private MovingHazard movingObstaclePrefab;
    [SerializeField] private Hazard staticJumpWallPrefab;
    [SerializeField] private float platformWidth = 8.0f;

    [Header("1 & 2: Moving Obstacle Lanes (Tower Defense Style)")]
    [SerializeField] private Vector3 movingObstacleScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private MovingLane[] movingLanes = new MovingLane[]
    {
        new MovingLane { distanceAlongPlatform = 2.5f, spawnInterval = 1.6f, moveSpeed = 3.5f, leftToRight = true },
        new MovingLane { distanceAlongPlatform = 5.5f, spawnInterval = 1.4f, moveSpeed = 4.0f, leftToRight = false }
    };

    [Header("3 & 4: Static Jump Barriers (Short & Tall Jump)")]
    [SerializeField] private float wallThickness = 0.6f;
    [SerializeField] private float shortJumpDistance = 8.5f;
    [SerializeField] private float shortJumpHeight = 0.9f;
    [SerializeField] private float tallJumpDistance = 11.5f;
    [SerializeField] private float tallJumpHeight = 1.8f;

    void Start()
    {
        SpawnStaticJumpBarriers();
    }

    void Update()
    {
        UpdateMovingLanes();
    }

    private void UpdateMovingLanes()
    {
        if (movingObstaclePrefab == null || movingLanes == null)
        {
            return;
        }

        for (int i = 0; i < movingLanes.Length; i++)
        {
            movingLanes[i].timer += Time.deltaTime;
            if (movingLanes[i].timer >= movingLanes[i].spawnInterval)
            {
                movingLanes[i].timer = 0.0f;
                SpawnMovingObstacle(movingLanes[i]);
            }
        }
    }

    private void SpawnMovingObstacle(MovingLane lane)
    {
        float halfWidth = platformWidth * 0.5f;
        float startX = lane.leftToRight ? -halfWidth : halfWidth;
        Vector3 localSpawnPos = new Vector3(startX, movingObstacleScale.y * 0.5f, lane.distanceAlongPlatform);

        Vector3 worldSpawnPos = transform.TransformPoint(localSpawnPos);
        Vector3 worldMoveDir = lane.leftToRight ? transform.right : -transform.right;

        MovingHazard instance = Instantiate(movingObstaclePrefab, worldSpawnPos, transform.rotation, transform);
        instance.transform.localScale = movingObstacleScale;
        instance.Initialize(worldMoveDir, lane.moveSpeed, platformWidth);
    }

    private void SpawnStaticJumpBarriers()
    {
        if (staticJumpWallPrefab == null)
        {
            return;
        }

        CreateStaticWall("ShortJumpBarrier", shortJumpDistance, shortJumpHeight);
        CreateStaticWall("TallJumpBarrier", tallJumpDistance, tallJumpHeight);
    }

    private void CreateStaticWall(string wallName, float distanceZ, float heightY)
    {
        Hazard wall = Instantiate(staticJumpWallPrefab, transform);
        wall.gameObject.name = wallName;
        wall.transform.localPosition = new Vector3(0.0f, heightY * 0.5f, distanceZ);
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = new Vector3(platformWidth, heightY, wallThickness);
    }

    void OnDrawGizmos()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        if (movingLanes != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < movingLanes.Length; i++)
            {
                float z = movingLanes[i].distanceAlongPlatform;
                float y = movingObstacleScale.y * 0.5f;
                Vector3 leftPoint = new Vector3(-platformWidth * 0.5f, y, z);
                Vector3 rightPoint = new Vector3(platformWidth * 0.5f, y, z);

                Gizmos.DrawLine(leftPoint, rightPoint);
                Gizmos.DrawWireCube(leftPoint, movingObstacleScale);
                Gizmos.DrawWireCube(rightPoint, movingObstacleScale);
            }
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3(0.0f, shortJumpHeight * 0.5f, shortJumpDistance),
            new Vector3(platformWidth, shortJumpHeight, wallThickness)
        );
        Gizmos.DrawWireCube(
            new Vector3(0.0f, tallJumpHeight * 0.5f, tallJumpDistance),
            new Vector3(platformWidth, tallJumpHeight, wallThickness)
        );

        Gizmos.matrix = oldMatrix;
    }
}