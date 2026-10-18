# Task backlog

Work items after the 10-phase roadmap (see `features.md`). Status: `[ ]` todo, `[~]` in progress, `[x]` done. Update this file when a task lands or the plan changes. Order matters: the recommended path is Phase 11, then Phase 12, then Phase 13.

## Phase 11: Game polish and engine hardening (recommended next)

Small, about one session. Fixes known gaps that affect game feel and that the editor will need.

- [x] **Render interpolation between fixed steps.** Add a previous-transform component, written by the fixed step and blended by the render systems with the frame alpha. Why: motion stutters when the fixed rate and refresh rate do not match.
- [x] **Logging.** A small `ILogger`-style abstraction in Core (no Silk.NET), with a console sink and an in-overlay log panel. Why: the editor and asset hot-reload both need it.
- [x] **Pause and menu.** Pause state in `Game`, a menu drawn from `Game.Hud()`.
- [ ] **Moving platforms.** Physics2D support (carry riders), then use in the sample level.
- [x] **Checkpoints.** Respawn at the last checkpoint instead of restarting the level; rules stay in `GameSession`.
- [x] **Saved high scores.** Persist to a JSON file; unit-test the load/save path.

## Phase 12: Editor (largest piece, three sub-phases)

### 12a. Editor foundation
- [ ] Dockable ImGui layout.
- [ ] Scene hierarchy panel.
- [ ] Click-to-select entities in the world.
- [ ] Move gizmos.
- [ ] Save and load scenes from the editor.

### 12b. Undo/redo
- [ ] Command stack that every inspector and gizmo edit goes through. Decide this early: adding it later means rewriting every edit path.

### 12c. Authoring tools
- [ ] Tile painting.
- [ ] Prefab-aware editing.
- [ ] Play/stop that snapshots and restores the scene.
- [ ] Hot-reload of assets.

## Phase 13: 3D (after the editor, so 3D scenes can be inspected while built)

- [ ] Mesh and material system, depth buffer, `Camera3D`.
- [ ] Basic lighting.
- [ ] Model loading (glTF).
- [ ] 3D physics decision: write our own or integrate one.

## Alternatives (pick when they pay off sooner than the main path)

- [ ] **Rendering depth.** Light shadows and occluders, HDR targets (fixes the 8-bit light map clamp), pixel-perfect low-resolution mode. Pays off in the platformer's look and prepares 3D.
- [ ] **Tiled import and animated tiles.** Fastest way to build real levels before the editor exists. Also: tile flip flags, parallax, cached chunk meshes.
- [ ] **Audio streaming and positional audio.** Only worth it if music is wanted soon. Also: audio component, mixer groups.

## Smaller known gaps (from `features.md`, no owner yet)

- Spatial hash, one-way platforms and slopes in physics; bodies spawned inside solids are not pushed out.
- Entity references in saved components, scene-in-scene, instance-to-prefab links.
- Per-camera and per-layer post effects; MSAA; render-to-texture for game code.
- Animation events and blending; more than one transition per update.
- High-DPI is untested; GPU timing in the profiler.
