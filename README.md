# Color Block Jam — Rollic Developer Case

A Unity project for the Rollic Game Developer Case 2026: a small Color Block Jam vertical slice with five playable levels and a visual level editor for non-technical designers.

**Status: initial project setup.** The supplied case assets and Lean Touch packages have been imported. The gameplay, screen flow, five levels, and custom level editor are not implemented yet. This repository is a development baseline, not a completed submission.

## Requirements and current environment

| Item | Case requirement | Current project |
| --- | --- | --- |
| Unity Editor | 2022.3.62f2 | 6000.5.6f1 |
| Rendering | Not prescribed | Universal Render Pipeline 17.5.0 |
| Layout | Portrait, 1080 × 1920; also support 20:9 | Pending implementation and verification |
| Delivery | Android APK or screen recording up to 3 minutes | Not available yet |

**Unity version compatibility is unresolved.** The current project and its packages were created with Unity 6. Do not open this checkout in Unity 2022 expecting automatic compatibility. Aligning the project and package versions with the case requirement is a prerequisite for the final submission; changing `ProjectVersion.txt` alone is not a migration.

## Open and run

1. Clone the repository with Git and install Git LFS.
2. Run `git lfs install` and `git lfs pull` inside the clone to retrieve binary assets.
3. Install **Unity 6000.5.6f1** through Unity Hub to inspect the current baseline.
4. In Unity Hub, select **Add project from disk** and choose the repository root.
5. Open the project and allow Unity to restore packages and import assets.
6. Open `Assets/Scenes/SampleScene.unity` and press **Play** to inspect the template scene. A playable Color Block Jam level is not present yet.

No Android build or runtime verification has been completed as part of repository preparation.

## Required scope

- [ ] Home screen with level entry, top UI, placeholder tabs, and button feedback.
- [ ] Settings screen with visible sound, music, and vibration toggle states and close navigation.
- [ ] Block movement and matching-color exits.
- [ ] First five levels authored with the custom editor.
- [ ] Countdown timer and failure at zero.
- [ ] Coin inventory that remains correct when leaving and returning to a level.
- [ ] Level completion and progression to the next level.
- [ ] Pause, restart, home navigation, and a working fail popup.
- [ ] Restart restores the complete initial level state.
- [ ] Visual editor for board dimensions, doors, blocks, and timer values.
- [ ] Create, save, and play levels without writing code or manually editing files.
- [ ] One shared level data format used by the editor and the game.
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

The following is the intended design, not an implemented architecture:

- **Data:** a shared level definition for board dimensions, block shapes and positions, door positions and colors, and the timer. The editor and game should consume the same definition to avoid conversion mismatches.
- **Logic:** board occupancy, movement constraints, exits, timer state, and win/fail rules separated from scene presentation. This makes rules easier to test and extend.
- **View and input:** Unity components handle dragging, visual feedback, animation, and UI while delegating gameplay decisions to the logic layer.
- **Level authoring:** a visual editor supports creating, saving, and playing levels without code. Editor-only code should remain separate from runtime code.

Concrete implementation choices and their reasons will be documented as development proceeds.

## Level editor

The custom level editor is not implemented yet. There is currently no menu entry or level-authoring workflow. This section will be replaced with exact instructions once the editor is available.

## Assets and dependencies

- The case art is stored in `Assets/Game Developer Case Assets`.
- Lean Common, Lean Touch, and Lean Touch+ are imported under `Assets/Lean`; their gameplay integration has not been implemented.
- Unity package versions are defined in `Packages/manifest.json` and `Packages/packages-lock.json`.
- Supplied assets and third-party packages remain subject to their respective owners' terms. This repository does not grant redistribution rights to those assets or packages.

## Known limitations

- Unity version does not currently match the case requirement.
- The required game screens, mechanics, progression, persistence, and editor remain to be built.
- The five required levels have not been authored.
- Device performance, aspect-ratio support, and Android compatibility have not been verified.
- No final APK or demonstration recording is included.

## AI assistance

OpenAI Codex was used to review the case brief, inspect the initial project, and prepare repository documentation and version-control housekeeping. No custom gameplay implementation has been produced in this stage. Record further AI-assisted work here as it occurs.

## Work time

Approximate total development time has not been recorded yet. Add the actual estimate before the final submission.
