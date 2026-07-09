# Building the plugin

You only need to do this if you're modifying the plugin yourself. If you just want to play, grab a pre-built `ScritchyScratchyAP.dll` in one of this repo's releases.

## Prerequisites

1. **[.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)** is not just the runtime, but the full SDK (it includes `dotnet build`).
2. **A code editor.** [Visual Studio Community](https://visualstudio.com/downloads) (free) is the easiest for C#/BepInEx projects, but VS Code and the C# extension works too, using `dotnet build` from a terminal instead of the IDE's Build button.
3. **BepInEx already installed in your game folder**, with the game launched at least once (see [step 1 of Installation](README.md#1-install-bepinex-il2cpp-build) in the README). This is required *before* you can build, because the plugin references BepInEx's auto-generated `interop` DLLs (found in `BepInEx\interop\`), which only exist after that first launch.

## Steps

1. **Point the project at your game install.** Open `plugin/ScritchyScratchyAP.csproj` in a text editor and find this line near the top:
   ```xml
   <GameDir>D:\SteamLibrary\steamapps\common\Scritchy Scratchy</GameDir>
   ```
   Change the path to wherever *your* copy of Scritchy Scratchy is installed (right-click the game in Steam > Manage > Browse local files to find it). Every interop DLL reference and the post-build copy step below reads from this one line, so it's the only path you need to edit.

2. **Open the project.**
   - *Visual Studio:* File > Open > Project/Solution > select `plugin/ScritchyScratchyAP.csproj`.
   - *VS Code / terminal:* just `cd` into `plugin/`.

3. **Restore NuGet packages.** The project pulls dependencies from two feeds: the default nuget.org feed, and BepInEx's own feed at `https://nuget.bepinex.dev/v3/index.json`. Visual Studio usually prompts to restore automatically on open. If not, or if you're using the CLI, add the BepInEx feed once with:
   ```
   dotnet nuget add source https://nuget.bepinex.dev/v3/index.json -n BepInEx
   ```
   then restore with `dotnet restore` (or right-click the project in Visual Studio > Restore NuGet Packages).

4. **Build.**
   - *Visual Studio:* Build > Build Solution (or `Ctrl + Shift + B`).
   - *CLI:* `dotnet build` from inside `plugin/`.

   A successful build produces `plugin/bin/Debug/net6.0/ScritchyScratchyAP.dll`, and the project's post-build step automatically copies it into `<GameDir>\BepInEx\plugins\` for you, so as long as step 1 was set correctly, there's nothing left to copy by hand.

5. **Make sure the game is closed before building.** If the game (or BepInEx) is running, the post-build copy step will fail with a file-lock error (`MSB3021`) because Windows won't let the build overwrite a DLL that's currently loaded. Close the game, then build again.

## Editing the apworld

`apworld/__init__.py` and friends import from `worlds.AutoWorld` and `BaseClasses`, which live in the actual Archipelago source, not this repo. For your editor (e.g. VS Code + Pylance) to resolve those imports instead of showing them as unresolved:

1. Clone/download the [Archipelago source](https://github.com/ArchipelagoMW/Archipelago) somewhere locally.
2. Create `.vscode/settings.json` in this repo (I have it gitignored) with:
   ```json
   {
       "python.analysis.extraPaths": ["<path-to-your-Archipelago-source>"],
       "python.autoComplete.extraPaths": ["<path-to-your-Archipelago-source>"]
   }
   ```
