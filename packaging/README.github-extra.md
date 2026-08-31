## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- Available on [Nexus Mods](https://www.nexusmods.com/peak/mods/221/).

## For developers

Build:
```bash
cd src/ItemSpawnerPlus
dotnet build -c Release                          # -> bin/Release/ItemSpawnerPlus.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the local r2modman profile
```
