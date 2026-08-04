/*
 * File: Build_Manager.cs
 * Purpose:
 *   Handles placement preview and confirmation for building objects
 *   in the game world, including reserving grid nodes during placement.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// summary: Manager that controls placement preview, validation and object instantiation for structures placed by the player.
public class Build_Manager : MonoBehaviour
{
    // summary: Singleton instance of the Build_Manager for global access.
    public static Build_Manager Instance { get; private set; }

    [Header("Placement")]
    public float snapSize = 1f;
    public LayerMask floorLayer = ~0;
    [Tooltip("Optional shared material used for preview visuals. If null, a copy of the renderer material will be used.")]
    public Material previewMaterial;
    [Range(0.05f, 1f)] public float previewAlpha = 0.6f;
    [Tooltip("When enabled, placed preview positions will be rounded to whole integer world coordinates.")]
    public bool forceIntegerPlacement = true;

    private GameObject currentPrefab;
    private GameObject previewInstance;
    private bool isPlacing = false;
    private Vector2Int currentFootprint = Vector2Int.one; // width (x), height (z)

    private GridManager gridManager;
    private List<GridNode> reservedNodes = new List<GridNode>();
    private List<Material> createdPreviewMaterials = new List<Material>();

    // Time-scaling during placement
    [Header("Time Scaling (Placement)")]
    [Tooltip("When true, game time will be slowed while a placement preview is active.")]
    public bool slowTimeDuringPlacement = true;
    [Range(0.01f, 1f)]
    [Tooltip("Target Time.timeScale value while placing (0.1 = 10% speed).")]
    public float placementTimeScale = 0.2f;

    // internal storage for restoring time settings
    private float previousTimeScale = 1f;
    private float originalFixedDeltaTime = 0.02f;
    private bool timeScaleModified = false;

    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this.gameObject);

        // Capture original time settings so we can restore them after placement
        originalFixedDeltaTime = Time.fixedDeltaTime;
        previousTimeScale = Time.timeScale;
    }

    void Start()
    {
        Debug.Log($"Build_Manager: Starting on GameObject '{gameObject.name}' (Instance set: {Instance != null})");
        gridManager = UnityEngine.Object.FindAnyObjectByType<GridManager>();
        Debug.Log(gridManager == null ? "Build_Manager: GridManager not found in scene." : $"Build_Manager: Found GridManager on '{gridManager.gameObject.name}'");
    }

    void Update()
    {
        if (!isPlacing) return;

        if (Time.frameCount % 60 == 0) Debug.Log("Build_Manager: isPlacing active — updating placement.");

        UpdatePlacement();

        // Confirm (left click / primary pointer)
        if (IsLeftClickDown())
        {
            if (IsPlacementValid()) ConfirmPlacement();
            else Debug.Log("Build_Manager: Confirm attempted but placement invalid.");
        }

        // Cancel with right-click / secondary pointer
        if (IsRightClickDown())
        {
            Debug.Log("Build_Manager: Placement cancelled via right-click.");
            CancelPlacement();
        }
    }

    public void StartPlacement(GameObject prefab)
    {
        StartPlacement(prefab, Vector2Int.one);
    }

    // summary: Begin placement mode for the provided prefab using a default 1x1 footprint.
    public void StartPlacement(GameObject prefab, Vector2Int footprint)
    {
        if (prefab == null) return;
        CancelPlacement();

        currentPrefab = prefab;
        currentFootprint = footprint;

        Debug.Log($"Build_Manager: StartPlacement called for prefab '{prefab.name}' with footprint {footprint}");
        if (previewMaterial != null) Debug.Log($"Build_Manager: previewMaterial assigned: {previewMaterial.name}");

        // Create preview instance
        previewInstance = Instantiate(prefab);
        if (previewInstance == null) Debug.LogError("Build_Manager: Failed to instantiate previewInstance");
        DisableColliders(previewInstance);
        SetupPreviewMaterials(previewInstance);

        isPlacing = true;

        // Apply slowed time while the player is placing an object
        ApplyPlacementTimeScale();
    }

    // summary: Begin placement mode for the provided prefab using the supplied footprint.
    public void CancelPlacement()
    {
        // Destroy any runtime-created preview materials to avoid leaking and avoid affecting shared assets
        foreach (var m in createdPreviewMaterials)
        {
            if (m != null) Destroy(m);
        }
        createdPreviewMaterials.Clear();

        if (previewInstance != null) Destroy(previewInstance);
        previewInstance = null;
        currentPrefab = null;
        isPlacing = false;
        reservedNodes.Clear();

        // Restore normal time scale when placement ends
        RestoreTimeScale();
    }

    // summary: Cancel any active placement preview and release reserved nodes.
    public void ConfirmPlacement()
    {
        if (!isPlacing || previewInstance == null || currentPrefab == null) return;

        if (!IsPlacementValid()) return;

        // Instantiate the real object at preview location/rotation
        GameObject placed = Instantiate(currentPrefab, previewInstance.transform.position, previewInstance.transform.rotation);

        // Mark as obstacle so GridManager's overlap checks detect it
        placed.tag = "Obstacle";

        // Mark nodes as non-walkable for the footprint
        if (gridManager != null && reservedNodes.Count > 0)
        {
            foreach (var n in reservedNodes)
            {
                if (n != null) n.SetWalkable(false);
            }
        }

        // Finish placement
        CancelPlacement();
    }

    // Helper: apply placement time scale
    private void ApplyPlacementTimeScale()
    {
        if (!slowTimeDuringPlacement) return;
        if (timeScaleModified) return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp(placementTimeScale, 0.01f, 1f);
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
        timeScaleModified = true;
        Debug.Log($"Build_Manager: Time scaled to {Time.timeScale} for placement.");
    }

    // Helper: restore original time scale
    private void RestoreTimeScale()
    {
        if (!timeScaleModified) return;
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        timeScaleModified = false;
        Debug.Log($"Build_Manager: Time restored to {Time.timeScale}.");
    }

    // summary: Confirm and place the currently previewed prefab into the scene if valid.
    private void UpdatePlacement()
    {
        if (previewInstance == null) return;

        Vector3 worldPoint;
        RaycastToFloor(out worldPoint);

        Vector3 snapped = SnapToGrid(worldPoint);
        Debug.Log($"Build_Manager: Raycast worldPoint={worldPoint}, snapped={snapped}");

        // If we have a grid manager use the grid nodes to align and validate
        if (gridManager != null)
        {
            GridNode center = gridManager.GetNodeAtPosition(snapped);
            if (center == null) center = gridManager.FindNodeClosestTo(snapped);

            if (center != null)
            {
                Vector3 centerPos = center.transform.position;
                if (forceIntegerPlacement)
                {
                    centerPos = new Vector3(Mathf.Round(centerPos.x), centerPos.y, Mathf.Round(centerPos.z));
                }
                previewInstance.transform.position = centerPos;
                PrepareReservedNodes(center);
                bool valid = CheckReservedNodesAvailable();
                UpdatePreviewVisual(previewInstance, valid);
                return;
            }
        }

        // Fallback: place at snapped world position (y=0)
        previewInstance.transform.position = new Vector3(snapped.x, 0f, snapped.z);
        UpdatePreviewVisual(previewInstance, true);
    }

    private bool RaycastToFloor(out Vector3 point)
    {
        // Ensure we have a camera
        if (Camera.main == null)
        {
            Debug.LogError("Build_Manager.RaycastToFloor: Camera.main is null. Cannot compute ray.");
            point = Vector3.zero;
            return false;
        }

        Vector2 pointer = GetPointerPosition();
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(pointer.x, pointer.y, 0f));

        // Primary: intersect with world Y=0 plane (always available)
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            Debug.Log($"Build_Manager.RaycastToFloor: Plane hit at {point}");
            return true;
        }

        // As a fallback, try physics raycast (if for some reason plane misses)
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, floorLayer))
        {
            point = hit.point;
            Debug.Log($"Build_Manager.RaycastToFloor: Physics raycast hit at {point}");
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private Vector3 SnapToGrid(Vector3 worldPoint)
    {
        // Snap the world point to the nearest grid cell based on snapSize
        if (forceIntegerPlacement)
        {
            float x = Mathf.Round(worldPoint.x);
            float z = Mathf.Round(worldPoint.z);
            return new Vector3(x, 0f, z);
        }
        else
        {
            float x = Mathf.Round(worldPoint.x / snapSize) * snapSize;
            float z = Mathf.Round(worldPoint.z / snapSize) * snapSize;
            return new Vector3(x, 0f, z);
        }
    }

    private void DisableColliders(GameObject obj)
    {
        // Disable all colliders in the preview instance to prevent physics interactions during placement
        foreach (var c in obj.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }

    private void SetupPreviewMaterials(GameObject obj)
    {
        // Apply the preview material to all renderers in the preview instance
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            Material mat = null;
            if (previewMaterial != null) mat = new Material(previewMaterial);
            else if (r.sharedMaterial != null) mat = new Material(r.sharedMaterial);

            if (mat != null)
            {
                Color col = mat.color;
                col.a = previewAlpha;
                mat.color = col;
                r.material = mat;
                createdPreviewMaterials.Add(mat);
            }
        }
    }

    private void UpdatePreviewVisual(GameObject obj, bool valid)
    {
        // Update the preview visual to indicate whether the current placement is valid
        Color baseCol = valid ? Color.green : Color.red;
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r == null || r.material == null) continue;
            Color c = r.material.color;
            c.r = baseCol.r;
            c.g = baseCol.g;
            c.b = baseCol.b;
            c.a = previewAlpha;
            r.material.color = c;
        }
    }

    private void PrepareReservedNodes(GridNode centerNode)
    {
        // Calculate and store the grid nodes that would be occupied by the current footprint centered on the given node
        reservedNodes.Clear();
        int halfX = currentFootprint.x / 2;
        int halfZ = currentFootprint.y / 2;

        for (int dx = -halfX; dx <= currentFootprint.x - halfX - 1; dx++)
        {
            for (int dz = -halfZ; dz <= currentFootprint.y - halfZ - 1; dz++)
            {
                Vector3 pos = centerNode.transform.position + new Vector3(dx * snapSize, 0f, dz * snapSize);
                GridNode node = gridManager.GetNodeAtPosition(pos);
                if (node == null) node = gridManager.FindNodeClosestTo(pos);
                if (node != null && !reservedNodes.Contains(node)) reservedNodes.Add(node);
            }
        }
    }

    private bool CheckReservedNodesAvailable()
    {
        // Check if all reserved nodes are walkable and not null, indicating that placement is valid
        if (reservedNodes.Count == 0) return true;
        foreach (var n in reservedNodes)
        {
            if (n == null) return false;
            if (!n.isWalkable) return false;
        }
        return true;
    }

    private bool IsPlacementValid()
    {
        return CheckReservedNodesAvailable();
    }

    private bool IsLeftClickDown()
    {
        var mouse = Mouse.current;
        if (mouse != null) return mouse.leftButton.wasPressedThisFrame;
        try { return UnityEngine.Input.GetMouseButtonDown(0); } catch { return false; }
    }

    private bool IsRightClickDown()
    {
        var mouse = Mouse.current;
        if (mouse != null) return mouse.rightButton.wasPressedThisFrame;
        try { return UnityEngine.Input.GetMouseButtonDown(1); } catch { return false; }
    }

    private Vector2 GetPointerPosition()
    {
        var mouse = Mouse.current;
        if (mouse != null) return mouse.position.ReadValue();
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch != null) return touch.primaryTouch.position.ReadValue();
        try { return (Vector2)UnityEngine.Input.mousePosition; } catch { return Vector2.zero; }
    }
}
