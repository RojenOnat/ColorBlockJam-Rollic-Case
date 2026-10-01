# Color Block Jam — Rollic Developer Case

A Unity project for the Rollic Game Developer Case 2026: a small Color Block Jam vertical slice with five playable levels and a visual level editor for non-technical designers.

**Status: active development.** The shared level format, visual level editor, generated board, grid-based block dragging, color/width-aware gate matching, and automatic block exit flow are implemented. The Gameplay scene now has a data-driven `LevelManager`, runtime level prefab, and an initial Home/HUD/outcome UI shell. The five production levels and final game flow validation are still pending.

## Requirements and current environment

| Item | Case requirement | Current project |
| --- | --- | --- |
| Unity Editor | 2022.3.62f2 | 2022.3.62f2 |
| Rendering | Not prescribed | Universal Render Pipeline 14.0.11 |
| Layout | Portrait, 1080 × 1920; also support 20:9 | Pending implementation and verification |
| Delivery | Android APK or screen recording up to 3 minutes | Not available yet |

## Open and run

1. Clone the repository with Git and install Git LFS.
2. Run `git lfs install` and `git lfs pull` inside the clone to retrieve binary assets.
3. Install **Unity 2022.3.62f2** through Unity Hub.
4. In Unity Hub, select **Add project from disk** and choose the repository root.
5. Open the project and allow Unity to restore packages and import assets.
6. Open `Assets/ColorBlockJam/Scenes/Gameplay.unity` and press **Play**. The Home panel opens first; its Level button loads the selected `LevelDefinition` through `LevelManager`.

No Android build or runtime verification has been completed as part of repository preparation.

## Required scope

- [x] Initial Home screen, gameplay HUD, success, and fail panel layouts.
- [ ] Settings screen with visible sound, music, and vibration toggle states and close navigation.
- [x] Block movement and matching-color, exact-width exits.
- [ ] First five levels authored with the custom editor.
- [ ] Countdown timer and failure at zero.
- [ ] Coin inventory that remains correct when leaving and returning to a level.
- [ ] Level completion and progression to the next level.
- [ ] Pause, restart, home navigation, and a working fail popup.
- [ ] Restart restores the complete initial level state.
- [x] Visual editor for board dimensions, doors, blocks, and timer values.
- [x] Create, save, and play levels without writing code or manually editing files.
- [x] One shared level data format used by the editor and the game.
- [ ] Verify portrait layout at 1080 × 1920 and on a 20:9 display.
- [ ] Final APK or screen recording and completed delivery documentation.

Functional boosters, ice blockers, arrow blocks, and solvability validation are optional. The life system and booster functionality are not required for the initial scope.

## Project structure

```text
Assets/
  Game Developer Case Assets/   Supplied UI, textures, and models
  Lean/                         Imported Lean Common, Touch, and Touch+ packages
  Scenes/                       Current template scene
  Settings/                     Render pipeline and rendering configuration
  TutorialInfo/                 Unity template tutorial content
Packages/                       Package manifest and dependency lock file
ProjectSettings/                Shared Unity project configuration
```

Unity-generated folders such as `Library`, `Temp`, `Logs`, `UserSettings`, and build output are excluded from version control. Asset `.meta` files are versioned to preserve Unity references. Binary asset types are tracked through the repository's Git LFS attributes.

## Architecture direction

The level-authoring foundation currently follows this design:

- **Data:** `LevelDefinition` is the shared ScriptableObject format for board dimensions, block shapes and positions, door positions and colors, and the timer. `LevelCatalog` orders these assets for runtime loading.
- **Logic:** `BoardGridState` owns playable cells, live occupancy, gate lookup, and remaining-block state. `GridMovableBlock` owns block state and delegates exit presentation to `BlockExitMotor`.
- **View and input:** `GameplayInputController` converts pointer input to board-space drag requests. It does not decide collision or gate rules. `BlockExitMotor` plays transform-based exits without Rigidbody physics.
- **Matching:** `ColorIdentity` provides a shared color contract. `GateGroup` stores connected gate cells and exit direction, then requires exact color, alignment, direction, and width matches.
- **Level authoring:** a visual editor supports creating, saving, and playing levels without code. Editor-only code should remain separate from runtime code.
- **Runtime flow:** `LevelManager` owns the selected level, persistence boundary, screen-root switching, and destroy/build lifecycle. It instantiates `Level_Runtime`, which owns the level camera, light, input controller, and board root.

Concrete implementation choices and their reasons will be documented as development proceeds.

## Level editor

Open the editor from **Color Block Jam > Level Editor** in the Unity menu.

1. Click **New** to create a level asset under `Assets/ColorBlockJam/Levels`.
2. Set the board width, height, and timer in the right panel.
3. Choose **Block**, then select a color and shape and click the visual grid to place it.
4. Choose **Door**, select a color, and click the band immediately outside a board edge.
5. Use **Select** to select or drag a block. Selected blocks can be recolored, reshaped, rotated, or deleted from the right panel.
6. Use **Eraser** to remove blocks and doors directly from the board.
7. Resolve the errors and warnings shown by the live validation panel, then click **Save**.

Board visuals are generated from `BoardVisualSettings`. The editor creates this asset automatically and searches `Assets/ColorBlockJam/Prefab` for `GroundGrid`, `Wall`, `Corner`, and `Gate` prefabs. It does not substitute FBX sub-assets when a required prefab is missing. Grid-size changes rebuild the tile layout and perimeter; gates replace the straight-wall slots they occupy. Every edge and corner receives its orientation automatically. Use **Rebuild Scene Preview** after changing prefab offsets or base rotations.

The editor keeps `LevelCatalog` synchronized with saved level assets. `LevelManager` reads the catalog and rebuilds the selected level at runtime.

## Assets and dependencies

- The case art is stored in `Assets/Game Developer Case Assets`.
- Lean Common, Lean Touch, and Lean Touch+ are imported under `Assets/Lean`; their gameplay integration has not been implemented.
- Unity package versions are defined in `Packages/manifest.json` and `Packages/packages-lock.json`.
- Supplied assets and third-party packages remain subject to their respective owners' terms. This repository does not grant redistribution rights to those assets or packages.

## Known limitations

- Unity 2022.3.62f2 package migration is configured; the full project must be reopened once in that editor to refresh imported assets and the package lock.
- Button action wiring, timer presentation, success/fail selection, and progression feedback still need final gameplay verification.
- The five required levels have not been authored.
- Device performance, aspect-ratio support, and Android compatibility have not been verified.
- No final APK or demonstration recording is included.

## AI assistance

OpenAI Codex was used to review the case brief and assist with the level editor, board generation, grid movement, gate matching, exit state flow, validation, architecture cleanup, and documentation. All generated code was reviewed and iterated in the Unity project.

## Work time

Approximate total development time has not been recorded yet. Add the actual estimate before the final submission.
