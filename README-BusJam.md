# Bus Jam — Rollic Case Study

A portrait Unity puzzle game: select passengers with a clear route, match them to the current bus, and manage the limited waiting area before time runs out.

## Installation and first run

1. Install **Unity Hub** and Unity Editor **6000.6.0f1**. This exact version is recorded in `ProjectSettings/ProjectVersion.txt`.
2. Clone the repository (GitHub access is required while it is private):
   ```sh
   git clone https://github.com/pinkceviz-blip/Rollic-Case-Study.git
   cd Rollic-Case-Study
   ```
3. In Unity Hub, select **Add → Add project from disk** and choose the cloned folder containing `Assets`, `Packages`, and `ProjectSettings`.
4. Open it with **6000.6.0f1**. Allow the first asset import and Package Manager restore to finish. Internet is needed for the initial package restore; dependencies and locked versions are included in `Packages`.
5. Open `Assets/Scenes/BusJam.unity`. Set Game view to a portrait resolution such as **1080 × 1920**, then press **Play**.
6. Select **PLAY** (or **CONTINUE** for an existing save). Settings allow Turkish / English, sound and light / dark mode. Mouse and touch input are supported.

If the scene has not been set up, use **Bus Jam → Install or open game**. Existing level assets are preserved. Do not clone the repository inside another Unity project; open the repository root itself.

The project uses Universal Render Pipeline, Input System and uGUI. Unity restores the packages automatically. No API keys, paid services or backend configuration are required for local play. `Library`, `Temp`, logs and local editor settings are generated on the receiving machine and are intentionally excluded from Git.

### Building

Install the desired platform support through Unity Hub before switching targets. In **File → Build Profiles**, select the target, ensure `Assets/Scenes/BusJam.unity` is enabled in the scene list, and build to a directory outside `Assets` (for example `Builds`). `LevelEditor.unity` is an editor-only scene and must stay disabled. Android requires Android Build Support and its SDK/NDK/OpenJDK modules; iOS requires iOS Build Support and Xcode on macOS for the exported project. Store signing, device performance and store submission are not configured or verified by this repository.

### Troubleshooting

- Wait for compilation/import to complete before pressing Play; use the required editor version if package or compilation errors appear.
- If keyboard or pointer input does not reach the game, focus the Game view; the project already includes Input System settings.
- Saved progress and preferences persist between launches via local PlayerPrefs. **Bus Jam → Reset saved progress** clears the active level save; it does not uninstall the game.

## Gameplay

- Ten mixed-color levels, all available from the Levels menu.
- Each level starts with at least 15 visible passengers. Buses carry three passengers; the temporary waiting area has six places.
- Only passengers with a clear orthogonal route can be selected. Rapid consecutive selections are supported; accepted moves reserve their destinations.
- New and restarted levels start the timer on the first valid selection. Menus, pause and application backgrounding stop it. Continuing a started game waits for a tap in the gameplay area before resuming the saved countdown and animations.
- Levels 5–10 include three bottom entrances, each showing its remaining hidden passengers. An entrance refills an existing rear passenger cell only when it is vacant and the entry route is clear. Entrances do not create extra rows. Empty passenger squares are hidden.
- Passengers appear in bus seats after boarding. Buses have animated doors and a visible incoming queue.
- Pause, retry, restart confirmation, success, level selection and runtime error dialogs use the illustrated UI.

## Persistence and settings

Progress is saved after accepted moves, periodically during play, when opening menus, when backgrounded and on shutdown. The save contains the level, remaining passengers, waiting queue, bus, seats, timer state and entrance queues. In-flight accepted actions restore as completed logical actions; visual animation positions are not replayed.

Lives start at **10**, with a maximum of 10. Restarting costs one life after confirmation. One life recharges every four hours, including time spent offline. The current update includes a one-time top-up of existing life balances to 10; it does not replenish lives on every launch.

Settings provide Turkish / English, background music on / off, and light / dark themes. Preferences persist. Street and station lamps illuminate the scene in dark mode; their state follows the selected theme, not the device clock.

Saved data uses PlayerPrefs keys prefixed with `Rollic.BusJam`. **Bus Jam → Reset saved progress** resets the active level save.

## Level editor

Open **Bus Jam → Open level editor**, then select **Level editor — select me** in the Hierarchy.

- Assign a `LevelData` asset in the Inspector.
- Edit the timer, dimensions, waiting capacity, bus capacity, bus order and entrance color queues.
- Paint passengers or obstacles in Scene view with the left mouse button; right-click removes a cell. Row zero faces the road.
- **Save level** updates the asset. **Create new level** adds an asset under `Assets/Resources/BusJamLevels`; alphabetical filenames determine order.
- **Test this level** starts the selected level and replaces the current local level save.
- **Oyuncu hakları (test)** provides a 0–10 life slider and **Hakları uygula** button for the local player save.
- **Bus Jam → Validate levels and rules** validates counts and uses a bounded solver, including entrance refill rules, to check solvability. A search-budget limit is reported separately from an invalid layout.

The editor scene is `Assets/Scenes/LevelEditor.unity` and is excluded from the player build.

## Structure

- `LevelData`: ScriptableObject level definitions and validation.
- `PuzzleRules`: grid route finding.
- `EntranceRules`: occupied-cell gating, hidden queues and entry routes.
- `BusJamGame`: game flow, persistence, input, animation and gameplay UI.
- `LivesService` / `GamePreferences`: life recharge, settings, localization and music.
- `MenuCityView` / `CityMainMenu`: cached moving city, traffic and menu screens.
- `LevelEditorBoard`, `BusJamEditor`, `LevelSolver`, `EntranceLevelSolver`: editing and validation.
- `Assets/BusJam/Resources`: runtime art, fonts, prefabs, shaders and illustrated UI.

## Validation

Latest checks (12 November 2026) used an isolated Unity project with the current game sources, leaving the original player save untouched:

- All ten levels passed the built-in solver and rule validation; runtime shaders compiled.
- English / Turkish LEVEL and LIVES heading centers and heights matched; guide descriptions fit their measured text areas; all five menu pages opened.
- Ten initial lives, zero-life spending protection, the exact four-hour recharge boundary and offline refill cap passed.
- Saved, paused and menu-resumed games held their countdown until an actual gameplay tap; fresh games still waited for the first valid move.
- Seven immediate selections, duplicate protection, concurrent walking, FIFO boarding, mid-animation restore, waiting-capacity reservations and reset cancellation passed.

The isolated editor emitted a Unity Quick Search indexing exception on startup; its stack was inside `UnityEditor.Search`, and the game validation and play-mode checks still completed successfully.


Validated in an isolated Unity editor project: all ten level definitions, runtime completion of entrance levels 5–10 under the current refill rule, occupied-cell-only passenger squares, rapid input, duplicate input protection, mid-animation save/load, first-move timer, life recharge boundaries, settings, error recovery and repeated mouse/simulated-touch UI cycles. Menu reuse was checked for stable generated meshes/materials and no scheduled background rendering under popups.

Physical iOS/Android performance, battery testing and store builds have not been performed. Editor QA is not a device FPS measurement.
