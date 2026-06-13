# Testing Log

Use this file to record testing evidence throughout development.

| Date | Build/Area Tested | Result | Change Made |
| --- | --- | --- | --- |
| 2026-05-05 | Project setup | Unity project connected to GitHub repository. | Added Unity gitignore and planning documents. |
| 2026-05-05 | Project structure | Separated class activity materials from the final Unity game project. | Moved the Unity project into its dedicated Unity project subfolder. |
| 2026-05-05 | First prototype implementation | Added scripts for player movement, echo replay, random chests, hazards, and extraction. | Needs Unity play test after scene generation. |
| 2026-05-05 | Tutorial flow | Added step-by-step tutorial prompts for movement, recording, replay, door puzzle, chest loot, and extraction. | Needs play testing in the editor for pacing and clarity. |
| 2026-05-06 | Brackeys asset integration | Imported CC0 platformer sprites, sounds, music, and fonts into `Assets/Resources/BrackeysPlatformer`. | Added runtime visual skinning and audio hooks for the prototype. |
| 2026-05-06 | Script compile check | `dotnet build Assembly-CSharp.csproj` and `dotnet build Assembly-CSharp-Editor.csproj` completed successfully. | 0 errors and 0 warnings. Needs Unity editor play test after script reload. |
| 2026-05-06 | Player animation refinement | Replaced two-frame hard switching with 32x32 sprite-sheet frame animation for idle and run states. | Enabled Rigidbody2D interpolation for smoother player and echo movement. Compile check passed with 0 errors and 0 warnings. |
| 2026-05-06 | Basic GitHub Actions CI | Added a repository hygiene workflow for required Unity structure, documentation, generated folder checks, and large tracked file checks. | Local equivalent checks passed; Unity C# compile checks passed; GitHub Actions will run on the pull request. |
| 2026-05-07 | Pixel-art main menu | Added `MainMenu.unity` with Start Game, Controls, Credits, Quit, animated knight, pixel-art scene dressing, and menu music. | Build settings now load `MainMenu` before `PrototypeScene`; needs interactive editor playtest for button feel. |
| 2026-06-13 | Level 1 to Level 3 gameplay flow | Confirmed current scene list uses `MainMenu`, `Level1_Tutorial`, `Level2_LootTutorial`, and `Level3_RiskReward`. | README updated to match current scene names and mechanics. |
| 2026-06-13 | Script compile check | `dotnet build .\Assembly-CSharp.csproj` completed successfully. | 0 errors and 0 warnings. |
| 2026-06-13 | Editor script compile check | `dotnet build .\Assembly-CSharp-Editor.csproj` completed successfully after rerunning sequentially to avoid a temporary DLL file lock. | 0 errors and 0 warnings. |
| 2026-06-13 | Code explanation readiness | Added Chinese summary comments, function comments, and inline comments to gameplay scripts. | Covered player movement, Echo recording, Gravity Flip, loot, enemy behavior, death flow, UI, audio, and scene flow. |
| 2026-06-13 | Audio and presentation pass | Added shared background music handling and documented the current visual/audio flow in README. | Background music is managed through a persistent `BackgroundMusic` object. |
