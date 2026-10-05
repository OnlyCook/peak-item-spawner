using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace ItemSpawnerPlus
{
    /// <summary>
    /// Hearted tiles, persisted one key per line next to the mod's config file.
    /// Items are keyed by prefab name, creatures by "creature:" + prefab name.
    /// </summary>
    internal static class Favorites
    {
        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, PluginInfo.Guid + ".favorites.txt");
        private static HashSet<string> _keys;

        internal static string ItemKey(Item item) => item.gameObject.name;
        internal static string CreatureKey(CreatureDef def) => "creature:" + def.PrefabName;

        internal static bool Contains(string key) => key != null && Keys.Contains(key);

        internal static bool Toggle(string key)
        {
            bool on = Keys.Add(key) || !Keys.Remove(key);
            try { File.WriteAllLines(FilePath, Keys); }
            catch (Exception e) { Plugin.Instance?.Log.LogWarning($"Item Spawner Plus: could not save favorites: {e.Message}"); }
            return on;
        }

        private static HashSet<string> Keys
        {
            get
            {
                if (_keys != null) return _keys;
                _keys = new HashSet<string>(StringComparer.Ordinal);
                try
                {
                    if (File.Exists(FilePath))
                        foreach (var line in File.ReadAllLines(FilePath))
                            if (line.Length > 0) _keys.Add(line);
                }
                catch (Exception e) { Plugin.Instance?.Log.LogWarning($"Item Spawner Plus: could not load favorites: {e.Message}"); }
                return _keys;
            }
        }
    }
}
