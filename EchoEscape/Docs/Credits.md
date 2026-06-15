# Echo Escape Credits and External Resource Declaration

This file records the external resources, licences, and AI assistance used in Echo Escape. It is intended to support the final report, professionalism portfolio, and README credits section.

## Where Licence Evidence Is Stored

- Main declaration: `EchoEscape/Docs/Credits.md`
- Brackeys local licence file: `EchoEscape/Assets/Resources/BrackeysPlatformer/LICENSE & CREDITS.txt`
- Lukya Forge source and licence evidence: the original `Ancient Forest 1.6` download page and local `License.txt` from the downloaded asset pack.
- Repository-level licence: not added. The project contains third-party assets under different permissions, so applying one MIT/CC0 licence to the whole repository would be misleading.

## External Resources

### 1. Unity 2022.3 LTS and 2D Project Setup

- Name of resource: Unity 2022.3 LTS / Unity 2D project setup
- Type: engine / editor / project template
- Source: Unity Hub and Unity 2022.3 LTS editor
- Licence or permission: Unity software terms and Unity project template usage
- What it provided: Unity editor, 2D scene workflow, physics, UI, audio, animation, build settings, and project structure
- What I used unchanged: Unity editor systems, built-in components, and standard project folders
- What I modified: project scenes, scripts, resources, settings, and gameplay logic were built for Echo Escape
- What I created myself: game-specific mechanics, scene flow, puzzles, UI logic, death flow, loot rules, and script integration
- Where it appears in my game: entire Unity project
- How it is credited: listed in this credits file and final submission documentation

### 2. Brackeys Platformer Bundle

- Name of resource: Brackeys Platformer Bundle
- Type: asset pack / sprites / audio / font
- Source: https://brackeysgames.itch.io/brackeys-platformer-bundle
- Licence or permission: CC0 1.0 Universal, confirmed by local `LICENSE & CREDITS.txt`
- What it provided: prototype platformer sprites, placeholder player/enemy/item art, sound effects, music, and Pixel Operator font
- What I used unchanged: pixel font, several sound effects, and some prototype assets during development
- What I modified: assets were imported into `Resources`, reorganised, and partly replaced by forest/Ruby-specific art
- What I created myself: Echo Escape gameplay systems, level design, UI flow, and final scene integration
- Where it appears in my game: UI font, some sound effects, early prototype visuals, and resource fallback paths
- How it is credited: this file and the original local `LICENSE & CREDITS.txt` kept inside the project

### 3. Ancient Forest 1.6 by Lukya Forge

- Name of resource: Ancient Forest 1.6
- Type: asset pack / character sprites / enemy sprites / tiles / animated props
- Source: https://lukyaforge.itch.io/ancientforest and the downloaded local asset folder
- Licence or permission: local `License.txt` permits personal and commercial project use, permits modification, forbids redistribution or resale of the asset pack itself, forbids NFT/tokenized use, and says credit is appreciated
- What it provided: Ruby player sprites, Cursed Ghost enemy sprites, Green Slime sprites, forest tiles, chest animation, button visuals, and other pixel-art resources
- What I used unchanged: original sprite sheets and animation frames as source art
- What I modified: imported and sliced resources for Unity use, selected frames for animations, adjusted scale/position/sorting, and integrated sprites into gameplay objects
- What I created myself: player controller, animation controller logic, enemy component scripts, combat rules, loot rules, and level scripting
- Where it appears in my game: player character, Echo visual, enemies, chests, buttons, platforms, and level art
- How it is credited: this file, README credits reference, and final report/portfolio asset declaration

### 4. Dark Forest 1.0 by Lukya Forge

- Name of resource: Dark Forest 1.0
- Type: asset pack / background art / music
- Source: downloaded as `Dark Forest 1.0.zip` from the same Lukya Forge itch.io download page as `FREE: Ancient Forest 1.6`
- Licence or permission: treated as Lukya Forge asset-pack content from that same download/source page; keep the itch.io page screenshot/download record and local package as evidence before public release
- What it provided: dark forest background layers and forest soundtrack files
- What I used unchanged: background images and the selected dark forest music track
- What I modified: background placement, brightness/visibility, scene scaling, Unity import settings, and music integration
- What I created myself: scene composition, camera/background placement, audio playback logic, and level-specific use
- Where it appears in my game: main menu and Level 1 to Level 3 forest backgrounds, plus shared background music
- How it is credited: this file and final report/portfolio asset declaration

## AI Assistance Declaration

### OpenAI ChatGPT / Codex

- Tool used: OpenAI ChatGPT / Codex
- What I asked: help with Unity C# debugging, scene setup, README/documentation, script comments, Kanban wording, gameplay polish, UI feedback, and build checks
- What output I used: code suggestions, debugging explanations, documentation drafts, task wording, and implementation guidance
- What I changed: reviewed and adapted generated suggestions to fit the Echo Escape project, scene objects, scripts, and gameplay requirements
- How I tested it: Unity play mode checks where possible, C# compilation with `dotnet build`, scene inspection, and gameplay testing of movement, Echo replay, loot, death, portals, and UI
- What I understand: how the main systems interact, including player movement, Echo recording/replay, gravity flip, enemy behavior, chest loot, death/restart flow, and UI feedback
- What I still do not fully understand: some Unity editor serialization details and external asset licence details should still be checked carefully before public commercial release
- Where it appears in the project: script comments, selected code structure improvements, README/documentation, and some gameplay/UI polish

## Original Work / Own Implementation

The following parts were implemented specifically for Echo Escape:

- Three-level Echo Escape vertical slice and level progression.
- Player movement, jump, attack, chest interaction, and death/restart flow.
- Echo recording and replay mechanic.
- Echo pressure-plate puzzle integration.
- Gravity Flip traversal and Gravity Flip void death zones.
- Pending loot and secured loot risk-reward system.
- Weighted collectible selection from chests.
- Cursed Ghost enemy behavior split into targeting, movement, attack, health, animation, and controller components.
- Story intro sequence, tutorial popups, recording/replay UI, loot feedback, death feedback, and ending flow.
- Main menu, How To Play panel, shared music, and scene transitions.

## Final Submission Note

For the final report and professionalism portfolio, this file should be used as the asset/resource and AI declaration source. If any additional external assets are added later, they should be added here using the same field format.
