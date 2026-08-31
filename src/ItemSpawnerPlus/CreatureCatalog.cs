using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zorro.Settings;

namespace ItemSpawnerPlus
{
    internal enum PhobiaKind { None, Bug, Zombie }

    internal sealed class CreatureDef
    {
        internal readonly string Label;
        internal readonly SpawnerText NameKey;
        // candidates tried in order; a name the game itself instantiates resolves on every client
        internal readonly string[] ResourceNames;
        // pool-injection key when the prefab is not under a Resources folder
        internal readonly string PrefabName;
        internal readonly string[] ComponentTypes;
        internal readonly bool RoomObject;
        // Mob.Start NREs without a MobManager, which is scene-only and never created from code
        internal readonly bool NeedsMobManager;
        internal readonly float SpawnDistance;
        internal readonly string Icon;
        // replaces Icon when the matching accessibility phobia mode is on
        internal readonly string PhobiaIcon;
        internal readonly PhobiaKind Phobia;

        internal CreatureDef(string label, SpawnerText nameKey, string[] resourceNames, string prefabName,
            string[] componentTypes, bool roomObject, bool needsMobManager,
            string icon, string phobiaIcon, PhobiaKind phobia, float spawnDistance = 4f)
        {
            Label = label;
            NameKey = nameKey;
            ResourceNames = resourceNames;
            PrefabName = prefabName;
            ComponentTypes = componentTypes;
            RoomObject = roomObject;
            NeedsMobManager = needsMobManager;
            Icon = icon;
            PhobiaIcon = phobiaIcon;
            Phobia = phobia;
            SpawnDistance = spawnDistance;
        }

        internal string LocalizedName() => SpawnerLocalization.Get(NameKey);
    }

    internal static class CreatureCatalog
    {
        private const string Ico = "ItemSpawnerPlus.creature.";

        internal static readonly CreatureDef[] All =
        {
            new CreatureDef("Bees", SpawnerText.CreatureBees, new[] { "BeeSwarm" }, "BeeSwarm",
                new[] { "BeeSwarm" }, false, false,
                Ico + "bee.png", Ico + "bee-phobia.png", PhobiaKind.Bug),
            new CreatureDef("Beetle", SpawnerText.CreatureBeetle, new[] { "Beetle", "BeetleTemp", "Mobs/Beetle", "0_Items/Beetle" }, "Beetle",
                new[] { "Beetle" }, false, true,
                Ico + "beetle.png", Ico + "beetle-phobia.png", PhobiaKind.Bug),
            // big and explodes fast, spawn it further out
            new CreatureDef("Big Ghost", SpawnerText.CreatureBigGhost, new[] { "GhostBall" }, "GhostBall",
                new[] { "GhostBall" }, false, false,
                Ico + "big-ghost.png", null, PhobiaKind.None, 14f),
            new CreatureDef("Scoutmaster Myres", SpawnerText.CreatureScoutmasterMyres, new[] { "Character_Scoutmaster" }, "Character_Scoutmaster",
                new[] { "Scoutmaster" }, true, false,
                Ico + "scoutmaster-myres.png", null, PhobiaKind.None),
            new CreatureDef("Zombie", SpawnerText.CreatureZombie, new[] { "MushroomZombie" }, "MushroomZombie",
                new[] { "MushroomZombie" }, false, false,
                Ico + "zombie.png", Ico + "zombie-phobia.png", PhobiaKind.Zombie),
        };

        private static readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

        internal static Texture2D GetIcon(CreatureDef def)
        {
            string res = (def.PhobiaIcon != null && PhobiaActive(def.Phobia)) ? def.PhobiaIcon : def.Icon;
            if (string.IsNullOrEmpty(res)) return null;
            if (_iconCache.TryGetValue(res, out var tex)) return tex;
            tex = ModChrome.LoadEmbeddedTexture(res);
            _iconCache[res] = tex;
            return tex;
        }

        private static bool PhobiaActive(PhobiaKind kind)
        {
            if (kind == PhobiaKind.None) return false;
            try
            {
                var sh = GameHandler.Instance != null ? GameHandler.Instance.SettingsHandler : null;
                if (sh == null) return false;
                if (kind == PhobiaKind.Bug)
                    return sh.GetSetting<BugPhobiaSetting>()?.Value == OffOnMode.ON;
                return sh.GetSetting<ZombiePhobiaSetting>()?.Value == OffOnMode.ON;
            }
            catch { return false; }
        }

        internal static GameObject Resolve(CreatureDef def, out string key, out string how)
        {
            key = null;
            how = null;

            if (def.ResourceNames != null)
            {
                foreach (var name in def.ResourceNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    var res = Resources.Load<GameObject>(name);
                    if (res != null) { key = name; how = "Resources.Load(\"" + name + "\")"; return res; }
                }
            }

            var prefab = FindPrefab(def);
            if (prefab != null)
            {
                key = "ItemSpawnerPlus/" + def.PrefabName;
                if (PhotonNetwork.PrefabPool is DefaultPool pool)
                    pool.ResourceCache[key] = prefab;
                how = "component scan + pool inject (host only)";
                return prefab;
            }

            return null;
        }

        private static GameObject FindPrefab(CreatureDef def)
        {
            if (def.ComponentTypes == null) return null;
            foreach (var typeName in def.ComponentTypes)
            {
                var t = AccessTools.TypeByName(typeName);
                if (t == null) continue;

                UnityEngine.Object[] objs;
                try { objs = Resources.FindObjectsOfTypeAll(t); }
                catch { continue; }

                GameObject fallback = null;
                foreach (var o in objs)
                {
                    if (!(o is Component comp)) continue;
                    var root = comp.transform.root.gameObject;
                    // a loaded prefab asset has no owning scene, a placed instance does
                    if (root.scene.IsValid()) continue;
                    if (root.GetComponentInChildren<PhotonView>(true) == null) continue;
                    if (root.name == def.PrefabName) return root;
                    fallback ??= root;
                }
                if (fallback != null) return fallback;
            }
            return null;
        }

        // Beetle is a Mob and its Start NREs without one
        internal static void EnsureMobManager()
        {
            if (MobManager.instance != null) return;
            if (Object.FindObjectOfType<MobManager>() != null) return;
            new GameObject("ItemSpawnerPlus_MobManager").AddComponent<MobManager>();
        }

        // spawning creatures in the Airport corrupts the next run's start (see RESEARCH.md)
        internal static bool InGameplayScene()
        {
            try { return GameHandler.IsInGameplayScene; }
            catch { }
            try
            {
                var n = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                return n != null && (n.Contains("Level_") || n.Contains("Island"));
            }
            catch { return false; }
        }
    }
}
