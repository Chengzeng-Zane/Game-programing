# Credits

## Existing project resources

This submission is based on the existing 2D space shooter class project in this Unity folder.

Existing art, audio, prefabs, UI assets, scripts, and scene structure were already part of the project before this improvement pass. These include the player ship, enemies, projectiles, asteroid borders, space backgrounds, UI images, sound effects, music, and win or lose effects.

## Text and font resources

- TextMesh Pro is used for UI text.
- The TextMesh Pro package includes the Liberation Sans font asset already present in the project.

## Imported package resources

- The project includes an imported Developer Console package already present before this improvement pass.

## New resources added in this update

- No new external art, audio, font, tutorial code, or third-party package was added.
- The Rapid Fire pickup prefab reuses the existing `Assets/Art/Reticles/Reticle_Gold.png` asset already present in the project.
- The added HUD text reuses the existing TextMesh Pro font style already present in the project.

## Code changes

The new improvement code was written for this assignment inside:

- `Assets/Scripts/Utility/GameManager.cs`
- `Assets/Scripts/PowerUps/RapidFirePowerUpPickup.cs`
- `Assets/Editor/ImprovementAssetSetup.cs`
- `Assets/Resources/PowerUps/RapidFirePowerUp.prefab`
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/LevelLoadButton.cs`
