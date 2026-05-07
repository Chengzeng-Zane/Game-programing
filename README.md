# Game Programming

Personal game programming project for the games development module.

## Working Title
EchoVault

## Game Idea
EchoVault is a small 2D level-based game built around self-recorded actions and risk-reward loot. The player can record a short sequence of movement or actions, then replay that sequence through a copy of themselves to help solve traversal and timing challenges.

The level also includes random treasure chests that can drop rare items. Those rewards are temporary until the player safely reaches the next secure point or completes the level section. If the player dies after opening a chest, the newly gained loot is lost.

The project is intentionally small in scope so the final result can be playable, stable, tested, and clearly explained.

## Current Prototype
The first prototype includes a tutorial-style opening sequence. It introduces movement, pressure plates, recording, echo replay, the door puzzle, random chest loot, loot loss on death, and extraction in a step-by-step order.

The prototype now uses CC0 assets from the Brackeys Platformer Bundle for pixel-art character, tiles, loot visuals, hazard visuals, sound effects, and background music.

The game now opens through a pixel-art main menu built with the same asset pack. The menu includes Start Game, Controls, Credits, and Quit options.

## Current Unity Project
- Unity version: 2022.3.62f3c1
- Template: 2D Unity project
- Main project folder: `EchoVault/`

## Controls
Controls will be updated as the prototype develops.

Planned controls:
- Move: WASD or Arrow Keys
- Jump: Space, W, or Up Arrow
- Record / Stop recording: Q
- Replay echo: E
- Interact / Open chest: F
- Restart: R

## How to Run
1. Open Unity Hub.
2. Add or open the `EchoVault` folder inside this repository.
3. Use Unity 2022.3.62f3c1 or a compatible Unity 2022.3 LTS version.
4. Open `EchoVault/Assets/Scenes/MainMenu.unity`.
5. Press Play in the Unity Editor.

## Continuous Integration
This repository uses GitHub Actions for basic repository checks on pull requests and pushes to `main`.

The current CI workflow checks that the Unity project structure, prototype scene, scripts, README, credits, and testing log are present. It also blocks tracked Unity generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`, and the source-only `ExternalAssets/` folder.

Full Unity build and test automation is planned for later because it requires Unity license secrets to be configured in GitHub Actions.

## Project Structure
- `EchoVault/` - the actual Unity game project.
- `EchoVault/Assets/` - scenes, scripts, prefabs, art, audio, and UI.
- `EchoVault/Packages/` - Unity package manifest and package lock files.
- `EchoVault/ProjectSettings/` - Unity project settings.
- `EchoVault/Docs/` - concept, development plan, testing log, and credits.
- `inclass activity/` - class activity evidence kept separate from the final game project.

## Progress Log
- 2026-04-16: Repository created.
- 2026-05-05: Connected Unity project to the GitHub repository and added planning documents.
- 2026-05-05: Moved the final Unity game into `EchoVault/` to separate it from class activity material.
- 2026-05-05: Added a first playable prototype with player movement, echo replay, random chests, hazards, and extraction.
- 2026-05-05: Added a guided tutorial flow for the first prototype.
- 2026-05-06: Imported Brackeys Platformer Bundle assets and connected prototype visual skinning and audio feedback.
- 2026-05-06: Added a basic GitHub Actions workflow for repository hygiene checks.
- 2026-05-07: Added a pixel-art main menu scene with Start Game, Controls, Credits, and Quit options.

## Plan
- [x] Define initial game concept and core mechanics
- [ ] Build playable prototype
- [ ] Polish and testing

## Weekly Update
- Week 1: Setup repository and project structure
- Week 2: Connect Unity project and prepare planning/testing documentation
- Week 2: Separate class activity evidence from the final Unity project

## Credits
External assets and references are recorded in `EchoVault/Docs/Credits.md`.
