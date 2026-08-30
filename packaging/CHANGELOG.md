## 1.0.0

Initial release. A from-scratch rewrite of QuackAndCheese's ItemSpawner for the
current PEAK build.

- Procedural in-front-of-UI menu in the OnlyCook style, no AssetBundle and no
  AutoHookGenPatcher dependency.
- Icon grid of every base-game and modded item, with the localized name.
- Search that ignores case, whitespace, hyphens, apostrophes and diacritics, and
  matches internal names too.
- Pinyin search for Simplified/Traditional Chinese (full pinyin and initials).
- Filters: Vanilla / Modded / Special item classes, plus Food / Equipment
  categories. Modded items tinted purple, props/unused items tinted teal.
- Cook-level slider (0-12): tints all icons to their cooked state and spawns
  items pre-cooked.
- Clear button in the search bar; scroll position kept across menu toggles.
- Rebindable toggle key, minimal-UI and show-internal-names config options.
- Escape closes the menu without opening the pause menu.
- Localized into all 15 in-game languages.
