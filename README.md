<!-- GENERATED FILE — do not edit by hand.
     Source: packaging/README.md + packaging/README.github-extra.md
     Regenerate with: bash packaging/gen-readme.sh -->

**A polished in-game menu for spawning any item.** Press **`F5`** (configurable) to open it, type to search, click an item to spawn it into your hands.

Client-sided: only you need the mod and only you will see it.

Fully localized in all 15 languages the game ships with: English, Français, Italiano, Deutsch, Español (España), 日本語, 한국어, Português (Brasil), Русский, 简体中文, 繁體中文, Español (Latinoamérica), Українська, Polski, Türkçe.

---

## Credits

A from-scratch rewrite of [**ItemSpawner**](https://thunderstore.io/c/peak/p/quackandcheese/ItemSpawner/) by QuackAndCheese, rebuilt for the current PEAK build with a polished UI, more features, and no AssetBundle or extra patcher dependency. All credit for the original idea goes to them.

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
