# Color Block Jam — Rollic Game Developer Case 2026

A portrait Unity vertical slice built for the Rollic Game Developer Case. The repository contains five playable levels, a data-driven runtime flow, persistent progression and coins, and a visual level editor intended for non-technical level designers.

## Requirements

- Unity **2022.3.62f2**
- Universal Render Pipeline **14.0.11**
- New Input System **1.7.0**
- Git LFS for binary assets
- Reference resolution: **1080 × 1920**, with the Canvas configured to scale across portrait aspect ratios including 20:9

## Run the project

1. Clone the repository and run `git lfs install` followed by `git lfs pull`.
2. Open the repository root with Unity 2022.3.62f2.
3. Open `Assets/ColorBlockJam/Scenes/Gameplay.unity`.
4. If this is the first import, run **Color Block Jam > Prepare Gameplay Scene for Submission** once.
5. Press **Play**. The app starts on the Home screen and loads the selected level through `LevelManager`.

`Gameplay.unity` is the only enabled build scene. Unity-generated directories (`Library`, `Temp`, `Logs`, `UserSettings`, and build output) are excluded from version control.

## Implemented case scope

- Home screen with level path, persistent coin display, settings access, and button feedback.
- Settings popup with persistent sound, music, and vibration state.
- Gameplay HUD with current level, countdown, coin inventory, restart, and pause controls.
- Pause, success, and fail flows with restart, home, and next-level actions.
- Five authored levels sharing the same `LevelDefinition` format used by the editor and runtime.
- Grid-constrained block movement, collision checks, matching-color gates, exact-width exits, and clean restart.
- Countdown failure handling that allows an exit already in progress to resolve.
- Persistent current-level progress and coin rewards.
- Visual level editor for board size, timer, reward, camera, lighting, walls, gates, blocks, and optional features.
- Live validation for invalid placement, duplicate IDs, overlaps, and missing block/gate color pairs.

## Architecture

The project keeps authored data, game rules, presentation, and editor tooling separate:

- **Level data:** `LevelDefinition` ScriptableObjects contain board layout and per-level presentation values. `LevelCatalog` defines play order.
- **Runtime composition:** `LevelManager` owns navigation and level lifecycle. `LevelRuntimeFactory` builds a disposable runtime instance and returns a `LevelRuntimeContext` containing the board, input, and countdown services.
- **Rules:** `BoardGridState`, `BoardTopology`, `BlockTopology`, and `LevelValidator` provide reusable board rules without UI dependencies.
- **Input and presentation:** `GameplayInputController`, `BlockExitMotor`, HUD views, and panel controllers present state and forward player intent.
- **Persistence:** `LevelProgress`, `GoldWallet`, and `PlayerSettings` isolate `PlayerPrefs` keys from gameplay and UI code.
- **Designer tuning:** `Assets/ColorBlockJam/Resources/ColorBlockJam/GameTuning.asset` exposes drag sensitivity, drag lift, block exit speed, and success-panel delay.
- **Assembly boundaries:** runtime, editor, and EditMode tests compile in separate assembly definitions.

Scene object references are serialized in Unity. Runtime code does not locate UI by object name or displayed text.

## Level authoring workflow

Open **Color Block Jam > Level Editor**.

1. Select an existing level or click **New**.
2. Set board width, height, timer, reward, camera, and directional-light values.
3. Use **Wall**, **Gate**, and **Block** modes to paint the board. Right-click removes an item where supported.
4. Use **Features** for optional direction and ice configuration.
5. Resolve errors in the validation panel.
6. Click **Save**. The editor stores the ScriptableObject and synchronizes `LevelCatalog`.
7. Click **Rebuild Preview** to inspect the same runtime prefab, camera, light, and board visuals used in play mode.

Level assets live in `Assets/ColorBlockJam/Levels` and use the `Level_###` naming convention. Board presentation is configured through `Assets/ColorBlockJam/Settings/BoardVisualSettings.asset`; reusable runtime prefabs are under `Assets/ColorBlockJam/Prefab`.

## Testing and delivery

EditMode tests cover shared topology and validation rules under `Assets/ColorBlockJam/Tests/EditMode`. Before submission, run the Unity Test Runner, test all five levels from Home through completion/failure, verify 1080 × 1920 and a 20:9 simulator profile, then create an Android APK or a screen recording no longer than three minutes.

## Third-party and supplied assets

Rollic-provided art is stored under `Assets/Game Developer Case Assets`. Unity packages and exact versions are declared in `Packages/manifest.json` and `Packages/packages-lock.json`. Supplied art remains subject to its owner's terms.

## AI assistance

OpenAI Codex was used for code review, architecture cleanup, editor/runtime separation, test scaffolding, and documentation. The resulting behavior and Unity scene setup were reviewed inside the project.

## Work time

Add the actual total development time before final submission.
