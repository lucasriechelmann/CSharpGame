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

## Phase 17: 3D rendering depth (done)

- [x] PBR shading (metallic-roughness, GGX) replacing Blinn-Phong; materials with metallic-roughness and normal maps, emissive, alpha modes; tangents generated from UVs; sky/ground reflection stand-in for image-based lighting.
- [x] Transparency with sorted draws (blend materials back to front, no depth write), cut-out (mask) materials.
- [x] Instancing: per-instance matrices, runs of identical mesh and material in one call.
- [x] Sun shadow map: fitted to the view, texel-snapped, PCF, cut-out aware, skinned meshes cast.
- [x] glTF skins and animation: `ModelAnimator`, `ModelAnimationSystem`, `AnimatedPose`, GPU skinning (64 joints), linear/step/cubic-spline channels.
- [ ] Follow-ups: cascaded shadow maps and point-light shadows, image-based lighting and a skybox, occlusion/emissive maps, blending between clips, animation events, morph targets, instancing of skinned meshes, reading a file's own TANGENT attribute, order-independent transparency.

## Phase 18: Level and content pipeline (done, except cached chunk meshes)

- [x] Tile flip flags (`TileId`), animated tiles (`TileSet.SetAnimation`), parallax layers, `SpriteFlip.Diagonal`.
- [x] Tiled `.tmj`/`.tsj` import (`TiledReader`, `TiledConverter`, `TiledSpawner`).
- [x] Editor: History window (click to jump), rectangle and fill tile brushes with flip flags, save-entity-as-prefab, undoable revert-to-prefab.
- [ ] Follow-ups: cached chunk meshes (every visible tile is still one `SpriteDraw` per frame), a demo map and a window check of flip/animated/parallax tiles, `.tmx` and infinite maps, Tiled object layers turned into entities by a registered spawner, editing a prefab file and updating open instances.

## Phase 19: 2D rendering and audio depth

- [x] HDR targets + tone map, pixel-perfect mode, MSAA (window-verified with `tools/drive-display.ps1`).
- [x] Light shadows: `ShadowCaster2D`, tile walls, `LightingEffect.CastShadows`, window-verified with `tools/drive-shadows.ps1`.
- [ ] Per-camera effects (the chain is one per window; a camera with its own effect list needs viewports). Follow-ups for the done parts: soft shadows, a shadow-casting polygon/segment component, shadows from sprites by alpha, per-light shadow toggle; bloom/other effects keep 8-bit internal targets, MSAA does not apply when nothing redirects the scene (use the chain), a pixel-perfect mouse mapping in the editor, integer zoom enforcement.
- [x] Audio: streaming (`WavStream`), positional audio, `AudioSource`/`AudioListener` + `AudioSourceSystem`, mixer groups (`AudioMixer`). Follow-ups: Ogg/MP3 decoding, effects (reverb, low-pass), doppler, ducking, a settings panel for the mixer, an audible check by ear (verified by tests and silent device runs only).


## Phase 20: Verify and finish (mostly done)

- [x] Window-checked the editor extras on the Platformer: Save as prefab (found and fixed: the default name was `coin__#29`, and an existing prefab could be overwritten silently; the popup now proposes a free name and says "Replace" when the file exists; `tools/drive-prefab.ps1`). Not window-verified: dragging the gizmo with synthetic mouse input (the script's drag did not move the entity, probably the way the mouse is injected, not the editor), so Revert to prefab is only unit-tested, as are the rectangle/fill tile brushes and the History window clicks.
- [x] Tiled demo map (`Assets/maps/demo.tmj`, F11 in the Platformer): flipped and diagonal tiles, an animated tile and a parallax backdrop import and draw correctly (`tools/drive-demomap.ps1`).
- [x] Mixer panel in the editor (`Mixer` window: device volume, per-group volume and mute, live). Not done: listening to the audio by ear, positional audio in the Platformer, a streamed music track.
- [x] Cached chunk meshes for tile layers (`TileMeshCache`, `TileChunkMesh`, `TileChunkMeshBuilder`): one GPU mesh per visible chunk, rebuilt when the chunk, the tile set or the atlas changes, animated tiles still through the batch, systems that are `IDisposable` are disposed by `Game`. Window-checked: the level and the demo map look identical.
- [ ] Per-camera effects (a camera with its own effect list and viewport). Deferred: the render systems each hold one `Camera2D`, so several views need either a camera set per pass or systems that take the camera as an argument, plus a viewport rectangle per view and one effect chain per view.
- [ ] Listen to the audio by ear; add a positional `AudioSource` and music to the Platformer.

## Phase 21: A standalone editor (done; design and decisions in `architecture.md`)

Problem: the editor and debug panels drew over the game window, and you could not open the editor without starting the game. Decision: the editor is its **own executable** (like Unity, Godot, Unreal): you edit a scene there and press Play to run the game inside the editor's Scene panel. Reason: editing and playing are different jobs; a shipped game must not contain editor code; one editor can open any game. Game code is loaded as a library from a shadow copy, so the editor can rebuild it while it is open.
- [x] Split each game into a library plus a launcher; the launcher is what ships and references no editor code.
- [x] `IGameModule` + `GameLaunch` in Runtime (the module only creates the game; `Game` stays the composition root).
- [x] `src/Engine.Editor` (`Editor.exe`): project files, the hub, `Game.AttachEditor`/`IEditorShell`.
- [x] Scene panel (game rendered into a texture), default dock layout (native dock-builder), Play/Pause/Stop toolbar, Assets window with prefab drag-and-drop, Mixer window.
- [x] Editor classes in `Engine.Editor`; shared edit state in `Engine.Runtime/Editing`.
- [x] Open dialog and recent projects: the hub (`Editor.exe` with no arguments) and Project > Open another project.
- [x] Game code reload: Project > Build and reload game code (builds, then restarts the editor on the project, asking to save first).
- [x] Windows 11 look: Segoe UI + Segoe Fluent Icons, dark Fluent palette with the system blue accent, rounded controls, dark title bar (`Win11Theme`).
- [ ] Asset browser follow-ups: thumbnails for textures, drag an entity into the browser to Save as prefab, open a scene or tile layer by double-click, create/rename/delete files.
- [ ] A drive script for the 3D editor view (orbit, select, gizmo drag in the Scene panel) and a way to inject mouse drags the editor accepts.
- [ ] Game-specific debug panels (`DebugGui`) dock by title in `DockLayout`; let a game register its panel titles.
- [ ] Tidy: the Post effects panel lists non-simple properties (`ShadowSources`) read-only; toolbar buttons overflow in a narrow Hierarchy panel; per-project instead of per-user saved layout.
- [ ] Mica/rounded window backdrop (needs the window handle and a Windows 11 build check), more icons in menus and the toolbar.

## Phase 22: Building and shipping a game (done for Windows)

- [x] The shipped exe is the launcher, which references no editor code; the debug overlay is hidden until F1.
- [x] Project > Publish game (and `Editor --publish <project> [folder]`): self-contained `dotnet publish` of the launcher for win-x64 into `Build/win-x64`, with the assets, the engine assemblies and the native libraries; checked by running the published exe.
- [x] Project > Validate assets: loads every scene, prefab, tile layer, tile set, animation graph, material, texture, sound and model through the game's loaders and lists the failures (`AssetValidator`).
- [x] Settings file for the shipped game (`game.settings.json`: window size, full screen, vsync, master volume; `GameSettings`).
- [x] New projects from the editor: `ProjectTemplate` creates the library, launcher, assets and `Directory.Build.props`; `Editor --new <folder> <name>` does it from the command line.
- [ ] Key bindings in the settings file; a `--editor` flag; other platforms (the natives and RIDs are win-x64 only); a self-contained, published *editor* has not been tried (the engine folder layout is assumed).
- [ ] Project templates beyond the empty 2D one (platformer starter, 3D starter); upgrade path when the engine changes (engine DLLs are referenced from the editor's folder).

## Phase 23: Pick the next game or engine depth (decision for you)

- [ ] A small playable 3D game on the character controller, PBR and physics (shows which engine pieces are missing).
- [ ] More platformer levels authored in Tiled with the new import and editor tools.
- [ ] Engine depth candidates: soft light shadows, a shadow-casting polygon component, image-based lighting and a skybox, rigid-body rotation in 3D physics, Ogg/MP3 audio, per-camera effects.

## Smaller known gaps (from `features.md`, no owner yet)

- Spatial hash, one-way platforms and slopes in physics; bodies spawned inside solids are not pushed out.
- Entity references in saved components, scene-in-scene, instance-to-prefab links.
- Per-camera and per-layer post effects; MSAA; render-to-texture for game code.
- Animation events and blending; more than one transition per update.
- High-DPI is untested; GPU timing in the profiler.
