# 2D Game Improvement Change Log

## What the game did before

The project was a 2D space shooter with a main menu, level select, player movement, mouse aiming, shooting, enemies, score, high score, pause, win, and game over screens.

## What changed

- Extended the existing in-game HUD from `GameManager` at runtime instead of adding a separate overlay.
- The added HUD text copies the existing score and high score font, size, color, and placement style.
- The HUD now shows player lives or health, the objective, enemy defeat progress, and current power-up status alongside the existing score and high score.
- Added an editor setup script that creates visible `Lives Text`, `Objective Text`, and `Power-Up Text` objects inside the `InGameUI` prefabs when Unity opens the project.
- Added clear objective text during play: the player can see how many enemies must be defeated.
- Filled in the Main Menu Instructions page with the objective, controls, HUD explanation, and Rapid Fire power-up explanation.
- Added a new gameplay feature: Rapid Fire power-ups.
- Rapid Fire pickups spawn near the player during playable levels.
- Added a visible `RapidFirePowerUp` prefab under `Assets/Resources/PowerUps`.
- When collected, the pickup temporarily lowers the player's fire cooldown, so the ship shoots faster.
- Added HUD feedback when a pickup appears, when Rapid Fire starts, when it counts down, and when it ends.
- Improved menu safety by hiding the Level 3 button when `Level3` is not included in Build Settings.
- Improved scene loading safety so missing scenes show a warning instead of breaking the flow.
- Improved UI page safety so missing page names or indexes show warnings instead of throwing errors.
- Made score reset and high score saving safer if a menu button calls them before a `GameManager` exists.

## Why these changes improve the player experience

- The player now knows the goal without guessing.
- The HUD gives more useful information during play while still matching the existing visual style.
- The Rapid Fire pickup changes how the player plays, not only how the game looks.
- The pickup gives immediate feedback through a visible rotating icon, HUD messages, and faster shooting.
- The menu is less confusing because it no longer offers an unavailable Level 3.
- The full player flow is easier to test: menu, instructions, level select, play, win or lose, and return to menu.

## What to test

1. Open `MainMenu`.
2. Check that Instructions opens and returns correctly.
3. Check that Level Select only shows levels that exist in Build Settings.
4. Start `Level1`.
5. Confirm the HUD still uses the original score style and now also shows lives, objective progress, and bonus text.
6. Wait for the Rapid Fire pickup to appear.
7. Collect the pickup and confirm the player shoots faster for a short time.
8. Defeat enough enemies to trigger the victory screen.
9. Restart or return to the menu.
10. Test player death to confirm the game over screen still appears.
