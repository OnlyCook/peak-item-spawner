using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItemSpawnerPlus
{
    internal enum ItemClass { Vanilla, Modded, Special }

    [System.Flags]
    internal enum ItemCategory { None = 0, Food = 1, Equipment = 2 }

    // Vanilla = a normal base-game item. Modded = added by another mod. Special = a
    // base-game prefab that is a prop / placeholder / unused variant / WIP and still
    // passes IsValidToSpawn() so it shows in the menu, but is not obtainable in a
    // normal run. See RESEARCH.md for how the Special list was derived
    internal static class ItemClassifier
    {
        private static readonly HashSet<string> Special = new HashSet<string>(StringComparer.Ordinal)
        {
            "Parachute", "Basketball", "Lollipop_Prop", "BingBong_Prop Variant", "Binoculars_Prop",
            "Bugle_Prop Variant", "ClimbingChalk", "Clusterberry_UNUSED", "Glizzy_CattailVariant",
            "Mandrake_Hidden", "Megaphone", "Parasol_Roots Variant", "Passport", "RescueHook_Infinite",
            "ScoutCookies_Vanilla", "ScoutmasterSoul", "Skull", "FireWood", "Stone", "Mushroom Glow",
            "Warpsketball", "Cheat Compass", "Cheat Compass 1", "Warp Compass",
        };

        // membership lists keyed by prefab (gameObject) name, derived from docs/*-item-list
        // cross-referenced against the item database dump (see RESEARCH.md). an item can
        // belong to several categories
        private static readonly HashSet<string> Food = new HashSet<string>(StringComparer.Ordinal)
        {
            "Airplane Food", "Apple Berry Green", "Apple Berry Red", "Apple Berry Yellow",
            "Berrynana Blue", "Berrynana Brown", "Berrynana Pink", "Berrynana Yellow",
            "BookOfBones", "Bugfix", "Clusterberry Black", "Clusterberry Red",
            "Clusterberry Yellow", "Clusterberry_UNUSED", "Cure-All", "EarlyWorm", "Egg",
            "EggRaven", "EggTurkey",
            "Energy Drink", "FortifiedMilk", "FrogLegs", "Glizzy", "Glizzy_CattailVariant",
            "Granola Bar", "Item_Coconut_half", "Item_Honeycomb", "Kingberry Green",
            "Kingberry Purple", "Kingberry Yellow", "Lollipop", "Lollipop_Prop", "Mandrake",
            "Mandrake_Hidden", "Marshmallow", "MedicinalRoot", "Mushroom Chubby",
            "Mushroom Cluster", "Mushroom Cluster Poison", "Mushroom Lace",
            "Mushroom Lace Poison", "Mushroom Normie", "Mushroom Normie Poison", "Napberry",
            "PandorasBox", "Pepper Berry", "Prickleberry_Gold", "Prickleberry_Red", "Scorpion",
            "ScoutCookies", "ScoutCookies_Vanilla", "Shroomberry_Blue", "Shroomberry_Green",
            "Shroomberry_Purple", "Shroomberry_Red", "Shroomberry_Yellow", "Sports Drink",
            "TrailMix", "Winterberry Orange", "Winterberry Yellow",
        };

        private static readonly HashSet<string> Equipment = new HashSet<string>(StringComparer.Ordinal)
        {
            "Amulet_Clone", "Amulet_Healing", "Amulet_InfiniteStamina", "Amulet_SuperJump",
            "AncientIdol", "Anti-Rope Spool", "AntiZooka", "Antidote", "Backpack", "Balloon",
            "BalloonBunch", "Bandages", "BingBong", "BingBong_Prop Variant", "Binoculars",
            "Binoculars_Prop", "BookOfBones", "BounceShroom", "Bugle", "Bugle_Magic",
            "Bugle_Prop Variant", "Bugle_Scoutmaster Variant", "Candle", "ChainShooter",
            "ClimbingSpike", "CloudFungus", "Compass", "Cure-All", "Cursed Skull", "Fannypack",
            "FirstAidKit", "Flag_Plantable_Checkpoint", "Flare", "Frisbee", "Glider",
            "Guidebook", "GuidebookPageScroll Variant", "HealingDart Variant",
            "HealingPuffShroom", "Heat Pack", "Jetpack", "Lantern", "Lantern_Faerie",
            "MagicBean", "PandorasBox", "Parasol", "Parasol_Roots Variant", "Pirate Compass",
            "PortableStovetopItem", "RescueHook", "RescueHook_Infinite", "RitualDagger",
            "Rocketpack", "RopeShooter", "RopeShooterAnti", "RopeSpool", "ScoutCannonItem",
            "ScoutEffigy", "ScoutsHonor", "ShelfShroom", "Sunscreen", "Torch",
        };

        internal static ItemCategory CategoriesOf(Item item)
        {
            ItemCategory c = ItemCategory.None;
            try
            {
                string n = item.gameObject.name;
                if (Food.Contains(n)) c |= ItemCategory.Food;
                if (Equipment.Contains(n)) c |= ItemCategory.Equipment;
            }
            catch { }
            return c;
        }

        internal static ItemClass Classify(Item item)
        {
            try
            {
                string n = item.gameObject.name;
                // every C_* prefab is a chess piece (King/Queen/Bishop/Knight/Rook/Pawn)
                if (n.StartsWith("C_", StringComparison.Ordinal) || Special.Contains(n))
                    return ItemClass.Special;
                if (IsFromAnotherMod(item))
                    return ItemClass.Modded;
            }
            catch { }
            return ItemClass.Vanilla;
        }

        // a base-game item's components all live in Assembly-CSharp (plus PhotonView);
        // a mod-added item almost always carries a component from its own assembly
        private static bool IsFromAnotherMod(Item item)
        {
            var comps = item.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                var mb = comps[i];
                if (mb == null) continue;
                string asm = mb.GetType().Assembly.GetName().Name;
                if (asm == "Assembly-CSharp" || asm == "Assembly-CSharp-firstpass"
                    || asm == "PhotonUnityNetworking" || asm == "mscorlib" || asm == "netstandard"
                    || asm.StartsWith("Unity", StringComparison.Ordinal)
                    || asm.StartsWith("System", StringComparison.Ordinal)
                    || asm.StartsWith("com.unity", StringComparison.Ordinal))
                    continue;
                return true;
            }
            return false;
        }
    }
}
