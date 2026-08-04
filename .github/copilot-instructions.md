# GitHub Copilot Instructions — Digital Bastion

## Project overview

**Digital Bastion** is a tower defense game developed in Unity.

The relevant project files that should normally be inspected or modified are located inside the `Assets/` folder. Avoid changing files outside `Assets/` unless the task clearly requires it and permission has been given.

## General development principles

- Maintain a clear separation of concerns.
- Prefer readable, maintainable code over clever or overly compact solutions.
- Keep classes and methods focused on a single responsibility.
- Follow the existing project structure and conventions before introducing new patterns.
- Reuse existing systems where practical instead of creating duplicate functionality.
- Avoid unrelated refactors while implementing a feature or fixing a bug.
- Make the smallest coherent change that fully solves the task.

## Unity implementation guidance

When implementing features or debugging Unity behaviour:

- Check the official Unity documentation for conventional implementation patterns.
- Pay particular attention to Unity lifecycle methods, built-in functions, components, events, physics behaviour, serialization, and editor-related functionality.
- Prefer documented Unity patterns over custom workarounds when a suitable built-in solution exists.
- Do not assume the behaviour of a Unity API when it can be verified through the documentation.
- Consider the Unity version used by the project when consulting documentation.

## New namespaces, packages, and libraries

Before adding any new namespace, package, library, framework, or external dependency that is not already used by the relevant file or project:

1. Ask for permission.
2. Explain why it is needed.
3. Explain what problem it solves.
4. Mention whether the same result could reasonably be achieved with existing project code or built-in Unity functionality.
5. Wait for approval before adding it.

Standard namespaces that are already used elsewhere in the project may be reused when appropriate, but unnecessary imports should be avoided.

## Code organization

- Separate presentation, gameplay rules, state management, input, data, and Unity-specific behaviour where practical.
- Avoid placing unrelated responsibilities in the same class.
- Keep `MonoBehaviour` classes focused on scene and component interaction.
- Move reusable or independent logic into suitable plain C# classes when appropriate.
- Prefer explicit dependencies over hidden global state.
- Avoid introducing singletons or static state unless the existing architecture already relies on them and the choice is justified.
- Keep public APIs small and intentional.
- Use descriptive names for files, classes, methods, fields, and properties.

## Comments and documentation

Comments should explain the purpose, responsibility, and reasoning behind code rather than restating individual lines.

### File header comments

At the top of each relevant C# file, include a structured comment that describes:

- The purpose of the file.
- The main responsibility of the class or classes it contains.
- How the file fits into the wider system.
- Important dependencies or interactions.
- Any major assumptions, limitations, or extension points.

Example:

```csharp
/*
 * File: TowerPlacementController.cs
 * Purpose:
 *   Coordinates player input and tower placement within the game world.
 *
 * Responsibilities:
 *   - Receives placement requests.
 *   - Validates whether a tower can be placed.
 *   - Delegates tower creation to the appropriate spawning system.
 *
 * Interactions:
 *   - Uses TowerPlacementValidator for placement rules.
 *   - Uses TowerSpawner to create approved towers.
 *
 * Notes:
 *   Keep placement rules outside this controller so they can be tested
 *   and extended independently.
 */
```

### Class comments

Each class should have a concise single-line summary comment that explains:

- What the class represents.
- Its main responsibility.
- What it intentionally does not control when that distinction is useful.

Example:

```csharp
// summary: Coordinates tower placement requests between player input, placement validation, and tower spawning.
```

### Method and function comments

Each non-trivial method should include a concise single-line summary comment that explains what the method does, plus optionally short `// param:` and `// returns:` comments when parameters or return values need clarification.

Example:

```csharp
// summary: Checks whether a tower may be placed at the supplied world position. Evaluates placement rules but does not create the tower.
// param: worldPosition - The requested placement position.
// returns: True when the position satisfies all active placement rules; otherwise, false.
```

### Inline comments

- Use inline comments only when the reason behind a section is not obvious from the code.
- Explain why something is done, not merely what the code does.
- Avoid excessive comments on straightforward assignments or control flow.
- Update comments whenever behaviour changes.
- Remove outdated or misleading comments.

## Implementing new features

Before implementing a new feature:

1. Inspect the relevant files inside `Assets/`.
2. Identify the existing architecture and extension points.
3. Determine which responsibilities belong in separate classes or modules.
4. Check the Unity documentation when built-in Unity behaviour is involved.
5. Ask permission before introducing new namespaces, packages, or libraries.
6. Present a brief implementation plan when the feature affects multiple systems.
7. Keep the implementation modular and avoid coupling unrelated systems.

After implementation:

- Confirm which files were changed.
- Explain the role of each meaningful change.
- Mention any assumptions or limitations.
- Run relevant tests or validation steps when available.
- Do not claim that a feature works without verifying it as far as the available environment allows.

## Debugging

When debugging:

- First identify the actual source of the problem instead of immediately rewriting code.
- Inspect console errors, stack traces, object references, serialized fields, scene configuration, and Unity lifecycle timing where relevant.
- Verify Unity-specific behaviour against the official documentation.
- Prefer a targeted fix over a broad refactor.
- Avoid suppressing errors without resolving their cause.
- Clearly distinguish confirmed causes from hypotheses.
- Explain the reason for the fix and any remaining risks.

## Scope and safety

- Do not modify scenes, prefabs, project settings, packages, or generated files unless the task requires it.
- Ask before making destructive or wide-reaching changes.
- Preserve existing behaviour unless the task explicitly requests a change.
- Do not remove code merely because it appears unused without checking its references and purpose.
- Do not expose secrets, credentials, keys, or local machine-specific information.
