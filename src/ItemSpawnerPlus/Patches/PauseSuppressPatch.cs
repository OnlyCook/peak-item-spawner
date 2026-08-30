using System;
using BepInEx.Logging;
using HarmonyLib;

namespace ItemSpawnerPlus
{
    // stops the Escape press that closed our menu from also opening the vanilla pause
    // menu the same frame; CharacterInput re-derives pauseWasPressed from the Input
    // System every frame so clearing it is not enough (mirrors PEAK Quick Resume)
    internal static class PauseSuppressPatch
    {
        private static bool _suppressOnce;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var target = AccessTools.Method(typeof(GUIManager), "UpdatePaused");
                if (target == null)
                {
                    log.LogWarning("PauseSuppressPatch: GUIManager.UpdatePaused not found; "
                        + "closing the menu with Escape may also open the pause menu.");
                    return;
                }
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(PauseSuppressPatch), nameof(Prefix)));
            }
            catch (Exception e)
            {
                log.LogError($"PauseSuppressPatch.Apply failed (non-fatal): {e}");
            }
        }

        // called the moment our menu closes; skips the next UpdatePaused, self-resetting
        internal static void SuppressNextOpen() => _suppressOnce = true;

        private static bool Prefix()
        {
            if (!_suppressOnce) return true;
            _suppressOnce = false;
            return false;
        }
    }
}
