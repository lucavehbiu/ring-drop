# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ring Drop is a 3D mobile game built in **Unity 6** with **URP** (Universal Render Pipeline). A ring flies through space toward a stick; the player steers and times the release to drop the ring onto the stick. There is also a standalone `ring-drop.html` Three.js web version (reference implementation, not connected to Unity).

## Engine & Tech Stack

- **Unity 6000.3.7**, URP 17.3.0, C# scripting
- **New Input System** 1.18.0 (touch + keyboard + mouse)
- **Cinemachine 3.1.3** — virtual cameras for each game state, smooth blending via CinemachineBrain
- **Unity Physics** — Rigidbody + Colliders + PhysicsMaterials for ring physics (gravity, bounce, collisions)
- **No prefabs** — entire scene is bootstrapped procedurally from `SceneBootstrap.cs`
- **OnGUI-based UI** — temporary; planned migration to Canvas/UI Toolkit
- Single scene: `Assets/Scenes/SampleScene.unity`

## Build & Run

Open the project in Unity 6 (Hub → Open). Press Play in the editor to test. There are no custom build scripts — use standard Unity build pipeline (File → Build Settings).

## Architecture

### Physics System
Ring uses `Rigidbody` with `MeshCollider` (convex torus). Gravity, bouncing, and collisions are handled natively by Unity physics:
- `PhysicsMaterial` on ring: bounciness 0.35, friction 0.5
- `PhysicsMaterial` on ground: bounciness 0.2, friction 0.6
- Ground is tagged "Ground" — ring detects landing via `OnCollisionEnter`
- Stick has native cylinder/sphere colliders for physical ring-stick interaction
- Player input applies forces via `Rigidbody.AddForce` (lift, steering, wind)

### Camera System (Cinemachine)
- `CinemachineBrain` on main camera (1.2s EaseInOut blending)
- `CM_Playing` — CinemachineFollow + CinemachineRotationComposer, follows ring
- `CM_Menu` — static with gentle float animation
- `CM_Success` — diagonal angle on stick with CinemachineFollow offset
- Camera switching: priority-based via `CameraFollow.OnStateChanged()`

### Singletons
- `GameManager.Instance` — central state machine, owns score/level/combo
- `GameInput.Instance` — unified input (touch zones use golden ratio: 19.1% sides, 61.8% center)
- `SFXManager.Instance` — procedural audio (runtime-generated AudioClips)

### Game State Flow
```
Menu → Countdown (2.5s) → Playing → Success/Fail → GameOver
```
States are managed in `GameManager.Update()`. State changes fire `OnStateChanged` UnityEvent, which triggers camera switching.

### Core Mechanic
- Ring flies forward at constant speed (Z velocity set directly)
- Player **holds** to rise (AddForce up), **releases** to let gravity pull ring down
- Player **steers** left/right (AddForce horizontal)
- When ring hits the ground (OnCollisionEnter with "Ground" tag): check if stick base is within the ring hole
- Success = distance from ring center to stick < holeRadius * tolerance
- Fail = ring hits ground but stick is NOT inside ring hole

### Scene Bootstrap (no prefabs)
`SceneBootstrap.Start()` creates ALL game objects procedurally: ring (torus mesh + Rigidbody + MeshCollider), stick (cylinder primitives with colliders), ground (Plane with PhysicsMaterial + "Ground" tag), Cinemachine cameras, lights, starfield, and wires up all components.

### Script Organization

**Core** (`Assets/Scripts/Core/`):
- `GameManager.cs` — state machine, scoring, level transitions, ring freeze/unfreeze
- `LevelConfig.cs` — `LevelData` struct + `LevelConfig.Get(level)` static generator for progressive difficulty
- `SceneBootstrap.cs` — procedural scene creation (Rigidbody, Colliders, PhysicsMaterials, Cinemachine)
- `SFXManager.cs` — procedural audio singleton (fail/success sounds generated at runtime)
- `ScoreManager.cs` — PlayerPrefs persistence wrapper (legacy, unused)

**Gameplay** (`Assets/Scripts/Gameplay/`):
- `RingController.cs` — Rigidbody-based ring: AddForce for lift/steer, OnCollisionEnter for ground detection
- `StickController.cs` — procedural stick with colliders + guide bands, `CreateStick()` static factory
- `CameraFollow.cs` — Cinemachine virtual camera setup and state-based switching
- `GameInput.cs` — New Input System polling: `IsHolding`, `SteerDirection`, `WasTapped`
- `WindSystem.cs` — sinusoidal base wind + random gusts (legacy, wind now inline in RingController)

**Util** (`Assets/Scripts/Util/`):
- `Constants.cs` — force magnitudes, physics material values, geometry, colors. Single source of truth.
- `GoldenRatio.cs` — phi-based design system (spacing, typography, touch zones)
- `TorusMeshGenerator.cs` — procedural torus mesh (48x24 segments)
- `MathHelpers.cs` — `Remap`, `SmoothDamp01`, `HorizontalDistance`

**UI** (`Assets/Scripts/UI/`):
- `UIManager.cs` — OnGUI-based HUD: score, level, combo, feedback text

### Key Patterns

- **All tuning constants live in `Constants.cs`** — do not scatter magic numbers in gameplay code
- **Level difficulty is purely formula-driven** in `LevelConfig.Get(int level)` — no data files
- **Ring forces applied in `FixedUpdate`** via Rigidbody.AddForce, camera managed by Cinemachine
- **Materials use URP Lit shader** with emission: `Shader.Find("Universal Render Pipeline/Lit")`
- **Unity 6 API**: uses `linearVelocity` (not `velocity`), `PhysicsMaterial` (not `PhysicMaterial`), `linearDamping`/`angularDamping`

## Not Yet Built

Enemy ships, particles (speed streaks, bursts), CRT post-processing, haptics, Canvas UI, tutorial, app icon/splash, production mobile builds. See `PLAN.md` for the full roadmap.
