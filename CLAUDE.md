# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ring Drop is a 3D mobile game built in **Unity 6 LTS** with **URP** (Universal Render Pipeline). A ring flies through space toward a stick; the player steers, aligns, and drops the ring onto the stick. There is also a standalone `ring-drop.html` Three.js web version (reference implementation, not connected to Unity).

## Engine & Tech Stack

- **Unity 6 LTS**, URP 17.3.0, C# scripting
- **New Input System** 1.18.0 (touch + keyboard + mouse)
- **No prefabs** — entire scene is bootstrapped procedurally from `SceneBootstrap.cs`
- **No physics engine usage** — all physics (gravity, lift, wind, collision) are custom in code
- **OnGUI-based UI** — temporary; planned migration to Canvas/UI Toolkit
- Single scene: `Assets/Scenes/SampleScene.unity`

## Build & Run

Open the project in Unity 6 LTS (Hub → Open). Press Play in the editor to test. There are no custom build scripts — use standard Unity build pipeline (File → Build Settings).

## Architecture

### Singletons
- `GameManager.Instance` — central state machine, owns score/level/combo
- `GameInput.Instance` — unified input (touch zones use golden ratio: 19.1% sides, 61.8% center)

### Game State Flow
```
Menu → Countdown (2.5s) → Playing → Threading (3s drop window) → Success/Fail → GameOver
```
States are managed in `GameManager.Update()`. State changes fire `OnStateChanged` UnityEvent.

### Scene Bootstrap (no prefabs)
`SceneBootstrap.Start()` creates ALL game objects procedurally: ring (torus mesh), stick (cylinder primitives), lights, starfield (300 combined-mesh quads), ground, and wires up all components. Components find each other via `FindAnyObjectByType<T>()`.

### Script Organization

**Core** (`Assets/Scripts/Core/`):
- `GameManager.cs` — state machine, scoring, level transitions
- `LevelConfig.cs` — `LevelData` struct + `LevelConfig.Get(level)` static generator for progressive difficulty
- `SceneBootstrap.cs` — procedural scene creation
- `ScoreManager.cs` — PlayerPrefs persistence wrapper

**Gameplay** (`Assets/Scripts/Gameplay/`):
- `RingController.cs` — three-phase ring: Playing (fly), Threading (hover+steer), Success (freefall+bounce+wobble)
- `StickController.cs` — procedural stick with guide bands, `CreateStick()` static factory
- `CameraFollow.cs` — state-dependent camera: chase (Playing), top-down (Threading), orbit (Success)
- `GameInput.cs` — New Input System polling: `IsHolding`, `SteerDirection`, `WasTapped`
- `WindSystem.cs` — sinusoidal base wind + random gusts

**Util** (`Assets/Scripts/Util/`):
- `Constants.cs` — ALL tuning values (physics, camera, threading, colors). Single source of truth.
- `GoldenRatio.cs` — phi-based design system (spacing, typography, touch zones)
- `TorusMeshGenerator.cs` — procedural torus mesh (48x24 segments)
- `MathHelpers.cs` — `Remap`, `SmoothDamp01`, `HorizontalDistance`

**UI** (`Assets/Scripts/UI/`):
- `UIManager.cs` — OnGUI-based HUD: score, level, combo, feedback text, threading countdown with color pulse

### Key Patterns

- **All tuning constants live in `Constants.cs`** — do not scatter magic numbers in gameplay code
- **Level difficulty is purely formula-driven** in `LevelConfig.Get(int level)` — no data files
- **Ring physics run in `FixedUpdate`**, camera follows in `LateUpdate`
- **Materials use URP Lit shader** with emission: `Shader.Find("Universal Render Pipeline/Lit")`
- **Threading phase**: ring enters when Z distance < 6 units from stick, 3-second countdown, player steers then taps to drop
- **Success animation**: real physics — freefall with gravity, bounce with restitution (0.45 * 0.35^n), spring-damper wobble, spin decay

## Not Yet Built

Enemy ships, particles (speed streaks, bursts), CRT post-processing, procedural audio, haptics, Canvas UI, tutorial, app icon/splash, production mobile builds. See `PLAN.md` for the full roadmap.
