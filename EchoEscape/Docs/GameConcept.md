# Echo Escape Game Concept

## 1. Game Title

Echo Escape

The main menu presents the game with the stylised title `FOREST VAULT: ECHO`, but the project and documentation title is Echo Escape.

## 2. One-Sentence Game Idea

Echo Escape is a 2D puzzle-platformer where the player records a short copy of their own movement, replays it as an Echo, uses Gravity Flip to reach unusual routes, collects risky forest relics, and escapes a mysterious forest vault.

## 3. Intended Player Experience

The intended experience is a small but complete vertical slice where the player feels that they are solving puzzles by cooperating with their own past actions. The player should understand the rules quickly, make short plans, test them, and recover from mistakes without feeling punished unfairly.

The forest setting is designed to feel mysterious rather than horror-focused. Story intro panels, dark forest backgrounds, glowing portals, relics, and wizard dialogue frame the mechanics as a magical escape challenge.

## 4. Core Mechanic

The core mechanic is Echo recording and replay:

- The player presses `Q` to record a short sequence of movement.
- The game stores position, facing direction, gravity state, and relevant movement data over time.
- The player presses `E` to replay that sequence as an Echo copy.
- The Echo can stand on pressure plates and help open doors while the real player continues moving.

Gravity Flip extends this core by changing how the player navigates vertical space. Loot adds risk because newly gained items are pending until the player reaches an exit.

## 5. What The Player Does Moment To Moment

- Move left and right with `A` / `D` or arrow keys.
- Jump with `Space`.
- Read short tutorial or story prompts with `C`.
- Record a route with `Q`.
- Replay the Echo with `E`.
- Use the Echo to hold buttons and open doors.
- Use Gravity Flip with Up Arrow / Down Arrow to move along upper routes.
- Open chests with `F`.
- Attack enemies with `J`.
- Avoid hazards such as rivers, pits, enemies, and Gravity Flip void zones.
- Reach the exit to secure loot and move to the next level.

## 6. Target Player

The target player is someone who enjoys short 2D platformer puzzles, timing challenges, and clear mechanical rules. The game is designed for a coursework vertical slice, so the target experience is accessible, readable, and easy to demonstrate within a few minutes.

## 7. Reference Games and Inspirations

- 2D puzzle-platform games that combine movement and switch puzzles.
- Echo / clone puzzle ideas where a past version of the player helps solve a problem.
- Forest fantasy platformer visuals using pixel art, glowing objects, and layered backgrounds.
- Risk-reward treasure routes where the player can choose a safer path or a more dangerous reward path.

## 8. Original or Creative Element

The original part of the idea is how the systems are combined in a small vertical slice. Echo recording is not only a visual ghost; it directly affects pressure plates and doors. Gravity Flip creates a second movement rule, and loot creates a reason to care about survival after collecting a reward. Together, the player is not just reaching an exit; they are planning, using their past movement, deciding whether to take treasure, and escaping with it.

## 9. Vertical Slice Plan

The final vertical slice contains:

- A main menu with How To Play and credits.
- Level 1: movement, tutorial prompts, Echo recording/replay, pressure plate door, Gravity Flip, and exit flow.
- Level 2: chest interaction, pending loot, enemy danger, river hazard, attack, and loot-loss feedback.
- Level 3: combined risk-reward level with Gravity Flip, Magic Barrier / button interaction, loot banking, hazards, and ending dialogue.
- Shared UI for recording/replay, loot, death, tutorial, story intro, and ending feedback.
- Shared audio for menu music, level music, and gameplay sound effects.

## 10. Feature Priorities

### Must-Have

- Player movement, jumping, collision, and camera follow.
- Echo recording and replay.
- Echo interaction with pressure plates.
- Door / button puzzle.
- Hazard death and restart flow.
- Level transitions.
- Chest interaction and loot feedback.
- Clear tutorial or story instructions.

### Should-Have

- Gravity Flip traversal.
- Enemy behavior and player attack.
- Pending and secured loot distinction.
- Death animation and death UI.
- Recording / replay UI.
- Forest pixel-art backgrounds and music.

### Could-Have

- More collectible types.
- More enemy variations.
- More complex puzzle layouts.
- More polished visual effects.
- More music and sound variations.

### Cut-First

- Large inventory system.
- Persistent save system.
- Multiple Echo copies at once.
- Procedural or random chest placement.
- Large open-world map.
- Complex enemy pathfinding.

## 11. Unity Development Plan

The project uses Unity components and scenes:

- Scenes define each level layout and story section.
- Rigidbody2D and Collider2D handle platforming, triggers, and hazards.
- MonoBehaviour scripts control player input, Echo recording, replay, Gravity Flip, loot, enemies, UI, audio, and scene flow.
- Build Settings define the final scene order from menu to Level 3.
- Resources hold imported sprites, animation frames, fonts, audio, and background music.

## 12. Main Systems and Scripts

- `PlayerController2D`: movement, jumping, facing direction, interaction input, and connection to recording.
- `PlayerAttack`: attack hitbox, attack timing, and enemy damage.
- `PlayerAnimationController`: Ruby idle, run, jump, attack, and death sprites.
- `ActionRecorder`: records player movement frames and starts Echo replay.
- `EchoReplayController`: replays recorded frames as an Echo object.
- `EchoAnimationController`: gives the Echo matching movement animation with transparent tint.
- `GravityFlipController`: flips gravity direction and updates player movement rules.
- `GravityFlipVoidKillZone`: kills only the real player when they fall out of flipped routes.
- `PressurePlate` and `Door`: Echo/button puzzle logic.
- `Chest`, `CollectibleDatabase`, and `CollectibleItem`: chest opening and weighted collectible selection.
- `EnemyController`, `EnemyTargeting`, `EnemyMovement`, `EnemyAttack`, `EnemyHealth`, and `EnemyAnimationController`: enemy behavior split into focused components.
- `EchoEscapeGameManager`: death flow, loot state, UI setup, and scene reload handling.
- `GoalZone`: level completion, loot securing, and scene transition.
- `LevelIntroSequence`, `TutorialPopupManager`, `TutorialPopupTrigger`, `LootFeedbackUI`, and `RecordingStatusUI`: story and gameplay UI.
- `BackgroundMusic` and `PrototypeAudio`: music and gameplay sound feedback.

## 13. Asset and Resource Plan

- Use Ancient Forest 1.6 and Dark Forest 1.0 resources for Ruby, enemies, tiles, backgrounds, props, and forest atmosphere.
- Use Brackeys Platformer Bundle for CC0 fonts, prototype assets, and some sound effects.
- Use personally recorded violin menu music, with classmate guitar accompaniment declared in credits.
- Keep all external resources documented in `Docs/Credits.md`.
- Use original level design, gameplay scripts, integration work, UI flow, and testing process.

## 14. Legal, Ethical, Social, Accessibility, and Security Considerations

- External assets are credited and licence evidence is documented.
- AI assistance is declared in `Docs/Credits.md` and final coursework documentation.
- The project does not collect personal data.
- Controls are simple and listed in the How To Play panel and project documentation.
- UI uses large pixel text and high-contrast panels.
- Known limitation: the project is a vertical slice, not a full commercial game.
- Known copyright note: the menu recording is my own violin performance, but *Half Moon Serenade* is not my own composition, so wider public or commercial use would need rights checking.

## 15. Development Schedule

- Week 1: repository setup, graybox layout, core movement, basic Echo recording/replay, and first pressure plate puzzle.
- Week 2: tutorial UI, loot, enemies, Ruby visuals, Level 2 gameplay, and player feedback.
- Week 3: Level 3 risk-reward route, Gravity Flip void death, Magic Barrier / button logic, and improved collectibles.
- Week 4: backgrounds, music, UI polish, scene renaming, testing, script comments, documentation, credits, and final submission preparation.
