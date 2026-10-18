# Task backlog

Work items after the 10-phase roadmap (see `features.md`). Status: `[ ]` todo, `[~]` in progress, `[x]` done. Update this file when a task lands or the plan changes. Order matters: the recommended path is Phase 11, then Phase 12, then Phase 13.

## Phase 11: Game polish and engine hardening (done)

Small, about one session. Fixes known gaps that affect game feel and that the editor will need.

- [x] **Render interpolation between fixed steps.** Add a previous-transform component, written by the fixed step and blended by the render systems with the frame alpha. Why: motion stutters when the fixed rate and refresh rate do not match.
- [x] **Logging.** A small `ILogger`-style abstraction in Core (no Silk.NET), with a console sink and an in-overlay log panel. Why: the editor and asset hot-reload both need it.
- [x] **Pause and menu.** Pause state in `Game`, a menu drawn from `Game.Hud()`.
- [x] **Moving platforms.** Physics2D support (carry riders), then use in the sample level.
- [x] **Checkpoints.** Respawn at the last checkpoint instead of restarting the level; rules stay in `GameSession`.
- [x] **Saved high scores.** Persist to a JSON file; unit-test the load/save path.

## Phase 12: Editor (done: 12a, 12b, 12c)

### 12a. Editor foundation (done)
- [x] Dockable ImGui layout (docking on; windows dock by dragging; the game still draws behind them to the whole window).
- [x] Scene hierarchy panel.
- [x] Click-to-select entities in the world.
- [x] Move gizmos.
- [x] Save and load scenes from the editor (flattens prefab instances; see 12c).
- [ ] Follow-ups noticed: render the game into a docked "Game" view panel; rotate/scale gizmos; multi-select; transform parents so the hierarchy can be a tree.

### 12b. Undo/redo (done)
- [x] Command stack that every inspector, gizmo and hierarchy edit goes through (tracked entity ids, gesture-level steps; see architecture).
- [ ] Follow-ups: a History window listing the steps; make Load/Reload undoable (today they clear the history); undo for edits made while the F1 debug inspector is used outside the editor.

### 12c. Authoring tools
- [x] Tile painting (palette, stroke undo, layer save; single-tile brush only: no fill, rectangle or multi-tile brushes yet).
- [x] Prefab-aware saving: instances are saved as prefab + overrides (`PrefabLink`, `JsonMerge.Diff`). Still open: "Save entity as prefab", revert-to-prefab, and showing which fields are overridden in the inspector.
- [x] Play/stop that snapshots and restores the scene (F3).
- [x] Hot-reload of assets (`AssetHotReloader`; consumers that keep the old object are not refreshed).

## Phase 13: 3D (done: 13a-d; follow-ups listed per item and below)

- [x] Mesh and material system, depth buffer, `Camera3D` (13a: `Engine.Rendering3D`, `samples/Scene3D`). Follow-ups: draw sorting and transparency, instancing, a 3D editor view with picking and gizmo, transform parents.
- [x] Basic lighting (13b: ambient, one sun, point lights). Follow-ups: shadows (shadow maps), normal maps, spot lights.
- [x] Model loading (glTF, 13c): `.gltf`/`.glb` import with node hierarchy, materials and embedded textures; follow-ups: skins and animation, sparse accessors, KHR extensions, normal/metallic-roughness maps, instancing identical models, a model hot-reload check.
- [x] 3D physics decision (13d): write our own, as `Engine.Physics3D`, in stages (reasons in `architecture.md`). Stage 1 built: boxes against static boxes, friction, raycasts. Next stages: body-vs-body, capsule and sphere colliders, oriented boxes, mesh colliders, rigid-body rotation.

### What to do after phase 13 (pick by what the next game needs)

- [ ] **3D editor view**: render the scene in a docked viewport with an orbit camera, 3D picking (`Camera3D.ScreenPointToRay` + `Raycast` or mesh bounds), and move/rotate/scale gizmos. Needs transform parents for a useful hierarchy.
- [ ] **3D rendering depth**: shadow maps for the sun, normal maps, transparency with sorted draws, instancing, skinned animation and glTF animation, PBR shading.
- [ ] **3D physics stage 2**: dynamic-vs-dynamic boxes, then a capsule character controller.

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
