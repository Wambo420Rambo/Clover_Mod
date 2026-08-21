using BepInEx;
using BepInEx.Logging;
using CloverMod.Configuration;
using CloverMod.Core;
using CloverMod.Patches;
using CloverMod.UI;
using HarmonyLib;
using UnityEngine;

namespace CloverMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("CloverPit.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "Clovermod";
        public const string PluginName = "Clover Mod";
        public const string PluginVersion = "2.0.0";

        private Harmony harmony;
        private CloverMenu menu;
        private QualityOfLifeController qualityOfLife;

        internal static ModConfig Settings { get; private set; }

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            qualityOfLife = new QualityOfLifeController(Settings, Logger);
            menu = new CloverMenu(new GameActions(Logger), Settings, Logger);
        }

        private void OnEnable()
        {
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(
                $"{PluginName} v{PluginVersion} loaded. Menu: {Settings.MenuKey.Value}; fallback: {Settings.FallbackMenuKey.Value}.");
        }

        private void Update()
        {
            if (menu == null)
            {
                return;
            }

            if (menu.IsRebinding)
            {
                menu.CapturePressedKey();
                return;
            }

            bool primaryPressed = IsPressed(Settings.MenuKey.Value);
            bool fallbackPressed = Settings.FallbackMenuKey.Value != Settings.MenuKey.Value &&
                                   IsPressed(Settings.FallbackMenuKey.Value);

            if (primaryPressed || fallbackPressed)
            {
                menu.Toggle();
                return;
            }

            if (menu.IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                menu.Close();
                return;
            }

            if (!menu.IsOpen)
            {
                qualityOfLife?.Update();
            }
        }

        private void OnGUI()
        {
            menu?.Draw();
        }

        private void OnDisable()
        {
            menu?.Close();
            SlotMachineAutoPatch.StopAutoMode();
            qualityOfLife?.Dispose();
            AnimationSpeedSafetyPatch.RestoreRequestedSpeed();
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private void OnDestroy()
        {
            menu?.Dispose();
            menu = null;
            qualityOfLife?.Dispose();
            qualityOfLife = null;
        }

        private static bool IsPressed(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }
    }
}
