<!-- GENERATED FILE — do not edit by hand.
     Source: packaging/README.md + packaging/README.github-extra.md
     Regenerate with: bash packaging/gen-readme.sh -->

**A polished in-game menu for spawning any item.** Press **`F5`** (configurable) to open it, type to search, click an item to spawn it into your hands (or at your feet if your inventory is full).

Renders in front of the game's UI, styled to match it. Client-sided: only you need the mod and only you see it.

---

## Features

- **Search** that ignores case, spaces, hyphens, apostrophes and accents, so `cure all` finds *Cure-All* and `grune knusperbeere` finds *Grüne Knusperbeere*
- **Pinyin search** for Chinese players: with the game in Chinese, type `xigua` or `xg` to find 西瓜
- **Filters** (funnel button): show/hide *Vanilla*, *Modded* and *Special* (props, placeholders, unused variants) items, and filter by *Food* / *Equipment*
- **Cook level** (flame button): a 0–12 slider that tints every icon to its cooked state and spawns items pre-cooked
- Modded items are picked up automatically and tinted purple; base-game props/unused items are tinted teal
- Every base-game and modded item is listed, laid out as an icon grid with the localized name
- Scroll position is kept when you reopen the menu
- Fully localized in all 15 languages the game ships with

## Configuration

Config file: `BepInEx/config/OnlyCook.ItemSpawnerPlus.cfg`.

- **`toggle-key`** — key that opens/closes the menu (default `F5`; also rebindable in the in-game ModConfig UI)
- **`minimal-ui`** — plain panel with no grain texture, torn edges or edge animation
- **`show-internal-names`** — show each item's internal (source) name instead of the localized one

## Credits

A from-scratch rewrite of [**ItemSpawner** by QuackAndCheese](https://thunderstore.io/c/peak/p/quackandcheese/ItemSpawner/), rebuilt for the current PEAK build with a procedural UI and no AssetBundle or extra patcher dependency. All credit for the original idea goes to them.

Button icons: Google Material Symbols (Apache 2.0). Pinyin data: [mozillazg/pinyin-data](https://github.com/mozillazg/pinyin-data) (MIT). See `NOTICE`.

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
