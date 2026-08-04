/*
 * File: Tower.cs
 * Purpose:
 *   Lightweight component describing the footprint for tower prefabs.
 */

using UnityEngine;

// summary: Simple data container that exposes the grid footprint for tower prefabs.
public class Tower : MonoBehaviour
{
    [Tooltip("Width (x) and Height (z) in grid cells this tower occupies")]
    public Vector2Int footprint = Vector2Int.one;
}
