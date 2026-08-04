/*
 * File: UI_Manager.cs
 * Purpose:
 *   Minimal UI manager for handling simple placement mode toggles.
 */

using UnityEngine;
using UnityEngine.InputSystem;

// summary: Handles simple UI-driven toggles such as entering tower placement mode.
public class UI_Manager : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private bool isTowerPlacementActive = false;

    // summary: Activate tower placement mode so the UI and cursor begin placement behavior.
    public void StartTowerPlacement()
    {
        isTowerPlacementActive = true;
        Debug.Log("Tower placement mode activated. Tower now following mouse position.");
    }

    private void Update()
    {
        if (!isTowerPlacementActive)
        {
            return;
        }

        bool clicked = false;
        var mouse = Mouse.current;
        if (mouse != null) clicked = mouse.leftButton.wasPressedThisFrame;
        else { try { clicked = UnityEngine.Input.GetMouseButtonDown(0); } catch { clicked = false; } }

        if (clicked)
        {
            Debug.Log("Tower placement mode activated. Tower now following mouse position.");
        }
    }
}
