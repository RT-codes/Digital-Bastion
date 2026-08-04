/*
 * File: GridNode.cs
 * Purpose:
 *   Represents a single cell in the grid used for pathfinding and
 *   provides reservation APIs so agents can claim nodes.
 */

using UnityEngine;

// summary: Represents a single grid cell. Tracks walkability and provides reservation methods to prevent multiple enemies from occupying the same node.
public class GridNode : MonoBehaviour
{
    public bool isWalkable = true;
    public Vector2Int gridPosition;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public GridNode parent;

    [SerializeField] private Color walkableColor = Color.white;
    [SerializeField] private Color blockedColor = Color.red;

    private Renderer nodeRenderer;
    private Enemy reservedBy = null;

    void Awake()
    {
        nodeRenderer = GetComponent<Renderer>();
    }

    public void SetWalkable(bool walkable)
    {
        isWalkable = walkable;
        UpdateVisuals();
    }


    // summary: Sets whether this node is considered walkable and updates visuals.

    public void ResetPathfindingState()
    {
        gCost = Mathf.Infinity;
        hCost = 0f;
        parent = null;
    }


    // summary: Reset pathfinding costs and parent so the node can be used in a new search.

    // Reservation API to prevent multiple enemies occupying the same node simultaneously
    public bool IsReserved => reservedBy != null;


    // summary: Returns true when this node is currently reserved by an enemy.

    public bool TryReserve(Enemy requester)
    {
        if (reservedBy == null || reservedBy == requester)
        {
            reservedBy = requester;
            return true;
        }
        return false;
    }


    // summary: Attempts to reserve this node for the provided requester. Returns true when reservation succeeds.

    public void ReleaseReservation(Enemy requester)
    {
        if (reservedBy == requester)
        {
            reservedBy = null;
        }
    }


    // summary: Releases the reservation held by the requester, if any.

    public void ForceRelease()
    {
        reservedBy = null;
    }

    // summary: Returns true when this node is reserved by another enemy than the requester.
    public bool IsReservedByOther(Enemy requester)
    {
        return reservedBy != null && reservedBy != requester;
    }


    // summary: Clears any reservation on this node unconditionally.

    private void UpdateVisuals()
    {
        if (nodeRenderer != null)
        {
            nodeRenderer.material.color = isWalkable ? walkableColor : blockedColor;
        }
    }
}
