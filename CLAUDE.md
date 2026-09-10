# CRESCENTIA

A 2D side-scrolling action platformer built in Unity 6000.4.9f1.

## Conventions

- **Input**: legacy Input Manager (`Input.GetAxisRaw`, `Input.GetButtonDown`).
  The Input System package is installed and `activeInputHandler: 2` (both),
  but gameplay code uses the legacy API. Match that when adding input.
- **Layers**: `Ground` is the collision layer for anything the player stands on.
  The Player's `whatIsGround` mask is set to Ground only (bit 3). `Hazard` is a
  trigger-only layer for spikes.
- **Physics**: Rigidbody2D with `linearVelocity` (Unity 6 naming, not `velocity`).
- **Singleton**: `PlayerController.Instance` is set in `Awake` and read by
  `CameraFollow`.

## Scene structure (Assets/Scenes/Map1.unity)

- `Grid` with three Tilemap children:
  - `walls` — solid geometry. TilemapCollider2D + CompositeCollider2D (Merge),
    static Rigidbody2D, layer Ground.
  - `platforms` — one-way ledges. Same colliders plus PlatformEffector2D,
    layer Ground.
  - `hazards` — spikes. TilemapCollider2D as trigger, layer Hazard.
- `ref` — the hand-drawn map sketch used to trace the level. Sorting order -10,
  alpha ~0.4. Purely a tracing aid; safe to hide or delete.
- `Main Camera` — CameraFollow with clamped Min/Max Bounds.
- `Player`, `Spawnpoint`.

Map extents are roughly x -61..63, y -21..13. Camera bounds are those extents
inset by half the camera view (8.9 horizontal, 5 vertical at Size 5, 16:9).
Recompute them if the map grows or the camera Size changes.

## Working with the live Editor

The Unity MCP server is available. Prefer inspecting real Editor state over
reading `.unity`/`.prefab` YAML by hand:

- `Unity_RunCommand` runs C# in the Editor (class must be `internal class
  CommandScript : IRunCommand`).
- `Unity_GetConsoleLogs` reads the Console.
- `Unity_SceneView_Capture2DScene` screenshots a world-space region. Its
  `worldX`/`worldY` are the **bottom-left corner**, not the center.
