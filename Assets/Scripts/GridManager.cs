/*
 * File: GridManager.cs
 * Purpose:
 *   Builds and maintains a grid of `GridNode` objects and provides
 *   pathfinding, reservation, and neighbor queries used by enemies.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// summary: Manages the grid of nodes used for pathfinding and walkability checks. Provides path search and node reservation helpers for agents.
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GameObject gridPointPrefab;
    [SerializeField] private int gridSizeX = 20;
    [SerializeField] private int gridSizeZ = 20;
    
    [Header("Spacing & Detection")]
    [SerializeField] private float spacing = 1.0f;
    [SerializeField] private float checkRadius = 0.4f;
    [SerializeField] private string obstacleTag = "Obstacle";

    [Header("Runtime Updates")]
    [SerializeField] private bool autoRebuild = true;
    [SerializeField] private float rebuildInterval = 0.25f;
    
    [Header("Debug Visuals")]
    [Tooltip("When enabled the MeshRenderer components under the grid will be visible. Toggle to hide the grid mesh in the editor/runtime.")]
    [SerializeField] private bool showGridMesh = true;

    [Header("Pathfinding")]
    [Tooltip("Extra cost added to nodes that are currently reserved by other enemies. Higher = more avoidance of reserved nodes.")]
    [SerializeField] private float reservationPenalty = 5.0f;
    [Tooltip("Number of nodes ahead to inspect heuristically for reservations when scoring neighbor penalties (not strict).")]
    [SerializeField] private int reservationLookaheadCost = 3;

    // Expose spacing so other systems (placement manager) can align to the same grid
    public float Spacing => spacing;

    private GridNode[,] gridNodes;

    void Start()
    {
        GenerateGrid();

        // Apply mesh visibility after generation
        ApplyGridMeshVisibility();

        if (autoRebuild)
        {
            StartCoroutine(RebuildRoutine());
        }
    }

    private IEnumerator RebuildRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(rebuildInterval);
            RebuildGrid();
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        // If grid hasn't been generated, do that first
        if (gridNodes == null)
        {
            GenerateGrid();
            return;
        }

        // Update existing nodes
        foreach (GridNode node in gridNodes)
        {
            UpdateNodeState(node);
        }
        
        // Debug.Log("Grid Rebuilt.");
        // Ensure mesh visibility is correct after rebuild
        ApplyGridMeshVisibility();
    }

    // summary: Re-samples the world and updates node walkability for the existing grid.

    private void GenerateGrid()
    {
        if (gridPointPrefab == null)
        {
            Debug.LogError("GridManager: No Grid Point Prefab assigned!");
            return;
        }

        // Clear existing children if any (in case of manual rebuild)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        gridNodes = new GridNode[gridSizeX, gridSizeZ];
        Vector3 startPos = transform.position;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Vector3 spawnPos = startPos + new Vector3(x * spacing, 0, z * spacing);
                GameObject nodeObj = Instantiate(gridPointPrefab, spawnPos, Quaternion.identity);
                nodeObj.transform.SetParent(this.transform);
                nodeObj.name = $"GridPoint_{x}_{z}";

                GridNode node = nodeObj.GetComponent<GridNode>();
                if (node == null) node = nodeObj.AddComponent<GridNode>();
                
                node.gridPosition = new Vector2Int(x, z);
                gridNodes[x, z] = node;

                UpdateNodeState(node);
            }
        }
        // Ensure the mesh renderer visibility matches the inspector setting for newly created children
        ApplyGridMeshVisibility();
        
        // Debug.Log($"Grid Generated: {gridSizeX}x{gridSizeZ} points.");
    }

    private void UpdateNodeState(GridNode node)
    {
        // Check for obstacles within radius
        Collider[] colliders = Physics.OverlapSphere(node.transform.position, checkRadius);
        bool blocked = false;

        foreach (var col in colliders)
        {
            if (col.CompareTag(obstacleTag))
            {
                blocked = true;
                break;
            }
        }

        node.SetWalkable(!blocked);
    }

    public GridNode GetNodeAtPosition(Vector3 worldPosition)
    {
        // Convert world position back to grid coordinates
        Vector3 relativePos = worldPosition - transform.position;
        int x = Mathf.RoundToInt(relativePos.x / spacing);
        int z = Mathf.RoundToInt(relativePos.z / spacing);

        if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
        {
            return gridNodes[x, z];
        }
        return null;
    }

    // summary: Returns the grid node at the provided world position, or null if out-of-bounds.

    // Reservation helpers
    public bool TryReserveNode(GridNode node, Enemy requester)
    {
        if (node == null) return false;
        return node.TryReserve(requester);
    }

    // summary: Attempts to reserve a node on behalf of the requester; returns true on success.

    public void ReleaseNode(GridNode node, Enemy requester)
    {
        if (node == null) return;
        node.ReleaseReservation(requester);
    }

    // summary: Releases a reservation previously held by the requester on a node.

    public void ForceReleaseNode(GridNode node)
    {
        if (node == null) return;
        node.ForceRelease();
    }

    // summary: Forcefully clears any reservation on the specified node.

    public List<GridNode> GetWalkableNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        Vector2Int[] dirs = {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var dir in dirs)
        {
            int nx = node.gridPosition.x + dir.x;
            int nz = node.gridPosition.y + dir.y;

            if (nx >= 0 && nx < gridSizeX && nz >= 0 && nz < gridSizeZ)
            {
                if (gridNodes[nx, nz].isWalkable)
                {
                    neighbors.Add(gridNodes[nx, nz]);
                }
            }
        }
        return neighbors;
    }

    // summary: Returns a list of neighboring nodes that are currently walkable.

    public List<GridNode> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition, Enemy requester = null)
    {
        if (gridNodes == null)
        {
            return new List<GridNode>();
        }

        GridNode startNode = GetNodeAtPosition(startWorldPosition);
        GridNode targetNode = GetNodeAtPosition(targetWorldPosition);

        if (startNode == null || !startNode.isWalkable)
        {
            startNode = FindNodeClosestTo(startWorldPosition);
        }

        if (targetNode == null || !targetNode.isWalkable)
        {
            targetNode = FindNodeClosestTo(targetWorldPosition);
        }

        if (startNode == null || targetNode == null || !startNode.isWalkable || !targetNode.isWalkable)
        {
            return new List<GridNode>();
        }

        foreach (GridNode node in gridNodes)
        {
            if (node != null)
            {
                node.ResetPathfindingState();
            }
        }

        List<GridNode> openSet = new List<GridNode> { startNode };
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        startNode.gCost = 0f;
        startNode.hCost = CalculateHeuristic(startNode, targetNode);

        while (openSet.Count > 0)
        {
            GridNode current = GetLowestCostNode(openSet);
            if (current == targetNode)
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            if (closedSet.Contains(current))
            {
                continue;
            }

            closedSet.Add(current);

            foreach (GridNode neighbor in GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                float tentativeGCost = current.gCost + 1f;
                // Add a reservation penalty when the neighbor is reserved by another enemy
                if (requester != null && neighbor.IsReservedByOther(requester))
                {
                    tentativeGCost += reservationPenalty;
                }
                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.parent = current;
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = CalculateHeuristic(neighbor, targetNode);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new List<GridNode>();
    }

    // summary: Performs a simple grid-based path search from start to target positions.

    private GridNode GetLowestCostNode(List<GridNode> nodes)
    {
        GridNode bestNode = null;
        float bestCost = float.MaxValue;

        foreach (GridNode node in nodes)
        {
            if (node != null && node.fCost < bestCost)
            {
                bestCost = node.fCost;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private float CalculateHeuristic(GridNode from, GridNode to)
    {
        return Mathf.Abs(from.gridPosition.x - to.gridPosition.x) + Mathf.Abs(from.gridPosition.y - to.gridPosition.y);
    }

    private List<GridNode> ReconstructPath(GridNode targetNode)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current = targetNode;

        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    // Helper to find the closest node to the base target
    public GridNode FindNodeClosestTo(Vector3 targetPos)
    {
        GridNode closest = null;
        float minDist = float.MaxValue;

        foreach (GridNode node in gridNodes)
        {
            if (!node.isWalkable) continue;
            
            float dist = Vector3.Distance(node.transform.position, targetPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        return closest;
    }

    // summary: Finds the closest walkable node to the provided world position.
    private void ApplyGridMeshVisibility()
    {
        // Toggle MeshRenderer enabled state for all children (including the prefab visuals)
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in renderers)
        {
            if (mr != null) mr.enabled = showGridMesh;
        }
    }

    private void OnValidate()
    {
        // When changing the toggle in the inspector, immediately apply the visibility in editor
        if (Application.isPlaying) ApplyGridMeshVisibility();
        else
        {
            // In editor mode ensure child renderers reflect the current setting
            ApplyGridMeshVisibility();
        }
    }

}
