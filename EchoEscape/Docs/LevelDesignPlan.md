# Echo Escape Level Design Plan

## Current Scope

These levels are prototype map layouts only. They are built to support a small playable vertical slice later, but the current work does not implement recording, ghost replay, random loot, inventory, enemy AI, saving, menu flow, or complex UI.

Use the Unity menu item `Echo Escape > Build Prototype Levels` to rebuild the level layout scenes.

## Level1_Tutorial

Purpose: teach basic movement and jumping in a simple left-to-right route.

Route:

- The player starts on the far left at the `PlayerStart placeholder`.
- A forest pixel-art background fills the camera view.
- A flat start runway gives room to test movement.
- Larger grass platform sections form a readable platformer route.
- Several `PlatformBlock` objects create a short jump practice sequence.
- A small pit contains a `DeathZone` trigger placeholder.
- A higher platform after the pit leads toward the right-side `Exit placeholder`.
- Three small question-mark markers show the intended movement, jump, and exit teaching points without large on-screen instructions.

No doors, pressure plates, chests, enemies, or puzzle blockers are used in this level.

Visual assets used:

- `Assets/Art/Backgrounds/pure-pixel-forest.png` for the forest background.
- `Assets/Resources/BrackeysPlatformer/Sprites/platforms.png` as the source for generated grass platform tiles.
- `Assets/Resources/BrackeysPlatformer/Sprites/coin.png` for small question marker and exit placeholder visuals.
- `Assets/Resources/BrackeysPlatformer/Sprites/fruit.png` for the player start marker.

Pixel-art import settings to check in Unity:

- Sprite Mode should be `Single` for the generated Level1 tile sprites.
- Filter Mode should be `Point (no filter)`.
- Compression should be off or low enough that the pixel art stays crisp.
- Generated tile sprites use `16` Pixels Per Unit; the forest background uses `100` Pixels Per Unit.

## Level2_EchoPuzzleIntro

Purpose: prepare the first future Echo/Ghost puzzle layout without implementing the mechanic yet.

Route:

- The player starts on the left in a recording preparation space.
- A `PressurePlatePlaceholder` is placed near the main path and labelled `Future Echo stands here`.
- A `DoorPlaceholder` blocks the route and is labelled `Door opens when pressure plate is held`.
- The exit is placed on the right side beyond the door.
- A small observation ledge marks the central puzzle area visually.

Future intent: the player will eventually record an Echo route, place the Echo on the pressure plate, and pass through the door. At this stage, the plate and door are only physical placeholders and do not open or react.

## Level3_RiskReward

Purpose: prepare the future chest risk-reward route.

Route:

- The player starts on the left and reaches a clear path split.
- The safe upper/main route uses `PlatformBlock` objects and leads toward the exit.
- The risky lower branch uses lower platforms, `HazardPlaceholder` markers, and a `DeathZone`.
- A `ChestPlaceholder` sits on the lower branch.
- A return platform guides the player back toward the main route and exit.

Future intent: the player can choose between reaching the exit safely or risking the lower branch to collect chest rewards. At this stage, the chest does not drop items and hazards do not apply gameplay effects.

## Placeholder Objects

- `PlayerStart` marks where a player spawn script should later position the player.
- `Exit` marks the future level completion trigger.
- `DoorPlaceholder` marks a future door blocker.
- `PressurePlatePlaceholder` marks a future trigger that can be held by the player or Echo.
- `ChestPlaceholder` marks a future interactable reward chest.
- `HazardPlaceholder` marks future damage or death hazards.
- `DeathZone` marks future fall/death trigger areas.
- `GroundBlock` is solid ground.
- `PlatformBlock` is a solid jump platform.
- `Background Placeholder` gives each scene a simple readable backdrop.

## Not Implemented Yet

- Player spawning from `PlayerStart`.
- Exit completion logic.
- Recording player actions.
- Echo/Ghost clone replay.
- Door opening from pressure plates.
- Chest interaction and random rewards.
- Inventory or secured loot state.
- Hazard damage/death behavior.
- Level transition or save systems.

## Next Gameplay Scripts

Suggested next scripts after the map layouts are reviewed:

- `PlayerSpawnPoint` or a lightweight scene bootstrapper to place the player at `PlayerStart`.
- `LevelExit` to detect reaching `Exit`.
- `DeathZoneTrigger` and `HazardTrigger` for restart/death behavior.
- `DoorController` and `PressurePlateTrigger` for the first Echo puzzle.
- Recording and Echo replay scripts after the Level2 layout is approved.
- `ChestInteractable` and reward-carrying scripts after the Level3 layout is approved.
