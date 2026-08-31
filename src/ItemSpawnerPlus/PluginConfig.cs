using BepInEx.Configuration;
using UnityEngine;

namespace ItemSpawnerPlus
{
    public class PluginConfig
    {
        // plain KeyCode, not KeyboardShortcut: ModConfig only renders a rebind widget for KeyCode
        public readonly ConfigEntry<KeyCode> ToggleKey;
        public readonly ConfigEntry<bool> MinimalUi;
        public readonly ConfigEntry<bool> ShowInternalNames;
        public readonly ConfigEntry<bool> IncludeCreaturesByDefault;

        public PluginConfig(ConfigFile cfg)
        {
            ToggleKey = cfg.Bind("General", "toggle-key", KeyCode.F5,
                "Opens and closes the Item Spawner menu. Click an item to spawn it into your hands (or at your feet if your inventory is full).");

            MinimalUi = cfg.Bind("General", "minimal-ui", false,
                "If enabled, the Item Spawner menu uses a plain, minimal panel: no procedural background grain texture, no hand-torn jagged edges, and no edge animation. Disabled by default.");

            ShowInternalNames = cfg.Bind("General", "show-internal-names", false,
                "If enabled, each tile shows the item's internal (source) name instead of the localized name the player normally sees. Search still matches both either way. Disabled by default.");

            IncludeCreaturesByDefault = cfg.Bind("General", "include-creatures-by-default", false,
                "If enabled, the Creatures category starts ticked so creatures / enemies show in the list as soon as you open the menu. Disabled by default, meaning creatures are hidden until you tick the Creatures filter yourself.");
        }
    }
}
