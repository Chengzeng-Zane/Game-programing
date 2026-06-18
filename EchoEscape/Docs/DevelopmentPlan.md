# Echo Escape Development Plan

This document records the planned and completed development route for Echo Escape. The final project is a three-level 2D puzzle-platformer vertical slice built around Echo recording, Gravity Flip traversal, loot risk, enemy danger, and forest-themed story presentation.

## Tools

- Unity `2022.3.62f3c1`
- C#
- Git and GitHub
- GitHub Issues / Projects for Kanban planning
- Itch.io asset packs and personally recorded menu audio

## Final Scene Structure

The final build is organised through Unity Build Settings in this order:

1. `MainMenu`
2. `Level 1 - The First Echo`
3. `Level 2 - Relics of the Forest`
4. `Level 3 - Escape from the Silent Forest`

The early Level 1 graybox version was used as development evidence while the level was being planned. After screenshots/evidence were captured, the temporary graybox scene was removed from the Unity project so the final project only keeps the playable polished scenes.

## Week 1 - Core Gameplay and Graybox Foundation

- Set up the Unity project, GitHub repository, `.gitignore`, and early documentation.
- Built the first graybox version of the platforming layout using simple blocks and placeholder objects.
- Implemented player movement, jumping, Rigidbody2D physics, Collider2D collision, camera follow, and basic level flow.
- Started the Echo mechanic: the player records movement with `Q`, stores frames, and replays them as a separate Echo object with `E`.
- Added the first pressure plate and door puzzle to prove that the Echo could affect the world instead of only being a visual replay.

## Week 2 - Tutorial, Loot, Enemy, and Feedback

- Expanded the game into Level 1 and Level 2.
- Added tutorial popups, question markers, story intro panels, and clearer player guidance.
- Added chest interaction with `F`, collectible items, pending loot, secured loot, and loot loss after death.
- Added the Cursed Ghost / slime enemy behavior, then later reorganised it into focused enemy components.
- Added player attack with `J`, enemy defeat feedback, and attack hitbox tuning.
- Improved UI feedback for recording, replaying, chest opening, loot collection, death, and level completion.
- Replaced many placeholder visuals with Ruby player sprites, Echo tinting, forest tiles, chest art, and enemy sprites.

## Week 3 - Level 3 and Risk-Reward Route

- Built Level 3 as the combined challenge level.
- Added a risk-reward route with treasure, hazards, enemy danger, Gravity Flip movement, Magic Barrier / button interaction, and final escape.
- Added Gravity Flip void death zones so falling out of flipped traversal areas triggers the same death and restart flow as normal hazards.
- Added river and pit hazards, then hid placeholder red visuals where they were not part of the final art style.
- Improved chest and collectible feedback and changed the loot system to use fixed chest locations with weighted collectible selection instead of random chest spawning.
- Split the enemy logic into separate scripts for controller, targeting, movement, attack, health, and animation.

## Week 4 - Polish, Testing, Audio, Documentation, and Submission Preparation

- Renamed the final scenes to match the story and game progression.
- Added dark forest backgrounds to all levels and adjusted placement so the camera view is filled during gameplay.
- Added playable-level background music from external forest asset resources.
- Added main menu music using a personally recorded violin performance of *Half Moon Serenade*, with guitar accompaniment provided by a classmate.
- Updated the main menu from `Controls` to `How To Play`.
- Improved button visuals so the player and Echo appear to press the button rather than being hidden behind it.
- Fixed Echo scale, collision, gravity-flip orientation, and button interaction issues.
- Improved death flow, story intro skipping after death reloads, and Level 3 ending order.
- Added Chinese script comments to support code explanation during presentation.
- Updated credits, testing notes, and final report evidence.

## Scope Decisions

### Kept

- Three playable levels.
- Echo recording and replay.
- Pressure plate and door puzzles.
- Gravity Flip traversal.
- Chests and weighted loot.
- Enemy behavior and player attack.
- Death, restart, and loot-loss feedback.
- Story intros, tutorial popups, ending dialogue, audio, and visual polish.

### Simplified or Removed

- Random chest spawning was removed because fixed chest placement gave better level design control.
- Large inventory and saving systems were avoided to keep the vertical slice stable.
- Complex enemy AI was kept small and readable.
- Multiple Echo copies were not added because one Echo was enough to demonstrate the core mechanic.
- Large open-world exploration was cut in favour of three short controlled levels.

## Submission Preparation Checklist

- Keep the final Unity scenes in Build Settings.
- Keep `Library`, `Temp`, `Obj`, `Logs`, and build-cache folders out of Git.
- Keep external asset and AI declarations updated in `Docs/Credits.md`.
- Keep testing evidence updated in `Docs/TestingLog.md`.
- Build the final Windows playable version.
- Record or prepare the five-minute demo presentation.
