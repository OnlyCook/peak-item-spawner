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
