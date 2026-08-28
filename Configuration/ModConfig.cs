using BepInEx.Configuration;
using UnityEngine;

namespace CloverMod.Configuration
{
    internal sealed class ModConfig
    {
        public ModConfig(ConfigFile config)
        {
            MenuKey = config.Bind(
                "Hotkeys",
                "MenuKey",
                KeyCode.M,
                "Primary key used to open and close CloverMod.");

            if (MenuKey.Value == KeyCode.F2)
            {
                MenuKey.Value = KeyCode.M;
            }

            FallbackMenuKey = config.Bind(
                "Hotkeys",
                "FallbackMenuKey",
                KeyCode.Insert,
                "Recovery key for laptops or keyboards where the primary function key is unavailable. Set to None to disable.");

            AutoSlotMode = config.Bind(
                "Slot Machine",
                "AutoSlotMode",
                false,
                "Automatically starts the next slot spin when the machine is ready.");

            AutoSkipIntro = config.Bind(
                "Quality of Life",
                "AutoSkipIntro",
                false,
                "Automatically skips scene 1 and loads the main game scene.");

            AutoCompleteCorpse = config.Bind(
                "Quality of Life",
                "AutoCompleteCorpse",
                false,
                "Once per run, puts missing skeleton limbs into available drawers during preparation.");

            SkipMemoryPackPunch = config.Bind(
                "Quality of Life",
                "SkipMemoryPackPunch",
                false,
                "Skips the punch animation when opening a memory-card pack.");

            AutoFlipMemoryPackCards = config.Bind(
                "Quality of Life",
                "AutoFlipMemoryPackCards",
                false,
                "Automatically requests flips for face-down cards while a memory-card deal is running.");

            FastMemoryPackFlow = config.Bind(
                "Quality of Life",
                "FastMemoryPackFlow",
                false,
                "Shortens memory-card pack waits and automatically continues non-dialogue pack prompts.");

            UsePhaseSpeedProfiles = config.Bind(
                "Quality of Life",
                "UsePhaseSpeedProfiles",
                false,
                "Automatically applies the configured normal, gambling, jackpot, cutscene, and charm-discard speeds.");

            NormalPhaseSpeed = BindSpeed(config, "NormalPhaseSpeed", 1,
                "Game and transition speed outside accelerated phases.", 1, 4);
            GamblingAnimationSpeed = BindSpeed(config, "GamblingAnimationSpeed", 4,
                "Payout/transition speed during gambling.", 1, 20);
            JackpotAnimationSpeed = BindSpeed(config, "JackpotAnimationSpeed", 10,
                "Payout/transition speed after the run has recorded its first jackpot.", 1, 20);
            CutsceneGameSpeed = BindSpeed(config, "CutsceneGameSpeed", 3,
                "Global game speed during cutscenes.", 1, 4);
            CharmDiscardSpeed = BindSpeed(config, "CharmDiscardSpeed", 4,
                "Temporary game and transition speed immediately after discarding a charm.", 1, 4);

            PauseWhileOpen = config.Bind(
                "Menu",
                "PauseWhileOpen",
                true,
                "Pause gameplay while the CloverMod menu is open.");

            UnlimitedMemoryCards = config.Bind(
                "Cheats",
                "UnlimitedMemoryCards",
                false,
                "Prevents owned memory-card counts from decreasing. Opt-in because it changes normal progression.");

            MultipleMemoryCardsEnabled = config.Bind(
                "Cheats",
                "MultipleMemoryCardsEnabled",
                false,
                "Allows multiple memory cards to be active during one run.");

            AdditionalMemoryCards = config.Bind(
                "Cheats",
                "AdditionalMemoryCards",
                string.Empty,
                "Comma-separated list of additional active memory cards.");

            CustomPreset = config.Bind(
                "Presets",
                "CustomPreset",
                string.Empty,
                "Internal storage for the custom CloverMod preset.");

            RemoveLegacyEntries(config);
        }

        public ConfigEntry<KeyCode> MenuKey { get; }

        public ConfigEntry<KeyCode> FallbackMenuKey { get; }

        public ConfigEntry<bool> AutoSlotMode { get; }

        public ConfigEntry<bool> AutoSkipIntro { get; }

        public ConfigEntry<bool> AutoCompleteCorpse { get; }

        public ConfigEntry<bool> SkipMemoryPackPunch { get; }

        public ConfigEntry<bool> AutoFlipMemoryPackCards { get; }

        public ConfigEntry<bool> FastMemoryPackFlow { get; }

        public ConfigEntry<bool> UsePhaseSpeedProfiles { get; }

        public ConfigEntry<int> NormalPhaseSpeed { get; }

        public ConfigEntry<int> GamblingAnimationSpeed { get; }

        public ConfigEntry<int> JackpotAnimationSpeed { get; }

        public ConfigEntry<int> CutsceneGameSpeed { get; }

        public ConfigEntry<int> CharmDiscardSpeed { get; }

        public ConfigEntry<bool> PauseWhileOpen { get; }

        public ConfigEntry<bool> UnlimitedMemoryCards { get; }

        public ConfigEntry<bool> MultipleMemoryCardsEnabled { get; }

        public ConfigEntry<string> AdditionalMemoryCards { get; }

        public ConfigEntry<string> CustomPreset { get; }

        private static ConfigEntry<int> BindSpeed(
            ConfigFile config,
            string key,
            int defaultValue,
            string description,
            int minimum,
            int maximum)
        {
            return config.Bind(
                "Quality of Life",
                key,
                defaultValue,
                new ConfigDescription(description, new AcceptableValueRange<int>(minimum, maximum)));
        }

        private static void RemoveLegacyEntries(ConfigFile config)
        {
            bool saveOnConfigSet = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;

            RemoveLegacyEntry(config, "Slot Machine", "SkipWinningAnimations", false);
            RemoveLegacyEntry(config, "Slot Machine", "TurboSlotMode", false);
            RemoveLegacyEntry(config, "Slot Machine", "TurboSpinLimit", 0);
            RemoveLegacyEntry(config, "Slot Machine", "TurboDelaySeconds", 0f);
            RemoveLegacyEntry(config, "Slot Machine", "TurboStopOnWin", false);
            RemoveLegacyEntry(config, "Slot Machine", "TurboStopOnSpecial", false);
            RemoveLegacyEntry(config, "Hotkeys", "SkipReelKey", KeyCode.Space);
            RemoveLegacyEntry(config, "Display", "ScientificNotation", false);
            RemoveLegacyEntry(config, "Display", "ScientificNotationDigits", 400);

            config.SaveOnConfigSet = saveOnConfigSet;
            config.Save();
        }

        private static void RemoveLegacyEntry<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue)
        {
            ConfigEntry<T> legacyEntry = config.Bind(section, key, defaultValue);
            config.Remove(legacyEntry.Definition);
        }
    }
}
