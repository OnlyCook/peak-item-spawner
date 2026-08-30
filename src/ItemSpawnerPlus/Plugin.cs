using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ItemSpawnerPlus
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }
        internal PluginConfig Cfg { get; private set; }
        internal BepInEx.Logging.ManualLogSource Log => Logger;

        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);

            var harmony = new Harmony(PluginInfo.Guid);
            PauseSuppressPatch.Apply(harmony, Logger);

            var go = new GameObject("ItemSpawnerPlus.Runtime");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<SpawnerBootstrap>();

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded. Toggle key: {Cfg.ToggleKey.Value}.");
        }
    }

    internal class SpawnerBootstrap : MonoBehaviour
    {
        private ItemSpawnerWindow _window;

        private void Update()
        {
            if (_window == null)
            {
                if (GUIManager.instance == null) return;
                if (GUIManager.instance.GetComponentInChildren<ItemSpawnerWindow>(true) != null) return;

                // parented under GUIManager so the window shares the scene EventSystem
                var host = new GameObject("ItemSpawnerPlus_Window");
                host.transform.SetParent(GUIManager.instance.transform, false);
                _window = host.AddComponent<ItemSpawnerWindow>();
                _window.Init(Plugin.Instance.Log, Plugin.Instance.Cfg);
                return;
            }

            if (Input.GetKeyDown(Plugin.Instance.Cfg.ToggleKey.Value))
                _window.ToggleMenu();
        }
    }
}
