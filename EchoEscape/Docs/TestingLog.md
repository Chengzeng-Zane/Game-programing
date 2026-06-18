# Echo Escape Testing Log

This log records important testing and iteration evidence for Echo Escape. It focuses on the main coursework requirement: testing, debugging, and showing what changed because of testing.

| Date | Build / Area Tested | Result | Change Made |
| --- | --- | --- | --- |
| 2026-05-05 | Project setup | Unity project was connected to GitHub and opened correctly. | Added Unity `.gitignore` and the first planning documents. |
| 2026-05-05 | First graybox prototype | Basic platform route, player movement, collision, and simple placeholders worked as a starting point. | Kept a graybox evidence version and continued building the playable Level 1 flow. |
| 2026-05-05 | Early Echo recording | Recording and replay worked as a visual idea, but needed to affect gameplay. | Connected Echo replay to pressure plates so the Echo could open doors. |
| 2026-05-06 | Script compile check | `dotnet build Assembly-CSharp.csproj` and `dotnet build Assembly-CSharp-Editor.csproj` completed successfully. | Confirmed scripts compiled before deeper play testing. |
| 2026-05-07 | Main menu flow | Menu loaded before gameplay and could start the first level. | Added Start Game, Controls/How To Play, Quit, and credits-style presentation. |
| 2026-06-10 | Scene progression | The project moved from old scene names to story-based names. | Updated Build Settings to `MainMenu`, `Level 1 - The First Echo`, `Level 2 - Relics of the Forest`, and `Level 3 - Escape from the Silent Forest`. |
| 2026-06-11 | Level intro sequence | Story intro replayed after death reloads, slowing down repeated attempts. | Changed intro logic so story panels are skipped after death reloads. |
| 2026-06-11 | Level 3 ending order | Loot feedback appeared after the ending text, which made the reward feel delayed. | Changed Level 3 flow so loot feedback appears before the final wizard ending message. |
| 2026-06-12 | Recording UI | Recording only printed logs, which would not be visible in the final build. | Added `RecordingStatusUI` with REC / PLAY feedback and elapsed time display. |
| 2026-06-12 | Record duration setting | Changing `maxRecordSeconds` did not clearly update the UI behaviour. | Connected the UI to `ActionRecorder.RecordingElapsedSeconds` and `MaxRecordSeconds` so it follows the recorder setting instead of hard-coded time. |
| 2026-06-12 | Player animation | Idle and run did not look correct with the selected Ruby frames. | Adjusted animation selection, frame rates, and idle frame handling. |
| 2026-06-12 | Echo visual | Echo scale and position did not match the player consistently, especially after Gravity Flip. | Updated Echo visual offset, tint, orientation, and animation logic. |
| 2026-06-12 | Player collider and attack hitbox | The collider was taller than the character and the attack box appeared too high. | Tuned capsule collider size/offset and attack hitbox offset to better match the sprite. |
| 2026-06-12 | Echo pressure plate bug | After collider tuning, the Echo could appear to stand on a button but not activate it. | Adjusted pressure plate trigger detection and Echo collider/visual alignment so Echo can press buttons reliably. |
| 2026-06-13 | Level 1 Gravity Flip death | Walking out of the upper flipped route did not trigger the same death behaviour as normal hazards. | Added/fixed `GravityFlipVoidKillZone` coverage and made it call the shared death flow. |
| 2026-06-13 | Level 3 Gravity Flip route | Gravity Flip area needed void death coverage without killing the player during normal movement. | Added Level 3 gravity-specific trigger zones that only kill the real player when flipped. |
| 2026-06-13 | Bottom red death visual | A red placeholder death-zone bar looked unfinished. | Hid/deleted the visual part while keeping the invisible hazard logic. |
| 2026-06-13 | Button visual | Button art covered the player's legs and did not look like a pressed plate. | Repositioned and layered the button visuals so the player and Echo appear to stand on it. |
| 2026-06-13 | Scene backgrounds | Backgrounds did not cover the whole game view at the level start and exit. | Resized/repositioned background objects across the levels. |
| 2026-06-13 | Level 3 moon composition | The moon was hidden behind an upper platform. | Adjusted the Level 3 background placement so the moon remains visible. |
| 2026-06-13 | Chest / collectible selection | Loot appeared to be selected evenly instead of using intended rarity. | Reviewed the chest logic and kept fixed chest placement with weighted collectible selection from `CollectibleDatabase`. |
| 2026-06-13 | Enemy script structure | Enemy logic was too crowded in one script. | Split enemy behaviour into controller, targeting, movement, attack, health, and animation scripts. |
| 2026-06-13 | Restart key removal | Pressing `R` to restart could cause unwanted state changes. | Removed the manual `R` restart flow and kept death/reload controlled by the game manager. |
| 2026-06-13 | Script compile check | `dotnet build .\Assembly-CSharp.csproj` passed. | 0 errors and 0 warnings. |
| 2026-06-13 | Editor compile check | `dotnet build .\Assembly-CSharp-Editor.csproj` passed after sequential run. | 0 errors and 0 warnings. |
| 2026-06-16 | Level 1 graybox evidence | Needed a real Unity graybox version instead of a fake screenshot for prototype evidence. | Created and captured graybox evidence, then removed the temporary graybox scene/builder from the final Unity project. |
| 2026-06-16 | Graybox cleanup compile check | Removed the temporary graybox scene and editor builder after evidence capture. | Runtime and editor builds were checked after cleanup. |

## Testing Summary

The main testing pattern was:

1. Play a level or system in Unity.
2. Observe where the player experience was unclear or broken.
3. Fix the scene setup or script logic.
4. Rebuild or retest the affected area.

The most important testing changes were the Echo pressure-plate fix, Gravity Flip void death coverage, intro skipping after death, recording UI, button visual layering, weighted loot confirmation, and enemy script reorganisation.

## Known Remaining Limitations

- The game is a vertical slice and intentionally short.
- Only one Echo replay is supported at a time.
- The enemy behaviour is simple and designed for coursework demonstration.
- Some external licence evidence should be kept with screenshots/local files for final submission.
- Graybox evidence is kept as screenshots/planning material and is not part of the final playable scene list.
