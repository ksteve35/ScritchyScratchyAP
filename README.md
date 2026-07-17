# Scritchy Scratchy AP

An Archipelago multiworld randomizer mod for the Steam game **Scritchy Scratchy** by Lunch Money Games/Funday Games.

Consists of two components:
- **`plugin/`** - BepInEx IL2CPP plugin (C#) that runs inside the game, hooks into game logic, and syncs with an Archipelago server.
- **`apworld/`** - the `.apworld` world definition (Python) for the Archipelago generator, defining items, locations, and logic.

## Installation

### 1. Install BepInEx (IL2CPP build)

Scritchy Scratchy is built with Unity IL2CPP, so you need the **IL2CPP** flavor of BepInEx, not the regular/Mono one most tutorials link.

1. Go to the [BepInEx bleeding-edge builds page](https://builds.bepinex.dev/projects/bepinex_be) and download the latest **IL2CPP x64** build. This mod was built against `6.0.0-be.755`, but any recent IL2CPP build should work.
2. Open the downloaded `.zip`. It contains more than just a plugin loader and you need **everything** in it:
   - `BepInEx/` - the mod loader itself, and where your plugins live.
   - `dotnet/` - a bundled .NET/CoreCLR runtime that BepInEx needs to actually run IL2CPP plugins. **This folder is easy to miss because it's easy to assume it's optional when it isn't.** Without it, the game just launches normally with no mod loaded and no error message.
   - `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` are three small files that sit next to the game's `.exe`. These are what hijack the game's startup to load BepInEx at all.
3. Extract **the entire contents of the zip**, not just the `BepInEx` folder, straight into your game's install folder. When you're done, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `dotnet/`, and `BepInEx/` should all sit right next to `ScritchyScratchy.exe`, not nested inside another folder.
   - Default Steam path: `...\steamapps\common\Scritchy Scratchy\`
4. Launch the game once, then close it. This lets BepInEx do all of its configuration stuff on the first run.
   - **How to tell it worked:** a console/terminal window should briefly pop up alongside the game window on launch, and a `BepInEx\LogOutput.txt` file should now exist.
   - **If nothing happened** (no console window, no `LogOutput.txt`): double-check that `dotnet\`, `winhttp.dll`, `doorstop_config.ini`, and `.doorstop_version` are directly next to the game's `.exe` and not inside a subfolder.

### 2. Install the mod plugin

1. Build `ScritchyScratchyAP.dll` from `plugin/` (see [BUILDING.md](BUILDING.md)), or download a pre-built release.
2. Copy `ScritchyScratchyAP.dll` into `BepInEx/plugins/`.

### 3. Install the apworld

1. Copy `apworld/` into your Archipelago install's `custom_worlds/` folder (or double click a `.apworld` zip for Scritchy Scratchy to have Archipelago do this for you).

### 4. Connect

Launch the game and enter your Archipelago server host/port/slot name in the connection menu. Pressing `F1` will toggle this menu on and off.

## Building the plugin

See [BUILDING.md](BUILDING.md).
