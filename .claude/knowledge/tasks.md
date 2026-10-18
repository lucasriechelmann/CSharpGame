# Task backlog

Work items after the 10-phase roadmap (see `features.md`). Status: `[ ]` todo, `[~]` in progress, `[x]` done. Update this file when a task lands or the plan changes. Order matters: phases 14 to 19 are listed below in the recommended order.

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
- [ ] Follow-ups noticed: render the game into a docked "Game" view panel; rotate/scale gizmos; multi-select (transform parents landed in phase 14).

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

## Phase 14: Transform parents and hierarchy (done)

- [x] `Parent` component, `TransformHierarchy` (world matrices from the chain, `SetParent` with loop check and `keepWorld`, subtree destroy), no cached matrices (reasons in `architecture.md`).
- [x] Render systems (sprites, 2D lights, meshes, models, 3D lights) and static physics solids honour parents; interpolation blends every level.
- [x] Scenes: `"parent"` index per entry, validated before load.
- [x] Editor: hierarchy tree, drag-to-reparent, "+ Child", subtree delete/duplicate, `ReparentCommand`, world-space gizmo and picking for children.
- [x] glTF keeps its node tree (`ModelData.Nodes`, `ModelPose`) for animation; drawing is still flattened.
- [ ] Follow-ups: a cached world matrix if deep trees show up in the profiler; moving physics bodies as children; rotate/scale gizmos use the same world/local conversion; `ModelRenderer` instances that expose per-node entities.

## Phase 15: 3D editor view (done)

- [x] Orbit-camera 3D view (`OrbitCamera`, `Game.SceneCamera3D`), 3D picking by oriented mesh bounds (`EntityPicking3D`), selection outlines, F to frame.
- [x] Move/rotate/scale gizmo for 2D and 3D (`TransformGizmo`, world or local axes, Ctrl snap), applied through `GizmoApply`, one undo step per gesture.
- [x] Multi-select (`EditorSelection`): Ctrl+click, group gizmo, group duplicate and delete.
- [ ] Not built: a docked viewport / game-view panel rendering into a `RenderTarget` (needs a viewport abstraction in `Game`; reasons in `architecture.md`).
- [ ] Follow-ups: triangle-accurate picking, box-select (marquee), gizmo handles for planes, pivot/centre toggle, camera bookmarks, showing physics colliders as wire boxes.

## Phase 16: 3D physics stage 2 (done)

- [x] Body-vs-body boxes: mass-weighted inelastic pushing, stacking (bodies stepped lowest first), contacts and rays that see bodies.
- [x] Sphere and capsule colliders (`CapsuleCollider3D`) that collide and slide; `CharacterController3D` + `CharacterControllerSystem3D` (walk, air control, coyote time, jump buffer); `MeshBuilder.Capsule` for the visual.
- [x] Oriented static boxes (`BoxCollider3D.Oriented`, `Obb3`, `Sat`): ramps; exact for capsules and rays, discrete push-out for box bodies.
- [ ] Follow-ups: rigid-body rotation and inertia (crates topple and roll), mesh/convex colliders, stepping up ledges and a slope limit for the character, a body riding a body or platform, swept tests against oriented solids, exact capsule-vs-capsule, sleeping, a spatial hash/BVH once static counts justify it, showing colliders as wireframes in the editor.

## Phase 17: 3D rendering depth

- [ ] Sun shadow maps.
- [ ] Normal maps.
- [ ] Transparency with sorted draws.
- [ ] Instancing.
- [ ] glTF animation and skins (uses `ModelData.Nodes` from phase 14).
- [ ] PBR shading.

## Phase 18: Level and content pipeline

- [ ] Tiled import, animated tiles, flip flags, parallax, cached chunk meshes.
- [ ] Editor extras: History window, save-entity-as-prefab, revert-to-prefab, fill/rectangle tile brushes.

## Phase 19: 2D rendering and audio depth

- [ ] Light shadows/occluders, HDR targets, pixel-perfect mode, per-camera effects, MSAA.
- [ ] Audio streaming, positional audio, audio component, mixer groups.

Fast path to a playable game instead: phase 16 then a small 3D game; or phase 18 for more platformer levels.

## Smaller known gaps (from `features.md`, no owner yet)

- Spatial hash, one-way platforms and slopes in physics; bodies spawned inside solids are not pushed out.
- Entity references in saved components, scene-in-scene, instance-to-prefab links.
- Per-camera and per-layer post effects; MSAA; render-to-texture for game code.
- Animation events and blending; more than one transition per update.
- High-DPI is untested; GPU timing in the profiler.
