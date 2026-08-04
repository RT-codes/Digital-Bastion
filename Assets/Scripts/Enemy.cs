/*
 * File: Enemy.cs
 * Purpose:
 *   Controls enemy movement, pathfinding interaction with the grid,
 *   and reservation logic to avoid node collisions with other enemies.
 *
 * Responsibilities:
 *   - Maintain and follow a path towards the player's base.
 *   - Reserve and release grid nodes while traversing.
 *   - Smooth movement and rotation for visual stability.
 */

using UnityEngine;
using System.Collections.Generic;

// summary: Controls an enemy that navigates the grid toward the player base while reserving nodes to avoid collisions with other enemies.
public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float moveSpeed = 5f;
    public int damageToBase = 10;
    public float nodeDetectionRadius = 0.2f;
    public float pathRebuildInterval = 1.0f;
    [Tooltip("Slerp rotation speed; higher = faster turning")]
    [Range(0f, 50f)]
    public float turnSpeed = 8f;
    

    private Transform targetBase;
    private GridManager grid;
    private GridNode currentTargetNode;
    private List<GridNode> pathNodes = new List<GridNode>();
    private int pathIndex = 0;
    private float pathRebuildTimer = 0f;
    private GridNode reservedNode = null;
    private EnemyWaveManager waveManagerRef = null;

    [Header("Movement Smoothing")]
    [Tooltip("How quickly the current speed moves towards `moveSpeed`. Higher = snappier response.")]
    public float speedLerpRate = 8f;

    private float currentSpeed = 0f;
    [Tooltip("Minimum movement speed when waiting for reserved nodes (units/sec).")]
    [Range(0f, 10f)]
    public float minFollowSpeed = 0.5f;

    private float targetSpeed = 0f;
    [Tooltip("How many nodes ahead to check for reservations before advancing.")]
    [Range(1,5)]
    public int reservationLookahead = 2;

    [Header("Replanning")]
    [Tooltip("Minimum seconds between automatic replans when blocked by reservations.")]
    public float replanCooldown = 0.3f;
    private float lastReplanTime = -Mathf.Infinity;

    [Tooltip("Smoothing time (seconds) used for position damping. Smaller = snappier, larger = smoother.")]
    [Range(0f, 0.5f)]
    public float positionSmoothTime = 0.06f;

    private Vector3 positionVelocity = Vector3.zero;

    // summary: Initializes the enemy with a target, speed multiplier, and grid reference.
    public void Initialize(Transform baseTarget, float speedMultiplier, GridManager gridManager, EnemyWaveManager waveManager)
    {
        targetBase = baseTarget;
        moveSpeed *= speedMultiplier;
        currentSpeed = moveSpeed;
        grid = gridManager;
        waveManagerRef = waveManager;
        if (waveManagerRef != null)
        {
            waveManagerRef.RegisterEnemy(this);
        }
        pathNodes.Clear();
        pathIndex = 0;
        currentTargetNode = null;
        pathRebuildTimer = 0f;

        RebuildPath();
    }

    void Update()
    {
        if (targetBase != null && grid != null)
        {
            pathRebuildTimer += Time.deltaTime;

            bool shouldRebuild = currentTargetNode == null ||
                !currentTargetNode.isWalkable ||
                pathNodes.Count == 0 ||
                pathIndex >= pathNodes.Count ||
                (pathRebuildTimer >= pathRebuildInterval && Vector3.Distance(transform.position, currentTargetNode.transform.position) < nodeDetectionRadius * 3f);

            if (shouldRebuild)
            {
                pathRebuildTimer = 0f;
                RebuildPath();
            }

            MoveTowardsTargetNode();
        }
    }

    private void MoveTowardsTargetNode()
    {
        if (currentTargetNode == null || !currentTargetNode.isWalkable)
        {
            RebuildPath();
            if (currentTargetNode == null)
            {
                MoveDirectly(targetBase.position);
                return;
            }
        }

        // Determine if path ahead is blocked by reserved nodes; if so, slow to minFollowSpeed
        bool blockedAhead = IsPathBlockedAhead();
        targetSpeed = blockedAhead ? minFollowSpeed : moveSpeed;

        MoveDirectly(currentTargetNode.transform.position);

        if (Vector3.Distance(transform.position, currentTargetNode.transform.position) < nodeDetectionRadius)
        {
            AdvanceAlongPath();
        }
    }

    

    private void MoveDirectly(Vector3 targetPos)
    {
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(targetPos.x, targetPos.y, targetPos.z);
        Vector3 direction = (targetPosition - currentPosition).normalized;

        if (direction.magnitude > 0.0001f)
        {
            Vector3 desiredForward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (desiredForward.sqrMagnitude > 0.0001f)
            {
                    Quaternion targetRotation = Quaternion.LookRotation(desiredForward, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }

                // Smooth current speed towards targetSpeed for dampened stopping/starting
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed > 0f ? targetSpeed : moveSpeed, Mathf.Clamp01(speedLerpRate * Time.deltaTime));
                // Smooth position to reduce jitter
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
        }
    }

    private void RebuildPath()
    {
        if (grid == null || targetBase == null)
        {
            return;
        }

        // Release any previously reserved node because path will change
        if (reservedNode != null && grid != null)
        {
            grid.ReleaseNode(reservedNode, this);
            reservedNode = null;
        }

        pathNodes = grid.FindPath(transform.position, targetBase.position, this);

        if (pathNodes.Count > 0)
        {
            pathIndex = 0;
            while (pathIndex < pathNodes.Count && pathNodes[pathIndex] == null)
            {
                pathIndex++;
            }

            if (pathIndex < pathNodes.Count)
            {
                currentTargetNode = pathNodes[pathIndex];
                // Try reserving the initial target node
                if (grid != null && currentTargetNode != null)
                {
                    if (grid.TryReserveNode(currentTargetNode, this))
                    {
                        reservedNode = currentTargetNode;
                    }
                }
            }
            else
            {
                currentTargetNode = null;
            }
        }
        else
        {
            currentTargetNode = null;
        }
    }

    private void AdvanceAlongPath()
    {
        if (pathNodes.Count == 0)
        {
            currentTargetNode = null;
            return;
        }
        int candidateIndex = pathIndex + 1;
        while (candidateIndex < pathNodes.Count && (pathNodes[candidateIndex] == null || pathNodes[candidateIndex] == currentTargetNode))
        {
            candidateIndex++;
        }

        if (candidateIndex >= pathNodes.Count)
        {
            currentTargetNode = null;
            return;
        }

        GridNode nextNode = pathNodes[candidateIndex];

        // Check lookahead nodes for existing reservations by others
        int maxCheck = Mathf.Min(pathNodes.Count - 1, candidateIndex + reservationLookahead - 1);
        for (int i = candidateIndex; i <= maxCheck; i++)
        {
            GridNode chk = pathNodes[i];
            if (chk == null) continue;
            if (chk.IsReserved && chk != reservedNode)
            {
                // someone else has reserved a node in our lookahead; attempt a replan to avoid busy nodes
                if (grid != null && Time.time - lastReplanTime > replanCooldown)
                {
                    lastReplanTime = Time.time;
                    List<GridNode> alt = grid.FindPath(transform.position, targetBase.position, this);
                    if (alt != null && alt.Count > 0)
                    {
                        // Prefer the alternate path if it begins with a different immediate target
                        if (alt[0] != currentTargetNode)
                        {
                            // Release previous reservation
                            if (reservedNode != null && grid != null)
                            {
                                grid.ReleaseNode(reservedNode, this);
                                reservedNode = null;
                            }
                            pathNodes = alt;
                            pathIndex = 0;
                            // Try to reserve initial node of the new path
                            if (pathNodes.Count > 0 && grid.TryReserveNode(pathNodes[0], this))
                            {
                                reservedNode = pathNodes[0];
                                currentTargetNode = reservedNode;
                            }
                            return;
                        }
                    }
                }

                // No suitable alternate path found or recently replanned; wait
                return;
            }
        }

        // Try to reserve the next node before advancing
        bool reserved = true;
        if (grid != null && nextNode != null)
        {
            reserved = grid.TryReserveNode(nextNode, this);
            if (!reserved && Time.time - lastReplanTime > replanCooldown)
            {
                // Try a replanning attempt immediately if reservation failed
                lastReplanTime = Time.time;
                List<GridNode> alt = grid.FindPath(transform.position, targetBase.position, this);
                if (alt != null && alt.Count > 0 && alt[0] != currentTargetNode)
                {
                    if (reservedNode != null && grid != null)
                    {
                        grid.ReleaseNode(reservedNode, this);
                        reservedNode = null;
                    }
                    pathNodes = alt;
                    pathIndex = 0;
                    if (pathNodes.Count > 0 && grid.TryReserveNode(pathNodes[0], this))
                    {
                        reservedNode = pathNodes[0];
                        currentTargetNode = reservedNode;
                    }
                    return;
                }
                // otherwise, keep waiting and try again later
            }
        }

        if (!reserved)
        {
            // Can't advance now; will try again later
            return;
        }

        // Release previous reservation (if any and not the same as next)
        if (reservedNode != null && reservedNode != nextNode && grid != null)
        {
            grid.ReleaseNode(reservedNode, this);
        }

        // Advance to reserved node
        pathIndex = candidateIndex;
        currentTargetNode = nextNode;
        reservedNode = nextNode;
    }

    private bool IsPathBlockedAhead()
    {
        if (pathNodes == null || pathNodes.Count == 0) return false;

        int start = pathIndex + 1;
        int end = Mathf.Min(pathNodes.Count - 1, pathIndex + reservationLookahead);
        for (int i = start; i <= end; i++)
        {
            GridNode chk = pathNodes[i];
            if (chk == null) continue;
            if (chk.IsReserved && chk != reservedNode)
                return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckCollision(collision.gameObject);
    }

    private void CheckCollision(GameObject hitObject)
    {
        // Check by tag or if it's the target base (or a child of it)
        if (hitObject.CompareTag("Base") || 
            hitObject.transform == targetBase || 
            hitObject.transform.IsChildOf(targetBase))
        {
            OnReachBase();
        }
    }

    private void OnReachBase()
    {
        // Handle damaging the base here later
        //Debug.Log($"{gameObject.name} hit the base and dealt {damageToBase} damage!");
        
        // Destroy the enemy
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // Release reservation if this enemy is destroyed/disabled
        if (reservedNode != null && grid != null)
        {
            grid.ReleaseNode(reservedNode, this);
            reservedNode = null;
        }
        if (waveManagerRef != null)
        {
            waveManagerRef.UnregisterEnemy(this);
            waveManagerRef = null;
        }
    }

    private void OnDestroy()
    {
        if (waveManagerRef != null)
        {
            waveManagerRef.UnregisterEnemy(this);
            waveManagerRef = null;
        }
        if (reservedNode != null && grid != null)
        {
            grid.ReleaseNode(reservedNode, this);
            reservedNode = null;
        }
    }
}
