/*
 * File: Player_Manager.cs
 * Purpose:
 *   Entry point for player actions related to building and placement.
 */

using UnityEngine;

// summary: Exposes player-facing APIs for requesting placement of prefabs. Responsible for forwarding placement requests to the `Build_Manager`.
public class Player_Manager : MonoBehaviour
{
    // Simple player manager entry points for building
    // In future this will hold resources/inventory and validation logic

    void Start()
    {
    }

    void Update()
    {
    }

    // summary: Request to start placement of a prefab. Will validate player resources in future. Forwards validated requests to `Build_Manager`.
    public void RequestStartPlacement(GameObject prefab)
    {
        if (prefab == null) return;

        Debug.Log($"Player_Manager: RequestStartPlacement called for prefab '{prefab.name}'");

        // Try to determine footprint from prefab's Tower component if present
        Vector2Int footprint = Vector2Int.one;
        var towerComp = prefab.GetComponent<Tower>();
        if (towerComp != null) footprint = towerComp.footprint;

        if (Build_Manager.Instance != null)
        {
            Debug.Log("Player_Manager: Forwarding to Build_Manager.StartPlacement");
            Build_Manager.Instance.StartPlacement(prefab, footprint);
        }
        else
        {
            Debug.LogError("Player_Manager: Build_Manager.Instance is null — ensure a Build_Manager exists in the scene.");
        }
    }
}
