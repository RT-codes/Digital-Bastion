# Digital Bastion — Tower Defense Prototype

Dive into a compact, tactical tower‑defense prototype built on a grid. Place and test towers, watch intelligent enemy waves adapt, and experiment with placement strategies in a small but expressive playfield.

What to expect

- Fast, grid-based action: place towers, block paths, and force enemies to adapt.
- Dynamic waves: enemies spawn in waves that scale over time and vary in count and speed.
- Intentional placement: previews snap to the grid and show valid/invalid positions before you build.
- Pause-for-precision: while placing a tower the game slows down so you can fine-tune positioning.

Standout features

- Smart grid pathfinding: a GridManager creates a cell grid and computes paths; nodes can be reserved so enemies avoid bumping into each other.
- Reservation-driven movement: enemies claim nodes as they move, which reduces clogging and produces smoother flows.
- Placement preview system: Build_Manager creates a translucent preview, disables physics for the preview, highlights validity, and reserves footprint nodes so placement is reliable.
- Wave progression: EnemyWaveManager controls wave timing, spawn pacing, and scaling (faster spawns and more enemies over time).

How it works (brief)

- GridManager generates a grid of GridNode objects and samples the world for obstacles (tagged "Obstacle") to mark walkability.
- Enemies query GridManager for paths and attempt to reserve nodes ahead of them; if a node is occupied, they replan or wait briefly.
- Build_Manager handles placement mode: instantiates a preview, snaps it to grid nodes, validates the footprint, and—optionally—slows game time while you place.
- On confirm, the real object is instantiated, footprint nodes are marked non-walkable, and the world resumes normal speed.

Getting started

1. Install Unity matching ProjectSettings/ProjectVersion.txt (m_EditorVersion: 6000.4.7f1).
2. Open the project in Unity Hub and open Assets/Scenes/SampleScene.unity.
3. Press Play to run. Use the in-scene UI or call Player_Manager.RequestStartPlacement(prefab) to begin placement and see the preview + slow-time behavior.

Key files

- Assets/Scripts/GridManager.cs, GridNode.cs — grid generation and pathfinding.
- Assets/Scripts/Enemy.cs, EnemyWaveManager.cs, AI_Manager.cs — enemy movement and wave spawning.
- Assets/Scripts/Build_Manager.cs, Player_Manager.cs, Tower.cs — placement, preview, and time-slow behavior.
- Assets/Scenes/SampleScene.unity — demo scene.
- Packages/manifest.json — Unity packages in use (URP, Input System, Visual Scripting, etc.).

License

Private project. Contact for demo builds or questions.

Contact

Rowan (via GitHub profile)
