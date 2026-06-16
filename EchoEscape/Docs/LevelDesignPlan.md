# Echo Escape Level Design Plan

This document describes the final level structure of Echo Escape and how each level teaches or tests the main systems. The game is designed as a short three-level vertical slice rather than a large complete game.

## Final Build Order

1. `MainMenu`
2. `Level 1 - The First Echo`
3. `Level 2 - Relics of the Forest`
4. `Level 3 - Escape from the Silent Forest`

The scene `Level 1 - Graybox Prototype` is kept separately as development evidence. It shows how Level 1 worked before final pixel art, backgrounds, story UI, and polish were added.

## Main Menu

Purpose: introduce the game identity and let the player start, read controls, view credits, or quit.

Main elements:

- Forest-themed pixel UI.
- Title presentation: `FOREST VAULT: ECHO`.
- Buttons: Start Game, How To Play, Quit.
- How To Play panel explaining movement, jump, recording, replay, chest interaction, attack, and Gravity Flip.
- Menu music using a personally recorded violin performance of *Half Moon Serenade*, with guitar accompaniment credited.

Design reason:

The menu keeps the controls visible before the player enters the first level. This supports the live presentation and makes the build easier to understand without external explanation.

## Level 1 - The First Echo

Purpose: teach the core Echo mechanic and introduce Gravity Flip safely.

Player experience:

- The player starts on a simple platform with enough space to test movement and jumping.
- Tutorial question markers introduce controls without large permanent text blocking gameplay.
- The first Echo puzzle asks the player to record a route and replay it so the Echo can hold a pressure plate.
- The door opens when the pressure plate is held, showing that the Echo affects gameplay.
- Gravity Flip introduces inverted movement and upper-platform traversal.
- Gravity Flip void zones punish walking out of the valid flipped route, using the same death/restart flow as normal hazards.
- The exit moves the player into Level 2.

Important objects and systems:

- `PlayerStart`
- `TutorialPopupTrigger`
- `ActionRecorder`
- `EchoReplayController`
- `PressurePlate`
- `Door`
- `GravityFlipController`
- `GravityFlipVoidKillZone`
- `HazardZone`
- `GoalZone`

Design reason:

Level 1 starts with low risk because the player needs to learn the controls and the Echo idea. The pressure plate puzzle is simple on purpose: it proves the core mechanic before adding loot, enemies, and higher risk.

## Level 2 - Relics of the Forest

Purpose: teach loot, enemy danger, combat, and pending-loot risk.

Player experience:

- The player enters a darker forest route with chest rewards.
- Chests contain collectible relics chosen through weighted loot selection.
- Loot starts as pending, meaning it is visible but not safely secured yet.
- The Cursed Ghost / slime enemy creates direct danger.
- The player can attack with `J` and defeat enemies.
- River or fall hazards show that dying after collecting loot can lose the pending reward.
- Reaching the exit secures the loot and moves to Level 3.

Important objects and systems:

- `Chest`
- `CollectibleDatabase`
- `CollectibleItem`
- `LootFeedbackUI`
- `EnemyController`
- `EnemyTargeting`
- `EnemyMovement`
- `EnemyAttack`
- `EnemyHealth`
- `EnemyAnimationController`
- `PlayerAttack`
- `HazardZone`
- `GoalZone`

Design reason:

Level 2 changes the player's goal from simply reaching an exit to making a risk-reward decision. The player learns that treasure matters, but also that treasure is not safe until the level is completed.

## Level 3 - Escape from the Silent Forest

Purpose: combine the main mechanics into the final challenge.

Player experience:

- The player must use movement, Echo planning, Gravity Flip, button logic, hazard avoidance, and loot collection in one route.
- The level includes a Magic Barrier / button section where correct interaction opens progress.
- The upper Gravity Flip route has void death coverage so falling out of the intended route causes a proper death and restart.
- The player can collect final loot before escaping.
- The final flow first shows loot feedback, then plays the wizard ending dialogue.
- Completing the level returns to the main menu.

Important objects and systems:

- `GravityFlipController`
- `GravityFlipVoidKillZone`
- `PressurePlate`
- `Door`
- `Chest`
- `LootFeedbackUI`
- `HazardZone`
- `GoalZone`
- `LevelIntroSequence`
- `EchoEscapeGameManager`

Design reason:

Level 3 works as the final vertical-slice proof. It does not introduce a completely new core rule; instead, it tests whether the player can combine previously learned systems under more pressure.

## Level Intro and Story Flow

Each gameplay level can show an intro sequence before control returns to the player. These story panels frame the game as a magical forest escape guided by the Echo Wizard.

Important rule:

- On first entry, the intro can play.
- After death and scene reload, the intro is skipped so the player does not repeat story text every time they fail.

This keeps the story useful during first-time play but avoids slowing down repeated attempts.

## Death and Restart Flow

Death should be consistent across hazards:

- Normal hazards use `HazardZone`.
- Gravity Flip off-route hazards use `GravityFlipVoidKillZone`.
- Enemy attacks call the same game manager death flow.
- The game shows death feedback, handles pending loot loss, plays player death feedback where available, and reloads the current scene.

This avoids separate death behaviours that would confuse the player.

## Loot Design

The current game uses fixed chest placement with weighted collectible selection.

Reason:

- Fixed chest placement makes level design easier to control.
- Weighted collectible selection still gives variety and rarity.
- This replaced the early random chest spawn idea because random placement made testing and balancing less predictable.

## Visual Design

The final levels use:

- Dark forest background art.
- Ancient Forest tiles and props.
- Ruby player sprites.
- Echo tinting to show the copy clearly.
- Pixel UI panels and large readable text.
- Hidden or transparent death zones where placeholder red visuals would look unfinished.

The aim is to keep the level readable while giving it a consistent forest-adventure mood.

## Known Level Limitations

- The game is a vertical slice, so levels are short.
- Only one Echo replay is supported at a time.
- Enemy AI is simple and designed for demonstration rather than complex combat.
- Loot is not saved permanently outside the current game run.
- The graybox prototype scene is for evidence, not final player-facing content.
