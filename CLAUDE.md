# CRESCENTIA

A 2D side-scrolling action platformer built in Unity 6000.4.9f1.
Combat is Hollow Knight flavoured: four-direction attacks and a rally
(regain-on-hit) health system.

## Conventions

- **Input**: legacy Input Manager (`Input.GetAxisRaw`, `Input.GetButtonDown`).
  The Input System package is installed and `activeInputHandler: 2` (both),
  but gameplay code uses the legacy API. Match that when adding input.
- **Physics**: Rigidbody2D with `linearVelocity` (Unity 6 naming, not `velocity`).
- **Naming**: attack members are abbreviated `atk` (`atkDamage`, `atkInput`,
  `timeSinceLastAtk`, `sideAtkRange`). Computed positions are PascalCase
  expression-bodied properties (`SideAtkCenter`). Keep both conventions.
- **Singleton**: `PlayerController.Instance` is set in `Awake`. `CameraFollow`
  and `UpdateHpUI` both read it.
- **Inspector-tuned Vector3 bundles**: several systems pack three tuning values
  into one `Vector3` with a `[Tooltip]` explaining each axis, rather than three
  separate fields. See `atkOffset` and `rallySettings`. Read the tooltip before
  touching them; the axis meanings are not guessable.

## Scripts

### PlayerController.cs
Movement (walk, jump, coyote time, jump buffer, air jumps), four-direction
attack, HP, and rally. Ground check is three downward raycasts from
`groundCheckPoint` at x offsets `0`, `+groundCheckX`, `-groundCheckX`, against
the `whatIsGround` mask.

### Enemy.cs
`health`, `isImmortal`, and `Enemyhit(damage)`. Damages the player by 1 on
collision with a Player-tagged object. Destroys itself at zero health.

### UpdateHpUI.cs
Subscribes to `PlayerController.OnHealthChanged` and drives two UI Sliders,
`realHpBar` and `rallyHpBar`, both as a 0..1 fraction of max HP.

### CameraFollow.cs
Lerps to `PlayerController.Instance` plus an offset, clamped to Min/Max Bounds.

## Four-direction attack

Triggered by `Fire1`, gated by `timeSinceLastAtk >= atkDuration`.
Direction comes from the **Vertical axis at the moment of the press**:

| `yAxis` | Direction | Center | Range |
|---|---|---|---|
| `0` | Side (faces `transform.localScale.x`) | `SideAtkCenter` | `sideAtkRange` |
| `> 0` | Up | `UpAtkCenter` | `upAtkRange` |
| `< 0` | Down | `DownAtkCenter` | `downAtkRange` |

`atkOffset` packs the three offsets: **X** = side, **Y** = up, **Z** = down.
`Hit()` does `Physics2D.OverlapBoxAll` against `atkLayer` and calls
`Enemyhit(atkDamage)` on every collider carrying an `Enemy`.

Attack boxes draw as red wire cubes via `OnDrawGizmosSelected`, so select the
Player in the Scene view to tune ranges visually.

**Note for any down-input feature** (for example dropping through one-way
platforms): S already means "down attack" when combined with Fire1. Any new
down binding must not fire at the same time as an attack.

## Rally (regain-on-hit) health

Three values: `realHp` (actual), `rallyHp` (recoverable ceiling), `maxHp`.

- Taking damage lowers `realHp`. `rallyHp` stays high, then after a delay
  decays toward `realHp`, so recoverable HP drains if you do not fight back.
- Hitting an enemy raises `realHp` by `hpRegainStep`, capped at `rallyHp` and
  `maxHp`.
- `rallySettings` packs the timing: **X** = delay before decay starts,
  **Y** = HP lost per step, **Z** = seconds between steps.
- Every change calls `NotifyHealthChanged()`, which fires
  `OnHealthChanged(realHp, rallyHp, maxHp)`.

The UI reads this as two stacked bars: `realHpBar` is current health,
`rallyHpBar` is the ceiling you could win back.

## Layers

| Index | Name | Use |
|---|---|---|
| 3 | `Ground` | Anything the player stands on. `whatIsGround` is set to it. |
| 6 | `Attackable` | Enemy hitboxes. `atkLayer` is mask `64`, i.e. bit 6. |

Layer names live in `ProjectSettings/TagManager.asset` and merge badly. Two
branches adding a layer to the same empty slot produce a one-line conflict, and
resolving it by taking either side silently drops the other name. Masks store
the *index*, not the name, so a dropped name leaves combat working while the
Inspector shows a blank layer, which is easy to miss. This happened once in
`e7d5db4` and was restored afterwards. After any merge that touches
TagManager, confirm layer 6 still reads `Attackable`.

## Scenes

- **`Assets/Scenes/Map1.unity`** is the real level. A greybox blockout traced
  over a hand-drawn sketch. Structure:
  - `Grid` with two Tilemap children: `walls` (solid; TilemapCollider2D +
    CompositeCollider2D set to Merge, static Rigidbody2D, layer Ground) and
    `platforms` (same plus PlatformEffector2D for one-way ledges).
  - `ref` — the sketch used for tracing, sorting order -10, alpha ~0.4.
    Purely a tracing aid; safe to hide or delete.
  - `Main Camera`, `Player`, and an `Enemy` on the Attackable layer.
  - Map extents roughly x -61..63, y -21..13. Camera bounds are those extents
    inset by half the camera view (8.9 horizontal, 5 vertical at Size 5, 16:9).
    Recompute if the map grows or the camera Size changes.
- **`Assets/Scenes/SampleScene.unity`** is the upstream test bed: `GroundParent`,
  a single `Enemy`, and the HP `Canvas`. New combat features are prototyped
  here first. It is usually the scene open in the Editor.

## Working with the live Editor

The Unity MCP server is available. Prefer inspecting real Editor state over
reading `.unity`/`.prefab` YAML by hand:

- `Unity_RunCommand` runs C# in the Editor (class must be `internal class
  CommandScript : IRunCommand`).
- `Unity_GetConsoleLogs` reads the Console.
- `Unity_SceneView_Capture2DScene` screenshots a world-space region. Its
  `worldX`/`worldY` are the **bottom-left corner**, not the center.

Check which scene is actually open before diagnosing anything; Map1 and
SampleScene get swapped often.
