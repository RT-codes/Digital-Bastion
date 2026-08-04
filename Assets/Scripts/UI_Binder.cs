/*
 * File: UI_Binder.cs
 * Purpose:
 *   Binds UI bar buttons to tower placement requests and connects
 *   the UI to the Player_Manager for placement behavior.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
// summary: Wires up UI buttons (class 'bar-action-button') to tower placement actions and forwards requests to the `Player_Manager`.
public class UI_Binder : MonoBehaviour
{
    [Tooltip("Assign tower prefabs in the same order as the UI bar buttons (left-to-right)")]
    public List<GameObject> towerPrefabs = new List<GameObject>();

    [Tooltip("Optional: assign the Player Manager instance here to avoid FindObjectOfType at runtime")]
    public Player_Manager playerManager;

    private UIDocument uiDoc;
    private List<(Button button, Action callback)> registered = new List<(Button, Action)>();

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            Debug.LogError($"UI_Binder: UIDocument not found on GameObject '{gameObject.name}'");
            return;
        }

        Debug.Log($"UI_Binder: Enabled on '{gameObject.name}'. UIDocument='{uiDoc.name}'");

        var root = uiDoc.rootVisualElement;
        var buttons = root.Query<Button>(null, "bar-action-button").ToList();

        Debug.Log($"UI_Binder: Found {buttons.Count} button(s) with class 'bar-action-button'.");

        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            Action cb = () => OnBarButtonClicked(idx);
            buttons[i].clicked += cb;
            registered.Add((buttons[i], cb));
            Debug.Log($"UI_Binder: Registered callback for button index {i} (name='{buttons[i].name}')");
        }

        if (playerManager == null)
        {
            playerManager = UnityEngine.Object.FindAnyObjectByType<Player_Manager>();
            Debug.Log(playerManager == null
                ? "UI_Binder: Player_Manager not found via FindAnyObjectByType. Assign it in inspector."
                : $"UI_Binder: Found Player_Manager on '{playerManager.gameObject.name}' via FindAnyObjectByType.");
        }
    }

    void OnDisable()
    {
        foreach (var pair in registered)
        {
            if (pair.button != null && pair.callback != null)
            {
                pair.button.clicked -= pair.callback;
            }
        }
        registered.Clear();
    }

    private void OnBarButtonClicked(int index)
    {
        Debug.Log($"UI_Binder: Button clicked index={index}");
        if (index < 0 || index >= towerPrefabs.Count) return;
        var prefab = towerPrefabs[index];
        Debug.Log(prefab == null ? $"UI_Binder: No prefab assigned at index {index}" : $"UI_Binder: Prefab '{prefab.name}' selected at index {index}");
        if (prefab == null) return;
        if (playerManager != null) playerManager.RequestStartPlacement(prefab);
        else Debug.LogWarning("UI_Binder: Player_Manager not found. Assign it in the inspector or add Player_Manager to the scene.");
    }
}
