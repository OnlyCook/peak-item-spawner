## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- Available on [Nexus Mods](https://www.nexusmods.com/games/peak/mods/221/).

## For developers

Build:
```bash
cd src/ItemSpawnerPlus
dotnet build -c Release                          # -> bin/Release/ItemSpawnerPlus.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the local r2modman profile
```

Machine-specific paths (game `Managed/` dir, BepInEx `core/`, r2modman profile) default to
a Linux + Steam + r2modman layout in `src/ItemSpawnerPlus/Directory.Build.props`; override
them in a git-ignored `Directory.Build.props.local` next to it, or pass `-p:GameManagedDir=…`.

Package the Nexus release zip:
```bash
bash packaging/build-release.sh   # -> dist/ItemSpawnerPlus-<version>.zip
```
The version in `src/ItemSpawnerPlus/PluginInfo.cs` is the single source of truth.
