# Color Block Jam — Rollic Game Developer Case 2026

A portrait Unity vertical slice built for the Rollic Game Developer Case. The repository contains five playable levels, a data-driven runtime flow, persistent progression and coins, and a visual level editor intended for non-technical level designers.

<p align="center">
  <img src="Documentation/Images/Gameplay.png" width="280" alt="Color Block Jam gameplay">
</p>

<p align="center">
  <strong>Move each color block through its matching gate before time runs out.</strong>
</p>

## Project documentation

The illustrated [Color Block Jam Case Study](Documentation/ColorBlockJam_Case_Study.pdf) presents the gameplay loop, UI flow, runtime architecture, level-authoring workflow, feature tools, validation, tuning, and delivery setup.

## Submission builds

- [Download the Android APK](Builds/Android/ColorBlockJam.apk)
- [Watch the case demonstration video on Google Drive](https://drive.google.com/file/d/1vAjPWpOq0-DibCB7NbBLL2B2JQmGmXXO/view?usp=sharing) — 2 minutes 36 seconds
- [Download the original demonstration video](Documentation/Video/ColorBlockJam_Case_Demo.mp4)
- [Read the illustrated case study](Documentation/ColorBlockJam_Case_Study.pdf)

The APK is a portrait Android build of the submitted project. The demonstration video covers the visual level editor, Home and Settings screens, gameplay, success, fail, retry, and Home navigation flows.

## Game flow

| Home and progression | Runtime gameplay |
|:---:|:---:|
| <img src="Documentation/Images/MainMenu.png" width="250" alt="Main menu and level progression"> | <img src="Documentation/Images/Gameplay.png" width="250" alt="Grid gameplay with HUD"> |

The Home screen shows persistent progression and coins. During gameplay, the HUD presents the visible level number, countdown, restart control, and current balance. Completing a level grants its authored reward and advances the visible level count; the five authored datasets loop behind that continuous progression.

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
- **Runtime composition:** `LevelManager` owns only level selection and runtime lifecycle. `LevelRuntimeFactory` builds a disposable runtime instance and returns a `LevelRuntimeContext` containing the board, input, and countdown services.
- **Session rules:** `LevelSessionController` resolves timer, pause, remaining-block, success, and failure state for one level attempt.
- **Screen flow:** `GameFlowController` binds navigation, HUD updates, rewards, and Home/gameplay/outcome screen transitions to lifecycle events.
- **Rules:** `BoardGridState`, `BoardTopology`, `BlockTopology`, and `LevelValidator` provide reusable board rules without UI dependencies.
- **Input and presentation:** `GameplayInputController`, `BlockExitMotor`, HUD views, and panel controllers present state and forward player intent.
- **Persistence:** `LevelProgress`, `GoldWallet`, and `PlayerSettings` isolate `PlayerPrefs` keys from gameplay and UI code.
- **Designer tuning:** `Assets/ColorBlockJam/Resources/ColorBlockJam/GameTuning.asset` exposes drag sensitivity, drag lift, block exit speed, and success-panel delay.
- **Assembly boundaries:** runtime, editor, and EditMode tests compile in separate assembly definitions.
- **Editor organization:** the level editor is split into drawing, input, inspector, mutation, asset, and geometry files. Board composition is split into gate, block, and wall builders.

Scene object references are serialized in Unity. Runtime code does not locate UI by object name or displayed text.

Project-owned assets follow one naming convention: PascalCase for folders and prefabs, `M_` for materials, and `Level_###` for authored level assets. Runtime prefabs live under `Assets/ColorBlockJam/Prefabs`; supplied case art remains unchanged under `Assets/Game Developer Case Assets`.

## Level authoring workflow

Open **Color Block Jam > Level Editor**.

<p align="center">
  <img src="Documentation/Images/LevelEditor.png" width="850" alt="Color Block Jam visual level editor">
</p>

1. Select an existing level or click **New**.
2. Set board width, height, timer, reward, camera, and directional-light values.
3. Use **Wall**, **Gate**, and **Block** modes to paint the board. Right-click removes an item where supported.
4. Use **Features** for optional direction and ice configuration.
5. Resolve errors in the validation panel.
6. Click **Save**. The editor stores the ScriptableObject and synchronizes `LevelCatalog`.
7. Click **Rebuild Preview** to inspect the same runtime prefab, camera, light, and board visuals used in play mode.

Level assets live in `Assets/ColorBlockJam/Levels` and use the `Level_###` naming convention. Board presentation is configured through `Assets/ColorBlockJam/Settings/BoardVisualSettings.asset`; reusable runtime prefabs are under `Assets/ColorBlockJam/Prefabs`.

<p align="center">
  <img src="Documentation/Images/LevelComplete.png" width="230" alt="Level complete reward panel">
</p>

## Testing and delivery

EditMode tests cover shared topology and validation rules under `Assets/ColorBlockJam/Tests/EditMode`. Before submission, run the Unity Test Runner, test all five levels from Home through completion/failure, verify 1080 × 1920 and a 20:9 simulator profile, then create an Android APK or a screen recording no longer than three minutes.

## Third-party and supplied assets

Rollic-provided art is stored under `Assets/Game Developer Case Assets`. Unity packages and exact versions are declared in `Packages/manifest.json` and `Packages/packages-lock.json`. Supplied art remains subject to its owner's terms.

## AI assistance

OpenAI Codex was used for code review, architecture cleanup, editor/runtime separation, test scaffolding, and documentation. The resulting behavior and Unity scene setup were reviewed inside the project.

## Work time

The case was completed in approximately **1.5 working days**.
