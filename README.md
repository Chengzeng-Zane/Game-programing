# Echo Escape

Echo Escape is a 2D Unity platform puzzle game built for the Game Programming module. The game focuses on recording the player's past movement, replaying it as an Echo, using Gravity Flip traversal, collecting risky loot, and escaping through three connected levels.

## Current Build

The project is now a playable three-level prototype:

- `MainMenu` - pixel-art start menu with controls, credits, quit, and shared background music.
- `Level1_Tutorial` - introduces movement, tutorial popups, pressure plates, Echo recording/replay, Gravity Flip, hazards, and portal flow.
- `Level2_LootTutorial` - teaches treasure chests, pending loot, enemy danger, river hazards, and loot loss on death.
- `Level3_RiskReward` - expands the route with risk-reward traversal, Magic Barrier / button logic, Gravity Flip void death zones, river hazards, final loot banking, and ending dialogue.

## Core Mechanics

- Player movement, jumping, facing direction, chest interaction, attack, and death animation.
- Echo recording with `Q`, replay with `E`, and Echo pressure-plate support.
- Gravity Flip traversal using upper platforms and gravity-aware ground detection.
- Gravity Flip void kill zones for off-platform failure cases.
- Treasure chests that grant collectible loot.
- Pending loot that is lost on death and secured only when reaching the exit.
- Cursed Ghost enemy behavior split into focused movement, targeting, attack, health, and animation components.
- Unified death flow with death feedback, loot loss feedback, and current-scene reload.
- Story intro popups that are skipped after death reloads.
- Level 3 ending sequence that shows secured loot feedback before the final wizard message.
- Shared background music and level sound effects.

## Controls

- Move: `A` / `D` or Left / Right Arrow
- Jump: `Space`
- Attack: `J`
- Record / Stop Recording: `Q`
- Replay Echo: `E`
- Open Chest / Interact: `F`
- Continue Story / Close Tutorial Popup: `C`
- Gravity Flip Up: Up Arrow
- Gravity Flip Down: Down Arrow

## Unity Project

- Unity version: `2022.3.62f3c1`
- Main Unity project folder: `EchoEscape/`
- Main menu scene: `EchoEscape/Assets/Scenes/MainMenu.unity`
- Gameplay scenes:
  - `EchoEscape/Assets/Scenes/Level1_Tutorial.unity`
  - `EchoEscape/Assets/Scenes/Level2_LootTutorial.unity`
  - `EchoEscape/Assets/Scenes/Level3_RiskReward.unity`

## How to Run

1. Open Unity Hub.
2. Add or open the `EchoEscape/` folder inside this repository.
3. Use Unity `2022.3.62f3c1` or a compatible Unity 2022.3 LTS version.
4. Open `EchoEscape/Assets/Scenes/MainMenu.unity`.
5. Press Play in the Unity Editor.

## Validation

Recent script compilation checks:

```powershell
cd .\EchoEscape
dotnet build .\Assembly-CSharp.csproj
dotnet build .\Assembly-CSharp-Editor.csproj
```

Both projects currently build with `0` errors and `0` warnings.

## Script Documentation

Gameplay scripts now include Chinese summary comments and function-level explanations. Important logic blocks also include inline comments for coursework presentation and code explanation, especially around:

- player movement and gravity-aware jumping
- Echo recording and replay
- Gravity Flip and void death checks
- enemy targeting and attack hitboxes
- loot pending / secured state
- death and scene reload flow
- tutorial, intro, and ending UI flow

## Project Structure

- `EchoEscape/Assets/Scenes/` - main menu and gameplay scenes.
- `EchoEscape/Assets/Scripts/` - player, recording, level, enemy, loot, UI, audio, art, and core systems.
- `EchoEscape/Assets/Resources/` - imported pixel art, animation frames, UI images, audio, and music.
- `EchoEscape/Docs/` - concept, level design, testing log, and asset credits.
- `EchoEscape/Packages/` - Unity package manifest and lock files.
- `EchoEscape/ProjectSettings/` - Unity project settings.
- `inclass activity/` - class activity evidence kept separate from the final game project.

## Development Progress

- Week 1 - Core Gameplay for Level 1 and Level 2: Set up the repository and Unity project, then built the first version of the main 2D platforming loop. Level 1 focused on movement, jumping, Echo recording and replay, pressure plate puzzles, door opening, and reaching the exit. Level 2 introduced chest interaction, loot collection, basic enemy danger, player attack, and the rule that pending loot can be lost on death.
- Week 2 - UI and Player Feedback: Improved the player-facing feedback for the first two levels. This included tutorial popups, recording status, Echo replay feedback, button and door feedback, chest opening feedback, pending and secured loot display, death messages, level completion feedback, Ruby player visuals, Level 2 forest visuals, and Cursed Ghost enemy behavior.
- Week 3 - Level 3 Development: Built Level 3 as the main combined challenge level. This added a risk-reward route that brings together Echo puzzle solving, enemy danger, treasure chest rewards, Gravity Flip traversal, river and pit hazards, extraction-style loot decisions, and a more complete end-of-level flow.
- Week 4 - Polish, Effects, Testing, and Documentation: Focused on final polish and submission preparation. This included improving level backgrounds, music, death and restart flow, Gravity Flip void death coverage, collectible drop weighting, chest and loot feedback, enemy script organisation, script comments, README content, asset credits, known issue checks, Kanban updates, and full Level 1 to Level 3 testing.

## Credits

External assets and references are recorded in `EchoEscape/Docs/Credits.md`.
