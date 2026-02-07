# RING DROP — Mobile App: Engine Decision & Build Plan

> Written like a Technical Director would write it. No diplomacy. No filler.

---

## CURRENT STATUS (Feb 2026)

### What's Built & Working

The Unity 6 LTS project is up and running with all core gameplay:

**Core (6 files):**
- `GameManager.cs` — State machine: Menu → Countdown → Playing → **Threading** → Success/Fail → GameOver
- `LevelConfig.cs` — Progressive difficulty: stick distance, speed, gravity, tolerance, wind, ships
- `SceneBootstrap.cs` — Procedural scene: all game objects created from code (no prefabs yet)
- `ScoreManager.cs` — PlayerPrefs-based score persistence

**Gameplay (5 files):**
- `RingController.cs` — Three-phase ring:
  - **Flying** (Playing): gravity + lift + wind + steering + slow-mo near stick
  - **Threading** (new): ring hovers above stick, player has 3s to align and tap to drop
  - **Success**: physics-based freefall down stick, bounce with restitution, spring-damper wobble
- `CameraFollow.cs` — Three camera modes:
  - **Chase cam** (Playing): smooth lerp follow, barrel roll, FOV speed effect
  - **Top-down** (Threading): near-vertical view above stick for precision alignment
  - **Orbit** (Success): slow orbit around stick watching ring fall
- `StickController.cs` — Procedural stick with guide bands
- `GameInput.cs` — New Input System 1.18.0: keyboard/mouse/touch + WasTapped for threading drop
- `WindSystem.cs` — Sinusoidal wind + random gusts

**UI (1 file):**
- `UIManager.cs` — OnGUI-based: score, level, combo, feedback text ("DROP IT!", "GOOD JOB!", etc.), threading countdown timer with color pulse (green→gold→red), alignment guide arrows

**Util (4 files):**
- `Constants.cs` — All tuning values (physics, threading, camera, colors)
- `TorusMeshGenerator.cs` — Procedural torus mesh (48×24 segments)
- `GoldenRatio.cs` — φ-based design system (touch zones, spacing, typography)
- `MathHelpers.cs` — Math utilities

**Visual:**
- 300-star combined mesh starfield (single draw call)
- 4 nebula point lights (purple, blue, magenta, teal)
- Exponential fog (0.008 density)
- 2 directional lights + ring point light
- Semi-reflective ground plane

### Game Flow (current)

```
MENU → tap → COUNTDOWN (2.5s) → PLAYING (fly toward stick)
  → ring enters threading range (6 units from stick)
  → THREADING: camera goes top-down, 3s countdown, steer to align
    → tap to drop + aligned → SUCCESS (ring falls down stick, bounces, settles) → next level
    → tap to drop + misaligned → FAIL → GAME OVER
    → 3s timeout → FAIL → GAME OVER
  → hit ground during flight → FAIL → GAME OVER
```

### What's NOT Built Yet (from original plan)

- [ ] Enemy ships (ShipSpawner, ShipController, CollisionDetector)
- [ ] Speed streaks, success burst particles, explosions
- [ ] CRT post-processing overlay (scanlines + vignette)
- [ ] Procedural audio (chiptune oscillator synthesis)
- [ ] Haptic feedback
- [ ] Canvas UI (currently using OnGUI — works but not production)
- [ ] Tutorial hints
- [ ] App icon, splash screen
- [ ] iOS/Android production builds
- [ ] Online leaderboard
- [ ] Ads/monetization

### Git

- **Repo:** https://github.com/lucavehbiu/ring-drop
- **Branch:** main
- **Note:** Force push needed from local terminal (`git push --force origin main`)

---

## THE VERDICT

### **Use Unity. Here's why.**

Ring Drop is a **real-time 3D game** that requires:
- Precise physics (gravity, wind, velocity damping, collision detection)
- Constant 60fps with no drops
- Sub-16ms input latency (hold = rise, release = fall — timing is the gameplay)
- Procedural spawning (ships, stars, nebulae)
- Adaptive audio (chiptune oscillator synthesis)
- Particle systems (500+ stars, 200 speed streaks, burst/explosion effects)
- Dynamic camera (follow, barrel roll, FOV shifts, slow-motion)

This is not a product visualizer. This is not a 3D-enhanced business app. This is a **game**, and it needs a **game engine**.

---

## HEAD-TO-HEAD: THE BRUTAL TRUTH

### Performance (the thing that actually matters)

| Metric | R3F + Expo | Unity | Godot |
|---|---|---|---|
| **FPS (real device)** | 30-60 variable | 60+ locked | 60+ (stylized 3D) |
| **Input latency** | 100-300ms | 16-33ms | 16-33ms |
| **Physics perf** | JS thread, 10x slower | PhysX 5.6 native | Jolt (AAA-grade) |
| **Draw calls** | JS bridge serialization | IL2CPP native | Native compiled |
| **Memory mgmt** | Manual .dispose(), GC fails on Android | Automated | Manual but manageable |
| **Shipped 3D mobile games** | ZERO commercial | 71% of top 1000 mobile games | Growing, mostly 2D |

### Developer Experience

| Metric | R3F + Expo | Unity | Godot |
|---|---|---|---|
| **Language** | TypeScript (you know it) | C# (learn in 2-3 months) | GDScript (learn in 2-3 weeks) |
| **Learning curve** | Low (React dev) | Moderate | Moderate-Low |
| **Dev iteration speed** | Fast (hot reload) | Moderate (editor + build) | Fast (editor + run) |
| **Asset pipeline** | Complex, fragile | Excellent | Good |
| **Community/ecosystem** | Small for games | Massive | Growing fast |
| **IDE/Editor** | VS Code | Unity Editor (visual) | Godot Editor (visual) |

### Business & Deployment

| Metric | R3F + Expo | Unity | Godot |
|---|---|---|---|
| **Price** | Free | Free under $200K rev | Free forever (MIT) |
| **iOS deploy** | EAS Build | Direct to Xcode/TestFlight | Requires macOS + Xcode |
| **Android deploy** | EAS Build | Direct APK/AAB | Direct APK/AAB |
| **Build size** | Large (JS runtime) | 25-50MB typical | Under 15MB |
| **App Store proven** | Rare | Industry standard | Growing |
| **Trust/stability** | React ecosystem stable | Damaged (runtime fee drama) but recovering | Rock solid (MIT, non-profit) |

---

## WHY NOT R3F + EXPO (killing my own earlier plan)

Let me be straight: the plan I wrote earlier was wrong for this game. Here's the kill list:

1. **100-300ms input latency** — Ring Drop's core mechanic is "hold = rise, release = fall." At 100ms+ latency, the player can't feel the gravity. The game is unplayable.

2. **Zero shipped 3D games** — Not one commercially successful 3D mobile game has shipped on R3F + React Native. Zero. That's not a gap, that's a red flag.

3. **JS bridge bottleneck** — Every frame: touch events cross the bridge → JS processes game logic → draw calls cross bridge back. At 60fps, that's 120+ bridge crossings per second. On iPhone 6S: 5-7 FPS.

4. **expo-gl uses deprecated OpenGL ES on iOS** — Apple deprecated it. It works through a Metal adapter. Double maintenance, fragile.

5. **Memory leaks on Android** — Three.js objects don't get garbage collected properly on React Native. Long play sessions = OOM crash.

6. **Texture/shader fragility** — KTX2 textures fail. GLTF textures fail. Custom shaders need platform hacks. Expo SDK version mismatches break things.

R3F is great for **web games** (the HTML version we already built runs perfectly in a browser). But wrapping it in React Native for mobile is putting a suit on a fish.

---

## WHY NOT GODOT (the tempting alternative)

Godot 4.6 is legitimately impressive. Jolt physics, MIT license, tiny builds. But:

1. **Plugin maintenance crisis** — iOS/Android official plugins are outdated and unmaintained. Google Play Billing plugin doesn't support the latest Google Play Billing Library.

2. **C# mobile is experimental** — No Android bindings (SSL crashes), iOS NativeAOT trimming breaks reflection. You're forced into GDScript.

3. **3D mobile still catching up** — Works for stylized/simple 3D. Ring Drop's 500 streaming stars + 12 nebulae + 200 speed streaks + 12 enemy ships + particles + dynamic fog will push it.

4. **Export bugs** — Black screens, crash-on-launch inconsistencies, Xcode failures on M4 Macs, export dialog size bugs.

5. **Ecosystem gap** — Analytics, ads, monetization SDKs need custom GDExtension work.

Godot is the **future** for indie games. But for a polished 3D mobile game shipping in 2026 that needs to actually work on real devices? Not quite there yet.

---

## WHY UNITY (despite the drama)

1. **60fps locked** — URP mobile renderer, Tile-Based Deferred Rendering, GPU optimization. Ring Drop's scene complexity is trivial for Unity.

2. **16ms input latency** — Native touch + accelerometer. When you lift your finger, the ring falls *immediately*. That's the game.

3. **PhysX 5.6** — Overkill for our needs, but it means physics is a non-issue. Gravity, collision, wind — all handled natively.

4. **Procedural geometry is well-supported** — Mesh class, Burst compiler, Job System. Our stars, nebulae, and particles? Standard Unity work.

5. **Audio** — Built-in AudioSource + code-generated clips. Can synthesize our chiptune sounds in C#.

6. **Proven at scale** — 71% of top 1000 mobile games. The deployment pipeline is battle-tested.

7. **Free tier** — $0 until $200K annual revenue. Ring Drop is not hitting $200K anytime soon.

8. **C# from TypeScript** — Same family. Strict typing, classes, async/await. A TypeScript dev learns C# in 8-12 weeks productively.

### Unity's baggage (being honest):

- **Trust damage** — Runtime fee fiasco. Walked back, but scars remain.
- **Layoffs** — 3,200+ jobs cut. Reduced roadmap velocity.
- **Price creep** — 5% annual increases on Pro tier.
- **Security vulnerability** — CVE-2025-59489 affecting all versions since 2017. Patched, but ugly.
- **IL2CPP build times** — 10-30 minutes for a full iOS build. Slow iteration.

These are real concerns. But none of them affect the *game running on a phone at 60fps with instant input response*. That's what matters.

---

## THE UPDATED PLAN: UNITY BUILD

### Tech Stack

| Layer | Choice |
|---|---|
| **Engine** | Unity 6 LTS (latest) |
| **Render Pipeline** | URP (Universal Render Pipeline) |
| **Scripting** | C# with IL2CPP for production |
| **Physics** | Built-in PhysX (simple Rigidbody + custom forces) |
| **Audio** | Unity AudioSource + procedural tone generation |
| **Input** | New Input System (touch + accelerometer + keyboard) |
| **UI** | Unity UI Toolkit or TextMeshPro for HUD |
| **Particles** | VFX Graph (for stars/streaks) + Particle System (bursts) |
| **Build** | Unity Cloud Build or local Xcode/Gradle |
| **Version Control** | Git + GitHub (existing repo) |
| **Testing** | Unity Test Framework + Play Mode tests |

### Project Structure

```
ring-drop-unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          # State machine (MENU→PLAYING→etc)
│   │   │   ├── LevelConfig.cs          # Level progression config
│   │   │   ├── ScoreManager.cs         # Score, combo, high score
│   │   │   └── AudioManager.cs         # Procedural chiptune + SFX
│   │   │
│   │   ├── Gameplay/
│   │   │   ├── RingController.cs       # Ring physics + input response
│   │   │   ├── StickController.cs      # Stick placement + guide bands
│   │   │   ├── ShipSpawner.cs          # Enemy ship spawning + AI
│   │   │   ├── ShipController.cs       # Individual ship behavior
│   │   │   ├── WindSystem.cs           # Wind + gust simulation
│   │   │   ├── CollisionDetector.cs    # Ring-stick, ring-ship collisions
│   │   │   └── CameraFollow.cs         # Follow + barrel roll + FOV
│   │   │
│   │   ├── Visual/
│   │   │   ├── GalaxyGenerator.cs      # Streaming stars + nebulae
│   │   │   ├── SpeedStreaks.cs         # Speed streak particles
│   │   │   ├── BurstEffect.cs         # Success/explosion bursts
│   │   │   ├── RunwayGenerator.cs      # Ground grid + lane markers
│   │   │   ├── FogController.cs        # Dynamic fog color shifting
│   │   │   └── CRTOverlay.cs          # Scanlines + vignette shader
│   │   │
│   │   ├── UI/
│   │   │   ├── HUDController.cs        # Score, level, distance bar
│   │   │   ├── AlignmentHUD.cs         # X/Y alignment bars
│   │   │   ├── StartScreen.cs          # Title + tap to start
│   │   │   ├── GameOverScreen.cs       # Fail/retry screen
│   │   │   ├── LevelAnnounce.cs        # Level transition overlay
│   │   │   └── WarningDisplay.cs       # Ship proximity warning
│   │   │
│   │   └── Util/
│   │       ├── GoldenRatio.cs          # φ-based design constants
│   │       ├── MathHelpers.cs          # Lerp, clamp, distance
│   │       └── Constants.cs            # Game-wide constants
│   │
│   ├── Prefabs/
│   │   ├── Ring.prefab
│   │   ├── Stick.prefab
│   │   ├── EnemyShip.prefab
│   │   └── Particles/
│   │       ├── StarField.prefab
│   │       ├── SuccessBurst.prefab
│   │       └── Explosion.prefab
│   │
│   ├── Materials/
│   │   ├── NeonCyan.mat               # Ring material (emissive)
│   │   ├── NeonMagenta.mat            # Stick material (emissive)
│   │   ├── GalaxyStars.mat            # Point cloud material
│   │   ├── Nebula.mat                 # Transparent BackSide
│   │   ├── EnemyShip.mat              # Red emissive wireframe
│   │   └── CRTOverlay.mat            # Post-processing overlay
│   │
│   ├── Shaders/
│   │   ├── CRTScanlines.shader        # Scanlines + vignette
│   │   ├── NeonGlow.shader            # Emissive bloom helper
│   │   └── SpaceBackground.shader     # Dynamic galaxy background
│   │
│   ├── Fonts/
│   │   └── PressStart2P-Regular.ttf
│   │
│   ├── Scenes/
│   │   ├── MainGame.unity
│   │   └── LoadingScreen.unity
│   │
│   └── Resources/
│       └── (audio clips, if pre-recorded)
│
├── Packages/
│   └── manifest.json
│
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   ├── QualitySettings.asset          # URP mobile quality levels
│   └── InputSystem.inputsettings.asset
│
├── Tests/
│   ├── EditMode/
│   │   ├── PhysicsTests.cs
│   │   ├── LevelConfigTests.cs
│   │   └── CollisionTests.cs
│   └── PlayMode/
│       ├── GameStartTests.cs
│       ├── InputResponseTests.cs
│       └── LevelProgressionTests.cs
│
├── .gitignore                          # Unity-specific gitignore
├── PLAN.md                             # This file
└── ring-drop.html                      # Original Three.js reference
```

### Golden Ratio Design System (same as before, ported to C#)

```csharp
public static class GoldenRatio
{
    public const float PHI = 1.6180339887f;

    // Spacing (base: 8)
    public const float XS = 5f;    // 8/φ
    public const float SM = 8f;    // base
    public const float MD = 13f;   // 8×φ
    public const float LG = 21f;   // 13×φ
    public const float XL = 34f;   // 21×φ
    public const float XXL = 55f;  // 34×φ

    // Typography (base: 12)
    public const float FONT_CAPTION = 7f;
    public const float FONT_SMALL = 10f;
    public const float FONT_BODY = 12f;
    public const float FONT_SUBTITLE = 19f;
    public const float FONT_TITLE = 31f;
    public const float FONT_HERO = 50f;

    // Layout
    public const float MAJOR = 0.618f;  // 1/φ
    public const float MINOR = 0.382f;  // 1 - 1/φ

    // Touch zones
    public const float ZONE_SIDE = 0.191f;   // MINOR/2
    public const float ZONE_CENTER = 0.618f;  // MAJOR
}
```

### Implementation Phases

#### Phase 1: Setup (Day 1-2)
- [ ] Install Unity 6 LTS + URP template
- [ ] Create project with Mobile 3D preset
- [ ] Configure URP quality settings for mobile (MSAA 2x, no HDR, shadows off)
- [ ] Import TextMeshPro + Press Start 2P font
- [ ] Set up New Input System with touch + keyboard actions
- [ ] Create the GoldenRatio.cs + Constants.cs utility classes
- [ ] Git setup: .gitignore for Unity, push initial project structure
- [ ] Verify builds: iOS Simulator + Android emulator

#### Phase 2: Core Mechanics (Day 3-5)
- [ ] RingController: gravity, lift (hold=rise), horizontal steering, velocity clamping
- [ ] StickController: place at Z distance, guide bands (green torus at valid Y range)
- [ ] CameraFollow: smooth follow with lerp, FOV speed effect, barrel roll on horizontal
- [ ] GameManager state machine: MENU → COUNTDOWN → PLAYING → SUCCESS → FAIL → GAMEOVER
- [ ] LevelConfig: distance, speed, gravity, tolerance, wind, ship count per level
- [ ] CollisionDetector: ring-reaches-stick (check alignment), ring-hits-ground
- [ ] ScoreManager: score, combo multiplier, high score (PlayerPrefs)
- [ ] Input: screen thirds (left=steer left, center=rise, right=steer right)
- [ ] Slow-motion when near stick (Time.timeScale or custom speed multiplier)

#### Phase 3: Galaxy Visual (Day 6-7)
- [ ] GalaxyGenerator: 500 streaming star particles (GPU Instancing or VFX Graph)
  - 10 galaxy color palettes, sizeAttenuation, recycle behind camera
- [ ] Nebula clouds: 12 transparent spheres with pulsing opacity
- [ ] Space dust: 100 lateral floating particles with drift
- [ ] SpeedStreaks: 200 colored particles streaming past
- [ ] FogController: slowly shifting fog color (HSL rotation)
- [ ] Galaxy ambient lights: two colored point lights that drift
- [ ] RunwayGenerator: subtle ground grid + lane markers
- [ ] CRT post-processing: scanlines + vignette (URP Renderer Feature or overlay shader)

#### Phase 4: Enemy Ships (Day 8)
- [ ] ShipSpawner: spawn along flight path based on level config
- [ ] ShipController: fly toward player, wobble, engine glow
- [ ] Collision: distance < 0.9 = hit (explosion), < 1.8 = near miss (warning)
- [ ] BurstEffect: success burst (cyan/green) + ship explosion (red/orange)
- [ ] Screen flash: white for success, red for hit (post-processing volume or UI overlay)

#### Phase 5: Audio (Day 9)
- [ ] AudioManager singleton with procedural tone generation
- [ ] Port all sounds: thrust, success, fail, level up, explosion, near miss, wind
- [ ] C# AudioClip generation: oscillator (square, sawtooth, sine) + gain envelope
- [ ] Haptic feedback: Handheld.Vibrate() patterns for hit, success, near miss

#### Phase 6: UI/HUD (Day 10-11)
- [ ] HUD: score, level, high score (golden ratio positioning)
- [ ] Distance bar: progress fill with gradient
- [ ] Alignment HUD: X/Y bars (green/yellow/red)
- [ ] Start screen: RING DROP title + FLY DODGE THREAD + TAP TO START
- [ ] Game over: fail message, final score, level, retry
- [ ] Level announce: LEVEL X with countdown
- [ ] Warning: ship proximity flash
- [ ] Combo display: Nx COMBO popup

#### Phase 7: Polish & Test (Day 12-14)
- [ ] Performance profiling on real devices (Unity Profiler, Frame Debugger)
- [ ] Target: locked 60fps, < 150MB memory, < 50MB build
- [ ] Object pooling for ships, particles, burst effects
- [ ] GPU instancing for stars and speed streaks
- [ ] Edit Mode tests: physics math, level config, collision detection
- [ ] Play Mode tests: game starts, input works, level progression
- [ ] Test on: iPhone 12+, Pixel 6+, older devices (iPhone 11, Pixel 4a)
- [ ] Tutorial hints for first play

#### Phase 8: Ship It (Day 15)
- [ ] App icon: ring + stick silhouette, neon cyan on deep space
- [ ] Splash screen: animated ring approaching stick
- [ ] iOS: Archive → TestFlight
- [ ] Android: AAB → Google Play internal track
- [ ] Git tag v1.0.0

---

## WHAT HAPPENS TO THE HTML VERSION?

Keep it. It's the web version. Serve it on GitHub Pages or itch.io. Players who find the game on web → funnel to the mobile app. Two distribution channels, one game.

```
ring-drop.html     → Web (browsers, itch.io, GitHub Pages)
ring-drop-unity/   → Mobile (App Store, Google Play)
```

---

## OPEN DECISIONS

| # | Question | My Lean |
|---|---|---|
| 1 | **Tilt controls?** | Opt-in. Touch is primary. Tilt is novelty that some players love. |
| 2 | **Online leaderboard?** | v1 local only. v2 add Firebase/PlayFab if there's traction. |
| 3 | **Monetization?** | Free + interstitial ads between levels (Unity Ads). No paywalls. |
| 4 | **Sound approach?** | Procedural C# oscillators, same as the HTML version. No .wav files. |
| 5 | **Unity version?** | Unity 6 LTS. Stable, long-term support, URP mature. |

---

## RISK REGISTER

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Unity raises prices again | Medium | Low (free tier) | Stay under $200K; evaluate Godot as backup |
| C# learning curve slows dev | Medium | Medium | TypeScript→C# is natural; 2-3 months max |
| IL2CPP build times frustrate | High | Low | Develop in Mono, production build in IL2CPP |
| Performance issues on old devices | Low | Medium | Profile early, set min spec (iPhone 11, Pixel 4a) |
| Godot catches up and Unity becomes unnecessary | Medium | Low | Game is shipped; engine can be reevaluated for v2 |

---

## SOURCES

### Unity
- [Unity 6 Features](https://unity.com/blog/unity-6-features-announcement)
- [URP Mobile Optimization](https://unity.com/features/srp/universal-render-pipeline)
- [Unity Pricing 2026](https://unity.com/products/pricing-updates)
- [PhysX 5.6 Integration](https://unity.com/solutions/programming-physics)
- [Input System Sensors](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Sensors.html)
- [Procedural Mesh Generation](https://docs.unity3d.com/Manual/GeneratingMeshGeometryProcedurally.html)

### Godot
- [Godot 4.6 Release](https://godotengine.org/article/dev-snapshot-godot-4-6-beta-3/)
- [Jolt Physics Integration](https://www.live-laugh-love.world/blog/godot-46-complete-guide-2026/)
- [GDScript vs C#](https://chickensoft.games/blog/gdscript-vs-csharp)
- [Godot Mobile Export Issues](https://toxigon.com/godot-4-mobile-game-development)

### R3F + React Native (why not)
- [R3F Scaling Performance](https://r3f.docs.pmnd.rs/advanced/scaling-performance)
- [expo-gl Limitations](https://docs.expo.dev/versions/latest/sdk/gl-view/)
- [React Native Performance](https://reactnative.dev/docs/performance)
- [Zero Shipped Games Evidence](https://github.com/EvanBacon/pillar-valley)
